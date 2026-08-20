@echo off
setlocal

REM ---------------------------------------------------------------------
REM  Builds SitStandTimer.exe from Program.cs using the C# compiler that
REM  ships with every Windows install (part of .NET Framework) - no
REM  downloads, no PowerShell, no admin rights required.
REM ---------------------------------------------------------------------

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist "%CSC%" (
    echo ERROR: Could not find csc.exe. Your machine may not have .NET
    echo Framework 4.x installed. Try running "dir %WINDIR%\Microsoft.NET"
    echo to see what versions are available and adjust this script.
    pause
    exit /b 1
)

echo Using compiler: %CSC%
echo Building SitStandTimer.exe ...

"%CSC%" /nologo /target:winexe /out:"%~dp0SitStandTimer.exe" ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:Microsoft.VisualBasic.dll ^
    "%~dp0Program.cs"

if exist "%~dp0SitStandTimer.exe" (
    echo.
    echo Build succeeded: SitStandTimer.exe
) else (
    echo.
    echo Build failed. See errors above.
)

pause
