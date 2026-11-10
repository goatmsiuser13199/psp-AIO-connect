#include <stdio.h>
#include <string.h>
#include <pspkernel.h>
#include <pspctrl.h>
#include <pspdisplay.h>
#include <pspthreadman.h>
#include <psppower.h>

#include "protocol.h"
#include "ui.h"
#include "usb.h"

PSP_MODULE_INFO("psp-connect", 0, 1, 1);
PSP_MAIN_THREAD_ATTR(THREAD_ATTR_USER | THREAD_ATTR_VFPU);
PSP_HEAP_SIZE_KB(2048);

#define STALE_TICKS 12

static int running = 1;

static int exit_callback(int arg1, int arg2, void *common)
{
    (void)arg1;
    (void)arg2;
    (void)common;
    running = 0;
    return 0;
}

static int callback_thread(SceSize args, void *argp)
{
    int cbid;
    (void)args;
    (void)argp;
    cbid = sceKernelCreateCallback("exit", exit_callback, NULL);
    sceKernelRegisterExitCallback(cbid);
    sceKernelSleepThreadCB();
    return 0;
}

static int setup_callbacks(void)
{
    int thid = sceKernelCreateThread("update_thread", callback_thread, 0x11, 0xFA0, 0, 0);
    if (thid >= 0)
        sceKernelStartThread(thid, 0, 0);
    return thid;
}

static void packet_clear(PspConnectPacket *packet)
{
    memset(packet, 0, sizeof(*packet));
    packet->magic = PSPCONNECT_MAGIC;
    packet->version = PSPCONNECT_VERSION;
    packet->flags = (uint16_t)PSPCONNECT_FLAG_DEFAULT;
    packet->color_bg = PSPCONNECT_COLOR_BG_DEFAULT;
    packet->color_accent = PSPCONNECT_COLOR_ACCENT_DEFAULT;
    packet->color_text = PSPCONNECT_COLOR_TEXT_DEFAULT;
    packet->color_muted = PSPCONNECT_COLOR_MUTED_DEFAULT;
    packet->color_cpu = PSPCONNECT_COLOR_CPU_DEFAULT;
    packet->color_gpu = PSPCONNECT_COLOR_GPU_DEFAULT;
    packet->color_vram = PSPCONNECT_COLOR_VRAM_DEFAULT;
    packet->cpu_load = PSPCONNECT_NA_F32;
    packet->cpu_temp_c = PSPCONNECT_NA_F32;
    packet->gpu_load = PSPCONNECT_NA_F32;
    packet->gpu_temp_c = PSPCONNECT_NA_F32;
    packet->ram_used_mb = PSPCONNECT_NA_F32;
    packet->ram_total_mb = PSPCONNECT_NA_F32;
    packet->vram_used_mb = PSPCONNECT_NA_F32;
    packet->vram_total_mb = PSPCONNECT_NA_F32;
}

static void keep_awake(void)
{
    scePowerTick(PSP_POWER_TICK_ALL);
}

int main(int argc, char *argv[])
{
    SceCtrlData pad;
    PspConnectPacket packet;
    int usb_ok;
    uint32_t last_seq = 0;
    int stale_ticks = STALE_TICKS;
    int link_ok = 0;
    int got;

    (void)argc;
    (void)argv;

    setup_callbacks();
    ui_init();
    packet_clear(&packet);
    keep_awake();

    ui_draw(&packet, 0);
    sceKernelDelayThread(200 * 1000);

    usb_ok = usb_start();

    sceCtrlSetSamplingCycle(0);
    sceCtrlSetSamplingMode(PSP_CTRL_MODE_ANALOG);

    while (running) {
        keep_awake();

        sceCtrlReadBufferPositive(&pad, 1);
        if (pad.Buttons & PSP_CTRL_START)
            break;

        got = 0;
        if (usb_ok == 0)
            got = usb_poll(&packet);

        if (got && packet.seq != last_seq) {
            last_seq = packet.seq;
            stale_ticks = 0;
            link_ok = 1;
        } else {
            if (stale_ticks < STALE_TICKS)
                stale_ticks++;
            if (stale_ticks >= STALE_TICKS)
                link_ok = 0;
        }

        ui_draw(&packet, link_ok);
        sceKernelDelayThread(150 * 1000);
    }

    usb_stop();
    sceKernelExitGame();
    return 0;
}
