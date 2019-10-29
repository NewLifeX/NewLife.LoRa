### get external defined data

ARCH ?=
CROSS_COMPILE ?=
CC := $(CROSS_COMPILE)gcc
AR := $(CROSS_COMPILE)ar

CFLAGS := -O2 -Wall -Wextra -std=c99 -Iinc -I.

ifeq ($(shell getconf LONG_BIT), 64)
    CFLAG64 = -fPIC
else
    CFLAG64 =
endif

OBJDIR = obj
INCLUDES = $(wildcard *.h)

### general build targets

all:libMacCryto.so

### library module target

$(OBJDIR):
	mkdir -p $(OBJDIR)

$(OBJDIR)/%.o: %.c $(INCLUDES)  | $(OBJDIR)
	$(CC) -c $(CFLAGS) $(CFLAG64) $< -o $@

libMacCryto.so:  $(OBJDIR)/LoRaMacCrypto.o $(OBJDIR)/aes.o $(OBJDIR)/cmac.o $(OBJDIR)/utilities.o
	$(CC) -shared -fPIC -o $@ $^

clean:
	rm -f libMacCryto.so
	rm -f $(OBJDIR)/*.o

### EOF
