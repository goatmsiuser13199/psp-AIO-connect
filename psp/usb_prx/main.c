#include <string.h>
#include <pspkernel.h>
#include <pspiofilemgr.h>
#include <pspusb.h>
#include <pspusbbus.h>
#include <pspthreadman.h>

#include "../../protocol/pspconnect.h"

PSP_MODULE_INFO("pspconnect_usb", 0x1006, 1, 1);

#define DRIVER_NAME "PSPConnect"
#define EP_COUNT 3
#define RX_PACKET_SIZE 64
#define LIVE_PATH "ms0:/PSP/GAME/psp-connect/live.bin"

enum {
    USB_EVENT_ATTACH = 1,
    USB_EVENT_DETACH = 2,
    USB_EVENT_PACKET = 4
};

static struct DeviceDescriptor g_devdesc_hi = {
    18, 0x01, 0x0200, 0, 0, 0, 64, 0, 0, 0x0100, 0, 0, 0, 1
};
static struct ConfigDescriptor g_confdesc_hi = {
    9, 2, (9 + 9 + (2 * 7)), 1, 1, 0, 0xC0, 0
};
static struct InterfaceDescriptor g_interdesc_hi = {
    9, 4, 0, 0, 2, 0xFF, 0x01, 0xFF, 1
};
static struct EndpointDescriptor g_endpdesc_hi[2] = {
    { 7, 5, 0x81, 2, 512, 0 },
    { 7, 5, 0x02, 2, 512, 0 }
};

static struct DeviceDescriptor g_devdesc_full = {
    18, 0x01, 0x0200, 0, 0, 0, 64, 0, 0, 0x0100, 0, 0, 0, 1
};
static struct ConfigDescriptor g_confdesc_full = {
    9, 2, (9 + 9 + (2 * 7)), 1, 1, 0, 0xC0, 0
};
static struct InterfaceDescriptor g_interdesc_full = {
    9, 4, 0, 0, 2, 0xFF, 0x01, 0xFF, 1
};
static struct EndpointDescriptor g_endpdesc_full[2] = {
    { 7, 5, 0x81, 2, 64, 0 },
    { 7, 5, 0x02, 2, 64, 0 }
};

static unsigned char g_strp[] = { 0x8, 0x3, 'P', 0, 'C', 0, 0, 0 };
static struct UsbEndpoint g_endp[EP_COUNT] = {
    { 0, 0, 0 }, { 1, 0, 0 }, { 2, 0, 0 }
};
static struct UsbInterface g_intp = { 0xFFFFFFFF, 0, 1 };
static struct UsbData g_usbdata[2];
static struct UsbDriver g_driver;
static struct UsbdDeviceReq g_bulkout_req;
static SceUID g_event = -1;
static SceUID g_writer = -1;
static volatile int g_attached;
static volatile int g_running;
static PspConnectPacket g_latest;
static volatile int g_has_latest;
static unsigned char g_rxbuf[RX_PACKET_SIZE] __attribute__((aligned(64)));
static int g_registered;
static int g_started;

static void write_live(const PspConnectPacket *pkt)
{
    SceUID fd = sceIoOpen(LIVE_PATH, PSP_O_WRONLY | PSP_O_CREAT | PSP_O_TRUNC, 0777);
    if (fd >= 0) {
        sceIoWrite(fd, pkt, sizeof(*pkt));
        sceIoClose(fd);
    }
}

static int queue_bulkout(void);

static int writer_thread(SceSize args, void *argp)
{
    PspConnectPacket local;
    u32 result;

    (void)args;
    (void)argp;

    while (g_running) {
        result = 0;
        sceKernelWaitEventFlag(g_event, USB_EVENT_PACKET,
                               PSP_EVENT_WAITOR | PSP_EVENT_WAITCLEAR,
                               &result, NULL);
        if (!g_running)
            break;
        if (g_has_latest) {
            local = g_latest;
            g_has_latest = 0;
            write_live(&local);
        }
    }
    return 0;
}

static int usb_request(int arg1, int arg2, struct DeviceRequest *req)
{
    (void)arg1;
    (void)arg2;
    (void)req;
    return 0;
}

