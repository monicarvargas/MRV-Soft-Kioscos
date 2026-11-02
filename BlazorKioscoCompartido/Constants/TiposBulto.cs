namespace BlazorKioscoCompartido.Constants
{
    /// <summary>
    /// Cómo viene un producto de un proveedor: suelto (UNIDAD) o en BULTO, CAJA, PACK, BOLSA, DISPLAY, PAQUETE
    /// con N unidades adentro (ej: alfajores, CAJA de 48). Se guarda en productos_proveedores.tipo_bulto (script 34).
    /// En el pedido, 1 = 1 caja (o el tipo que sea).
    /// </summary>
    public static class TiposBulto
    {
        public const string Unidad = "UNIDAD";

        /// <summary>Los que se pueden elegir, en este orden.</summary>
        public static readonly List<string> Lista = new() { Unidad, "BULTO", "CAJA", "PACK", "BOLSA", "DISPLAY", "PAQUETE" };

        /// <summary>
        /// El tipo a mostrar. Se puede elegir CAJA sin decir cuántas trae (ese dato puede estar en "cómo lo llama el proveedor").
        /// Sin tipo: si trae más de 1 unidad, "BULTO"; si no, suelto.
        /// </summary>
        public static string Nombre(string? tipo, int unidades)
        {
            var t = (tipo ?? "").Trim().ToUpperInvariant();
            if (t != "" && t != Unidad) return t;
            return unidades > 1 ? "BULTO" : Unidad;
        }

        /// <summary>¿Viene suelto (ni caja, ni bulto, ni pack...)?</summary>
        public static bool EsSuelto(string? tipo, int unidades) => Nombre(tipo, unidades) == Unidad;

        /// <summary>En minúscula y en plural si hace falta: "caja", "cajas", "bulto(s)"...</summary>
        public static string Texto(string? tipo, int unidades, decimal cantidad)
        {
            var n = Nombre(tipo, unidades).ToLowerInvariant();
            if (cantidad == 1) return n;
            return n switch
            {
                "pack" => "packs",
                "display" => "displays",
                _ => n + "s"
            };
        }

        /// <summary>Lo que se guarda: null si es suelto (así se ve igual que antes).</summary>
        public static string? ParaGuardar(string? tipo, int unidades) => EsSuelto(tipo, unidades) ? null : Nombre(tipo, unidades);
    }
}
