;
; TBBlue / ZX Spectrum Next project
; Copyright (c) 2010-2019 
;
; RTC ACK - Victor Trucco and Tim Gilberts
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
; RTCACK.SYS for NextZXOS only.
;
; Thanks to VELESOFT for the help.
;
; Time stamps added by Tim Gilberts on behalf of Garry (Dios de) Lancaster 9/12/2017
; by code optimization - some notes left in for assumptions.
; V2.1 corrected bit bugs in Month resulting from adding time.
; v2.2 added Signature check for RTC to return Carry if error
;      removed old DivMMC redundant code for Next
; v1.0 ACK checked for on receipt of data instead of using signature 
; v1.1 Added support for extension to exact seconds and 100ths - H is seconds
;      and L is 100's if either not available then set to 255.
; v1.2 Saved IRQ state to avoid some bugs that appeared.
;
; Max size for this compiled ASM is 512 bytes!!! (256 bytes to fit in ESXDOS!)
;
; OUTPUT
; reg BC is Date 
;	year - 1980 (7 bits) + month (4 bits) + day (5 bits)
;	note that DS1307 only supports 2000-2099.
;
; reg DE is Time
;	hours (5 bits) + minutes (6 bits) + seconds/2 (5 bits)
;
; reg H is actual seconds
; reg L is 100ths if supported or 255 if not.
;
; Not in this code (but maybe good idea to add back as extra check):
; *Carry set if no valid signature in 0x3e and 0x3f i.e. letters 'ZX'
; *this is used to detect no RTC or unset date / time.
; for us
; Carry set if no valid ACK received on BUS from RTC chip.
;
; ds1307 serial I2C RTC 
;  	11010001 = 0xD0 = read
;	11010000 = 0xD1 = write
;
; SCL port at 0x103B
; SDA port at 0x113B
;
; Built for Z80ASM in Z88DK
;
; Reference Documents
;
; DS1307 data sheet and MSFAT32 Hardware White Paper
; DS3231 data sheet
;
; RTCACK version to test for ACK on data transmission - needed for other device support.
;

	defc PORT = 0x3B
	defc PORT_CLOCK = 0x10
	defc PORT_DATA = 0x11

	org 0x2700

  
START:

	LD HL,RETNOEI		;Will return enabling interrupts if they were on
	CALL System_CheckEIState
	JP PO,TALK		;IRQ not enabled already
	DI
	DEC HL			;Need to re-enable on way out

TALK:
	PUSH HL			;Return now on stack to either EI or real RET
	
	;---------------------------------------------------
	; Talk to DS1307 OR DS3231 and request the first reg

	call START_SEQUENCE
	
	ld l,0xD0
	call SEND_DATA
	JR C, end_stop
	
	ld l,0x00		;Read only the first 7 registers 
	call SEND_DATA
	JR C, end_stop	
		
	call START_SEQUENCE
	
	ld l,0xD1
	call SEND_DATA
	JR C,end_stop	
	;---------------------------------------------------
	
	;point to the first storage space (signature) in table
	LD HL,SEC
	
	;there are 7 regs to read that we need
	LD e, 7
	
loop_read:
	call READ

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
;	XOR A			;CCF (not needed complement not clear so always 
				;set! as above routine does the Clear...)

end_stop:

	PUSH AF

;STOP_SEQUENCE
	CALL SDA0
	CALL SCL1
	CALL SDA1

	POP AF

	RET C			; Exit if we had no acks, EI if needed.

;As we have more space on NextOS - check that the values read are in range - 
;the SIG is a good system but, does not work for other similar chips like the
;DS3231
	
	;-------------------------------------------------
	;prepare the bytes
PREP_TIME:	
	;prepare SECONDS
	LD HL,SEC
	
	CALL LOAD_PREPARE_AND_MULT
	CP 60
	JR NC,RANGE_ERROR

