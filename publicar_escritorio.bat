@echo off
rem =====================================================================
rem  ARMAR EL PROGRAMA DE ESCRITORIO (BlazorKioscoDesktop) PARA INSTALAR EN LOS KIOSCOS
rem  1) Compila en modo Release, "autocontenido" (no hace falta instalar .NET en la PC del kiosco)
rem  2) Deja todo en _publicar\escritorio\MRVSoft  +  el instalador  instalar_en_esta_pc.bat
rem  3) Arma _publicar\escritorio\MRVSoft.zip  -> eso se lleva al kiosco (pendrive, Drive, WhatsApp...)
rem
rem  OJO: el programa sale apuntando a la base que este activa en BlazorKioscoDesktop\wwwroot\appsettings.json
rem =====================================================================
setlocal
cd /d "%~dp0"

echo.
echo === Base a la que va a apuntar el programa (appsettings.json del escritorio) ===
findstr /i "Url //" "BlazorKioscoDesktop\wwwroot\appsettings.json"
echo.
echo Si la linea SIN // no es la de PRODUCCION, cerra esta ventana y corregi el appsettings.json.
pause

if exist "_publicar\escritorio" rmdir /s /q "_publicar\escritorio"

echo.
echo === 1) Compilando (Release, autocontenido)... tarda unos minutos ===
dotnet publish "BlazorKioscoDesktop\BlazorKioscoDesktop.csproj" -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o "_publicar\escritorio\MRVSoft"
if errorlevel 1 (
    echo.
    echo *** NO SE PUDO COMPILAR. Mira los errores de arriba.
    pause
    exit /b 1
)

echo.
echo === 2) Agregando el instalador... ===
copy /y "instalar_en_esta_pc.bat" "_publicar\escritorio\MRVSoft\instalar_en_esta_pc.bat" >nul

echo.
echo === 3) Armando el ZIP... ===
powershell -NoProfile -Command "Compress-Archive -Path '_publicar\escritorio\MRVSoft' -DestinationPath '_publicar\escritorio\MRVSoft.zip' -Force"
if errorlevel 1 (
    echo *** No se pudo armar el ZIP. La carpeta _publicar\escritorio\MRVSoft igual sirve: copiala entera.
    pause
    exit /b 1
)

echo.
echo Listo: _publicar\escritorio\MRVSoft.zip
echo En la PC del kiosco: descomprimir el ZIP y hacer doble clic en  instalar_en_esta_pc.bat
explorer "_publicar\escritorio"
pause
