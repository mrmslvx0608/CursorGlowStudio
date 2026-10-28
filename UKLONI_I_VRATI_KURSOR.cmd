@echo off
setlocal
chcp 65001 >nul
set "TARGET=%LOCALAPPDATA%\CursorGlowStudio"
title CursorGlow Studio - uklanjanje

echo.
echo CursorGlow Studio - uklanjanje i vracanje Windows kursora
if not exist "%TARGET%\CursorGlowStudio.exe" (
    echo Program nije instaliran u standardnoj mapi.
    pause
    exit /b 1
)

echo Zatvaram program...
taskkill /F /IM CursorGlowStudio.exe >nul 2>nul

echo Vracam prethodne kursore...
"%TARGET%\CursorGlowStudio.exe" --restore-cursors

echo Uklanjam automatsko pokretanje...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v CursorGlowStudio /f >nul 2>nul

echo Uklanjam desktop precac...
powershell.exe -NoProfile -Command "$p=[Environment]::GetFolderPath('Desktop')+'\CursorGlow Studio.lnk'; if(Test-Path $p){Remove-Item $p -Force}" >nul 2>nul

echo Uklanjam programske datoteke...
rmdir /S /Q "%TARGET%"
echo.
echo Gotovo - program uklonjen i kursori vraceni ako je originalna shema bila spremljena.
echo.
pause
