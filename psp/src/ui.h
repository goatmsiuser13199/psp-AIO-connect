#ifndef PSP_CONNECT_UI_H
#define PSP_CONNECT_UI_H

#include "protocol.h"

void ui_init(void);
void ui_draw(const PspConnectPacket *packet, int link_ok);

#endif
