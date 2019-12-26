;
; TBBlue / ZX Spectrum Next project
; Copyright (c) 2010-2018 
;
; RTC TIME - Victor Trucco and Tim Gilberts
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
; .TIME for ESXDOS
;
; - Return the time saved in DS1307
; - Save the time in DS1307
;
; Bug fixed by Tim Gilberts 9/12/2017 (re inability to set time due to non quoted colons)
; and used to test/debug code optimization for RTC.SYS - left as a useful test system for RTC chips...
; v3.1 added config register to RTC debug and sorted RTC.SYS debug to BC/DE loss of bit.
; v3.2 added signature management for RTC.SYS - signature set whenever time is written unless -i on debug
; v3.3 added ability to write registers but plan to seperate debug code to seperate DS1307 program?
; v3.4 support HL return from RTC API with seconds in H and 100ths in L or 255
;      if not supported.

;	DEVICE   ZXSPECTRUM48

;	output "TIME"

	org 0x2000

	
	defc PORT = 0x3B
	defc PORT_CLOCK = 0x10 ;0x103b
	defc PORT_DATA = 0x11 ;0x113b

  
MAIN:
	ld a,h
	or l
	JP z,READ_TIME_NODEBUG  ;if we dont have parameters it is a read command

	LD A,(HL)

	CP '-'		;options flag
	JR NZ,not_options
	
	INC HL
	LD A,(HL)
	CP 'd'		; 
	JP Z,READ_TIME_DEBUG
	
	CP 'w'		; Write a register
	JP Z,WRITE_REGISTER
	
	CP 'r'		; Read registers
	JP Z,READ_REGISTER

	CP 'n'		; Use NextOS RTC hook
	JP Z,NEXTOS_HOOK

	JP end_error
	
not_options:
	CP 34		;Time should be quoted
	JR NZ,end_error
	INC HL

	;HL point to parameters
	CALL CONVERT_DIGITS_BCD
	jr c,end_error; return to basic with error message
	and 0x3f ; set the 24 hour mode
	LD (HOU),a ; store in the table
	
	inc HL ; separator
	
	inc HL
	CALL CONVERT_DIGITS_BCD
	jr c,end_error; return to basic with error message
	LD (MIN),a ; store in the table
	
	inc HL ;separator
	
	inc HL ; 
	CALL CONVERT_DIGITS_BCD
	jr c,end_error; return to basic with error message
	LD (SEC),a ; store in the table

	INC HL
	LD A,(HL)	;Time should be quoted
	CP A,34
	JR NZ,end_error

	CALL WRITE_TIME
	JR C,no_ack_error

	;it´s ok, lets show the current date but read back first to confirm OK
	CALL READ_TIME
	JP NC,PRINT_TIME

no_ack_error:
	LD HL,NoACKmessage
	JR print_error

debug_error:
	CALL prt_hex
	LD HL, ErrorMessage
	CALL print
	
end_error:
	LD HL, MsgUsage

print_error:
	CALL print	
	ret
	
	

;---------------------------------------------------
WRITE_TIME:

	call START_SEQUENCE
	
	ld l,0xD0 		;7bit Address 0x68 1101000 + 8th Zero Bit for write data
	call SEND_DATA
	JP C,STOP_SEQUENCE_CCF
	
	ld l,0x3E  		;Start at register 0x3E
	call SEND_DATA
	JP C,STOP_SEQUENCE_CCF

;Self modify here for diagnostics if asked
DEFC SIG_BYTE_1 = ASMPC+1
	 
	LD L,'Z'		;Stamp a signature in when time written
	CALL SEND_DATA
	JP C,STOP_SEQUENCE_CCF
	
DEFC SIG_BYTE_2 = ASMPC+1

	LD L,'X'		;This will tigger RTC.SYS to return valid time.
	CALL SEND_DATA
	JP C,STOP_SEQUENCE_CCF
	
	ld hl, (SEC)
	call SEND_DATA
	JP C,STOP_SEQUENCE_CCF
		
	ld hl, (MIN)
	call SEND_DATA
	JP C,STOP_SEQUENCE_CCF

	ld hl, (HOU)
	call SEND_DATA
	JP C,STOP_SEQUENCE_CCF

	JP STOP_SEQUENCE_CCF
	

