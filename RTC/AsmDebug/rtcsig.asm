;
; TBBlue / ZX Spectrum Next project
; Copyright (c) 2010-2018
;
; RTC SIG - Victor Trucco and Tim Gilberts
;
; All rights reserved
;
; Redistribution and use in source and synthezised forms, with or without
; modification, are permitted provided that the following conditions are met:
;
; Redistributions of source code must retain the above copyright notice,
; this list of conditions and the following disclaimer.
;
; Redistributions in synthesized form must reproduce the above copyright
; notice, this list of conditions and the following disclaimer in the
; documentation and/or other materials provided with the distribution.
;
; Neither the name of the author nor the names of other contributors may
; be used to endorse or promote products derived from this software without
; specific prior written permission.
;
; THIS CODE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
; AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
; THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
; PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE
; LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
; CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
; SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
; INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
; CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
; ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
; POSSIBILITY OF SUCH DAMAGE.
;
; You are responsible for any legal issues arising from your use of this code.
;
;-------------------------------------------------------------------------------
;
; RTC(SIG).SYS for NextZXOS
;
; Thanks to VELESOFT for the help.
;
; Time stamps added by Tim Gilberts on behalf of Garry (Dios de) Lancaster 9/12/2017
; by code optimization - some notes left in for assumptions.
; V2.1 corrected bit bugs in Month resulting from adding time.
; v2.2 added Signature check for RTC to return Carry if error, removed old DivMMC redundant code for Next
; v2.3 added support for actual seconds in H under NextZXOS with L as 100ths
; if supported or 255 otherwise
;
; This version >256 bytes for extra features in NextZXOS
;
; OUTPUT
; reg BC is Date
;       year - 1980 (7 bits) + month (4 bits) + day (5 bits)
;       note that DS1307 only supports 2000-2099.
;
; reg DE is Time
;       hours (5 bits) + minutes (6 bits) + seconds/2 (5 bits)
;
; reg H is Actual seconds, L is 100ths or 255 if not supported.
;
; Carry set if no valid signature in 0x3e and 0x3f i.e. letters 'ZX'
; this is used to detect no RTC or unset date / time.
;
; ds1307 serial I2C RTC
;       11010001 = 0xD0 = read
;       11010000 = 0xD1 = write
;
; SCL port at 0x103B
; SDA port at 0x113B
;
; Built for Z80ASM in Z88DK
;
; Reference Documents
;
; DS1307 data sheet and MSFAT32 Hardware White Paper
;
; V2 prep to test for ACK on data transmission - needed for other devices
;

        PORT equ 0x3B
        PORT_CLOCK equ 0x10
        PORT_DATA equ 0x11

        org 0x2700


START:
        ; save A and HL
        ; BC and DE will contain our date and time
;       push hl
        LD (END_label+1),A

        ;---------------------------------------------------
        ; Talk to DS1307 and request the first reg
        call START_SEQUENCE
        //Freeze(1,4)
        ld l,0xD0
        call SEND_DATA
;Need to check Carry here and return with error if so? Maybe too far for JR RET C does not restore
;       JR C, END

        //Freeze(1,3)

        ld l,0x3E               ;Read from just before registers will loop
        call SEND_DATA
;Could check here as well or we could allow SEND_DATA to end

        //Freeze(1,5)

        call START_SEQUENCE

        //Freeze(1,6)

        ld l,0xD1
        call SEND_DATA

        //Freeze(1,2)

        ;---------------------------------------------------

        ;point to the first storage space (signature) in table
        LD HL,SIG

        ;there are 8 regs to read and 2 bytes get SIGNATURE
        LD e, 10

loop_read:

        //CSBreak()

        call READ

        Freeze(6,3)

        ;point to next reg
        inc l

        ;dec number of regs
        dec e
        jr z, end_read

        ;if don´t finish, send as ACK and loop
        call SEND_ACK
        jr loop_read

        ;we just finished to read the I2C, send a NACK and STOP
end_read:
        ld a,1
        call SEND_ACK_NACK

        CALL SDA0
        CALL SCL1
        CALL SDA1

;       OR A            ;Clear Carry (byte saved as SDA0 does XOR A and nothing else affects carry)
        LD HL,(SIG)
        LD DE,585Ah     ;ZX=Sig
        SBC HL,DE
        SCF             ;Flag an error
        JR NZ,END_label

        ;-------------------------------------------------
        ;prepare the bytes to ESXDOS and NextOS

        ;prepare SECONDS
        LD HL,SEC

        CALL LOAD_PREPARE_AND_MULT

        LD (HL),A       ; Save real seconds.

        srl a   ;seconds / 2

        ld e,a  ;save the SECONDS first 5 bits in E

        ;prepare MINUTES
        inc HL

        CALL LOAD_PREPARE_AND_MULT

        ; 3 MSB bits fom minutes in D
        ld d,a
        srl d
        srl d
        srl d
