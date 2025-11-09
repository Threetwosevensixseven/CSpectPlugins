:: Run CSpect with 64-bit Windows
:: Install CSpect from: https://mdf200.itch.io/cspect
C:
::C:\spec\CSpect2_19_4_4
::cd C:\spec\CSpect3_0_1_0
cd C:\spec\CSpect3_0_15_2
cspect.exe -w3 -zxnext -nextrom -basickeys -exit -brk -tv -emu -mmc=..\sd209\cspect-next-2gb.img
:: > log.txt 2>&1