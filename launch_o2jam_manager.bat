@echo off
setlocal
cd /d "%~dp0"
set PYTHONPATH=%~dp0;%PYTHONPATH%

title O2Jam Music Manager
echo Lancement de O2Jam Music Manager...
python "%~dp0main.py" %*
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Une erreur est survenue lors de l'execution.
    pause
)