static int usb_func28(int arg1, int arg2, int arg3)
{
    (void)arg1;
    (void)arg2;
    (void)arg3;
    return 0;
}

static int usb_attach(int speed, void *arg2, void *arg3)
{
    (void)speed;
    (void)arg2;
    (void)arg3;
    g_attached = 1;
    if (g_event >= 0)
        sceKernelSetEventFlag(g_event, USB_EVENT_ATTACH);
    queue_bulkout();
    return 0;
}

static int usb_detach(int arg1, int arg2, int arg3)
{
    (void)arg1;
    (void)arg2;
    (void)arg3;
    g_attached = 0;
    if (g_event >= 0)
        sceKernelSetEventFlag(g_event, USB_EVENT_DETACH);
    return 0;
}

static int bulkout_done(struct UsbdDeviceReq *req, int arg2, int arg3)
{
    (void)arg2;
    (void)arg3;

    if (req->retcode == 0 && req->recvsize >= (int)sizeof(PspConnectPacket)) {
        PspConnectPacket *pkt = (PspConnectPacket *)g_rxbuf;
        if (pkt->magic == PSPCONNECT_MAGIC && pkt->version >= 1 &&
            pkt->version <= 16) {
            g_latest = *pkt;
            g_has_latest = 1;
            if (g_event >= 0)
                sceKernelSetEventFlag(g_event, USB_EVENT_PACKET);
        }
    }

    if (g_attached)
        queue_bulkout();
    return 0;
}

static int queue_bulkout(void)
{
    u32 addr = (u32)g_rxbuf;
    u32 block = addr & ~63u;
    u32 top = (addr + RX_PACKET_SIZE + 63u) & ~63u;

    sceKernelDcacheInvalidateRange((void *)block, top - block);
    memset(&g_bulkout_req, 0, sizeof(g_bulkout_req));
    g_bulkout_req.endp = &g_endp[2];
    g_bulkout_req.data = g_rxbuf;
    g_bulkout_req.size = RX_PACKET_SIZE;
    g_bulkout_req.func = bulkout_done;
    g_bulkout_req.link = NULL;
    return sceUsbbdReqRecv(&g_bulkout_req);
}

static void fill_usbdata(void)
{
    memset(g_usbdata, 0, sizeof(g_usbdata));

    memcpy(g_usbdata[0].devdesc, &g_devdesc_hi, sizeof(g_devdesc_hi));
    g_usbdata[0].config.pconfdesc = &g_usbdata[0].confdesc;
    g_usbdata[0].config.pinterfaces = &g_usbdata[0].interfaces;
    g_usbdata[0].config.pinterdesc = &g_usbdata[0].interdesc;
    g_usbdata[0].config.pendp = g_usbdata[0].endp;
    memcpy(g_usbdata[0].confdesc.desc, &g_confdesc_hi, sizeof(g_confdesc_hi));
    g_usbdata[0].confdesc.pinterfaces = &g_usbdata[0].interfaces;
    g_usbdata[0].interfaces.pinterdesc[0] = &g_usbdata[0].interdesc;
    g_usbdata[0].interfaces.intcount = 1;
    memcpy(g_usbdata[0].interdesc.desc, &g_interdesc_hi, sizeof(g_interdesc_hi));
    g_usbdata[0].interdesc.pendp = g_usbdata[0].endp;
    memcpy(g_usbdata[0].endp[0].desc, &g_endpdesc_hi[0], sizeof(g_endpdesc_hi[0]));
    memcpy(g_usbdata[0].endp[1].desc, &g_endpdesc_hi[1], sizeof(g_endpdesc_hi[1]));

    memcpy(g_usbdata[1].devdesc, &g_devdesc_full, sizeof(g_devdesc_full));
    g_usbdata[1].config.pconfdesc = &g_usbdata[1].confdesc;
    g_usbdata[1].config.pinterfaces = &g_usbdata[1].interfaces;
    g_usbdata[1].config.pinterdesc = &g_usbdata[1].interdesc;
    g_usbdata[1].config.pendp = g_usbdata[1].endp;
    memcpy(g_usbdata[1].confdesc.desc, &g_confdesc_full, sizeof(g_confdesc_full));
    g_usbdata[1].confdesc.pinterfaces = &g_usbdata[1].interfaces;
    g_usbdata[1].interfaces.pinterdesc[0] = &g_usbdata[1].interdesc;
    g_usbdata[1].interfaces.intcount = 1;
    memcpy(g_usbdata[1].interdesc.desc, &g_interdesc_full, sizeof(g_interdesc_full));
    g_usbdata[1].interdesc.pendp = g_usbdata[1].endp;
    memcpy(g_usbdata[1].endp[0].desc, &g_endpdesc_full[0], sizeof(g_endpdesc_full[0]));
    memcpy(g_usbdata[1].endp[1].desc, &g_endpdesc_full[1], sizeof(g_endpdesc_full[1]));

    g_driver.devp_hi = g_usbdata[0].devdesc;
    g_driver.confp_hi = &g_usbdata[0].config;
    g_driver.devp = g_usbdata[1].devdesc;
    g_driver.confp = &g_usbdata[1].config;
}

