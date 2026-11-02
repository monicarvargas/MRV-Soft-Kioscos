using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Pages;

namespace BlazorKioscoCompartido.Services
{
    public class ItemCarrito
    {
        public Producto Producto { get; set; } = new();

        private int _cantidad = 1;
        /// <summary>En unidades o en GRAMOS si es pesable. Al cambiarla se revisa si corresponde el precio por mayor.</summary>
        public int Cantidad
        {
            get => _cantidad;
            set { _cantidad = value; RecalcularPrecio(); }
        }

        // Guarda el precio unitario congelado por unidad o por KILOGRAMO
        public decimal PrecioUnitario { get; set; }

        /// <summary>Precio normal (de la lista activa: día / noche). 0 = precio fijo (ej: "varios"): no se recalcula.</summary>
        public decimal PrecioLista { get; set; }

        /// <summary>true si se está cobrando el precio por mayor (llegó a la cantidad mínima).</summary>
        public bool EsPorMayor => PrecioLista > 0 && TienePorMayor && _cantidad >= (Producto.CantxMayor ?? 0);

        /// <summary>El producto tiene precio por mayor cargado (cantidad mínima + precio). No aplica a productos por peso.</summary>
        public bool TienePorMayor => Producto != null && !EsProductoVario && !Producto.EsVentaPorPeso()
                                     && (Producto.CantxMayor ?? 0) > 1 && (Producto.PrecioVentaxMayor ?? 0) > 0;

        /// <summary>Precio por mayor: desde "cant_por_mayor" unidades se cobra "precio_venta_por_mayor" cada una.</summary>
        public void RecalcularPrecio()
        {
            if (PrecioLista <= 0 || Producto == null) return;
            PrecioUnitario = EsPorMayor ? Producto.PrecioVentaxMayor!.Value : PrecioLista;
        }


        // Cálculo dinámico del subtotal considerando gramos vs unidades
        //public decimal Subtotal => Producto.IsMedidaGramos()
        //    ? (Cantidad / 1000m) * PrecioUnitario
        //    : Cantidad * PrecioUnitario;


        // Propiedad para almacenar la referencia del estado
        public AppState? State { get; set; }

        /// <summary>Precios por cantidad del producto (ej: 10 u x $250 · 23 u x $500). null = no tiene.</summary>
        public List<PrecioCantidad>? Escalas { get; set; }

        /// <summary>¿Se está aplicando algún precio por cantidad?</summary>
        public bool UsaEscalas => Armado().Packs.Any();

        /// <summary>Cómo se armó el precio, ej: "1 x 23 u ($ 500) + 1 x 10 u ($ 250) + 2 sueltas". Vacío si no hay packs.</summary>
        public string DetalleEscalas
        {
            get
            {
                var a = Armado();
                if (!a.Packs.Any()) return "";
                var partes = a.Packs.Select(p => $"{p.Veces} x {p.Escala.Cantidad} u ($ {p.Escala.Precio:N0})").ToList();
                if (a.Sueltas > 0) partes.Add(a.Sueltas == 1 ? "1 suelta" : $"{a.Sueltas} sueltas");
                return string.Join(" + ", partes);
            }
        }

        /// <summary>
        /// La combinación más barata para el cliente: packs + unidades sueltas (a PrecioUnitario).
        /// Ej: 35 gomitas con c/u $30, 10 x $250, 23 x $500 → 23 + 10 + 2 sueltas = $810.
        /// </summary>
        /// <summary>
        /// "Llevando 1 más paga menos": si agregando pocas unidades el total queda igual o más barato, lo avisa.
        /// Ej: 9 gomitas = $270, pero 10 = $250 → (1, 250, 20). null = no conviene llevar más.
        /// </summary>
        public (int Mas, decimal Total, decimal Ahorro)? Sugerencia
        {
            get
            {
                if (Escalas == null || Escalas.Count == 0 || EsProductoVario || Producto == null
                    || Utilidades.EsMedidaGramos(Producto.UnidadMedida) || Cantidad < 1 || Cantidad > 5000) return null;
                decimal actual = Armado(Cantidad).Total;
                int hasta = Escalas.Max(e => e.Cantidad);
                for (int k = 1; k <= hasta; k++)
                {
                    decimal t = Armado(Cantidad + k).Total;
                    if (t <= actual) return (k, t, actual - t);
                }
                return null;
            }
        }

        private (decimal Total, List<(PrecioCantidad Escala, int Veces)> Packs, int Sueltas) Armado() => Armado(Cantidad);

