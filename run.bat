@echo off
echo Flappy Bird oyunu baslatiliyor...
if exist FlappyBird.exe (
    FlappyBird.exe
) else (
    echo FlappyBird.exe bulunamadi!
    echo Once build.bat dosyasini calistirin.
    pause
)