static int start_func(int size, void *args)
{
    (void)size;
    (void)args;
    g_attached = 0;
    g_has_latest = 0;
    fill_usbdata();
    g_event = sceKernelCreateEventFlag("PSPCUsb", 0x200, 0, NULL);
    if (g_event < 0)
        return -1;

    g_running = 1;
    g_writer = sceKernelCreateThread("PSPCWriter", writer_thread, 0x18, 0x2000, 0, NULL);
    if (g_writer >= 0)
        sceKernelStartThread(g_writer, 0, NULL);
    return 0;
}

static int stop_func(int size, void *args)
{
    (void)size;
    (void)args;
    g_attached = 0;
    g_running = 0;
    if (g_event >= 0)
        sceKernelSetEventFlag(g_event, USB_EVENT_PACKET);
    if (g_writer >= 0) {
        sceKernelWaitThreadEnd(g_writer, NULL);
        sceKernelDeleteThread(g_writer);
        g_writer = -1;
    }
    if (g_event >= 0) {
        sceKernelDeleteEventFlag(g_event);
        g_event = -1;
    }
    return 0;
}

static int usb_activate(void)
{
    int ret;

    memset(&g_driver, 0, sizeof(g_driver));
    g_driver.name = DRIVER_NAME;
    g_driver.endpoints = EP_COUNT;
    g_driver.endp = g_endp;
    g_driver.intp = &g_intp;
    g_driver.str = (struct StringDescriptor *)g_strp;
    g_driver.recvctl = usb_request;
    g_driver.func28 = usb_func28;
    g_driver.attach = usb_attach;
    g_driver.detach = usb_detach;
    g_driver.start_func = start_func;
    g_driver.stop_func = stop_func;

    ret = sceUsbbdRegister(&g_driver);
    if (ret < 0)
        return ret;
    g_registered = 1;

    ret = sceUsbStart(PSP_USBBUS_DRIVERNAME, 0, 0);
    if (ret != 0)
        return ret;

    ret = sceUsbStart(DRIVER_NAME, 0, 0);
    if (ret != 0)
        return ret;
    g_started = 1;

    ret = sceUsbActivate(PSPCONNECT_USB_PID);
    if (ret != 0)
        return ret;

    queue_bulkout();
    return 0;
}

static void usb_deactivate(void)
{
    if (g_started) {
        sceUsbDeactivate(PSPCONNECT_USB_PID);
        sceUsbStop(DRIVER_NAME, 0, 0);
        sceUsbStop(PSP_USBBUS_DRIVERNAME, 0, 0);
        g_started = 0;
    }
    if (g_registered) {
        sceUsbbdUnregister(&g_driver);
        g_registered = 0;
    }
}

int module_start(SceSize args, void *argp)
{
    (void)args;
    (void)argp;
    return usb_activate() == 0 ? 0 : 1;
}

int module_stop(SceSize args, void *argp)
{
    (void)args;
    (void)argp;
    usb_deactivate();
    return 0;
}