;	LD (SEC),A		;Destroy the BCD with actual value for later
	LD (HL),A
	
	srl a 	;seconds / 2
	
	ld e,a 	;save the SECONDS first 5 bits in E
	
	;prepare MINUTES
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT
	CP 60
	JR NC,RANGE_ERROR
	
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
	AND @11100000
	
	or e 	; combine with SECONDS
	ld e,a 	; save the 3 LSB minute bits in E
	
	;prepare HOURS
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT
	CP 24
	JR NC,RANGE_ERROR
	
	RLCA
	RLCA
	RLCA
	AND @11111000
	
	OR D
	LD D,A
	
	;skip DAY (of week 1-7)
	INC HL
	INC HL	;Point at DATE

	;-------------------------------------------

	call LOAD_PREPARE_AND_MULT
	CP 32
	JR NC,RANGE_ERROR

	ld c,a ; save day in c
	
	;prepare MONTH
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT
	CP 13
	JR NC,RANGE_ERROR
	
	; MSB bit from month in B 
	RRA
	RRA
	RRA
	RRA			;The MSB we need is in Carry now

	LD B,0
	RL B			;So put it at the bottom
	AND @11100000
	
	or c ; combine with day
	LD C,A ;store
	
	;prepare YEAR
	inc HL
	
	PUSH BC
	CALL LOAD_PREPARE_AND_MULT
	POP BC
	
	;now we have the year in A. format 00-99 (2000 to 2099)
	add a,20 	;(current year - 1980)
	sla a 		;get 7 LSB (range is below 127 so bit 7 = 0 means carry will be zero... (if year was mad value C=1 so error)
	or B 		;and combine with MONTH
	LD B,A		;STORE the result in B


;return without error as the Carry flag is clearead by the sla a above.
END:	
	LD HL,(HUN)	;Get the extension value and the full precision seconds
	RET		;Will return either enabling interrupts or not


RANGE_ERROR:
;	LD D,A		; In case A never makes it back we know what range issue was...
	SCF
	RET

; I had previously devised the bytes below SP changing on an IRQ as a
; detection system for timing effects so when I looked into how to save irq 
; disable state accurately on a Z80 I was pleased to find:
; https://www.msx.org/forum/msx-talk/development/do-not-use-ld-ai-read-interrupt-enabled-state?page=

RETEI:	
	EI
RETNOEI:
	RET

; Workaround for LD A,I / LD A,R lying to us if an interrupt occurs during the
; instruction. We detect this by examining if (sp - 1) was overwritten.
; f: pe <- interrupts enabled
; Modifies: af

System_CheckEIState:
    xor a
    push af  ; set (sp - 1) to 0
    pop af
    ld a,i
    ret pe  ; interrupts enabled? return with pe
    dec sp
    dec sp  ; check whether the Z80 lied about ints being disabled
    pop af  ; (sp - 1) is overwritten w/ MSB of ret address if an ISR occurred
    sub 1
    sbc a,a
    and 1  ; (sp - 1) is not 0? return with pe, otherwise po
    ret


;This routine gets the BCD bytes and converts to a number
	
LOAD_PREPARE_AND_MULT:

	XOR A
	RLD ;(HL)

	ld b,a		;x10
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
	CALL SDA1			;17 + xxx

;	JR PULSE_CLOCK			;12
	;so we now do the same but, check for an ACK coming back!
	;http://www.gammon.com.au/forum/?id=10896 useful to see the timing diagrams
	CALL SCL1			;17	;17 + 

	LD L,4				;loop for a short while looking for the ACK (NB H=0 anyway)
	
WAIT_ACK_LOOP:
	LD B,PORT_DATA			;7
	IN A,(C)			;12
	RRCA
	JR C,LINE_HIGH			
	
	INC H				;Something on the bus pulled SDA low - count how long
	
LINE_HIGH:
	DEC L
	JR NZ,WAIT_ACK_LOOP

	CALL SCL0			;If stitched back into RTC etc could be a JP to save a byte this will CCF due to XOR A

	LD A,H
	SUB 2				;Carry will be set if we did not receive at least duration 2 of pulse.

	RET



;-- Read 8 bits from the line
READ:

;	free the data line
	CALL SDA1
	
; lets read 8 bits
	ld D,8	

READ_LOOP:
	
	;clock is high
	CALL SCL1	
	
	;read the bit
	ld b,PORT_DATA
	in a,(c)
	
	RRCA		;Shift direct into memory through Carry
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
	ld c, PORT
	CALL SCL1 
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
	CALL SCL1			;17
	NOP
	CALL SCL1			;Lengthen high state

SCL0:
	xor a 
	jr SCL
SCL1:
	ld a,1
SCL:
	ld b,PORT_CLOCK
SCLO:	OUT (c), a
	NOP
	ret



;Data storage - don't need CON saves a byte - make sure it is within a 256 boundary...
HUN:		defb 255		;Keep track of 100ths if available
SEC:		defb 0			;Read as BCD bytes, ends up as actual
MIN:		defb 0	
HOU:		defb 0	
DAY:		defb 0			;Wasted byte no easy way to recover it	 
DATE:		defb 0	
MON:		defb 0	
YEA:		defb 0	
;CON:		defb 0



	
