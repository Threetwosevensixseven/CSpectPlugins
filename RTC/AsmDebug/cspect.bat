:: Set current directory
::@echo off
C:
CD %~dp0


pskill.exe -t cspect.exe
hdfmonkey.exe put C:\spec\cspect-next-2gb.img date dot
cd C:\spec\CSpect2_12_1
CSpect.exe -w2 -zxnext -nextrom -basickeys -exit -brk -tv -com="COM5:115200" -mmc=..\cspect-next-2gb.img


::pause