TARGET = psp-connect
OBJS = src/main.o src/ui.o src/usb.o

INCDIR =
CFLAGS = -O2 -G0 -Wall -I../protocol -Isrc
CXXFLAGS = $(CFLAGS) -fno-exceptions -fno-rtti
ASFLAGS = $(CFLAGS)

LIBDIR =
LIBS = -lpspdebug -lpspdisplay -lpspgu -lpspge -lpspctrl -lpsppower -lpspsdk -lm
LDFLAGS =

EXTRA_TARGETS = EBOOT.PBP
PSP_EBOOT_TITLE = psp-connect
PSP_EBOOT_ICON = ICON0.PNG
PSP_EBOOT_PIC1 = PIC1.PNG

PSPSDK=$(shell psp-config --pspsdk-path)
include $(PSPSDK)/lib/build.mak

.PHONY: prx all-bundle
prx:
	$(MAKE) -C usb_prx

all-bundle: prx $(EXTRA_TARGETS)
	@echo "Built EBOOT.PBP + usb_prx/usb.prx"
