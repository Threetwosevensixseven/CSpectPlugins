# CSpectPlugins
Various plugins for [CSpect](http://www.cspect.org/), an emulator for the [ZX Spectrum](https://en.wikipedia.org/wiki/ZX_Spectrum) and [ZX Spectrum Next](https://www.specnext.com/about/)™.

Download the latest release [here](https://github.com/Threetwosevensixseven/CSpectPlugins/releases/latest).

## Plugins
### UARTLogger
A configurable logger for the Next ESP and Pi UARTs emulated by CSpect. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/UART-Logger) for installation and configuration details.

### UARTReplacement
A buffered UART replacing the internal CSpect emulated [ESP](https://wiki.specnext.dev/ESP8266-01) and [Raspberry Pi Accelerator](https://wiki.specnext.dev/Pi:Main_Page) UARTs. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/UART-Replacement) for installation and configuration details.

The replacement UARTs write binary bytes to the serial port, whereas the internal CSpect UARTs constrain bytes to ASCII characters `0x00..0x3f`. This doesn't matter so much for sending AT commands to the ESP-01, but programming the ESP with [low-level SLIP commands](https://github.com/espressif/esptool/wiki/Serial-Protocol) requires a binary UART. Also the [NextPi](https://wiki.specnext.dev/Pi:NextPi) SUPervisor uses non-ASCII control bytes.

The UARTs dynamically respond to baud rate changes written to the Next's [UART I/O ports](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/1a711c9af61876f22ffd9512fa8d8544d3f53b16/cores/zxnext/ports.txt#L364), using prescaler calculations taking into account the [current video timing](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L232).

The UARTs also drive the serial [DTR](https://en.wikipedia.org/wiki/Data_Terminal_Ready)
and
[RTS](https://en.wikipedia.org/wiki/RS-232#RTS,_CTS,_and_RTR)
lines in response to ESP 
[RST](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L48)
and GPIO
[enable](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L927)
and
[control](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L929)
nextregs; and Pi GPIO
[enable](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L887)
and
[control](https://gitlab.com/SpectrumNext/ZX_Spectrum_Next_FPGA/-/blob/dad52d4a91e43850c55bc1e3c76cebfe45d49164/cores/zxnext/nextreg.txt#L882)
nextregs. This is required for ESP
[boot mode selection](https://docs.espressif.com/projects/esptool/en/latest/esp8266/advanced-topics/boot-mode-selection.html),
and for a proposed NextPi GPIO protocol to control the SUPervisor and applications when the UART is otherwise engaged.

**If you're using this plugin, _do not_ start CSpect with the `-com` or `-com2` arguments.**

### RTCSys
A simple date/time plugin which works in tandem with a custom `RTC.SYS` driver to provide date/time on the NextZXOS main menu, and to the `M_GETDATE` and `IDE_RTC API` calls. See its [wiki page](https://github.com/Threetwosevensixseven/CSpectPlugins/wiki/RTCSys) for installation details.

### RTC
A configurable RTC provider to supply your PC's date/time over the [I2C](https://en.wikipedia.org/wiki/I%C2%B2C) bus, emulating the [DS1307](https://github.com/Threetwosevensixseven/CSpectPlugins/blob/master/RTC/Docs/ds1307-1177772.pdf) RTC chip. This will make CSpect work with `.date`, `.time` and `RTC.SYS`, and display the time on the NextZXOS menus. Work in progress, coming soon!

### i2C_Sample
The sample plugin included with CSpect.

## CSpect
CSpect is a ZXSpectrum emulator by Mike Dailly.

Download the latest version [here](http://www.cspect.org/). These plugins only work with v3.0.15.2 or newer.

## Copyright and Licence
All plugins except i2C_Sample are copyright © 2019-2025 Robin Verhagen-Guest, and are licensed under [Apache 2.0](https://github.com/Threetwosevensixseven/CSpectPlugins/blob/master/LICENSE).

CSpect and the i2C_Sample example project are copyright © 1998-2025 Mike Dailly All rights reserved.

[hdfmonkey](https://github.com/gasman/hdfmonkey) is copyright © Matt Westcott 2010, and is licensed under [GPL-3.0](https://github.com/gasman/hdfmonkey/blob/master/COPYING).

ZX Spectrum Next is a trademark of SpecNext Ltd.
