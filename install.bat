@echo off
setlocal

REM ---------------------------------------------------------------------
REM  Sit/Stand Timer - Installer (exe version)
REM  Copies SitStandTimer.exe to %LocalAppData%\SitStandTimer and creates
REM  a Startup-folder shortcut so it launches automatically at login.
REM  Run build.bat first to produce SitStandTimer.exe, then run this.
REM ---------------------------------------------------------------------

set "SRC=%~dp0"
set "DEST=%LOCALAPPDATA%\SitStandTimer"
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"

if not exist "%SRC%SitStandTimer.exe" (
    echo ERROR: SitStandTimer.exe not found next to this installer.
    echo Run build.bat first to compile it.
    pause
    exit /b 1
)

echo Installing Sit/Stand Timer to %DEST% ...

if not exist "%DEST%" mkdir "%DEST%"

copy /Y "%SRC%SitStandTimer.exe" "%DEST%\SitStandTimer.exe" >nul

echo Creating Startup shortcut ...

powershell -NoProfile -Command ^
    "$ws = New-Object -ComObject WScript.Shell;" ^
    "$s = $ws.CreateShortcut('%STARTUP%\SitStandTimer.lnk');" ^
    "$s.TargetPath = '%DEST%\SitStandTimer.exe';" ^
    "$s.WorkingDirectory = '%DEST%';" ^
    "$s.Description = 'Sit/Stand Desk Timer';" ^
    "$s.Save()"

if exist "%STARTUP%\SitStandTimer.lnk" (
    echo Startup shortcut created.
) else (
    echo WARNING: Could not create the Startup shortcut. You can create it manually:
    echo   1. Press Win+R, type shell:startup, hit Enter
    echo   2. Right-click %DEST%\SitStandTimer.exe and choose "Create shortcut"
    echo   3. Move that shortcut into the Startup folder
)

echo.
echo Install complete. Launching now ...
start "" "%DEST%\SitStandTimer.exe"

echo.
echo Done. The timer is running in your system tray and will start
echo automatically each time you log in.
pause
