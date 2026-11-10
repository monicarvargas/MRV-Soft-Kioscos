@echo off
rem =====================================================================
rem  ARMAR EL PROGRAMA DE ESCRITORIO (BlazorKioscoDesktop) PARA INSTALAR EN LOS KIOSCOS
rem  1) Pone los datos de PRODUCCION (appsettings.produccion.json) solo para compilar
rem  2) Compila en modo Release, "autocontenido" (no hace falta instalar .NET en la PC del kiosco)
rem  3) Vuelve a dejar tu appsettings.json como estaba (TEST), aunque falle la compilacion
rem  4) Deja todo en _publicar\escritorio\MRVSoft  +  el instalador  instalar_en_esta_pc.bat
rem  5) Arma _publicar\escritorio\MRVSoft.zip  -> eso se lleva al kiosco (pendrive, Drive, WhatsApp...)
rem
rem  Necesita (una sola vez): BlazorKioscoDesktop\wwwroot\appsettings.produccion.json con los datos de PRODUCCION
rem  Tu appsettings.json queda SIEMPRE en TEST para programar.
rem =====================================================================
setlocal
cd /d "%~dp0"

set "CFG=BlazorKioscoDesktop\wwwroot\appsettings.json"
set "CFG_PROD=BlazorKioscoDesktop\wwwroot\appsettings.produccion.json"
set "CFG_GUARDADO=_publicar\appsettings_test_guardado_escritorio.json"
set "SALIDA=_publicar\escritorio\MRVSoft"
rem codigo del proyecto de PRODUCCION en Supabase (esta en la direccion: https://CODIGO.supabase.co)
set "REF_PROD=snhfotjgraipantkypda"

if not exist "_publicar" mkdir "_publicar"

rem si una publicacion anterior se corto a la mitad, primero se vuelve a poner tu appsettings.json de TEST
if exist "%CFG_GUARDADO%" (
    copy /y "%CFG_GUARDADO%" "%CFG%" >nul
    del "%CFG_GUARDADO%"
    echo Se recupero tu appsettings.json de TEST de una publicacion que se habia cortado.
)

if not exist "%CFG_PROD%" (
    echo.
    echo *** Falta el archivo %CFG_PROD%
    echo *** Copia tu appsettings.json con ese nombre y dejale activos los datos de PRODUCCION.
    echo *** No se armo nada.
    pause
    exit /b 1
)
findstr /c:"%REF_PROD%" "%CFG_PROD%" >nul
if errorlevel 1 (
    echo.
    echo *** appsettings.produccion.json NO tiene la direccion de PRODUCCION ^(%REF_PROD%^).
    echo *** Revisalo. No se armo nada.
    pause
    exit /b 1
)

echo.
echo === El programa se arma apuntando a PRODUCCION (appsettings.produccion.json) ===
echo === Tu appsettings.json de TEST se guarda y se vuelve a poner al terminar ===
echo.

if exist "_publicar\escritorio" rmdir /s /q "_publicar\escritorio"

copy /y "%CFG%" "%CFG_GUARDADO%" >nul
copy /y "%CFG_PROD%" "%CFG%" >nul
powershell -NoProfile -Command "(Get-Item '%CFG%').LastWriteTime = Get-Date"

echo === 1) Compilando (Release, autocontenido)... tarda unos minutos ===
dotnet publish "BlazorKioscoDesktop\BlazorKioscoDesktop.csproj" -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o "%SALIDA%"
set "ERR=%errorlevel%"

rem ---- siempre: vuelve tu appsettings.json de TEST ----
copy /y "%CFG_GUARDADO%" "%CFG%" >nul
del "%CFG_GUARDADO%"
powershell -NoProfile -Command "(Get-Item '%CFG%').LastWriteTime = Get-Date"
echo.
echo (tu appsettings.json volvio a quedar como estaba, en TEST)

if not "%ERR%"=="0" (
    echo.
    echo *** NO SE PUDO COMPILAR. Mira los errores de arriba.
    pause
    exit /b 1
)

rem el archivo con los datos de produccion no tiene que viajar aparte
del /q "%SALIDA%\wwwroot\appsettings.produccion.json*" >nul 2>&1

echo.
echo === 2) Agregando el instalador... ===
copy /y "instalar_en_esta_pc.bat" "%SALIDA%\instalar_en_esta_pc.bat" >nul

echo.
echo === 3) Armando el ZIP... ===
powershell -NoProfile -Command "Compress-Archive -Path '_publicar\escritorio\MRVSoft' -DestinationPath '_publicar\escritorio\MRVSoft.zip' -Force"
if errorlevel 1 (
    echo *** No se pudo armar el ZIP. La carpeta _publicar\escritorio\MRVSoft igual sirve: copiala entera.
    pause
    exit /b 1
)

echo.
echo Listo: _publicar\escritorio\MRVSoft.zip  (apunta a PRODUCCION)
echo En la PC del kiosco: descomprimir el ZIP y hacer doble clic en  instalar_en_esta_pc.bat
explorer "_publicar\escritorio"
pause