;---------------------------------------------------
WRITE_REGISTER:

	PUSH HL
	LD HL,WritingMessage
	CALL print
	POP HL

	INC HL
	;HL point to parameters so get register to write
	CALL CONVERT_HEX_DEC
	jr c,debug_error; return to basic with error message
	CP 40h
	JR NC,debug_error
	
	PUSH HL
	LD (REGNUM),A ; store in the table
	CALL prt_hex
	CALL print_newline
	POP HL

	LD DE,BYT
	LD C,54
	
LOOP_READ_BYTES:
	INC HL
	LD A,(HL)
	CP 13
	JR Z,DONE_READ_BYTES
	
	CALL CONVERT_HEX_DEC
	jr c,ERROR_READ_BYTES		; return to basic with error message if needed

	LD (DE),A 			; store in the table

	PUSH HL
	CALL prt_hex
	POP HL
	
	INC DE
	DEC C
	JR NZ,LOOP_READ_BYTES

DONE_READ_BYTES:
	LD A,53
	SUB C				; See how many bytes read
	JR NC,ALL54

	OR @10000000		;Set high bit as syntax flag
	
ERROR_READ_BYTES:
	JP debug_error		; None was a syntax error

ALL54:
	INC A
	LD (REGCNT),A
	
	PUSH AF
	LD HL,HexLead
	CALL print
	POP AF
	CALL prt_hex
	LD HL,HexTail
	CALL print
	
	call START_SEQUENCE
	
	ld l,0xD0 
	call SEND_DATA
	JP C,EXIT_STOP_AND_ERROR
		
	ld a,(REGNUM)  ;Start at register parameter
	LD L,A
	call SEND_DATA
	JP C,EXIT_STOP_AND_ERROR
		
	LD HL,BYT
	LD A,(REGCNT)

LOOP_SEND_BYTES:
	PUSH AF	
	PUSH HL
	LD L,(HL)
	
	call SEND_DATA
	JP C,EXIT_STOP_SEQUENCE_SCF

	POP HL
	INC HL
	POP AF
	DEC A
	JR NZ,LOOP_SEND_BYTES

	JP STOP_SEQUENCE_CCF

EXIT_STOP_SEQUENCE_SCF:
	POP HL
	POP AF

EXIT_STOP_AND_ERROR:

	CALL STOP_SEQUENCE_SCF
	JP no_ack_error	


;---------------------------------------------------
READ_TIME:					

	call START_SEQUENCE
	
	ld l,0xD0		;7bit Address 0x68 1101000 + 8th 1 Bit for read data 
	call SEND_DATA
	JR C, STOP_SEQUENCE_SCF
	
	ld l,0x3E		;Start near end for signature read 
	call SEND_DATA
	JR C, STOP_SEQUENCE_SCF
	
	call START_SEQUENCE
	
	ld l,0xD1
	call SEND_DATA
	JR C, STOP_SEQUENCE_SCF
	
	;point to the first reg in table
	LD HL,SIG
	
	;there are 10 regs to read (7 only for time but 8 needed for debug, 2 for sig at end)
	;We read all anyway starting at -2 for diagnostic purposes
	LD e, 0x42
	
loop_read:
	call READ

	;point to next reg
	inc hl	
	
	;dec number of regs
	dec e
	jr z, end_read
	
	;if don´t finish, send as ACK and loop
	call SEND_ACK
	jr loop_read

	;we just finished to read the I2C, send a NACK and STOP
end_read:
	LD A,1	
	call SEND_ACK_NACK
;	call SEND_NACK
	
	
STOP_SEQUENCE_CCF:
	CALL SDA0
	CALL SCL1
	CALL SDA1
	EI
	RET

STOP_SEQUENCE_SCF:
	CALL SDA0
	CALL SCL1
	CALL SDA1
	EI
	SCF
	RET

;-----------------------------------------------------------	
READ_TIME_NODEBUG:
	CALL READ_TIME
	JP C,no_ack_error

	OR A		;Clear Carry 
	LD HL,(SIG)
	LD DE,585Ah	;ZX=Sig
	SBC HL,DE
	SCF		;Flag an error
	JR NZ,NO_RTC_FOUND


