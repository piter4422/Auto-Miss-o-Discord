@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERRO] Compilador C# nao encontrado em %CSC%
    pause
    exit /b 1
)

echo Compilando AutoMissaoDiscord.exe...
"%CSC%" /nologo /target:winexe /optimize+ /r:System.Windows.Forms.dll,System.Drawing.dll,System.Web.Extensions.dll /out:AutoMissaoDiscord.exe src\Program.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCESSO] AutoMissaoDiscord.exe compilado com sucesso!
) else (
    echo [ERRO] Falha na compilacao!
)
