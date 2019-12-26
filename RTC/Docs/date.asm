;
; TBBlue / ZX Spectrum Next project
; Copyright (c) 2010-2018 
;
; RTC DATE - Victor Trucco and Tim Gilberts
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
; .DATE for ESXDOS and NextZXOS
;
; - Return the date saved in DS1307
; - Save the date in DS1307
;
; Updated by Tim Gilberts Dec 2017 to standardise with TIME and RTC.SYS
; V2.0 Added use of quotes for consistency with TIME on setting the clock.
; and bugfix for dates > 2079 along with some basic checks, support of RTC signature.
;
; Built using Z80ASM for Z88DK

	org 0x2000

	
	defc PORT = 0x3B
	defc PORT_CLOCK = 0x10 ;0x103b
	defc PORT_DATA = 0x11 ;0x113b


  
MAIN:
	ld a,h
	or l
	JP z,READ_DATE  ;if we dont have parameters it is a read command

	LD A,(HL)
	
	CP '-'		;options flag (anything gives the help)
	JP Z,end_error

	CP 34		;Date should be quoted
	JP NZ,end_error
	INC HL

;--DATE IN MONTH	
	CALL CONVERT_DIGITS
	JP c,end_error; return to basic with error message
	
	LD (DATE),a 	; store in the table for diag after if needed
	OR A
	JP Z,end_error

;TODO check for >31 (need to calculate number for that)
	
	inc HL ; separator (can be anything really)

;--MONTH	
	inc HL
	CALL CONVERT_DIGITS
	jr c,end_error; return to basic with error message

	LD (MON),a ; store in the table
	OR A
	JR Z,end_error

;TODO check for > 12
	
	inc HL ;separator
	inc HL ; 2
	LD A,(HL)
	CP '2'
	JR NZ,end_error
	
	inc HL ; 0
	LD A,(HL)
	CP '0'
	JR NZ,end_error

;YEAR - no check as can be 00-99	
	inc HL ; 
	CALL CONVERT_DIGITS
	jr c,end_error; return to basic with error message

	LD (YEA),a ; store in the table

	INC HL
	LD A,(HL)	;Date should be quoted
	CP A,34
	JR NZ,end_error
	
	;---------------------------------------------------
	; Talk to DS1307 
	call START_SEQUENCE
	
	ld l,0xD0 
	call SEND_DATA
	
	ld l,0x04  ;start to send at reg 0x04 (date) 
	call SEND_DATA
	
	;---------------------------------------------------

	ld hl, (DATE)
	call SEND_DATA
	
	ld hl, (MON)
	call SEND_DATA

	ld hl, (YEA)
	call SEND_DATA

	;STOP_SEQUENCE
	CALL SDA0
	CALL SCL1
	CALL SDA1
	
	;---------------------------------------------------
	; Talk to DS1307 
WRITE_SIG:
	call START_SEQUENCE
	
	ld l,0xD0 
	call SEND_DATA
	
	ld l,0x3E  ;start to send at reg 0x3E (sig) 
	call SEND_DATA
	
	;---------------------------------------------------

	LD L,'Z'
	call SEND_DATA
	
	LD L,'X'
	call SEND_DATA

	;STOP_SEQUENCE
	CALL SDA0
	CALL SCL1
	CALL SDA1
	
	;-------------------------------------------------	
	
end:
	;it´s ok, lets show the current date
	JR READ_DATE
	ret
	
	;return to basic with an error message

diag_code: call prt_hex
	call print_newline
	
end_error:
	LD HL, MsgUsage

	CALL PrintMsg
		
	ret
	
	
	
CONVERT_DIGITS:
	LD a,(HL)
	;test ascii for 0 to 9
	CP 48
	jr C,CHAR_ERROR 
	
	CP 58
	jr NC,CHAR_ERROR
	
	or a; clear the carry
	
	sub 48 ; convert asc to number
	
	;first digit in upper bits
	SLA A
	SLA A
	SLA A
	SLA A

	LD b,a ; store in b
	
	; next digit or seperator for 0-9 case.
	inc HL
	LD a,(HL)
	
	CP '/'
	JR Z,SINGLE_DIGIT
	
	;test ascii for 0 to 9
	CP 48
	jr C,CHAR_ERROR 
	
	CP 58
	jr NC,CHAR_ERROR
	
	OR A
	sub 48 ; convert asc to number

	and 0x0f ; get just the lower bits
	or b ;combine with first digit

	
	or a; clear the carry
	ret
	
SINGLE_DIGIT:

	DEC HL
	LD A,(HL)
	OR A		;Clear Carry
	SUB 48		;There should be no carry after this.
	OR A
	RET
	
	
CHAR_ERROR:
	
	scf ; set the carry
	ret
	
	
	
READ_DATE:					
	;---------------------------------------------------
	; Talk to DS1307 and request all the regisers and 0x3e 0x3f
	call START_SEQUENCE
	
	ld l,0xD0 
	call SEND_DATA
	
	ld l,0x3E		;Start at last two bytes to get signature 
	call SEND_DATA
	
	call START_SEQUENCE
	
	ld l,0xD1
	call SEND_DATA
	;---------------------------------------------------
	
	;point to the first reg in table
	LD HL,SIG
	
	;there are 7 regs to read and 2 bytes of signature
	LD e, 9
	
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
	call SEND_NACK
	
	;STOP_SEQUENCE:
	CALL SDA0
	CALL SCL1
	CALL SDA1