;---------------------------------------------------
PRINT_TIME:
	;get the sec
	LD HL, SEC
	LD a,(HL)
	and 0x7f
	call NUMBER_TO_ASC
	ld a,b
	LD (sec_txt),a
	ld a,c
	LD (sec_txt + 1),a
	
	;get the minutes
	inc HL
	LD a,(HL)
	call NUMBER_TO_ASC
	ld a,b
	LD (min_txt),a
	ld a,c
	LD (min_txt + 1),a

	;get the hour
	inc HL
	LD a,(HL)
	and 0x3f
	call NUMBER_TO_ASC
	ld a,b
	LD (hou_txt),a
	ld a,c
	LD (hou_txt + 1),a
	
	ld hl,MsgTime
	CALL print

	ret


NO_RTC_FOUND:

	LD HL,NoRTCmessage
	CALL print
	
	RET


;---------------------------------------------------
READ_REGISTER:					

	PUSH HL
	LD HL,ReadingMessage
	CALL print
	POP HL

	INC HL
	;HL point to parameters so get register to write
	CALL CONVERT_HEX_DEC
	JP C,debug_error; return to basic with error message
;	CP 40h
;	JR NC,debug_error
	
	PUSH HL
	LD (REGCNT),A ; store in the table
	CALL prt_hex
	LD HL,ReadingNoBytes
	POP HL
	
	INC HL

	CALL CONVERT_HEX_DEC
	JP C,debug_error		; return to basic with error message if needed

	LD (REGNUM),A 			; store in the table

	PUSH HL
	CALL prt_hex
	CALL print_newline
	POP HL
	
	call START_SEQUENCE
	
	ld l,0xD0		;7bit Address 0x68 1101000 + 8th 1 Bit for read data 
	call SEND_DATA
	JP C, STOP_SEQUENCE_SCF
	
	LD A,(REGNUM)
	ld l,A
	call SEND_DATA
	JP C, STOP_SEQUENCE_SCF
	
	call START_SEQUENCE
	
	ld l,0xD1
	call SEND_DATA
	JP C, STOP_SEQUENCE_SCF
	
	;point to the first reg in table
	LD HL,BYT
	
	LD A,(REGCNT)
	LD E,A
	
loop_read_regs:
	call READ

	;point to next reg
	inc hl	
	
	;dec number of regs
	dec e
	jr z, end_read_regs
	
	;if don´t finish, send as ACK and loop
	call SEND_ACK
	jr loop_read_regs

	;we just finished to read the I2C, send a NACK and STOP
end_read_regs:
	LD A,1	
	call SEND_ACK_NACK
;	call SEND_NACK
	
	
	CALL STOP_SEQUENCE_CCF

	LD A,(REGCNT)
	LD HL,BYT
	CALL print_reg

	CALL print_newline
	
	RET

;---------------------------------------------------
SEND_DATA:

	;8 bits
	ld h,8				;7
	
SEND_DATA_LOOP:	
	
	;next bit
	RLC L				;8
		
	ld a,L				;4
	CALL SDA			;17 + xxx
		
	call PULSE_CLOCK		;17 + xxx
		
	dec h				;4
	jr nz, SEND_DATA_LOOP		;12 or 7
		
WAIT_ACK:

	;free the line to wait the ACK
	CALL SDA1			;17 + xxx

;	JR PULSE_CLOCK			;12
	;so we now do the same but, check for an ACK coming back!
	;http://www.gammon.com.au/forum/?id=10896 useful to see the timing diagrams
	CALL SCL1			;17	;17 + 

	LD HL,4				;loop for a short while looking for the ACK
	
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


;---------------------------------------------------
;-- Read 8 bits from the bus - no automatic ACK
READ:

;	free the data line
	CALL SDA1			;17 + xxx
	
; lets read 8 bits
	ld D,8				;7
			
READ_LOOP:
	;clock is high
	CALL SCL1			;17 + xxx
	
	;read the bit
	ld b,PORT_DATA			;7
	in a,(c)			;12

	RRCA				;4
	RL (HL)				;15
	
	;clock is low
	CALL SCL0			;17 + xxx
	
	dec d				;4
	
	;go to next bit
	jr nz, READ_LOOP		;12 or 7
	
	;finish the byte read
	ret				;10

