#ifndef PSPCONNECT_H
#define PSPCONNECT_H

#include <stdint.h>

#define PSPCONNECT_MAGIC       0x43505350u /* 'PSPC' LE */
#define PSPCONNECT_VERSION     3u
#define PSPCONNECT_PACKET_SIZE 64u

#define PSPCONNECT_USB_VID     0x054Cu
#define PSPCONNECT_USB_PID     0xC001u

#define PSPCONNECT_FLAG_SHOW_CPU   (1u << 0)
#define PSPCONNECT_FLAG_SHOW_GPU   (1u << 1)
#define PSPCONNECT_FLAG_SHOW_RAM   (1u << 2)
#define PSPCONNECT_FLAG_SHOW_VRAM  (1u << 3)
#define PSPCONNECT_FLAG_SHOW_DISK  (1u << 4)
#define PSPCONNECT_FLAG_SHOW_FPS   (1u << 5)
#define PSPCONNECT_FLAG_SHOW_TEMP  (1u << 6)

#define PSPCONNECT_FLAG_DEFAULT ( \
    PSPCONNECT_FLAG_SHOW_CPU | PSPCONNECT_FLAG_SHOW_GPU | \
    PSPCONNECT_FLAG_SHOW_RAM | PSPCONNECT_FLAG_SHOW_VRAM | \
    PSPCONNECT_FLAG_SHOW_TEMP)

#define PSPCONNECT_NA_F32 (-1.0f)

#define PSPCONNECT_RGB565(r, g, b) (uint16_t)( \
    ((((uint16_t)(r) >> 3) & 0x1Fu) << 11) | \
    ((((uint16_t)(g) >> 2) & 0x3Fu) << 5) | \
    (((uint16_t)(b) >> 3) & 0x1Fu))

#define PSPCONNECT_RGB565_R(c) (uint8_t)((((c) >> 11) & 0x1Fu) << 3)
#define PSPCONNECT_RGB565_G(c) (uint8_t)((((c) >> 5) & 0x3Fu) << 2)
#define PSPCONNECT_RGB565_B(c) (uint8_t)(((c) & 0x1Fu) << 3)

#define PSPCONNECT_COLOR_BG_DEFAULT     PSPCONNECT_RGB565(10, 12, 16)
#define PSPCONNECT_COLOR_ACCENT_DEFAULT PSPCONNECT_RGB565(64, 210, 190)
#define PSPCONNECT_COLOR_TEXT_DEFAULT   PSPCONNECT_RGB565(236, 242, 250)
#define PSPCONNECT_COLOR_MUTED_DEFAULT  PSPCONNECT_RGB565(120, 132, 148)
#define PSPCONNECT_COLOR_CPU_DEFAULT    PSPCONNECT_RGB565(94, 176, 255)
#define PSPCONNECT_COLOR_GPU_DEFAULT    PSPCONNECT_RGB565(72, 214, 168)
#define PSPCONNECT_COLOR_VRAM_DEFAULT   PSPCONNECT_RGB565(180, 140, 255)

#pragma pack(push, 1)
typedef struct PspConnectPacket {
    uint32_t magic;
    uint16_t version;
    uint16_t flags;
    uint32_t seq;
    uint32_t timestamp_ms;
    float cpu_load;
    float cpu_temp_c;
    float gpu_load;
    float gpu_temp_c;
    float ram_used_mb;
    float ram_total_mb;
    float vram_used_mb;
    float vram_total_mb;
    uint16_t color_cpu;
    uint16_t color_gpu;
    uint16_t color_vram;
    uint16_t reserved;
    uint16_t color_bg;
    uint16_t color_accent;
    uint16_t color_text;
    uint16_t color_muted;
} PspConnectPacket;
#pragma pack(pop)

#ifdef __cplusplus
static_assert(sizeof(PspConnectPacket) == PSPCONNECT_PACKET_SIZE, "packet size");
#endif

#endif
