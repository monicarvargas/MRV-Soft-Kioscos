
using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Pages;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace BlazorKioscoCompartido.Services
{
    public static class Utilidades
    {
        /// <summary>
        /// Determina si la unidad de medida del producto corresponde a gramos.
        /// </summary>
        public static bool EsMedidaGramos(string? unidadMedida)
        {
            if (string.IsNullOrWhiteSpace(unidadMedida)) return false;

            var u = unidadMedida.Trim();
            return u.Equals("GR", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Grs", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Gramos", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("KG", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Kilo", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Kilos", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determina si la unidad de medida del producto corresponde a kilos.
        /// </summary>
        public static bool EsMedidaKilos(string? unidadMedida)
        {
            if (string.IsNullOrWhiteSpace(unidadMedida)) return false;

            var u = unidadMedida.Trim();
            return u.Equals("KG", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Kilos", StringComparison.OrdinalIgnoreCase) ||
                   u.Equals("Kilo", StringComparison.OrdinalIgnoreCase);
        }




        // Métodos de extensión directa para el objeto Producto
        public static bool IsMedidaGramos(this Producto? producto)
            => EsMedidaGramos(producto?.UnidadMedida);

        public static bool IsMedidaKilos(this Producto? producto)
            => EsMedidaKilos(producto?.UnidadMedida);

        public static bool EsVentaPorPeso(this Producto? producto)
            => producto.IsMedidaGramos() || producto.IsMedidaKilos();

        // === BULTO ("Lo compro por bulto") ===  las cuentas están todas en Services/Bultos.cs
        // BultoUnidades guarda UNIDADES si se vende por unidad, y GRAMOS si se vende por peso (bolsa de 15 kg = 15000).

        /// <summary>Lo que trae el bulto del producto en la unidad del costo: unidades, o KILOS. 0 = sin bulto.</summary>
        public static decimal CantidadBulto(this Producto? producto)
            => producto == null ? 0m : Bultos.Factor(producto.BultoUnidades, producto.EsVentaPorPeso());

        /// <summary>"48 u" o "15 kg" / "4,35 kg".</summary>
        public static string TextoCantidadBulto(this Producto? producto)
            => producto == null ? "" : Bultos.Texto(producto.BultoUnidades, producto.EsVentaPorPeso());

        /// <summary>Gramos escritos en kilos: 15000 → "15 kg", 4350 → "4,35 kg".</summary>
        public static string TextoKilos(int gramos) => Bultos.TextoCantidad(gramos, enKilos: true);

        /// <summary>
        /// ¿El precio de venta se muestra (y se redondea) cada 100 g? Solo en GRAMOS (golosinas sueltas).
        /// En KILO el precio es por kilo, igual que el costo. (Al vender, los dos se pesan en gramos.)
        /// </summary>
        public static bool PrecioCada100g(this Producto? producto)
            => producto.IsMedidaGramos() && !producto.IsMedidaKilos();

        // === NUEVAS FUNCIONES DE TEXTO DE MODO PESO ===

        /// <summary>
        /// Devuelve "(1 kg)" si el producto se vende por peso (gramos o kilos), o un texto vacío si es unidad.
        /// </summary>
        public static string ObtenerTextoModoPesoCosto(this Producto? producto)
        {
            return producto.EsVentaPorPeso() ? "(1 kg)" : "";
        }

        /// <summary>
        /// Devuelve "(100 gr)", "(1 kg)" o "FINAL" según la unidad de medida del producto.
        /// </summary>
        public static string ObtenerTextoModoPesoVenta(this Producto? producto)
        {
            if (producto.PrecioCada100g()) return "(100 gr)";
            if (producto.IsMedidaKilos()) return "(1 kg)";
            return "FINAL";
        }
        public static string ObtenerTextoModoPesoVentaCorto(this Producto? producto)
        {
            if (producto.PrecioCada100g()) return "(x 100 g)";
            if (producto.IsMedidaKilos()) return "(x 1 kg)";
            return "";
        }
        public static string ObtenerTituloModoPesoVenta(this Producto? producto)
        {
            if (producto.PrecioCada100g()) return "(Gramos)";
            if (producto.IsMedidaKilos()) return "(Kilos)";
            return "";
        }
        // === NUEVO MÉTODO PARA MOSTRAR PRECIO DE VENTA FORMATEADO ===
        /// <summary>
        /// Obtiene el precio de venta formateado según el tipo de lista, aplicando la división por 10 y el redondeo si el producto es en gramos.
        /// </summary>
        public static string ObtenerPrecioVentaMostrar(
            this Producto? producto,
            string tipoLista,
            AppState state,
            CultureInfo culture,
            decimal pasoRedondeoDefault = 50m)
        {
            if (producto == null) return (0m).ToString("#,##0.00", culture);

            decimal? precioOriginal = tipoLista switch
            {
                "Lista2" => producto.PrecioLista2,
                "Lista3" => producto.PrecioLista3,
                "XMayor" => producto.PrecioVentaxMayor,
                _ => producto.PrecioVenta
            };

            decimal valorMostrar = precioOriginal ?? 0;

            if (producto.PrecioCada100g())
            {
                valorMostrar /= 10m;

                if (producto.UsaRedondeo(state))   // salvo los productos "sin redondeo"
                {
                    decimal paso = state.MultiploRedondeo > 0 ? (decimal)state.MultiploRedondeo : pasoRedondeoDefault;
                    valorMostrar = Math.Ceiling(valorMostrar / paso) * paso;
                }
            }

            return valorMostrar.ToString("#,##0.00", culture);
        }
        /// <summary>
        /// Calcula el precio final sugerido según el costo, el margen de utilidad y las reglas de redondeo/unidad del kiosco.
        /// </summary>
        //public static decimal? CalcularPrecioPorUtilidad(this Producto? producto, decimal? precioCosto, decimal? utilidadLista, AppState state)
        //{
        //    if (precioCosto == null || utilidadLista == null) return null;

        //    decimal costo = precioCosto.Value;
        //    decimal margen = utilidadLista.Value;
        //    decimal precioSugeridoKg = costo + (costo * (margen / 100));

        //    if (state != null && state.UsarRedondeo)
        //    {
        //        decimal paso = state.MultiploRedondeo > 0 ? (decimal)state.MultiploRedondeo : 50m;

        //        if (producto.IsMedidaGramos())
        //        {
        //            decimal precio100g = precioSugeridoKg / 10m;
        //            decimal precio100gRedondeado = Math.Ceiling(precio100g / paso) * paso;

        //            return precio100gRedondeado * 10m;
        //        }
        //        else
        //        {
        //            return Math.Ceiling(precioSugeridoKg / paso) * paso;
        //        }
        //    }

        //    return Math.Round(precioSugeridoKg, 2);
        //}
        public static decimal? CalcularPrecioPorUtilidad(this Producto? producto, decimal? precioCosto, decimal? utilidadLista, AppState state)
        {
            if (precioCosto == null || utilidadLista == null) return null;

            decimal costo = precioCosto.Value;
            decimal margen = utilidadLista.Value;
            decimal precioSugeridoKg = costo + (costo * (margen / 100m));

            if (producto?.UsaRedondeo(state) ?? (state != null && state.UsarRedondeo))   // salvo los productos "sin redondeo"
            {
                decimal paso = state.MultiploRedondeo > 0 ? (decimal)state.MultiploRedondeo : 50m;

                if (producto.PrecioCada100g())
                {
                    // 1. Obtener precio por 100g de costo + utilidad
                    decimal precio100g = precioSugeridoKg / 10m;

                    // 2. Redondear hacia arriba según el múltiplo del Kiosco (ej: $1900)
                    decimal precio100gRedondeado = Math.Ceiling(precio100g / paso) * paso;

                    // 3. Devolver el precio equivalente por kilo ($19.000)
                    return precio100gRedondeado * 10m;
                }
                else
                {
                    return Math.Ceiling(precioSugeridoKg / paso) * paso;
                }
            }

            return Math.Round(precioSugeridoKg, 2);
        }
        /// <summary>
        /// Calcula el margen de utilidad y actualiza los precios del producto a partir del precio de venta ingresado en un input.
        /// </summary>
        public static void CalcularMargenDesdeVenta(this Producto? producto, ChangeEventArgs e, string tipoLista, CultureInfo culture)
        {
            if (producto == null) return;
            if (!decimal.TryParse(e.Value?.ToString(), NumberStyles.Currency, culture, out decimal ventaIngresada)) return;

            // Si es en gramos, la venta ingresada corresponde a 100g. Lo multiplicamos x10 para obtener el valor del Kg.
            decimal ventaRealKg = producto.PrecioCada100g() ? (ventaIngresada * 10m) : ventaIngresada;

            if (producto.PrecioCosto == null || producto.PrecioCosto == 0)
            {
                producto.AsignarUtilidadYPrecio(tipoLista, ventaRealKg, 0);
                return;
            }

            decimal costo = producto.PrecioCosto.Value;
            decimal margenCalculado = ((ventaRealKg - costo) / costo) * 100;
            producto.AsignarUtilidadYPrecio(tipoLista, ventaRealKg, Math.Round(margenCalculado, 2));
        }

        /// <summary>
        /// Asigna el precio final en Kg/Unidad y la utilidad calculada a la lista correspondiente del producto.
        /// </summary>
        public static void AsignarUtilidadYPrecio( this Producto producto, string tipoLista, decimal venta, decimal margen)
        {
            switch (tipoLista)
            {
                case "Principal":
                    producto.PrecioVenta = venta;
                    producto.UtilidadPV = margen;
                    break;
                case "Lista2":
                    producto.PrecioLista2 = venta;
                    producto.UtilidadLista2 = margen;
                    break;
                case "Lista3":
                    producto.PrecioLista3 = venta;
                    producto.UtilidadLista3 = margen;
                    break;
                case "XMayor":
                    producto.PrecioVentaxMayor = venta;
                    producto.UtilidadVentaxMayor = margen;
                    break;
            }
        }
        /// <summary>
        /// Formatea un valor decimal o nulo a representación de moneda (ej: "1.250,50").
        /// </summary>
        public static string FormatearMonto(this decimal? monto, CultureInfo? culture = null)
        {
            var culturaUsar = culture ?? CultureInfo.GetCultureInfo("es-AR");
            return (monto ?? 0m).ToString("#,##0.00", culturaUsar);
        }

        /// <summary>
        /// Sobrecarga para valores decimales no nulos.
        /// </summary>
        public static string FormatearMonto(this decimal monto, CultureInfo? culture = null)
        {
            var culturaUsar = culture ?? CultureInfo.GetCultureInfo("es-AR");
            return monto.ToString("#,##0.00", culturaUsar);
        }
        /// <summary>
        /// Formatea el porcentaje de utilidad a un número entero como cadena (ej: 25.00 -> "25").
        /// </summary>
        public static string FormatearUtilidadEntera(this decimal? utilidad)
        {
            if (utilidad == null || utilidad == 0) return "0";
            return Math.Round(utilidad.Value, 0).ToString("0");
        }
        /// <summary>
        /// Devuelve el ícono de Bootstrap según el nombre de la forma de pago
        /// </summary>
        public static string ObtenerIconoFormaPago(string nombre)
        {
            return nombre.ToLower() switch
            {
                string n when n.Contains("efectivo") => "bi-cash",
                string n when n.Contains("mercado") || n.Contains("mp") => "bi-qr-code",
                string n when n.Contains("transf") => "bi-bank",
                string n when n.Contains("fiado") || n.Contains("crédito") || n.Contains("credito") => "bi-journal-bookmark-fill",
                string n when n.Contains("débito") || n.Contains("debito") || n.Contains("tarjeta") => "bi-credit-card",

                _ => "bi-wallet2" // Ícono por defecto para otros medios de pago
            };
        }
        /// <summary>
        /// Devuelve la clase de estilo activa específica según la forma de pago
        /// </summary>
        public static string ObtenerClaseActivaFormaPago(string nombre)
        {
            return nombre.ToLower() switch
            {
                string n when n.Contains("efectivo") => "active-efectivo",
                string n when n.Contains("mercado") || n.Contains("mp") => "active-mercado",
                string n when n.Contains("transf") => "active-transferencia",
                string n when n.Contains("fiado") || n.Contains("crédito") || n.Contains("credito") => "active-fiado",
                _ => "btn-primary text-white" // Clase activa por defecto si agregás una nueva forma de pago
            };
        }
        public static long ObtenerFamiliaIdRapido(long KioscoId)
        {
            return KioscoId switch
            {
                1 => 1,
                5 => 11,
                _ => 1 // Valor por defecto para cualquier otro kiosco
            };
        }   

    }
}