;-- ACK the bus, if A-1 can enter to send a NACK at second entry
	
SEND_ACK:
	
	xor a  				;4
	
SEND_ACK_NACK:

	CALL SDA			;17 + xxx
	
	call PULSE_CLOCK		;17 + xxx
	
	JR SDA1				;12
	
	
;---------------------------------------------------
START_SEQUENCE:	
	DI

	;high in both i2c, before begin
	ld a,1				;7	;Could save this by calling SCL1 not SCL...
	ld c, PORT			;7
	CALL SCL 			;17 + xxx
	CALL SDA			;17 + xxx

	;high to low when clock is high
	CALL SDA0			;17 + xxx
	
	;low the clock to start sending data
	JR SCL				;12


;---------------------------------------------------
SDA0:
	xor a 				;4
	jr SDA				;12
SDA1: 
	ld a,1				;7
SDA: 
	ld b,PORT_DATA			;7
	JR SCLO				;12
;One byte less to use out later - does mean we have same delay as Clock
;	 OUT (c), a
;	 ret

PULSE_CLOCK:
	CALL SCL1			;17
	NOP
	CALL SCL1			;Lengthen high state

SCL0:
	xor a 				;4
	jr SCL				;12
SCL1:
	ld a,1				;7
SCL:
	ld b,PORT_CLOCK			;7
SCLO:	OUT (c), a			;12
	NOP				;Wait 16 t states or approx 4usec at 4Mhz so more like 5 at 3.5

	ret				;10


;-------------------------------------------------
READ_TIME_DEBUG:

	INC HL
	LD A,(HL)
	CP 'i'
	JR NZ, PRINT_DIAGS

	CALL READ_TIME			;Get current data (ignore error return)
	CALL C,no_ack_error

BLANK_SIG:
	XOR A				;Stamp on signature
	LD (SIG_BYTE_1),A
	LD (SIG_BYTE_2),A
	
	CALL WRITE_TIME
	CALL C,no_ack_error

PRINT_DIAGS:

	CALL READ_TIME	
	CALL C,no_ack_error

	CALL PRINT_TIME

	CALL print_rtc

	;TEST HARNESS FOR RTC.SYS code can be commented out if you want.	
	;-------------------------------------------------
	;prepare the bytes to ESXDOS
	;
	; reg DE is Time
	;	hours (5 bits) + minutes (6 bits) + seconds/2 (5 bits)
	
	;prepare SECONDS
	LD HL,SEC
	
	CALL LOAD_PREPARE_AND_MULT	

	srl a ;seconds / 2
	
	ld e,a ; save the SECONDS first (MSB) 5 bits in E

	;debug sec
	LD (HL),a
		
	;prepare MINUTES
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT
	
	; 3 MSB bits fom minutes in D (we know 7 and 6 are zero in A)
	ld d,a
	srl d
	srl d
	srl d

	RRCA
	RRCA
	RRCA
	AND @11100000

	;debug min
 	LD (HL),a
 	
	or e ; combine with SECONDS
	ld e,a ; save the 3 LSB minute bits in E

	;prepare HOURS
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT
	
	RLCA
	RLCA
	RLCA
	AND @11111000
	
	OR D		;Combine saved 3 MSB of minutes
	LD D,A		;Complete value now in D

	;debug hours
 	LD (HL),a
		
	;skip DAY (0-7) use DATE instead (0-31)
	INC HL
	INC HL

	call LOAD_PREPARE_AND_MULT
	
;	 reg BC is Date 
;		year - 1980 (7 bits) + month (4 bits) + day (5 bits)

	ld c,a ; save day of month in c
	
	;debug day
	LD (HL),a
	
	;prepare MONTH
	inc HL
	
	CALL LOAD_PREPARE_AND_MULT

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
	
	;debug year
	LD (HL),a
	
	;now we have the year in A. format 00-99 (2000 to 2099) 
	add a,20 	;(current year - 1980) (will not overflow as only 0-99)
	sla a 		;get 7 LSB
	or B 		;and combine with MONTH
	LD B,A		;STORE the result in B
	
