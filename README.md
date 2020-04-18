# CSpectPlugins
Various plugins for [CSpect](http://www.cspect.org/), an emulator for the [ZX Spectrum](https://en.wikipedia.org/wiki/ZX_Spectrum) and [ZX Spectrum Next](https://www.specnext.com/about/)™.

Download the latest release [here](https://github.com/Threetwosevensixseven/CSpectPlugins/releases/latest).

## Plugins
### UARTLogger
A configurable logger for the Next ESP and Pi UARTs emulated by CSpect. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/UART-Logger) for installation and configuration details.

### UARTReplacement
A buffered UART replacing the internal CSpect emulated [ESP](https://wiki.specnext.dev/ESP8266-01) UART. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/UART-Replacement) for installation and configuration details.

The replacement UART writes binary bytes to the serial port, whereas the internal CSpect UART constrains bytes to ASCII characters `0x00..0x3f`. This doesn't matter so much for sending AT commands to the ESP-01, but programming the ESP with [low-level SLIP commands](https://github.com/espressif/esptool/wiki/Serial-Protocol) requires a binary UART.

THe UART dynamically responds to baud rate changes written to the [Next](https://www.specnext.com/about/)'s UART I/O ports, using prescaler calculations taking into account the current video timing.

### RTCSys
A simple date/time plugin which works in tandem with a custom `RTC.SYS` driver to provide date/time on the NextZXOS main menu, and to the `M_GETDATE` and `IDE_RTC API` calls. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/RTCSys) for installation details.

### RTC
A configurable RTC provider to supply your PC's date/time over the [I2C](https://en.wikipedia.org/wiki/I%C2%B2C) bus, emulating the [DS1307](https://github.com/Threetwosevensixseven/CSpectPlugins/blob/master/RTC/Docs/ds1307-1177772.pdf) RTC chip. This will make CSpect work with `.date`, `.time` and `RTC.SYS`, and display the time on the NextZXOS menus. Work in progress, coming soon!

### i2C_Sample
The sample plugin included with CSpect.

## CSpect
CSpect is a ZXSpectrum emulator by Mike Dailly.

Download the latest version [here](http://www.cspect.org/). These plugins only work with v2.12.20 or newer.

## Copyright and Licence
All plugins except i2C_Sample are copyright © 2019-2020 Robin Verhagen-Guest, and are licensed under [Apache 2.0](https://github.com/Threetwosevensixseven/CSpectPlugins/blob/master/LICENSE).

CSpect and the i2C_Sample example project are copyright © 1998-2020 Mike Dailly All rights reserved.

[hdfmonkey](https://github.com/gasman/hdfmonkey) is copyright © Matt Westcott 2010, and is licensed under [GPL-3.0](https://github.com/gasman/hdfmonkey/blob/master/COPYING).

ZX Spectrum Next is a trademark of SpecNext Ltd.
