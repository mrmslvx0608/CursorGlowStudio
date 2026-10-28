@echo off
setlocal
chcp 65001 >nul
set "TARGET=%LOCALAPPDATA%\CursorGlowStudio"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
title CursorGlow Studio 3 - Nadogradnja

echo.
echo ==============================================================
echo     CURSORGLOW STUDIO 3 - KURSOR + GLOW + TRAGOVI MISA
echo ==============================================================
echo.
if not exist "%CSC%" (
    echo GRESKA: Microsoft .NET Framework 4.x C# compiler nije pronaden.
    echo Provjeri je li ukljucen .NET Framework na Windowsu.
    pause
    exit /b 1
)
if not exist "%~dp0TrailEngine.cs" goto :missing
if not exist "%~dp0CursorsV2\09-KaoNaSlici\01-Crna\02-Normalna\Arrow.cur" goto :missing

set "PREVIOUS=0"
if exist "%TARGET%\CursorGlowStudio.exe" set "PREVIOUS=1"
echo [1/6] Zatvaram postojeci CursorGlow Studio...
taskkill /F /IM CursorGlowStudio.exe >nul 2>nul
if not exist "%TARGET%" mkdir "%TARGET%"

echo [2/6] Kopiram izvorni kod i kursore (cuvam postavke i backup)...
copy /Y "%~dp0CursorGlowStudio.cs" "%TARGET%\CursorGlowStudio.cs" >nul
if errorlevel 1 goto :error
copy /Y "%~dp0TrailEngine.cs" "%TARGET%\TrailEngine.cs" >nul
if errorlevel 1 goto :error
copy /Y "%~dp0CursorGlowStudio.ico" "%TARGET%\CursorGlowStudio.ico" >nul
if errorlevel 1 goto :error
xcopy "%~dp0CursorsV2\*" "%TARGET%\CursorsV2\" /E /I /Y /Q >nul
if errorlevel 1 goto :error
copy /Y "%~dp0OBLIK_KAO_NA_SLICI.png" "%TARGET%\OBLIK_KAO_NA_SLICI.png" >nul

echo [3/6] Sastavljam program lokalno (bez preuzimanja s Interneta)...
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:"%TARGET%\CursorGlowStudio.ico" /out:"%TARGET%\CursorGlowStudio.next.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "%TARGET%\CursorGlowStudio.cs" "%TARGET%\TrailEngine.cs"
if errorlevel 1 goto :compile_error
if not exist "%TARGET%\CursorGlowStudio.next.exe" goto :compile_error
move /Y "%TARGET%\CursorGlowStudio.next.exe" "%TARGET%\CursorGlowStudio.exe" >nul
if errorlevel 1 goto :error

echo [4/6] Cuvam settings.ini i original-cursors.ini...
if "%PREVIOUS%"=="0" (
    "%TARGET%\CursorGlowStudio.exe" --apply-default-cursor
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v CursorGlowStudio /t REG_SZ /d "\"%TARGET%\CursorGlowStudio.exe\" --background" /f >nul
)

echo [5/6] Postavljam precac na radnu povrsinu...
powershell.exe -NoProfile -Command "$w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut([Environment]::GetFolderPath('Desktop')+'\CursorGlow Studio.lnk'); $s.TargetPath=$env:LOCALAPPDATA+'\CursorGlowStudio\CursorGlowStudio.exe'; $s.WorkingDirectory=$env:LOCALAPPDATA+'\CursorGlowStudio'; $s.Save()" >nul 2>nul

echo [6/6] Pokrecem CursorGlow Studio 3...
start "" "%TARGET%\CursorGlowStudio.exe"
echo.
echo NADOGRADNJA GOTOVA. U kartici IZGLED KURSORA odaberi KAO NA SLICI.
echo U kartici TRAGOVI I EFEKTI podesi tip, duljinu i jacinu traga.
echo Sacuvane su tvoje dosadasnje postavke i backup prethodnih kursora.
echo.
pause
exit /b 0

:missing
echo GRESKA: Nisu pronadene sve datoteke. Raspakiraj cijeli ZIP!
pause
exit /b 1

:compile_error
echo.
echo GRESKA PRI PREVODENJU C# KODA. Postojeci .exe nije zamijenjen.
if exist "%TARGET%\CursorGlowStudio.next.exe" del "%TARGET%\CursorGlowStudio.next.exe"
pause
exit /b 1

:error
echo.
echo GRESKA: Pogledaj poruku iznad. Settings.ini i backup nisu obrisani.
pause
exit /b 1
