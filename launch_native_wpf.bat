@echo off
setlocal
cd /d "%~dp0"

if exist "publish\O2JamMusicManager.exe" (
    start "" "publish\O2JamMusicManager.exe" "Sample_Music"
) else if exist "publish_standalone\O2JamMusicManager.exe" (
    start "" "publish_standalone\O2JamMusicManager.exe" "Sample_Music"
) else (
    echo Compilation requise. Lancement du build...
    call build.bat
)