;	PUSH BC
;	PUSH DE
;	
;	CALL print_rtc		;Print our updated ones
;
;	POP DE
;	POP BC

	JP print_regs

	
	
;---------------------------------------------------	
LOAD_PREPARE_AND_MULT:
	XOR A		;1 less byte than an AND after
	RLD ;(HL)
	
	ld b,a
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
	
;---------------------------------------------------
;Use the NextOS RTC hook
;m_getdate          ; $8e (142)		get current date/time
NEXTOS_HOOK:
	RST $08
	DEFB $8E
	
	PUSH AF
	
	CALL print_regs

	POP HL
	CALL prt_hex
	CALL space
	LD A,L		;Flags
	CALL prt_hex
	JP print_newline

;---------------------------------------------------
CONVERT_HEX_DEC:

	CALL CONVERT_HEX_DEC_DIGIT
	JR C,DIGIT_ERROR

	RLCA	; A*16
	RLCA
	RLCA	
	RLCA
	
	LD B,A

	INC HL

	CALL CONVERT_HEX_DEC_DIGIT
	JR C,DIGIT_ERROR

	ADD A,B
			
	or a; clear the carry
	ret	

;---------------------------------------------------
CONVERT_HEX_DEC_DIGIT:
	LD a,(HL)
	;test ascii for 0 to 9
	CP 48
	JR C,DIGIT_ERROR 
	
	CP 58				;This is a colon - why we need a string but in this case is just first char above 9...
	JR C,ONLY_0TO9
	
	AND @11011111			;Force upper case
	
	CP 'A'
	JR C,DIGIT_ERROR
	
	CP 'G'
	JR NC,DIGIT_ERROR
	
	SUB 7				;Offset for A to be after 9
	
ONLY_0TO9:

	sub 48 				; convert asc to number
	
	OR A
	RET
	
DIGIT_ERROR:

	SCF
	RET

	
	
;---------------------------------------------------	
CONVERT_DIGITS_BCD:
	LD a,(HL)
	;test ascii for 0 to 9
	CP 48
	jr C,CHAR_ERROR 
	
	CP 58				;This is a colon - why we need a string but in this case is just first char above 9...
	jr NC,CHAR_ERROR
	
	or a; clear the carry
	
	sub 48 ; convert asc to number

	;first digit in upper bits
	SLA A
	SLA A
	SLA A
	SLA A

	LD b,a ; store in b
	
	; next parameter
	inc HL
	LD a,(HL)
	
	;test ascii for 0 to 9
	CP 48
	jr C,CHAR_ERROR 
	
	CP 58
	jr NC,CHAR_ERROR
	
	sub 48 ; convert asc to number

	and 0x0f ; get just the lower bits
	or b ;combine with first digit
	
	or a; clear the carry
	ret
	
	
CHAR_ERROR:
	
	scf ; set the carry
	ret

	

;---------------------------------------------------
NUMBER_TO_ASC:
	LD a,(HL)
	
	; get just the upper bits
	SRA A
	SRA A
	SRA A
	SRA A 
	add 48 ;convert number to ASCII
	LD b,a
	
	;now the lower bits
	LD a,(HL)
	and 0x0f ;just the lower bits
	add 48 ;convert number to ASCII
	LD c,a
	
	ret

	
	
	
;---------------------------------------------------

;-- Print out DE, BC and now HL registers. 
print_regs:		PUSH HL
			push BC
			push DE

		
			ld HL, str_DE
			call print
			pop hl
			call prt_hex_16
			call print_newline
			
			ld HL, str_BC
			call print
			pop hl
			call prt_hex_16
			call print_newline

			ld HL, str_HL
			call print
			pop hl
			call prt_hex_16
			call print_newline
			
			ret
			
;-- Print Diagnostic dump of DS1307
print_rtc:		ld HL, str_REG
			call print

			LD HL,SEC
			LD A,8

			CALL print_row
								
			LD HL,(SIG)
			call prt_hex_16
			
			JP print_newline

