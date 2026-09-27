@echo off
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0run_multivault_tests.ps1"
exit /b %ERRORLEVEL%
