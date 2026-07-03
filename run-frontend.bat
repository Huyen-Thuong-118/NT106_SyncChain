@echo off
REM Restore + build + run only the SyncChain MAUI Desktop app.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run-frontend.ps1" %*
