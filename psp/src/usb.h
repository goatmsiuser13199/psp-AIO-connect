#ifndef PSP_CONNECT_USB_H
#define PSP_CONNECT_USB_H

#include "protocol.h"

int usb_start(void);
void usb_stop(void);
int usb_poll(PspConnectPacket *out);
int usb_last_error(void);

#endif
