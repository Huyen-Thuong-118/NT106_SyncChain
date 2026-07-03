@echo off
REM Restore + build + run only the SyncChain API.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run-backend.ps1" %*
