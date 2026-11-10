#include <stdio.h>
#include <string.h>
#include <pspdisplay.h>
#include <pspgu.h>
#include <pspkernel.h>
#include <pspdebug.h>

#include "ui.h"

#define BUF_WIDTH 512
#define SCR_WIDTH 480
#define SCR_HEIGHT 272
#define MAX_SLOTS 4
#define SPARK_N 32

static unsigned int __attribute__((aligned(16))) list[262144];
static void *draw_buf;
static void *disp_buf;
static float spark[MAX_SLOTS][SPARK_N];
static unsigned char spark_pos[MAX_SLOTS];
static unsigned char spark_fill[MAX_SLOTS];

typedef struct {
    unsigned int color;
    short x, y, z;
} Vert;

typedef struct {
    int active;
    const char *tag;
    int is_pct;
    char value[16];
    char sub[16];
    float pct;
    unsigned int fill;
    int id;
    int x, y, w, h;
} Slot;

static unsigned int abgr(unsigned int a, unsigned int r, unsigned int g, unsigned int b)
{
    return (a << 24) | (b << 16) | (g << 8) | r;
}

static unsigned int abgr_from_rgb565(uint16_t c)
{
    unsigned int r = PSPCONNECT_RGB565_R(c);
    unsigned int g = PSPCONNECT_RGB565_G(c);
    unsigned int b = PSPCONNECT_RGB565_B(c);
    return abgr(255, r, g, b);
}

static void draw_rect(int x, int y, int w, int h, unsigned int color)
{
    Vert *v;
    if (w <= 0 || h <= 0)
        return;
    v = (Vert *)sceGuGetMemory(2 * sizeof(Vert));
    v[0].color = color;
    v[0].x = (short)x;
    v[0].y = (short)y;
    v[0].z = 0;
    v[1].color = color;
    v[1].x = (short)(x + w);
    v[1].y = (short)(y + h);
    v[1].z = 0;
    sceGuDrawArray(GU_SPRITES, GU_COLOR_8888 | GU_VERTEX_16BIT | GU_TRANSFORM_2D, 2, 0, v);
}

static const unsigned char SEG7[12] = {
    0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F, 0x40, 0x00
};

static void draw_seg7(int x, int y, int dw, int dh, int thick, unsigned char mask, unsigned int color)
{
    int mid = y + dh / 2;
    if (mask & 0x01) draw_rect(x + thick, y, dw - thick * 2, thick, color);
    if (mask & 0x02) draw_rect(x + dw - thick, y + thick, thick, mid - y - thick, color);
    if (mask & 0x04) draw_rect(x + dw - thick, mid, thick, y + dh - thick - mid, color);
    if (mask & 0x08) draw_rect(x + thick, y + dh - thick, dw - thick * 2, thick, color);
    if (mask & 0x10) draw_rect(x, mid, thick, y + dh - thick - mid, color);
    if (mask & 0x20) draw_rect(x, y + thick, thick, mid - y - thick, color);
    if (mask & 0x40) draw_rect(x + thick, mid - thick / 2, dw - thick * 2, thick, color);
}

static void draw_value(int x, int y, const char *text, int dw, int dh, int thick, unsigned int color)
{
    int cx = x;
    int i;
    for (i = 0; text[i]; i++) {
        char ch = text[i];
        if (ch == '.') {
            draw_rect(cx + 1, y + dh - thick, thick + 1, thick + 1, color);
            cx += thick + 6;
            continue;
        }
        if (ch == '-')
            draw_seg7(cx, y, dw, dh, thick, SEG7[10], color);
        else if (ch >= '0' && ch <= '9')
            draw_seg7(cx, y, dw, dh, thick, SEG7[ch - '0'], color);
        cx += dw + 5;
    }
}

static int value_width(const char *text, int dw, int thick)
{
    int w = 0;
    int i;
    for (i = 0; text[i]; i++) {
        if (text[i] == '.')
            w += thick + 6;
        else
            w += dw + 5;
    }
    return w > 0 ? w - 5 : 0;
}

static void draw_unit_percent(int x, int y, int h, unsigned int color)
{
    int dot = h / 4;
    int gap;
    int slash_w;
    int i;
    int x0, y0, x1, y1, steps;

    if (dot < 5)
        dot = 5;
    if (dot > 8)
        dot = 8;

    gap = 3;
    slash_w = 3;

    draw_rect(x, y, dot, dot, color);
    draw_rect(x + dot + gap, y + h - dot, dot, dot, color);

    x0 = x + dot + gap + dot - 2;
    y0 = y + 1;
    x1 = x + 1;
    y1 = y + h - 2;
    steps = h;
    if (steps < 6)
        steps = 6;
    for (i = 0; i < steps; i++) {
        int px = x0 + ((x1 - x0) * i) / (steps - 1);
        int py = y0 + ((y1 - y0) * i) / (steps - 1);
        draw_rect(px, py, slash_w, 2, color);
    }
}

