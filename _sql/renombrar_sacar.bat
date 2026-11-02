@echo off
chcp 65001 >nul
rem Renombra con _sacar los archivos que ya no se usan.
rem Cerra Visual Studio antes de correrlo. Se puede correr mas de una vez.
cd /d "%~dp0.."

echo === BlazorKiosco_v1
if exist "BlazorKiosco_v1\Pages\Counter.razor" ren "BlazorKiosco_v1\Pages\Counter.razor" "Counter_sacar.razor" && echo   Counter.razor  -^>  Counter_sacar.razor
if exist "BlazorKiosco_v1\Pages\Weather.razor" ren "BlazorKiosco_v1\Pages\Weather.razor" "Weather_sacar.razor" && echo   Weather.razor  -^>  Weather_sacar.razor
if exist "BlazorKiosco_v1\Pages\MenuAdicional.razor" ren "BlazorKiosco_v1\Pages\MenuAdicional.razor" "MenuAdicional_sacar.razor" && echo   MenuAdicional.razor  -^>  MenuAdicional_sacar.razor
if exist "BlazorKiosco_v1\Pages\Configuracion_viejo.razor" ren "BlazorKiosco_v1\Pages\Configuracion_viejo.razor" "Configuracion_viejo_sacar.razor" && echo   Configuracion_viejo.razor  -^>  Configuracion_viejo_sacar.razor
if exist "BlazorKiosco_v1\Pages\IngresoMercaderia.razor" ren "BlazorKiosco_v1\Pages\IngresoMercaderia.razor" "IngresoMercaderia_sacar.razor" && echo   IngresoMercaderia.razor  -^>  IngresoMercaderia_sacar.razor
if exist "BlazorKiosco_v1\Layout\NavMenu.razor" ren "BlazorKiosco_v1\Layout\NavMenu.razor" "NavMenu_sacar.razor" && echo   NavMenu.razor  -^>  NavMenu_sacar.razor
if exist "BlazorKiosco_v1\Layout\NavMenu.razor.css" ren "BlazorKiosco_v1\Layout\NavMenu.razor.css" "NavMenu_sacar.razor.css" && echo   NavMenu.razor.css  -^>  NavMenu_sacar.razor.css

echo === BlazorKioscoDesktop
if exist "BlazorKioscoDesktop\Components\Pages\Counter.razor" ren "BlazorKioscoDesktop\Components\Pages\Counter.razor" "Counter_sacar.razor" && echo   Counter.razor  -^>  Counter_sacar.razor
if exist "BlazorKioscoDesktop\Components\Pages\Weather.razor" ren "BlazorKioscoDesktop\Components\Pages\Weather.razor" "Weather_sacar.razor" && echo   Weather.razor  -^>  Weather_sacar.razor
if exist "BlazorKioscoDesktop\Components\Layout\NavMenu.razor" ren "BlazorKioscoDesktop\Components\Layout\NavMenu.razor" "NavMenu_sacar.razor" && echo   NavMenu.razor  -^>  NavMenu_sacar.razor
if exist "BlazorKioscoDesktop\Components\Layout\NavMenu.razor.css" ren "BlazorKioscoDesktop\Components\Layout\NavMenu.razor.css" "NavMenu_sacar.razor.css" && echo   NavMenu.razor.css  -^>  NavMenu_sacar.razor.css

echo === BlazorKioscoCompartido
if exist "BlazorKioscoCompartido\Component1.razor" ren "BlazorKioscoCompartido\Component1.razor" "Component1_sacar.razor" && echo   Component1.razor  -^>  Component1_sacar.razor
if exist "BlazorKioscoCompartido\Component1.razor.css" ren "BlazorKioscoCompartido\Component1.razor.css" "Component1_sacar.razor.css" && echo   Component1.razor.css  -^>  Component1_sacar.razor.css
if exist "BlazorKioscoCompartido\ExampleJsInterop.cs" ren "BlazorKioscoCompartido\ExampleJsInterop.cs" "ExampleJsInterop_sacar.cs" && echo   ExampleJsInterop.cs  -^>  ExampleJsInterop_sacar.cs
if exist "BlazorKioscoCompartido\wwwroot\exampleJsInterop.js" ren "BlazorKioscoCompartido\wwwroot\exampleJsInterop.js" "exampleJsInterop_sacar.js" && echo   exampleJsInterop.js  -^>  exampleJsInterop_sacar.js
if exist "BlazorKioscoCompartido\Pages\ReporteActualizacionProductos.razor" ren "BlazorKioscoCompartido\Pages\ReporteActualizacionProductos.razor" "ReporteActualizacionProductos_sacar.razor" && echo   ReporteActualizacionProductos.razor  -^>  ReporteActualizacionProductos_sacar.razor
if exist "BlazorKioscoCompartido\Components\Scanner.razor" ren "BlazorKioscoCompartido\Components\Scanner.razor" "Scanner_sacar.razor" && echo   Scanner.razor  -^>  Scanner_sacar.razor
if exist "BlazorKioscoCompartido\Services\FiadoService.cs" ren "BlazorKioscoCompartido\Services\FiadoService.cs" "FiadoService_sacar.cs" && echo   FiadoService.cs  -^>  FiadoService_sacar.cs
if exist "BlazorKioscoCompartido\Services\FunctionAux.cs" ren "BlazorKioscoCompartido\Services\FunctionAux.cs" "FunctionAux_sacar.cs" && echo   FunctionAux.cs  -^>  FunctionAux_sacar.cs
if exist "BlazorKioscoCompartido\Models\Vistas\ResumenDeudores.cs" ren "BlazorKioscoCompartido\Models\Vistas\ResumenDeudores.cs" "ResumenDeudores_sacar.cs" && echo   ResumenDeudores.cs  -^>  ResumenDeudores_sacar.cs
if exist "BlazorKioscoCompartido\Models\DTO\GestionVentaRapidaDTO.cs" ren "BlazorKioscoCompartido\Models\DTO\GestionVentaRapidaDTO.cs" "GestionVentaRapidaDTO_sacar.cs" && echo   GestionVentaRapidaDTO.cs  -^>  GestionVentaRapidaDTO_sacar.cs
if exist "BlazorKioscoCompartido\Models\DTO\ItemVentaDto.cs" ren "BlazorKioscoCompartido\Models\DTO\ItemVentaDto.cs" "ItemVentaDto_sacar.cs" && echo   ItemVentaDto.cs  -^>  ItemVentaDto_sacar.cs
if exist "BlazorKioscoCompartido\Models\VentaPago.cs" ren "BlazorKioscoCompartido\Models\VentaPago.cs" "VentaPago_sacar.cs" && echo   VentaPago.cs  -^>  VentaPago_sacar.cs
if exist "BlazorKioscoCompartido\wwwroot\css\EstilosNegocios.css" ren "BlazorKioscoCompartido\wwwroot\css\EstilosNegocios.css" "EstilosNegocios_sacar.css" && echo   EstilosNegocios.css  -^>  EstilosNegocios_sacar.css

echo.
echo Listo. Abri Visual Studio y compila (Rebuild).
pause