        private (decimal Total, List<(PrecioCantidad Escala, int Veces)> Packs, int Sueltas) Armado(int n)
        {
            var escalas = (Escalas ?? new()).Where(e => e.Cantidad > 1 && e.Cantidad <= n && e.Precio > 0).ToList();
            if (EsProductoVario || n <= 1 || !escalas.Any() || Producto == null || Utilidades.EsMedidaGramos(Producto.UnidadMedida))
                return (n * PrecioUnitario, new(), n);

            // cuentas exactas hasta 5000 unidades; más que eso (raro), de mayor a menor
            if (n > 5000)
            {
                var packs = new List<(PrecioCantidad, int)>();
                int resto = n; decimal tot = 0;
                foreach (var e in escalas.OrderByDescending(e => e.Cantidad))
                {
                    int v = resto / e.Cantidad;
                    if (v > 0 && e.Precio < e.Cantidad * PrecioUnitario) { packs.Add((e, v)); resto -= v * e.Cantidad; tot += v * e.Precio; }
                }
                return (tot + resto * PrecioUnitario, packs, resto);
            }

            var costo = new decimal[n + 1];
            var eleccion = new int[n + 1];   // -1 = una suelta; si no, índice de la escala
            for (int i = 1; i <= n; i++)
            {
                costo[i] = costo[i - 1] + PrecioUnitario; eleccion[i] = -1;
                for (int j = 0; j < escalas.Count; j++)
                {
                    int q = escalas[j].Cantidad;
                    if (q <= i && costo[i - q] + escalas[j].Precio < costo[i]) { costo[i] = costo[i - q] + escalas[j].Precio; eleccion[i] = j; }
                }
            }
            var veces = new int[escalas.Count];
            int sueltas = 0;
            for (int i = n; i > 0;)
            {
                if (eleccion[i] < 0) { sueltas++; i--; }
                else { veces[eleccion[i]]++; i -= escalas[eleccion[i]].Cantidad; }
            }
            var lista = escalas.Select((e, j) => (e, veces[j])).Where(x => x.Item2 > 0).OrderByDescending(x => x.e.Cantidad).ToList();
            return (costo[n], lista, sueltas);
        }

        // La propiedad Subtotal usa el State asignado
        public decimal Subtotal
        {
            get
            {
                if (Producto != null && Utilidades.EsMedidaGramos(Producto.UnidadMedida))
                {
                    decimal bruto = (Cantidad / 1000m) * PrecioUnitario;

                    if (Producto.UsaRedondeo(State))   // salvo los productos marcados "sin redondeo"
                    {
                        decimal paso = State!.MultiploRedondeo > 0 ? (decimal)State.MultiploRedondeo : 50m;
                        return Math.Ceiling(bruto / paso) * paso;
                    }

                    return Math.Round(bruto, 2);
                }

                return Escalas != null && Escalas.Count > 0 ? Armado().Total : Cantidad * PrecioUnitario;
            }
        }



        public bool EsProductoVario { get; set; }
    }

    public class CarritoService
    {
        private readonly AppState _state;

        // Blazor inyectará automáticamente AppState aquí
        public CarritoService(AppState state)
        {
            _state = state;
        }
        public List<ItemCarrito> Items { get; set; } = new();

        /// <summary>Precios por cantidad de los productos del negocio (los carga Vender). Clave: id del producto.</summary>
        public Dictionary<long, List<PrecioCantidad>> Escalas { get; private set; } = new();

        /// <summary>Reemplaza los precios por cantidad y se los pone también a lo que ya está en el carrito.</summary>
        public void FijarEscalas(IEnumerable<PrecioCantidad>? escalas)
        {
            Escalas = (escalas ?? Enumerable.Empty<PrecioCantidad>())
                .GroupBy(e => e.ProductoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Cantidad).ToList());
            foreach (var i in Items) i.Escalas = i.EsProductoVario ? null : Escalas.GetValueOrDefault(i.Producto.Id);
        }
        public decimal Total => Items.Sum(x => x.Subtotal);

        /// <summary>
        /// Lista de precios de ESTA venta: se fija con el primer producto y se mantiene hasta cobrar o vaciar.
        /// Ej: la venta empezó 23:58 → toda la venta va con el precio de venta, aunque se cobre 00:05.
        /// La venta empezó 07:58 → toda va con la Lista 2 (NOCHE), aunque se cobre 08:03.
        /// </summary>
        public string? ListaDeLaVenta { get; private set; }