;
        ; 3 LSB from minutes
        RRCA
        RRCA
        RRCA
        AND %11100000

        or e    ; combine with SECONDS
        ld e,a  ; save the 3 LSB minute bits in E

        ;prepare HOURS
        inc HL

        CALL LOAD_PREPARE_AND_MULT

        RLCA
        RLCA
        RLCA
        AND %11111000

        OR D
        LD D,A

        ;skip DAY (of week 1-7)
        INC HL
        INC HL  ;Point at DATE

        ;-------------------------------------------

        call LOAD_PREPARE_AND_MULT

        ld c,a ; save day in c

        ;prepare MONTH
        inc HL

        CALL LOAD_PREPARE_AND_MULT

        ; MSB bit from month in B
        RRA
        RRA
        RRA
        RRA                     ;The MSB we need is in Carry now

        LD B,0
        RL B                    ;So put it at the bottom
        AND %11100000

        or c ; combine with day
        LD C,A ;store

        ;prepare YEAR
        inc HL

        PUSH BC
        CALL LOAD_PREPARE_AND_MULT
        POP BC

        ;now we have the year in A. format 00-99 (2000 to 2099)
        add a,20        ;(current year - 1980)
        sla a           ;get 7 LSB (range is below 127 so bit 7 = 0 means carry will be zero...
        or B            ;and combine with MONTH
        LD B,A          ;STORE the result in B

;Victor thinks this code was a signal on the Velesoft interface to tell ESXDOS that an RTC was onboard
;REMOVED for Next but, left here as you will may need to squeeze it back in on non Next Hardware. (No space left)
;       push bc
;       ld bc, 0x0d03
;       in a,(c)
;       res 0,a
;       out (c),a
;       pop bc

        ;4951 = 17/10/2016

;return without error as the Carry flag is clearead by the sla a above.

;Self modify the saved A on exit
END_label:
        LD A,0

        LD HL,(SIG+1)           ; H will be seconds
        LD L,255                ; No 100ths

;       POP HL
        ret


;This routine gets the BCD bytes and coverts to a number

LOAD_PREPARE_AND_MULT:

        XOR A
        RLD ;(HL)

        ld b,a          ;x10
        add a,a
        add a,a
        add a,a
        add a,b
        add a,b
        ld b,a

        RLD ;(HL)
        and 0x0F

        add a,b

        ret


;Actual loops to bit bang I2C

SEND_DATA:

        ;8 bits
        ld h,8

SEND_DATA_LOOP:

        ;next bit
        RLC L

        ld a,L
        CALL SDA

        call PULSE_CLOCK

        dec h
        jr nz, SEND_DATA_LOOP

WAIT_ACK:

        ;free the line to wait the ACK
        CALL SDA1

;But it does not check the ack it just pulse
;
        JR PULSE_CLOCK


READ:

;       free the data line
        CALL SDA1



; lets read 8 bits
        ld D,8

READ_LOOP:

        ;clock is high
        CALL SCL1



        ;read the bit
        ld b,PORT_DATA
        in a,(c)

        RRCA            ;Shift direct into memory through Carry
        RL (HL)

        ;clock is low
        CALL SCL0

        dec d

        ;go to next bit
        jr nz, READ_LOOP

        ;finish the byte read
        ret

SEND_ACK:

        xor a  ;a=0

SEND_ACK_NACK:

        CALL SDA

        call PULSE_CLOCK

        ;free the data line
        JR SDA1

START_SEQUENCE:

        ;high in both i2c, before begin
        ld a,1                                          ;Could save two bytes here change CALL SCL to CALL SCL1 to remove ld a,1
        ld c, PORT
        CALL SCL
        CALL SDA

        ;high to low when clock is high
        CALL SDA0

        ;low the clock to start sending data
        JR SCL

;Poss replace

SDA0:
        xor a
        jr SDA
SDA1:
        ld a,1
SDA:
        ld b,PORT_DATA
        JR SCLO


PULSE_CLOCK:
        CALL SCL1

SCL0:
        xor a
        jr SCL
SCL1:
        ld a,1
SCL:
        ld b,PORT_CLOCK
SCLO:   OUT (c), a
        ret

        defs    12      ;move onto next page

;Data storage space note that SIG is from 0x3E and 0x3F but
;counter will wrap to 0x00 to get date registers, we read time as well as no harm
SIG:            defw 0
SEC:            defb 0
MIN:            defb 0
HOU:            defb 0
DAY:            defb 0
DATE:           defb 0
MON:            defb 0
YEA:            defb 0
CON:            defb 0



