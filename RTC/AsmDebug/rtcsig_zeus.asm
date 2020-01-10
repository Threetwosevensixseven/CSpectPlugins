; rtcsig_zeus.asm
; Built using Zeus

zeusemulate "48K", "RAW", "NOROM"                       ; because that makes it easier to assemble dot commands;
zoSupportStringEscapes = true                           ; Download Zeus.exe from http://www.desdes.com/products/oldfiles/
optionsize 5
CSpect optionbool 15, -15, "CSpect", false              ; Option in Zeus GUI to launch CSpect
UploadNext optionbool 80, -15, "Next", false            ; Copy dot command to Next FlashAir card
//ErrDebug optionbool 130, -15, "Debug", false          ; Print errors onscreen and halt instead of returning to BASIC

include "rtcsig.asm"

include "macros.asm"

Length equ $-START
zeusprinthex "Command size: ", Length

if zeusver >= 74
  zeuserror "Does not run on Zeus v4.00 (TEST ONLY) or above, Get v3.991 available at http://www.desdes.com/products/oldfiles/zeus.exe"
endif

if (Length > $2000)
  zeuserror "DOT command is too large to assemble!"
endif

output_bin "RTC.SYS", START, Length

if enabled UploadNext
  output_bin "R:\\NextZXOS\\RTC.SYS", START, Length
endif

if enabled CSpect
  zeusinvoke "cspect_rtcsig.bat"
endif
