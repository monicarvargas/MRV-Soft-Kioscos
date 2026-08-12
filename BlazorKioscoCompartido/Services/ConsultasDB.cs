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
        /// Busca una familia por su descripción y kiosco. Si no existe, la crea y devuelve su ID.
        /// </summary>

        public async Task<long> ObtenerOInsertarFamiliaIdAsync_ori(string descripcionFamilia, long kioscoId)
        {
            if (string.IsNullOrWhiteSpace(descripcionFamilia))
            {
                descripcionFamilia = "MERCADERIA1";
            }

            string nombreLimpio = descripcionFamilia.Trim();

            try
            {
                // 1. BUSCAR SI YA EXISTE
                // En Supabase filtramos usando .Where o usando las expresiones Postgrest de la librería
                //var respuesta = await _supaClient
                //    .From<Familia>()
                //    .Filter(f => f.KioscoId, Supabase.Postgrest.Constants.Operator.Equals, kioscoId)
                //    .Get();

                var respuesta = await _supaClient.From<Familia>().Filter(f => f.KioscoId, Operator.Equals, (long)kioscoId).Get();

                // Buscamos en memoria ignorando mayúsculas/minúsculas para estar seguros
                var familiaExistente = respuesta.Models
                    .FirstOrDefault(f => f.Nombre.Equals(nombreLimpio, StringComparison.OrdinalIgnoreCase));

                if (familiaExistente != null)
                {
                    return (long)familiaExistente.Id; // Suponiendo que tu modelo Familia tiene la propiedad Id
                }

                // 2. SI NO EXISTE, LA INSERTAMOS
                var nuevaFamilia = new Familia
                {
                    Nombre = nombreLimpio,
                    KioscoId = kioscoId,

                    // Agrega acá cualquier otro campo obligatorio de tu tabla de Supabase (ej: Estado = true)
                };

                // Insert devuelve el registro recién creado con su ID asignado por la base de datos
                var respuestaInsert = await _supaClient.From<Familia>().Insert(nuevaFamilia);
                var familiaCreada = respuestaInsert.Models.FirstOrDefault();

                if (familiaCreada != null)
                {
                    return (long)familiaCreada.Id;
                }

                throw new Exception("No se pudo obtener el ID de la familia creada.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en ConsultasDB al procesar la familia '{descripcionFamilia}': {ex.Message}");
            }
        }


        /// <summary>
        /// Busca una familia por su nombre y kiosco en Supabase. Si no existe, la crea y devuelve su ID (long).
        /// </summary>
        public async Task<long> ObtenerOInsertarFamiliaIdAsync(string nombreFamilia, long kioscoId)
        {
            if (string.IsNullOrWhiteSpace(nombreFamilia))
            {
                nombreFamilia = "MERCADERIA";
            }

            string nombreLimpio = nombreFamilia.Trim();

            try
            {
                // 1. BUSCAR SI YA EXISTE
                // Usamos strings planos con el nombre de la columna para evitar errores de tipo de criterio
                var respuesta = await _supaClient
                    .From<Familia>()
                    .Filter("kiosco_id", Supabase.Postgrest.Constants.Operator.Equals, kioscoId)
                    .Get();

                // Buscamos en memoria comparando contra la propiedad .Nombre (que mapea a la columna "nombre")
                //var familiaExistente = respuesta.Models
                //    .FirstOrDefault(f => f.Nombre != null && f.Nombre.Equals(nombreLimpio, StringComparison.OrdinalIgnoreCase));

                var familiaExistente = respuesta.Models
                   .FirstOrDefault(p => p.Nombre.Equals(nombreLimpio, StringComparison.OrdinalIgnoreCase));


                if (familiaExistente != null)
                {
                    return familiaExistente.Id; // Devolvemos el id (long) existente
                }

                // 2. SI NO EXISTE, LA INSERTAMOS
                var nuevaFamilia = new Familia
                {
                    Nombre = nombreLimpio,         // Asignamos la propiedad obligatoria 'Nombre'
                    Descripcion = nombreLimpio,    // Opcional: seteamos la descripción igual o dejala en null
                    KioscoId = kioscoId,           // Pasamos el long limpio
                    fechaCreacion = DateTime.Now   // Inicializa la fecha localmente de forma explícita si hace falta
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
            //if (string.IsNullOrWhiteSpace(descripcionProveedor))
            //{
            //    descripcionProveedor = "PROVEEDOR1";
            //}

            //string nombreLimpio = descripcionProveedor.Trim();

            //try
            //{
            //    // 1. BUSCAR SI YA EXISTE
            //    // En Supabase filtramos usando .Where o usando las expresiones Postgrest de la librería
            //    var respuesta = await _supaClient
            //        .From<Proveedor>()
            //        .Filter(f => f.KioscoId, Supabase.Postgrest.Constants.Operator.Equals, kioscoId)
            //        .Get();

            //    // Buscamos en memoria ignorando mayúsculas/minúsculas para estar seguros
            //    var proveedorExistente = respuesta.Models
            //        .FirstOrDefault(p => p.Nombre.Equals(nombreLimpio, StringComparison.OrdinalIgnoreCase));


            //    if (proveedorExistente != null)
            //    {
            //        return (long)proveedorExistente.Id; // Suponiendo que tu modelo Familia tiene la propiedad Id
            //    }

            //    // 2. SI NO EXISTE, LA INSERTAMOS
            //    var nuevoProveedor = new Proveedor
            //    {
            //        Nombre = nombreLimpio,
            //        KioscoId = kioscoId,
            //        // Agrega acá cualquier otro campo obligatorio de tu tabla de Supabase (ej: Estado = true)
            //    };

            //    // Insert devuelve el registro recién creado con su ID asignado por la base de datos
            //    var respuestaInsert = await _supaClient.From<Proveedor>().Insert(nuevoProveedor);
            //    var proveedorCreado = respuestaInsert.Models.FirstOrDefault();

            //    if (proveedorCreado != null)
            //    {
            //        return (long)proveedorCreado.Id;
            //    }

            //    throw new Exception("No se pudo obtener el ID del proveedor creado.");
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception($"Error en ConsultasDB al procesar el proveedor '{descripcionProveedor}': {ex.Message}");
            //}
            return 1;
        }
        /// <summary>
        /// Guarda un producto en Supabase. Si el código ya existe para ese kiosco, lo actualiza. Si no, lo crea.
        /// </summary>
        public async Task GuardarOActualizarProductoAsync(ProductoImportarPreview item, long kioscoId)
        {
            try
            {

                // 1. BUSCAR SI EL PRODUCTO YA EXISTE POR CÓDIGO Y KIOSCO
                //var respuesta = await _supaClient
                //    .From<Producto>()
                //    .Filter(p => p.KioscoId, Supabase.Postgrest.Constants.Operator.Equals, kioscoId)
                //    .Filter(p => p.CodigoBarras, Supabase.Postgrest.Constants.Operator.Equals, item.Codigo)
                //    .Get();

                // 1. BUSCAR SI EL PRODUCTO YA EXISTE POR CÓDIGO Y KIOSCO
                // Usamos strings directos con los nombres reales de las columnas en Supabase
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
                    // ESCENARIO A: EL PRODUCTO NO EXISTE -> SE INSERTA
                    var nuevoProd = new Producto
                    {
                        KioscoId = kioscoId,
                        CodigoBarras = item.Codigo,
                        Nombre = item.Descripcion,
                        //FamiliaId = await ObtenerOInsertarFamiliaIdAsync(item.Familia, kioscoId),
                        PrecioCosto = item.PrecioCosto,
                        PrecioVenta = item.PrecioVenta,
                        PrecioLista2 = item.PrecioLista2,
                        PrecioLista3 = item.PrecioLista3,
                        StockActual = item.Stock,
                        ProveedorId = await ObtenerOInsertarProveedorIdAsync(item.Proveedor, kioscoId)
                        // Agregá acá más campos si tu modelo 'Producto' lo requiere (ej: Estado = true)
                    };

                    await _supaClient.From<Producto>().Insert(nuevoProd);
                }
                else
                {
                    // ESCENARIO B: EL PRODUCTO YA EXISTE -> SE ACTUALIZA
                    productoExistente.Nombre = item.Descripcion;
                    //productoExistente.FamiliaId = await ObtenerOInsertarFamiliaIdAsync(item.Familia, kioscoId);
                    productoExistente.PrecioCosto = item.PrecioCosto;
                    productoExistente.PrecioVenta = item.PrecioVenta;
                    productoExistente.PrecioLista2 = item.PrecioLista2;
                    productoExistente.PrecioLista3 = item.PrecioLista3;
                    productoExistente.StockActual = item.Stock; // O 'productoExistente.Stock += item.Stock' si querés sumar al stock actual
                    productoExistente.ProveedorId = await ObtenerOInsertarProveedorIdAsync(item.Proveedor, kioscoId);
                    productoExistente.CodigoBarras = item.Codigo;

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