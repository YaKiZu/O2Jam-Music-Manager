@echo off
setlocal
cd /d "%~dp0"

echo =========================================================================
echo   O2JAM MUSIC MANAGER - COMPILATEUR D'EXECUTABLE (.EXE)
echo =========================================================================
echo.
echo   [1] Compiler l'application native C# WPF Ultra-Legere (~890 Ko) [RECOMMANDE]
echo   [2] Compiler l'application native C# WPF Standalone (~72 Mo, 100%% Autonome)
echo   [3] Executer la suite de tests unitaires (9 tests)
echo   [4] Compiler la version Python (via PyInstaller + soundfile/numpy/pygame)
echo.
set /p CHOICE="Votre choix [1-4, defaut 1] : "
if "%CHOICE%"=="" set CHOICE=1

if "%CHOICE%"=="1" (
    echo.
    echo [*] Compilation C# Release Ultra-Legere...
    dotnet publish O2JamManager\O2JamManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Fichier genere avec succes dans : publish\O2JamMusicManager.exe
    ) else (
        echo [ERREUR] Echec de la compilation.
    )
) else if "%CHOICE%"=="2" (
    echo.
    echo [*] Compilation C# Release Standalone (Autonome)...
    dotnet publish O2JamManager\O2JamManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish_standalone
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Fichier autonome genere dans : publish_standalone\O2JamMusicManager.exe
    ) else (
        echo [ERREUR] Echec de la compilation.
    )
) else if "%CHOICE%"=="3" (
    echo.
    echo [*] Execution des tests unitaires MSTest...
    dotnet test O2JamManager.Tests\O2JamManager.Tests.csproj
) else if "%CHOICE%"=="4" (
    echo.
    echo [*] Verification de pyinstaller et PySide6...
    pip install --upgrade PySide6 pyinstaller
    echo [*] Compilation Python PyInstaller (Standalone dans dist)...
    python -m PyInstaller --noconfirm --onefile --windowed --name "O2JamMusicManager" main.py
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Executable Python autonome genere dans : dist\O2JamMusicManager.exe
    ) else (
        echo [ERREUR] Echec du build Python.
    )
) else (
    echo Choix invalide.
)

echo.
pause
