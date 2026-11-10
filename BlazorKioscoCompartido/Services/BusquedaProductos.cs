using BlazorKioscoCompartido.Models;
using static Supabase.Postgrest.Constants;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Ayudas para buscar productos. Además del nombre y de mi código de barras, se busca por el
    /// CÓDIGO DEL PROVEEDOR (productos_proveedores.codigo_barras_proveedor): lo usan Productos,
    /// el buscador de Vender / Recibir mercadería y el buscador de varios (carteles, precios, pedidos).
    /// </summary>
    public static class BusquedaProductos
    {
        /// <summary>
        /// Ids de los productos cuyo código del proveedor contiene lo escrito (hasta 50).
        /// Lista vacía si no hay, si escribieron menos de 2 caracteres o si falla (la búsqueda sigue igual).
        /// </summary>
        public static async Task<List<object>> IdsPorCodigoProveedorAsync(Supabase.Client db, long kioscoId, string? texto)
        {
            // los códigos no llevan espacios; se sacan los caracteres que rompen el filtro de Supabase
            var t = System.Text.RegularExpressions.Regex.Replace(texto ?? "", @"[,()%""\\*\s]", "");
            if (t.Length < 2) return new();
            try
            {
                var res = await db.From<ProductoProveedor>()
                    .Select("id_producto")
                    .Where(r => r.IdKiosco == kioscoId)
                    .Filter("codigo_barras_proveedor", Operator.ILike, $"%{t}%")
                    .Limit(50)
                    .Get();
                return (res.Models ?? new()).Select(r => r.IdProducto).Distinct().Select(id => (object)id).ToList();
            }
            catch
            {
                return new();
            }
        }
    }
}
