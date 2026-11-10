#include <string.h>
#include <pspkernel.h>
#include <pspiofilemgr.h>
#include <pspmodulemgr.h>

#include "usb.h"

#define PRX_PATH "ms0:/PSP/GAME/psp-connect/usb.prx"
#define LIVE_PATH "ms0:/PSP/GAME/psp-connect/live.bin"

static SceUID g_mod = -1;
static int g_last_load_err;

int usb_start(void)
{
    int status = 0;
    int ret;

    g_last_load_err = 0;
    g_mod = sceKernelLoadModule(PRX_PATH, 0, NULL);
    if (g_mod < 0) {
        g_last_load_err = g_mod;
        return g_mod;
    }

    ret = sceKernelStartModule(g_mod, 0, NULL, &status, NULL);
    if (ret < 0) {
        g_last_load_err = ret;
        return ret;
    }

    return 0;
}

void usb_stop(void)
{
    int status = 0;
    if (g_mod >= 0) {
        sceKernelStopModule(g_mod, 0, NULL, &status, NULL);
        sceKernelUnloadModule(g_mod);
        g_mod = -1;
    }
}

int usb_poll(PspConnectPacket *out)
{
    SceUID fd;
    int n;
    PspConnectPacket tmp;

    fd = sceIoOpen(LIVE_PATH, PSP_O_RDONLY, 0777);
    if (fd < 0)
        return 0;

    memset(&tmp, 0, sizeof(tmp));
    n = sceIoRead(fd, &tmp, sizeof(tmp));
    sceIoClose(fd);

    if (n != (int)sizeof(tmp))
        return 0;
    if (tmp.magic != PSPCONNECT_MAGIC)
        return 0;

    *out = tmp;
    return 1;
}

int usb_last_error(void)
{
    return g_last_load_err;
}