;-- Print A rows of 8 registers at HL
print_row:		PUSH AF
			PUSH HL	
			call print_newline			
			POP HL

			LD A,8
			CALL print_reg

			POP AF
			DEC A
			JR NZ,print_row

			RET

;-- Print A registers at HL
print_reg:		PUSH AF
			PUSH HL
			LD A,(HL)
			call prt_hex
			CALL space
			POP HL
			INC HL
			POP AF
			DEC A
			JR NZ,print_reg
			
			RET			
		
;---------------------------------------------		
	
;In .DOT command should be an RST someting not JP	
;openscreen:		ld a,2
;			jp $1601
			
sprint:			pop	hl
			call print
			jp (hl)

print:			ld	a,(hl)
			inc hl
			or a
			ret z
			bit 7,a
			ret nz
			rst 16
			jr print

print_newline:		ld hl,newline
			call print
			ret

hextab:			DEFM	"0123456789ABCDEF"

space:			ld	a,' '
			jp 16

prt_hex_16:		ld	a,h
			call prt_hex
			ld a,l
			
prt_hex:		push af
			rra
			rra
			rra
			rra
			call prt_hex_4
			pop af

prt_hex_4:		push hl
			and	15
			add a,hextab&255
			ld l,a
			adc a,hextab/256
			sub l
			ld h,a
			ld a,(hl)
			pop hl
			jp 16

prt_dec:		ld bc,10000
			call dl
			ld bc,1000
			call dl
			ld bc,100
			call dl
			ld bc,10
			call dl
			ld a,l
			add a,'0'
			jp 16
			
dl:			ld a,'0'-1

lp2:			inc a
			or a
			sbc hl,bc
			jr nc,lp2
			add hl,bc
			jp 16


str_DE:		DEFM "DE  : "
		DEFB 0
str_BC:		DEFM "BC  : "
		DEFB 0
str_HL:		DEFM "HL  : "
		DEFB 0
str_REG:	DEFM "RTC : "
		DEFB 0

newline:	DEFB 13,0	
	
	
	

MsgTime: defm "The time is "
hou_txt: defm "  "
slash1:  defm ":"
min_txt: defm "  "
slash2:  defm ":"
sec_txt: defm "  "
endmsg:  defb 13,13,0

ErrorMessage:	DEFM " - Error.",13,13,0

ReadingMessage: DEFM "Reading 0x",0

ReadingNoBytes:	DEFM " bytes from register 0x",0

WritingMessage: DEFM "Writing @ Register"
HexLead:	DEFM " 0x",0

HexTail:	DEFM " bytes to write.",13,13,0

NoRTCmessage:	DEFM "No valid RTC signature found.",13
		DEFM "Try setting time first or use -d",13,13,0

NoACKmessage:	DEFM "No ACK on address/reg select.",13
		DEFM "Probably no RTC clock at 0x68.",13,13,0

MsgUsage: defm "TIME V3.4 usage: ",13
	  defm "time <ENTER>"
	  defb 13
	  defm "show current time"
	  defb 13,13
	  defm "time \"HH:MM:SS\" <ENTER>"
	  defb 13
	  defm "set the time"
	  defb 13,13
	  defm "time -d{i} <ENTER>"
	  defb 13
	  defm "show current time, no ACK test"
	  defb 13
	  defm "i - wipe RAM ID signature."
	  defb 13,13
	  defm "-wRRHH{HH} - write upto 54 0xHH"
	  defb 13
	  defm "bytes into reg/ram from 0xRR."
	  defb 13,13
	  defm "-rNNRR - read 0xNN bytes "
	  defb 13,13
	  defm "-n - RTC.SYS API call result." 
	  defb 13,13
	  defm "Time is in 24 Hour mode"
	  defb 13,13,0

REGNUM:	DEFB 0				;Used to store start register for write
REGCNT:	DEFB 0				;and how many

SIG:	DEFW 0				;We start read at -2 0x3E so last bytes in Battery RAM used for signature (ZX) 	
SEC:	defb 0		
MIN:	defb 0	
HOU:	defb 0	
DAY:	defb 0		 
DATE:	defb 0	
MON:	defb 0	
YEA:	defb 0
CON:	defb 0
BYT:	DEFS 56  	
	
