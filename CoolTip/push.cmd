echo off
choice /C GN /n /m "[G]itHub or [N]uGet?"
if %ERRORLEVEL% EQU 1 set source=github
if %ERRORLEVEL% EQU 2 set source=nuget
echo Selected source is "%source%"
nuget push *.nupkg -Source %source%
