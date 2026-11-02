using BlazorKioscoCompartido.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlazorKioscoCompartido.Pages.ImportarProductosCompartido;
using static Supabase.Postgrest.Constants;


namespace BlazorKioscoCompartido.Services
{
    public class ConsultasDB
    {
        private readonly Supabase.Client _supaClient;

        // Inyectamos el cliente de Supabase por constructor
        public ConsultasDB(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        /// <summary>
        /// Calcula el precio de venta basado en el precio de costo, el precio de venta y la utilidad deseada.
        /// </summary>
        public async Task<decimal> CalcularPrecioVentaAsync(decimal precio_costo, decimal  precio_venta, decimal utilidadPV)
        {
            if (precio_venta <= precio_costo)
            {
                if (utilidadPV > 0)
                { 
                    return precio_costo + (precio_costo * utilidadPV / 100);
                }
            } 
            return precio_venta;
        }
        
        /// <summary>
        /// Busca una familia por su nombre y kiosco en Supabase. Si no existe, la crea y devuelve su ID (long).
        /// </summary>
        public async Task<long> ObtenerOInsertarFamiliaIdAsync(string nombreFamilia, long kioscoId)
        {
            if (string.IsNullOrWhiteSpace(nombreFamilia))
            {
                nombreFamilia = "ARTICULOS";
            }

            string nombreLimpio = nombreFamilia.Trim();

            try
            {
                // 1. BUSCAR SI YA EXISTE
                // Usamos strings planos con el nombre de la columna para evitar errores de tipo de criterio


                Familia? familiaExistente = null;

                if (!string.IsNullOrWhiteSpace(nombreFamilia))
                {
                    familiaExistente = await _supaClient
                        .From<Familia>()
                        .Where(p => p.KioscoId == kioscoId)
                        .Filter("nombre", Supabase.Postgrest.Constants.Operator.ILike, $"%{nombreFamilia}%")
                        .Single();
                }


                if (familiaExistente != null)
                {
                    return familiaExistente.Id; // Devolvemos el id (long) existente
                }

                // 2. SI NO EXISTE, LA INSERTAMOS
                var nuevaFamilia = new Familia
                {
                    Nombre = nombreLimpio.ToUpper(),         // Asignamos la propiedad obligatoria 'Nombre'
                    Descripcion = nombreLimpio.ToUpper(),    // Opcional: seteamos la descripción igual o dejala en null
                    KioscoId = kioscoId,           // Pasamos el long limpio
                    FechaCreacion = DateTime.Now   // Inicializa la fecha localmente de forma explícita si hace falta
                };

                var respuestaInsert = await _supaClient.From<Familia>().Insert(nuevaFamilia);
                var familiaCreada = respuestaInsert.Models.FirstOrDefault();

                if (familiaCreada != null)
                {
                    return familiaCreada.Id;
                }

                throw new Exception("No se pudo recuperar el ID de la familia recién creada.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en ConsultasDB al procesar la familia '{nombreFamilia}': {ex.Message}");
            }
        }
        public async Task<long> ObtenerOInsertarProveedorIdAsync(string descripcionProveedor, long kioscoId)
        {
            if (string.IsNullOrWhiteSpace(descripcionProveedor))
            {
                descripcionProveedor = "PROVEEDOR1";
            }

            string nombreLimpio = descripcionProveedor.Trim();

            try
            {
                // 1. BUSCAR SI YA EXISTE
                // En Supabase filtramos usando .Where o usando las expresiones Postgrest de la librería
                Proveedor? proveedorExistente = null;

                if (!string.IsNullOrWhiteSpace(descripcionProveedor))
                {
                    proveedorExistente = await _supaClient
                        .From<Proveedor>()
                        .Where(p => p.KioscoId == kioscoId)
                        .Filter("nombre", Supabase.Postgrest.Constants.Operator.ILike, $"%{descripcionProveedor}%")
                        .Single();
                }

                if (proveedorExistente != null)
                {
                    return (long)proveedorExistente.Id; // Suponiendo que tu modelo Familia tiene la propiedad Id
                }

                // 2. SI NO EXISTE, LA INSERTAMOS
                var nuevoProveedor = new Proveedor
                {
                    Nombre = nombreLimpio,
                    KioscoId = kioscoId,
                    // Agrega acá cualquier otro campo obligatorio de tu tabla de Supabase (ej: Estado = true)
                };

                // Insert devuelve el registro recién creado con su ID asignado por la base de datos
                var respuestaInsert = await _supaClient.From<Proveedor>().Insert(nuevoProveedor);
                var proveedorCreado = respuestaInsert.Models.FirstOrDefault();

                if (proveedorCreado != null)
                {
                    return (long)proveedorCreado.Id;
                }

                throw new Exception("No se pudo obtener el ID del proveedor creado.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en ConsultasDB al procesar el proveedor '{descripcionProveedor}': {ex.Message}");
            }
            //return 1;
        }
        /// <summary>
        /// Guarda un producto en Supabase. Si el código ya existe para ese kiosco, lo actualiza. Si no, lo crea.
        /// </summary>
        public async Task GuardarOActualizarProductoAsync(ProductoImportarPreview item, long kioscoId)
        {
            try
            {
                // 1. BUSCAR SI EL PRODUCTO YA EXISTE POR CÓDIGO Y KIOSCO
                var query = _supaClient.From<Producto>();

                var res = await query.And(new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
                 {
                     new Supabase.Postgrest.QueryFilter("kiosco_id", Operator.Equals, kioscoId),
                     new Supabase.Postgrest.QueryFilter("codigo_barras", Operator.Equals,item.Codigo)
                 }).Get();

                // !!! FUNCIONALIDAD CLAVE !!!
                // Si la base de datos no devolvió nada Y el usuario presionó Enter (o vino del escáner)

                var productoExistente = res.Models.FirstOrDefault();

                if (productoExistente == null)
                {
                    var precioVentaFinal = await CalcularPrecioVentaAsync(item.PrecioCosto, item.PrecioVenta, item.UtilidadPV);
                    var precioLista2Final = precioVentaFinal + PrecioService.CalcularPrecioVentaConRecargo(precioVentaFinal, kioscoId);
                    // ESCENARIO A: EL PRODUCTO NO EXISTE -> SE INSERTA
                    var nuevoProd = new Producto
                    {
                        KioscoId = kioscoId,
                        CodigoBarras = item.Codigo,
                        Nombre = item.Descripcion,
                        UtilidadPV=item.UtilidadPV,
                        FamiliaId = await ObtenerOInsertarFamiliaIdAsync(item.Familia, kioscoId),
                        PrecioCosto = item.PrecioCosto,
                        PrecioVenta = precioVentaFinal,
                        UnidadMedida = "UNIDAD",
                        PrecioLista2 = precioLista2Final,
                        UtilidadLista2 = PrecioService.CalcularPrecioCostoDeLista2desdePrecioLista2(precioLista2Final, item.PrecioCosto),
                        //PrecioLista3 = item.PrecioLista3,
                        StockActual = item.Stock,
                        ProveedorId = await ObtenerOInsertarProveedorIdAsync(item.Proveedor, kioscoId),
                        Activo = true,
                    };

                    await _supaClient.From<Producto>().Insert(nuevoProd);
                }
                else
                {
                    var precioVentaActualizado = await CalcularPrecioVentaAsync(item.PrecioCosto, item.PrecioVenta, item.UtilidadPV);
                    var precioLista2Final = precioVentaActualizado + PrecioService.CalcularPrecioVentaConRecargo(precioVentaActualizado, kioscoId);
                    // ESCENARIO B: EL PRODUCTO YA EXISTE -> SE ACTUALIZA
                    productoExistente.Nombre = item.Descripcion;
                    productoExistente.FamiliaId = await ObtenerOInsertarFamiliaIdAsync(item.Familia, kioscoId);
                    productoExistente.PrecioCosto = item.PrecioCosto;
                    productoExistente.PrecioVenta = precioVentaActualizado;
                    productoExistente.UtilidadPV = item.UtilidadPV;
                    productoExistente.PrecioLista2 = precioLista2Final;
                    productoExistente.UtilidadLista2 = PrecioService.CalcularPrecioCostoDeLista2desdePrecioLista2(precioLista2Final, item.PrecioCosto);
                    productoExistente.StockActual += item.Stock; 
                    productoExistente.ProveedorId = await ObtenerOInsertarProveedorIdAsync(item.Proveedor, kioscoId);
                    productoExistente.CodigoBarras = item.Codigo;
                    // Asigna "UNIDAD" si UnidadMedida es null, está vacío o solo tiene espacios
                    if (string.IsNullOrWhiteSpace(productoExistente.UnidadMedida))
                    {
                        productoExistente.UnidadMedida = "UNIDAD";
                    }
                    productoExistente.Activo = true;
                    await _supaClient.From<Producto>().Update(productoExistente);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar el producto '{item.Codigo}': {ex.Message}");
            }
        }
    }
}