        private string ListaParaAgregar(string columnaPedida)
        {
            if (!Items.Any() || string.IsNullOrEmpty(ListaDeLaVenta))
                ListaDeLaVenta = PrecioService.ColumnaSegunMomento(_state.KioscoId, DateTime.Now) ?? columnaPedida;
            return ListaDeLaVenta;
        }

        public void Agregar(Producto p, string columnaPrecioActiva, bool esProductoVario = false)
        {
            columnaPrecioActiva = ListaParaAgregar(columnaPrecioActiva);
            decimal precioAplicado = ResolverPrecioBase(p, columnaPrecioActiva);

            var existente = Items.FirstOrDefault(i => i.Producto.Id == p.Id);

            if (existente != null)
            {
                existente.Cantidad++;   // si llega a la cantidad por mayor, cambia el precio solo
            }
            else
            {
                var nuevo = new ItemCarrito
                {
                    Producto = p,
                    PrecioUnitario = precioAplicado,
                    PrecioLista = esProductoVario ? 0 : precioAplicado,
                    EsProductoVario = esProductoVario,
                    State = _state, // <-- AQUÍ LE PASAS EL APPSTATE
                    Escalas = esProductoVario ? null : this.Escalas.GetValueOrDefault(p.Id)
                };
                nuevo.Cantidad = 1;
                Items.Add(nuevo);
            }
        }

        public void AgregarManual(Producto p, string columnaPrecioActiva, bool esProductoVario = false, int cantidad = 1)
        {
            columnaPrecioActiva = ListaParaAgregar(columnaPrecioActiva);
            decimal precioAplicado = ResolverPrecioBase(p, columnaPrecioActiva);

            var existente = Items.FirstOrDefault(i => i.Producto.Id == p.Id);

            if (existente != null)
            {
                existente.Cantidad += cantidad;   // si llega a la cantidad por mayor, cambia el precio solo
            }
            else
            {
                var nuevo = new ItemCarrito
                {
                    Producto = p,
                    PrecioUnitario = precioAplicado,
                    PrecioLista = esProductoVario ? 0 : precioAplicado,
                    EsProductoVario = esProductoVario,
                    State = _state, // <-- AQUÍ TAMBIÉN LE PASAS EL APPSTATE
                    Escalas = esProductoVario ? null : this.Escalas.GetValueOrDefault(p.Id)
                };
                nuevo.Cantidad = cantidad;
                Items.Add(nuevo);
            }
        }

        /// <summary>Agrega con un precio fijo (productos "varios" sin código: el precio lo escribe el vendedor y no se toca).</summary>
        public void AgregarConPrecio(Producto p, decimal precioUnitario, int cantidad, bool esProductoVario)
        {
            ListaParaAgregar("precio_venta");   // si la venta empieza con esto, también queda fijada la lista
            Items.Add(new ItemCarrito
            {
                Producto = p,
                Cantidad = cantidad < 1 ? 1 : cantidad,
                PrecioUnitario = precioUnitario,
                EsProductoVario = esProductoVario,
                State = _state
            });
        }

        /// <summary>Reemplaza la cantidad (unidades, o gramos si es por peso).</summary>
        public void FijarCantidad(ItemCarrito item, int cantidad)
        {
            if (cantidad <= 0) Items.Remove(item);
            else item.Cantidad = cantidad;
        }

        public void Eliminar(ItemCarrito item) => Items.Remove(item);

        public void Eliminar(long id) => Items.RemoveAll(x => x.Producto.Id == id);
        public void Vaciar() { Items.Clear(); ListaDeLaVenta = null; }

        // HELPER PRIVADO: Resuelve la lista activa considerando si la BD guarda el valor en base a 100g
        private decimal ResolverPrecioBase(Producto p, string columnaPrecioActiva)
        {
            // misma regla que muestra el buscador (antes acá se sumaban $500 fijos a la Lista 2 y no coincidía con la pantalla)
            decimal precioCalculado = PrecioService.PrecioSegunLista(p, columnaPrecioActiva, _state.KioscoId);

            if (precioCalculado == 0)
            {
                precioCalculado = (p.PrecioVenta ?? 0) + 500;
            }

            // Si el precio guardado en BD ya representa el precio del Kilo entero ($23.500), no lo multiplicamos de nuevo.
            //if (p.IsMedidaGramos() && precioCalculado < 5000)
            //if (p.IsMedidaGramos())
            //{
            //    precioCalculado *= 10m;
            //}

            return precioCalculado;
        }
    }
}