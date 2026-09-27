@echo off
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0run_single_instance_tests.ps1"
exit /b %ERRORLEVEL%