static void draw_unit_go(int x, int y, int h, unsigned int color)
{
    int t = h >= 16 ? 3 : 2;
    int gw = (h * 4) / 5;
    int ow = (h * 3) / 5;
    int ox;
    int oh = (h * 2) / 3;
    int oy = y + h - oh;

    draw_rect(x, y, gw, t, color);
    draw_rect(x, y + h - t, gw, t, color);
    draw_rect(x, y, t, h, color);
    draw_rect(x + gw - t, y + h / 2, t, h / 2, color);
    draw_rect(x + gw / 2, y + h / 2 - t / 2, gw / 2, t, color);

    ox = x + gw + 5;
    draw_rect(ox, oy, ow, t, color);
    draw_rect(ox, y + h - t, ow, t, color);
    draw_rect(ox, oy, t, oh, color);
    draw_rect(ox + ow - t, oy, t, oh, color);
}

static int unit_width(int is_pct, int h)
{
    int dot;
    if (is_pct) {
        dot = h / 4;
        if (dot < 5) dot = 5;
        if (dot > 8) dot = 8;
        return dot + 3 + dot;
    }
    return (h * 4) / 5 + 5 + (h * 3) / 5;
}

static void draw_letter(int x, int y, char ch, int s, unsigned int color)
{
    int t = s < 3 ? 1 : 2;
    int w = s * 3;
    int h = s * 5;

    switch (ch) {
    case 'C':
        draw_rect(x, y, w, t, color);
        draw_rect(x, y + h - t, w, t, color);
        draw_rect(x, y, t, h, color);
        break;
    case 'P':
        draw_rect(x, y, t, h, color);
        draw_rect(x, y, w, t, color);
        draw_rect(x, y + h / 2, w, t, color);
        draw_rect(x + w - t, y, t, h / 2 + t, color);
        break;
    case 'U':
        draw_rect(x, y, t, h - t, color);
        draw_rect(x + w - t, y, t, h - t, color);
        draw_rect(x, y + h - t, w, t, color);
        break;
    case 'G':
        draw_rect(x, y, w, t, color);
        draw_rect(x, y + h - t, w, t, color);
        draw_rect(x, y, t, h, color);
        draw_rect(x + w - t, y + h / 2, t, h / 2, color);
        draw_rect(x + w / 2, y + h / 2, w / 2, t, color);
        break;
    case 'R':
        draw_rect(x, y, t, h, color);
        draw_rect(x, y, w, t, color);
        draw_rect(x, y + h / 2, w, t, color);
        draw_rect(x + w - t, y, t, h / 2 + t, color);
        draw_rect(x + w / 2, y + h / 2, t, h / 2, color);
        draw_rect(x + w - t, y + h - t, t, t, color);
        break;
    case 'A':
        draw_rect(x, y + t, t, h - t, color);
        draw_rect(x + w - t, y + t, t, h - t, color);
        draw_rect(x + t, y, w - t * 2, t, color);
        draw_rect(x, y + h / 2, w, t, color);
        break;
    case 'M':
        draw_rect(x, y, t, h, color);
        draw_rect(x + w - t, y, t, h, color);
        draw_rect(x + w / 2 - t / 2, y + t, t, h / 2, color);
        draw_rect(x + t, y, t, t, color);
        draw_rect(x + w - t * 2, y, t, t, color);
        break;
    case 'V':
        draw_rect(x, y, t, h - s, color);
        draw_rect(x + w - t, y, t, h - s, color);
        draw_rect(x + w / 2 - t / 2, y + h - t, t, t, color);
        break;
    default:
        break;
    }
}

static void draw_tag(int x, int y, const char *tag, unsigned int color)
{
    int i;
    int s = 3;
    int advance = s * 3 + 4;
    for (i = 0; tag[i]; i++)
        draw_letter(x + i * advance, y, tag[i], s, color);
}

