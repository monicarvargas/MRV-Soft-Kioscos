using BlazorKioscos_v1.Models;

namespace BlazorKioscos_v1.Services
{
    public class ItemCarrito
    {
        public Producto Producto { get; set; } = new();
        public int Cantidad { get; set; } = 1;

        // 1. NUEVA PROPIEDAD: Guarda el precio unitario congelado al momento de agregar , va a tomar el precio_venta o el precio_listaX según corresponda
        public decimal PrecioUnitario { get; set; }

        // 2. MODIFICADA: Ahora el subtotal se calcula con el precio congelado
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }

    public class CarritoService
    {
        public List<ItemCarrito> Items { get; set; } = new();
        public decimal Total => Items.Sum(x => x.Subtotal);

        //public void Agregar(Producto p)
        //{
        //    var existente = Items.FirstOrDefault(i => i.Producto.Id == p.Id);
        //    if (existente != null) existente.Cantidad++;
        //    else Items.Add(new ItemCarrito { Producto = p });
        //}
        public void Agregar(Producto p, string columnaPrecioActiva)
        {
            // 1. Evaluamos dinámicamente qué precio corresponde según la lista horaria activa
            decimal precioAplicado = columnaPrecioActiva switch
            {
                //"precio_lista2" => p.PrecioLista2 ?? p.PrecioVenta ?? 0,
                "precio_lista2" => (p.PrecioLista2 == null || p.PrecioLista2 == 0)
                       ? (p.PrecioVenta ?? 0) + 500
                       : p.PrecioLista2.Value,
                "precio_lista3" => p.PrecioLista3 ?? p.PrecioVenta ?? 0, // Por si agregás más listas a futuro
                _ => p.PrecioVenta ?? 0 // Por defecto ('precio_venta' o cualquier otro valor)
            };

            // 2. Buscamos si el producto ya está en el carrito
            var existente = Items.FirstOrDefault(i => i.Producto.Id == p.Id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                if (precioAplicado == 0)
                { 
                    precioAplicado = p.PrecioVenta +500 ?? 0; // Aseguramos que siempre haya un precio, aunque sea el de venta
                }

                // 3. Al crear el nuevo ítem, le asignamos el precio calculado de la lista horaria
                Items.Add(new ItemCarrito
                {
                    Producto = p,
                    PrecioUnitario = precioAplicado // Asegurate de tener esta propiedad en tu ItemCarrito
                });
            }
        }
        public void Eliminar(long id) => Items.RemoveAll(x => x.Producto.Id == id);
        public void Vaciar() => Items.Clear();

    }
}
