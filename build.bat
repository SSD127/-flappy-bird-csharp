@echo off
echo Flappy Bird oyunu derleniyor...
csc /target:winexe /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll FlappyBird.cs
if %errorlevel% equ 0 (
    echo Derleme basarili! FlappyBird.exe olusturuldu.
    echo Oyunu baslatmak icin FlappyBird.exe dosyasina cift tiklayin.
    pause
) else (
    echo Derleme basarisiz! Visual Studio Community yuklu oldugundan emin olun.
    echo https://visualstudio.microsoft.com/vs/community/ adresinden indirebilirsiniz.
    pause
)