;-----------------------------------------------------------	

	OR A		;Clear Carry 
	LD HL,(SIG)
	LD DE,585Ah	;ZX=Sig
	SBC HL,DE
	SCF		;Flag an error
	JR NZ,NO_RTC_FOUND

	;get the date
	LD HL, DATE
	LD a,(HL)
	call NUMBER_TO_ASC
	ld a,b
	LD (day_txt),a
	ld a,c
	LD (day_txt + 1),a
	
	
	
	;get the month
	inc HL
	LD a,(HL)
	call NUMBER_TO_ASC
	ld a,b
	LD (mon_txt),a
	ld a,c
	LD (mon_txt + 1),a

	;get the year
	inc HL
	LD a,(HL)
	call NUMBER_TO_ASC
	ld a,b
	LD (yea_txt),a
	ld a,c
	LD (yea_txt + 1),a

	
	ld hl,MsgDate
	CALL PrintMsg

	ret

NO_RTC_FOUND:

	LD HL,NoRTCmessage
	CALL PrintMsg
	
	RET


	
NUMBER_TO_ASC:
	LD a,(HL)
	
	; get just the upper bits
	SRL A
	SRL A
	SRL A
	SRL A 
	add 48 ;convert number to ASCII
	LD b,a
	
	;now the lower bits
	LD a,(HL)
	and 0x0f ;just the lower bits
	add 48 ;convert number to ASCII
	LD c,a
	
	ret
	
LOAD_PREPARE_AND_MULT:
	ld a,(HL)
;	and 0x7F ; clear the bit 7 
PREPARE_AND_MULT:
	SRL a
	SRL a
	SRL a
	SRL a
	CALL X10
	ld b,a
	ld a,(HL)
	and 0x0F
	add a,b
	
	ret
	
SEND_DATA:
	; 8 bits
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
	;free the line to wait for the ACK
	CALL SDA1
	call PULSE_CLOCK

	ret

READ:
	;free the data line
	CALL SDA1
	
	; lets read 8 bits
	ld D,8	
READ_LOOP:

	;next bit
	rlc (hl)
	
	;clock is high
	CALL SCL1	
	
	;read the bit
	ld b,PORT_DATA
	in a,(c)
	
	;is it 1?
	and 1
	
	jr nz, set_bit
	res 0,(hl)
	jr end_set
	
set_bit:
	set 0,(hl)
	
end_set:

	
	;clock is low
	CALL SCL0
	
	
	dec d
	
	;go to next bit
	jr nz, READ_LOOP
	
	;finish the byte read
	ret
	
SEND_NACK:
	ld a,1
	jr SEND_ACK_NACK
	
SEND_ACK:	
	xor a  ;a=0	
	
SEND_ACK_NACK:
	
	CALL SDA
	
	call PULSE_CLOCK
	
;	free the data line
	CALL SDA1

	ret


START_SEQUENCE:	

	;high in both i2c pins, before begin
	ld a,1
	ld c, PORT
	CALL SCL 
	CALL SDA
	
	;high to low when clock is high
	CALL SDA0
	
	;low the clock to start sending data
	CALL SCL
	
	ret
	
SDA0:
	xor a 
	jr SDA
	
SDA1: 
	ld a,1

SDA: 
	ld b,PORT_DATA
	 OUT (c), a
	 ret

SCL0:
	xor a 
	jr SCL
SCL1:
	ld a,1
SCL:
	ld b,PORT_CLOCK
	OUT (c), a
	ret
	
PULSE_CLOCK:
	CALL SCL1
	CALL SCL0
	ret
	
; input A, output A = A * 10
X10:
	ld b,a
	add a,a
	add a,a
	add a,a
	add a,b
	add a,b
	ret

PrintMsg:         
	ld a,(hl)
	or a
	ret z
	rst 10h
	inc hl
	jr PrintMsg


			
;---------------------------------------------		

openscreen:		ld a,2
			jp $1601
			
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
str_REG:	DEFM "RTC : "
		DEFB 0

newline:	DEFB 13,0	
	



NoRTCmessage:	DEFM "No valid RTC signature found.",13
		DEFM "Try setting date first",13,13,0

MsgDate: 	DEFB "The date is "
day_txt:	DEFB "  "
slash1:		DEFB "/"
mon_txt:	DEFB "  "
slash2:		DEFB "/20"
yea_txt:	DEFB "  "
endmsg:		DEFB 13,13,0

MsgUsage:	DEFB "DATE V2.0 usage: ",13
	 	DEFB "date <ENTER>",13
	 	DEFB "show current date",13,13
	 	DEFB "date \"DD/MM/YYYY\" <ENTER>",13
	 	DEFB "set the date",13,13
	 	DEFB "Date must be greater than",13
	 	DEFB "or equal to 01/01/2000",13
	 	DEFB "and less than 01/01/2100",13,13,0

SIG:		DEFW 0					
SEC:		DEFB 0		
MIN:		DEFB 0	
HOU:		DEFB 0	
DAY:		DEFB 0		 
DATE:		DEFB 0	
MON:		DEFB 0	
YEA:		DEFB 0	
	
