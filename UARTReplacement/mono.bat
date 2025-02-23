:: Run CSpect with 64-bit Mono for Windows
:: Install Mono from: https://www.mono-project.com/download/stable/#download-win
:: Install CSpect from: https://mdf200.itch.io/cspect
C:
cd C:\spec\CSpect2_19_4_4
::cd C:\spec\CSpect3_0_1_0
call "C:\Program Files\Mono\bin\setmonopath.bat"
mono cspect.exe -w2 -zxnext -nextrom -basickeys -exit -brk -tv -emu -mmc=..\sd209\cspect-next-2gb.img > log.txt 2>&1