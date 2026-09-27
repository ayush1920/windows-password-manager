@echo off
setlocal
cd /d "%~dp0.."
set "PROJECT_ROOT=%CD%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%PROJECT_ROOT%\scripts\run_tests.ps1"
exit /b %ERRORLEVEL%
