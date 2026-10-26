@echo off
setlocal
cd /d "%~dp0"
set PYTHONPATH=%~dp0;%PYTHONPATH%

echo =======================================================
echo   DEMARRAGE EN MODE DEBUG (TRACEBACK DIRECT)
echo =======================================================
echo.
python main.py
echo.
echo =======================================================
echo   Fin du processus (Code retour : %ERRORLEVEL%)
echo =======================================================
pause
