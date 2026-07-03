@echo off
REM Bring up the whole SyncChain system (database check -> backend -> desktop).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run-all.ps1" %*
