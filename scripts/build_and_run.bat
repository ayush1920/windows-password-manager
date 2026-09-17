@echo off
setlocal
cd /d "%~dp0.."
set "PROJECT_ROOT=%CD%"

echo ==========================================================
echo   Building KeyCraft Credential Manager (Clean Project)
echo ==========================================================
echo Project Root: %PROJECT_ROOT%

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist %CSC% (
    echo [ERROR] C# compiler not found at %CSC%
    pause
    exit /b 1
)

if not exist "%PROJECT_ROOT%\bin" mkdir "%PROJECT_ROOT%\bin"
if not exist "%PROJECT_ROOT%\data" mkdir "%PROJECT_ROOT%\data"

taskkill /f /im PasswordManager.exe 2>nul

echo Compiling C# source files into bin\PasswordManager.exe ...
%CSC% /nologo /target:winexe /out:"%PROJECT_ROOT%\bin\PasswordManager.exe" /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll "%PROJECT_ROOT%\*.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] Build completed successfully: bin\PasswordManager.exe
    echo Launching KeyCraft Credential Manager...
    start "" "%PROJECT_ROOT%\bin\PasswordManager.exe"
) else (
    echo.
    echo [ERROR] Build failed. See compiler output above.
    pause
)
