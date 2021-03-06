; macros.asm

Border                  macro(Colour)
                        if Colour=0
                          xor a
                        else
                          ld a, Colour
                        endif
                        out ($FE), a
                        if Colour=0
                          xor a
                        else
                          ld a, Colour*8
                        endif
                        ld (23624), a
mend

Freeze                  macro(Colour1, Colour2)
Loop:                   Border(Colour1)
                        Border(Colour2)
                        jr Loop
mend

CSBreak                 macro()                         ; Intended for CSpect debugging
                        push bc                         ; enabled when the -brk switch is supplied
                        noflow                          ; Mitigate the worst effect of running on real hardware
                        db $DD, $01                     ; On real Z80 or Z80N, this does NOP:LD BC, NNNN
                        nop                             ; so we set safe values for NN
                        nop                             ; and NN,
                        pop bc                          ; then we restore the value of bc we saved earlier
mend

CSExit                  macro()                         ; Intended for CSpect debugging
                        noflow                          ; enabled when the -exit switch is supplied
                        db $DD, $00                     ; This executes as NOP:NOP on real hardware
mend

