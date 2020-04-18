; main.asm

;  Copyright 2019-2020 Robin Verhagen-Guest
;
; Licensed under the Apache License, Version 2.0 (the "License");
; you may not use this file except in compliance with the License.
; You may obtain a copy of the License at
;
;     http://www.apache.org/licenses/LICENSE-2.0
;
; Unless required by applicable law or agreed to in writing, software
; distributed under the License is distributed on an "AS IS" BASIS,
; WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
; See the License for the specific language governing permissions and
; limitations under the License.
                                                        ; Assembles with Next version of Zeus
zeusemulate             "Next", "RAW", "NOROM"          ; from http://www.desdes.com/products/oldfiles/zeustest.exe
zxnextmap -1,DotBank1,-1,-1,-1,-1,-1,-1                 ; Assemble into Next RAM bank but displace back down to $2000
zoSupportStringEscapes  = true;
optionsize 5
CSpect optionbool 15, -15, "CSpect", false              ; Option in Zeus GUI to launch CSpect

org $2700                                               ; RTC.SYS always starts at $2700
Start                   proc
                        nextreg 0, $12                  ; Write first magic value to read-only register
                        nextreg 14, $34                  ; Write second magic value to read-only register
                        ld bc, $243B
                        call ReadReg                    ; Read date LSB
                        ld l, a                         ; into L register
                        call ReadReg                    ; Read date MSB
                        ld h, a                         ; into H register
                        push hl                         ; Save date on stack
                        call ReadReg                    ; Read time LSB
                        ld e, a                         ; into E register
                        call ReadReg                    ; Read time MSB
                        ld d, a                         ; into D register
                        call ReadReg                    ; Read whole seconds
                        ld h, a                         ; into H register
                        ld l, $FF                       ; Signal no milliseconds
                        pop bc                          ; Restore date from stack
                        ccf                             ; Signal success
                        ret                             ; Return from RTC.SYS
pend

ReadReg                 proc
                        ld a, $7F
                        out (c), a
                        inc b
                        in a, (c)
                        dec b
                        ret
pend
                        include "constants.asm"         ; Global constants
                        include "macros.asm"            ; Zeus macros

Length equ $-Start
zeusprinthex "Command size: ", Length

zeusassert zeusver<=75, "Upgrade to Zeus v4.00 (TEST ONLY) or above, available at http://www.desdes.com/products/oldfiles/zeustest.exe"

if (Length > $2000)
  zeuserror "DOT command is too large to assemble!"
endif

output_bin "..\\bin\\RTC.SYS", zeusmmu(DotBank1)+$700, Length

if enabled CSpect
  zeusinvoke "..\\build\\cspect.bat", "", false
else
  //zeusinvoke "..\\..\\build\\builddot.bat"
endif