static void draw_sub_text(int x, int y, const char *sub, unsigned int color)
{
    int cx = x;
    int i;
    int dw = 10;
    int dh = 16;
    int thick = 2;

    for (i = 0; sub[i]; i++) {
        char ch = sub[i];
        if (ch >= '0' && ch <= '9') {
            draw_seg7(cx, y, dw, dh, thick, SEG7[ch - '0'], color);
            cx += dw + 4;
        } else if (ch == 'C') {
            draw_letter(cx, y + 1, 'C', 3, color);
            cx += 14;
        } else if (ch == 'G') {
            draw_unit_go(cx, y, dh, color);
            break;
        } else if (ch == ' ' || ch == 'o' || ch == '/') {
            cx += 3;
        }
    }
}

static int sub_width(const char *sub)
{
    int w = 0;
    int i;
    for (i = 0; sub[i]; i++) {
        if (sub[i] >= '0' && sub[i] <= '9')
            w += 14;
        else if (sub[i] == 'C')
            w += 14;
        else if (sub[i] == 'G')
            return w + 28;
        else
            w += 3;
    }
    return w;
}

static void format_pct(char *dst, size_t n, float v)
{
    if (v < 0.0f)
        snprintf(dst, n, "--");
    else if (v >= 99.5f)
        snprintf(dst, n, "100");
    else
        snprintf(dst, n, "%.0f", v);
}

static void format_temp(char *dst, size_t n, float v)
{
    if (v < 0.0f)
        dst[0] = '\0';
    else
        snprintf(dst, n, "%.0fC", v);
}

static int blink_on(void)
{
    return (sceKernelGetSystemTimeLow() / 220000u) & 1u;
}

static void format_go(char *dst, size_t n, float used_mb)
{
    if (used_mb < 0.0f)
        snprintf(dst, n, "--");
    else if (used_mb >= 10240.0f)
        snprintf(dst, n, "%.0f", used_mb / 1024.0f);
    else
        snprintf(dst, n, "%.1f", used_mb / 1024.0f);
}

static float clamp_pct(float pct)
{
    if (pct < 0.0f)
        return 0.0f;
    if (pct > 100.0f)
        return 100.0f;
    return pct;
}

static void spark_push(int id, float pct)
{
    if (id < 0 || id >= MAX_SLOTS)
        return;
    spark[id][spark_pos[id]] = clamp_pct(pct < 0.0f ? 0.0f : pct);
    spark_pos[id] = (unsigned char)((spark_pos[id] + 1) % SPARK_N);
    if (spark_fill[id] < SPARK_N)
        spark_fill[id]++;
}

static void draw_spark(int x, int y, int w, int h, int id, unsigned int color)
{
    int n = spark_fill[id];
    int i;
    int start;
    if (n < 2)
        return;
    start = (spark_fill[id] < SPARK_N) ? 0 : spark_pos[id];
    for (i = 0; i < n; i++) {
        float v = spark[id][(start + i) % SPARK_N];
        int px = x + (i * (w - 1)) / (n - 1);
        int bh = 1 + (int)((v / 100.0f) * (float)(h - 1));
        draw_rect(px, y + h - bh, 2, bh, color);
    }
}

void ui_init(void)
{
    memset(spark, 0, sizeof(spark));
    memset(spark_pos, 0, sizeof(spark_pos));
    memset(spark_fill, 0, sizeof(spark_fill));

    draw_buf = (void *)0;
    disp_buf = (void *)0x88000;

    sceGuInit();
    sceGuStart(GU_DIRECT, list);
    sceGuDrawBuffer(GU_PSM_8888, draw_buf, BUF_WIDTH);
    sceGuDispBuffer(SCR_WIDTH, SCR_HEIGHT, disp_buf, BUF_WIDTH);
    sceGuDepthBuffer((void *)0x110000, BUF_WIDTH);
    sceGuOffset(2048 - (SCR_WIDTH / 2), 2048 - (SCR_HEIGHT / 2));
    sceGuViewport(2048, 2048, SCR_WIDTH, SCR_HEIGHT);
    sceGuScissor(0, 0, SCR_WIDTH, SCR_HEIGHT);
    sceGuEnable(GU_SCISSOR_TEST);
    sceGuDisable(GU_DEPTH_TEST);
    sceGuDisable(GU_TEXTURE_2D);
    sceGuEnable(GU_BLEND);
    sceGuBlendFunc(GU_ADD, GU_SRC_ALPHA, GU_ONE_MINUS_SRC_ALPHA, 0, 0);
    sceGuClearColor(abgr(255, 8, 10, 14));
    sceGuClear(GU_COLOR_BUFFER_BIT);
    sceGuFinish();
    sceGuSync(0, 0);

    pspDebugScreenInitEx(draw_buf, GU_PSM_8888, 0);

    sceDisplayWaitVblankStart();
    sceGuDisplay(GU_TRUE);
}

