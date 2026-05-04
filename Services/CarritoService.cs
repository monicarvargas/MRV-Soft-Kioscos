using BlazorKioscos_v1.Models;

namespace BlazorKioscos_v1.Services
{
    public class ItemCarrito
    {
        public Producto Producto { get; set; } = new();
        public int Cantidad { get; set; } = 1;
        public decimal Subtotal => (decimal)Producto.PrecioVenta * Cantidad;
    }

    public class CarritoService
    {
        public List<ItemCarrito> Items { get; set; } = new();
        public decimal Total => Items.Sum(x => x.Subtotal);

        public void Agregar(Producto p)
        {
            var existente = Items.FirstOrDefault(i => i.Producto.Id == p.Id);
            if (existente != null) existente.Cantidad++;
            else Items.Add(new ItemCarrito { Producto = p });
        }

        public void Eliminar(int id) => Items.RemoveAll(x => x.Producto.Id == id);
        public void Vaciar() => Items.Clear();

    }
}
