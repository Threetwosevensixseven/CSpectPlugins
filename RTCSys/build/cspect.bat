:: Set current directory
::@echo off
C:
CD %~dp0


::ZXVersion.exe
pskill.exe -t cspect.exe
hdfmonkey.exe put C:\spec\cspect-next-2gb.img ..\bin\RTC.SYS NextZXOS
cd C:\spec\CSpect2_12_5
CSpect.exe -w2 -zxnext -nextrom -basickeys -exit -brk -tv -mmc=..\cspect-next-2gb.img


::pause