void ui_draw(const PspConnectPacket *packet, int link_ok)
{
    Slot slots[MAX_SLOTS];
    int i;
    unsigned int text, accent, muted, bg;
    unsigned int cpu_col, gpu_col, vram_col;
    unsigned int alert = abgr(255, 255, 48, 48);
    unsigned int alert_dim = abgr(255, 90, 18, 18);
    int stale_blink;
    uint16_t flags;
    const int gap = 10;
    const int margin = 12;
    int cell_w = (SCR_WIDTH - margin * 2 - gap) / 2;
    int cell_h = (SCR_HEIGHT - margin * 2 - gap) / 2;

    stale_blink = !link_ok && blink_on();

    bg = packet->color_bg ? abgr_from_rgb565(packet->color_bg) : abgr_from_rgb565(PSPCONNECT_COLOR_BG_DEFAULT);
    text = packet->color_text ? abgr_from_rgb565(packet->color_text) : abgr_from_rgb565(PSPCONNECT_COLOR_TEXT_DEFAULT);
    accent = packet->color_accent ? abgr_from_rgb565(packet->color_accent) : abgr_from_rgb565(PSPCONNECT_COLOR_ACCENT_DEFAULT);
    muted = packet->color_muted ? abgr_from_rgb565(packet->color_muted) : abgr_from_rgb565(PSPCONNECT_COLOR_MUTED_DEFAULT);
    cpu_col = packet->color_cpu ? abgr_from_rgb565(packet->color_cpu) : abgr_from_rgb565(PSPCONNECT_COLOR_CPU_DEFAULT);
    gpu_col = packet->color_gpu ? abgr_from_rgb565(packet->color_gpu) : abgr_from_rgb565(PSPCONNECT_COLOR_GPU_DEFAULT);
    vram_col = packet->color_vram ? abgr_from_rgb565(packet->color_vram) : abgr_from_rgb565(PSPCONNECT_COLOR_VRAM_DEFAULT);

    if (!link_ok) {
        text = stale_blink ? alert : alert_dim;
        muted = stale_blink ? alert : alert_dim;
        accent = stale_blink ? alert : alert_dim;
        cpu_col = stale_blink ? alert : alert_dim;
        gpu_col = stale_blink ? alert : alert_dim;
        vram_col = stale_blink ? alert : alert_dim;
    }

    flags = packet->flags ? packet->flags : (uint16_t)PSPCONNECT_FLAG_DEFAULT;
    flags &= (uint16_t)~(PSPCONNECT_FLAG_SHOW_DISK | PSPCONNECT_FLAG_SHOW_FPS);
    memset(slots, 0, sizeof(slots));

    slots[0].tag = "CPU";
    slots[0].is_pct = 1;
    slots[0].fill = cpu_col;
    slots[0].id = 0;
    slots[0].x = margin;
    slots[0].y = margin;
    slots[0].w = cell_w;
    slots[0].h = cell_h;
    if (flags & PSPCONNECT_FLAG_SHOW_CPU) {
        slots[0].active = 1;
        format_pct(slots[0].value, sizeof(slots[0].value), packet->cpu_load);
        format_temp(slots[0].sub, sizeof(slots[0].sub), packet->cpu_temp_c);
        slots[0].pct = packet->cpu_load;
    }

    slots[1].tag = "GPU";
    slots[1].is_pct = 1;
    slots[1].fill = gpu_col;
    slots[1].id = 1;
    slots[1].x = margin + cell_w + gap;
    slots[1].y = margin;
    slots[1].w = cell_w;
    slots[1].h = cell_h;
    if (flags & PSPCONNECT_FLAG_SHOW_GPU) {
        slots[1].active = 1;
        format_pct(slots[1].value, sizeof(slots[1].value), packet->gpu_load);
        format_temp(slots[1].sub, sizeof(slots[1].sub), packet->gpu_temp_c);
        slots[1].pct = packet->gpu_load;
    }

    slots[2].tag = "RAM";
    slots[2].is_pct = 0;
    slots[2].fill = accent;
    slots[2].id = 2;
    slots[2].x = margin;
    slots[2].y = margin + cell_h + gap;
    slots[2].w = cell_w;
    slots[2].h = cell_h;
    if (flags & PSPCONNECT_FLAG_SHOW_RAM) {
        slots[2].active = 1;
        format_go(slots[2].value, sizeof(slots[2].value), packet->ram_used_mb);
        if (packet->ram_total_mb > 0.0f)
            snprintf(slots[2].sub, sizeof(slots[2].sub), "%.0fG", packet->ram_total_mb / 1024.0f);
        slots[2].pct = (packet->ram_used_mb >= 0.0f && packet->ram_total_mb > 0.0f)
                           ? (packet->ram_used_mb / packet->ram_total_mb) * 100.0f
                           : 0.0f;
    }

    slots[3].tag = "VRAM";
    slots[3].is_pct = 0;
    slots[3].fill = vram_col;
    slots[3].id = 3;
    slots[3].x = margin + cell_w + gap;
    slots[3].y = margin + cell_h + gap;
    slots[3].w = cell_w;
    slots[3].h = cell_h;
    if (flags & PSPCONNECT_FLAG_SHOW_VRAM) {
        slots[3].active = 1;
        format_go(slots[3].value, sizeof(slots[3].value), packet->vram_used_mb);
        if (packet->vram_total_mb > 0.0f)
            snprintf(slots[3].sub, sizeof(slots[3].sub), "%.0fG", packet->vram_total_mb / 1024.0f);
        slots[3].pct = (packet->vram_used_mb >= 0.0f && packet->vram_total_mb > 0.0f)
                           ? (packet->vram_used_mb / packet->vram_total_mb) * 100.0f
                           : 0.0f;
    }

    for (i = 0; i < MAX_SLOTS; i++) {
        if (slots[i].active)
            spark_push(slots[i].id, slots[i].pct);
    }

    sceGuStart(GU_DIRECT, list);
    sceGuClearColor(bg);
    sceGuClear(GU_COLOR_BUFFER_BIT);

    for (i = 0; i < MAX_SLOTS; i++) {
        int x = slots[i].x;
        int y = slots[i].y;
        int w = slots[i].w;
        int h = slots[i].h;
        int dw = 18;
        int dh = 30;
        int thick = 3;
        int vw;
        int vx;
        int vy;
        int uh;
        int uw;
        int ux;
        int uy;
        int meter_w;
        int fill_w;
        int digits;
        unsigned int panel = abgr(255, 16, 18, 24);
        unsigned int track = abgr(255, 28, 32, 40);
        unsigned int soft = abgr(180,
                                 (slots[i].fill) & 0xFF,
                                 (slots[i].fill >> 8) & 0xFF,
                                 (slots[i].fill >> 16) & 0xFF);

        draw_rect(x, y, w, h, panel);
        draw_rect(x, y, 3, h, slots[i].active ? slots[i].fill : muted);

        if (!slots[i].active)
            continue;

        draw_tag(x + 16, y + 12, slots[i].tag, muted);

        if (slots[i].sub[0]) {
            int sw = sub_width(slots[i].sub);
            int sx = x + w - 14 - sw;
            if (sx < x + 90)
                sx = x + 90;
            draw_sub_text(sx, y + 12, slots[i].sub, soft);
        }

        digits = (int)strlen(slots[i].value);
        dw = 20;
        dh = 34;
        thick = 3;
        if (digits >= 4) {
            dw = 15;
            dh = 26;
            thick = 2;
        } else if (digits == 3) {
            dw = 17;
            dh = 30;
            thick = 3;
        }

        uh = (dh * 3) / 4;
        if (uh < 16)
            uh = 16;
        uw = unit_width(slots[i].is_pct, uh);

        vw = value_width(slots[i].value, dw, thick);
        vx = x + 16;
        vy = y + 44;
        if (vx + vw + 10 + uw > x + w - 14)
            vx = x + w - 14 - uw - 10 - vw;
        if (vx < x + 16)
            vx = x + 16;

        draw_value(vx, vy, slots[i].value, dw, dh, thick, text);

        ux = vx + vw + 10;
        uy = vy + dh - uh - 1;
        if (slots[i].is_pct)
            draw_unit_percent(ux, uy, uh, muted);
        else
            draw_unit_go(ux, uy, uh, muted);

        draw_spark(x + 14, y + h - 40, w - 28, 18, slots[i].id, soft);

        meter_w = w - 28;
        fill_w = (int)((float)meter_w * (clamp_pct(slots[i].pct) / 100.0f));
        draw_rect(x + 14, y + h - 14, meter_w, 4, track);
        if (fill_w > 0)
            draw_rect(x + 14, y + h - 14, fill_w, 4, slots[i].fill);
    }

    sceGuFinish();
    sceGuSync(0, 0);
    sceDisplayWaitVblankStart();
    draw_buf = sceGuSwapBuffers();
}
