using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorKioscoCompartido.Models; // <-- añadir para los modelos

namespace BlazorKioscoCompartido.Services
{
    public class ProveedorService
    {
        private readonly Supabase.Client _supaClient;

        public ProveedorService(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        public async Task<List<Proveedor>> ObtenerProveedoresAsync(long kioscoId)
        {
            var response = await _supaClient.From<Proveedor>()
                .Where(p => p.KioscoId == kioscoId)
                .Get();
            return response.Models.OrderBy(p => p.Nombre).ToList();
        }

        public async Task<List<ProductoProveedor>> ObtenerRelacionesPorProductoAsync(long productoId)
        {
            var res = await _supaClient.From<ProductoProveedor>()
                .Where(x => x.IdProducto == productoId)
                .Get();
            return res.Models.ToList();
        }

        public async Task GuardarRelacionesProductoAsync(long productoId, long kioscoId, List<ProductoProveedor> relaciones)
        {
            try
            {
                // 1) Eliminar relaciones existentes para ese producto+kiosco
                await _supaClient.From<ProductoProveedor>()
                    .Where(r => r.IdProducto == productoId && r.IdKiosco == kioscoId)
                    .Delete();

                // 2) Insertar sólo si hay relaciones válidas
                if (relaciones == null || !relaciones.Any()) return;

                // Normalizar las entidades: crear listas con sólo las columnas mapeadas en la tabla
                var relacionesAInsertar = relaciones
                    .Select(r =>
                    {
                        // Asegurar IdProducto/IdKiosco correctos
                        r.IdProducto = productoId;
                        r.IdKiosco = kioscoId;

                        return new ProductoProveedor
                        {
                            // No incluir propiedades de navegación (Proveedor) para evitar problemas al insertar
                            IdProducto = r.IdProducto,
                            IdProveedor = r.IdProveedor,
                            IdKiosco = r.IdKiosco,
                            CodigoBarrasProveedor = string.IsNullOrWhiteSpace(r.CodigoBarrasProveedor) ? null : r.CodigoBarrasProveedor.Trim(),
                            Habilitado = r.Habilitado,
                            EsPrincipal = r.EsPrincipal,
                            UnidadesPorBulto = r.UnidadesPorBulto < 1 ? 1 : r.UnidadesPorBulto,
                            // siempre con valor ("UNIDAD" si es suelto): al guardar varios proveedores juntos todas las filas llevan las mismas columnas
                            TipoBulto = BlazorKioscoCompartido.Constants.TiposBulto.Nombre(r.TipoBulto, r.UnidadesPorBulto),
                            DescripcionProveedor = string.IsNullOrWhiteSpace(r.DescripcionProveedor) ? null : r.DescripcionProveedor.Trim()
                        };
                    })
                    .ToList();

                if (relacionesAInsertar.Any())
                {
                    var insertResponse = await _supaClient.From<ProductoProveedor>().Insert(relacionesAInsertar);
                    // Log minimal para facilitar debug
                    Console.WriteLine($"GuardarRelacionesProductoAsync: insertados {insertResponse.Models?.Count ?? 0} relaciones para producto {productoId}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar relaciones de proveedor: {ex.Message}");
                throw;
            }
        }
    }
}