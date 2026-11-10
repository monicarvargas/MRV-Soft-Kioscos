using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Models.DTO;
using BlazorKioscoCompartido.Models.Vistas;
using static Supabase.Postgrest.Constants;

namespace BlazorKioscoCompartido.Services.Reportes
{
    // ===================== RESULTADO DEL REPORTE DEL DÍA =====================
    public class ResumenDia
    {
        /// <summary>Ventas pagadas en el momento (efectivo, transferencia, etc.). NO incluye lo fiado.</summary>
        public decimal VentasCobradas { get; set; }
        /// <summary>Entregas a cuenta de deudas pagadas con plata (no las descontadas del sueldo).</summary>
        public decimal CobrosDeudas { get; set; }
        /// <summary>Lo que se vendió fiado ese día (no entró plata).</summary>
        public decimal Fiado { get; set; }
        /// <summary>Entregas "descontadas" a empleados (no entró plata).</summary>
        public decimal Descontado { get; set; }
        public int CantidadVentas { get; set; }

        /// <summary>Plata que realmente entró: ventas cobradas + cobros de deudas.</summary>
        public decimal TotalRecaudado => VentasCobradas + CobrosDeudas;

        public List<FilaMedioPago> Medios { get; set; } = new();
        public List<FilaEmpleado> Empleados { get; set; } = new();
    }

    public class FilaTurno
    {
        public long TurnoId { get; set; }
        public string Nombre { get; set; } = "";
        public string Rango { get; set; } = "";
        public decimal Cobrado { get; set; }
        public decimal Fiado { get; set; }
        public decimal CobrosDeudas { get; set; }
        public decimal Descontado { get; set; }
        public int CantidadVentas { get; set; }
        /// <summary>Plata que entró en el turno (ventas cobradas + cobros de deudas).</summary>
        public decimal TotalRecaudado => Cobrado + CobrosDeudas;
        public decimal TotalVendido => Cobrado + Fiado;
        /// <summary>Lo que vendió cada usuario en el turno (cobrado + fiado).</summary>
        public Dictionary<string, decimal> PorEmpleado { get; set; } = new();
        /// <summary>Cada usuario en el turno, con lo que cobró por forma de pago.</summary>
        public Dictionary<string, EmpleadoTurno> Empleados { get; set; } = new();
        /// <summary>Plata que entró por medio de pago.</summary>
        public Dictionary<string, decimal> PorMedio { get; set; } = new();
    }

    /// <summary>Lo que hizo un empleado en un turno, separado por forma de pago.</summary>
    public class EmpleadoTurno
    {
        public string Nombre { get; set; } = "";
        public decimal Cobrado { get; set; }
        public decimal Fiado { get; set; }
        public decimal CobrosDeudas { get; set; }
        public HashSet<long> Movimientos { get; } = new();
        public int CantidadVentas => Movimientos.Count;
        public decimal TotalVendido => Cobrado + Fiado;
        /// <summary>La plata que tendría que tener en la caja de ese turno.</summary>
        public decimal EnCaja => Cobrado + CobrosDeudas;
        /// <summary>Por forma de pago (ventas + cobros de deudas; FIADO aparte, no es plata).</summary>
        public Dictionary<string, decimal> PorMedio { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class FilaMedioPago
    {
        public string Nombre { get; set; } = "";
        public decimal Ventas { get; set; }
        public decimal CobrosDeudas { get; set; }
        public decimal Total => Ventas + CobrosDeudas;
    }

    public class FilaEmpleado
    {
        public Guid? UsuarioId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Rol { get; set; }
        public int CantidadVentas { get; set; }
        public decimal Cobrado { get; set; }
        public decimal Fiado { get; set; }
        public decimal CobrosDeudas { get; set; }
        public decimal Descontado { get; set; }
        /// <summary>Todo lo que vendió (cobrado + fiado).</summary>
        public decimal TotalVendido => Cobrado + Fiado;
        /// <summary>La plata que tendría que tener en la caja.</summary>
        public decimal EnCaja => Cobrado + CobrosDeudas;
        /// <summary>Por forma de pago que confirmó (ventas + cobros de deudas; FIADO aparte, no es plata).</summary>
        public Dictionary<string, decimal> PorMedio { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Las ventas que hizo (los productos se cargan recién cuando se piden: CargarProductosAsync).</summary>
        public List<TicketVenta> Ventas { get; set; } = new();
        public bool ProductosCargados { get; set; }
    }

    /// <summary>Una venta (un "ticket"): todo lo que se cobró junto.</summary>
    public class TicketVenta
    {
        public long Movimiento { get; set; }
        /// <summary>Hora de la venta en UTC (la pantalla la pasa a la hora del equipo).</summary>
        public DateTime? HoraUtc { get; set; }
        public string FormaPago { get; set; } = "";
        public bool TieneFiado { get; set; }
        public decimal Total { get; set; }
        public List<ItemVendido> Items { get; set; } = new();
    }

    public class ItemVendido
    {
        public long ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Unidad { get; set; }
        /// <summary>Unidades, o gramos si es por peso.</summary>
        public decimal Cantidad { get; set; }
        /// <summary>Precio al que se vendió (por unidad, o por kilo si es por peso).</summary>
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public bool EsVario { get; set; }
    }

    /// <summary>Solo para leer la hora de la venta como texto (ventas.hora_venta::text).</summary>
    [Supabase.Postgrest.Attributes.Table("ventas")]
    public class VentaHora : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
        public long Id { get; set; }
        [Supabase.Postgrest.Attributes.Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Supabase.Postgrest.Attributes.Column("fecha")]
        public DateTime Fecha { get; set; }
        [Supabase.Postgrest.Attributes.Column("hora_txt")]
        public string? HoraTexto { get; set; }
    }

    public class ReporteRecaudacionDiariaService
    {
        private readonly Supabase.Client _supaClient;

        public ReporteRecaudacionDiariaService(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        /// <summary>
        /// Lee "2026-10-12 22:40:34.69+00" (la hora tal cual la guarda la base, como texto) y la devuelve en UTC.
        /// Se pide como texto porque al convertirla a fecha el cliente de Supabase la corría de hora.
        /// </summary>
        private static DateTime? TextoAUtc(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var t = texto.Trim().Replace(' ', 'T');
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"[+-]\d{2}$")) t += ":00";   // "+00" → "+00:00"
            return DateTimeOffset.TryParse(t, System.Globalization.CultureInfo.InvariantCulture,
                       System.Globalization.DateTimeStyles.AssumeUniversal, out var dto)
                ? dto.UtcDateTime : null;
        }

        private static bool EsFiado(string? nombre) => string.Equals(nombre?.Trim(), "FIADO", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Recaudación del día. Cómo se reconoce cada renglón de "ventas":
        ///  - Movimiento que tiene algún total NEGATIVO = es un pago de deuda.
        ///      negativos  → la entrega (si la forma de pago es FIADO, es "descontado" del sueldo)
        ///      positivos  → renglones de "equilibrio" del sistema de fiados: no son plata, se ignoran
        ///  - Si no: es una venta. Forma FIADO → fiado; el resto → cobrado. Los ajustes por aumento se ignoran.
        /// </summary>
        /// <remarks>
        /// Con turnos cargados, el DÍA va de turno a turno (como "Por turno"): el turno noche que empieza el 09/11
        /// a las 23:00 cuenta entero para el 09/11, y lo vendido el 10/11 de 00:00 a 07:00 NO entra en el 10/11.
        /// Sin turnos, el día es de 00:00 a 24:00 (como antes).
        /// </remarks>
        public async Task<ResumenDia> ObtenerResumenDelDia(DateTime fecha, long kioscoId, List<Turno>? turnos = null, int minutosUtc = 180)
        {
            var r = new ResumenDia();
            var dia = fecha.Date;
            bool porJornada = turnos != null && turnos.Any(t => t.Activo);
            DateTime desde = dia, hasta = porJornada ? dia.AddDays(2) : dia.AddDays(1);   // la madrugada siguiente es del turno noche de hoy

            var datos = await CargarDatosAsync(kioscoId, desde, hasta);
            if (!datos.Filas.Any()) return r;
            var filas = datos.Filas; var horas = datos.Horas; var movsPago = datos.MovsPago;
            var formas = datos.Formas; var usuarios = datos.Usuarios;

            var medios = new Dictionary<string, FilaMedioPago>(StringComparer.OrdinalIgnoreCase);
            var empleados = new Dictionary<Guid, FilaEmpleado>();
            var sinUsuario = new FilaEmpleado { Nombre = "Sin usuario" };
            var ventasPorEmpleado = new Dictionary<FilaEmpleado, HashSet<long>>();

            FilaMedioPago Medio(string nombre)
            {
                if (!medios.TryGetValue(nombre, out var m)) medios[nombre] = m = new FilaMedioPago { Nombre = nombre };
                return m;
            }
            FilaEmpleado Empleado(Guid? id)
            {
                if (id == null) return sinUsuario;
                if (!empleados.TryGetValue(id.Value, out var e))
                {
                    usuarios.TryGetValue(id.Value, out var u);
                    empleados[id.Value] = e = new FilaEmpleado { UsuarioId = id, Nombre = u?.Username ?? "Usuario borrado", Rol = u?.Rol };
                }
                return e;
            }

            var ventasDelDia = new HashSet<long>();
            foreach (var f in filas)
            {
                if (porJornada && !EsDeLaJornada(f, dia, horas, turnos!, minutosUtc)) continue;
                string forma = formas.TryGetValue(f.FormaPagoId, out var n) ? n : "Sin especificar";
                bool fiado = EsFiado(forma);
                var emp = Empleado(f.UsuarioOperadorId);

                if (movsPago.Contains(f.NumeroUnicoIdentificatorioMovimiento))
                {
                    if (f.Total >= 0) continue;                // equilibrio del sistema de fiados
                    decimal monto = Math.Abs(f.Total);
                    if (fiado) { r.Descontado += monto; emp.Descontado += monto; }
                    else
                    {
                        r.CobrosDeudas += monto; emp.CobrosDeudas += monto; Medio(forma).CobrosDeudas += monto;
                        emp.PorMedio[forma] = emp.PorMedio.GetValueOrDefault(forma) + monto;
                    }
                    continue;
                }

                if (f.EsAjustePrecio == true) continue;       // ajuste por aumento: no es una venta
                ventasDelDia.Add(f.NumeroUnicoIdentificatorioMovimiento);
                if (!ventasPorEmpleado.TryGetValue(emp, out var set)) ventasPorEmpleado[emp] = set = new HashSet<long>();
                set.Add(f.NumeroUnicoIdentificatorioMovimiento);

                if (fiado) { r.Fiado += f.Total; emp.Fiado += f.Total; }
                else { r.VentasCobradas += f.Total; emp.Cobrado += f.Total; Medio(forma).Ventas += f.Total; }
                emp.PorMedio[forma] = emp.PorMedio.GetValueOrDefault(forma) + f.Total;

                // el ticket de esa venta (una venta puede tener varias formas de pago)
                var ticket = emp.Ventas.FirstOrDefault(t => t.Movimiento == f.NumeroUnicoIdentificatorioMovimiento);
                if (ticket == null) emp.Ventas.Add(ticket = new TicketVenta { Movimiento = f.NumeroUnicoIdentificatorioMovimiento });
                ticket.Total += f.Total;
                if (horas.TryGetValue(f.Id, out var utc) && (ticket.HoraUtc == null || utc < ticket.HoraUtc)) ticket.HoraUtc = utc;
                ticket.TieneFiado |= fiado;
                ticket.FormaPago = string.IsNullOrEmpty(ticket.FormaPago) ? forma
                    : ticket.FormaPago.Contains(forma, StringComparison.OrdinalIgnoreCase) ? ticket.FormaPago : ticket.FormaPago + " + " + forma;
            }

            r.CantidadVentas = ventasDelDia.Count;
            foreach (var kv in ventasPorEmpleado) kv.Key.CantidadVentas = kv.Value.Count;

            r.Medios = medios.Values.OrderByDescending(m => m.Total).ToList();
            var lista = empleados.Values.ToList();
            if (sinUsuario.TotalVendido != 0 || sinUsuario.CobrosDeudas != 0 || sinUsuario.Descontado != 0) lista.Add(sinUsuario);
            r.Empleados = lista.OrderByDescending(e => e.TotalVendido).ToList();
            return r;
        }


        /// <summary>
        /// ¿Ese renglón es de la jornada "dia"? Por la hora: el turno noche cuenta para el día en que empezó;
        /// fuera de los turnos, por la fecha de esa hora. Sin hora (ventas viejas): por la fecha de la venta.
        /// </summary>
        private static bool EsDeLaJornada(Venta f, DateTime dia, Dictionary<long, DateTime> horas, List<Turno> turnos, int minutosUtc)
        {
            if (horas.TryGetValue(f.Id, out var utc))
                return AsistenciaService.Ubicar(utc.AddMinutes(-minutosUtc), turnos).jornada == dia;
            return f.Fecha.Date == dia;
        }

        // ===================== CARGA COMÚN (día o rango) =====================
        private sealed record Datos(List<Venta> Filas, Dictionary<long, DateTime> Horas, HashSet<long> MovsPago,
                                    Dictionary<long, string> Formas, Dictionary<Guid, Usuario> Usuarios);

        private async Task<Datos> CargarDatosAsync(long kioscoId, DateTime desde, DateTime hasta)
        {
            // 1) renglones del día (de a 1000: es el máximo que devuelve Supabase por consulta)
            var filas = new List<Venta>();
            for (int i = 0; ; i += 1000)
            {
                var res = await _supaClient.From<Venta>()
                    .Select("*")
                    .Where(v => v.KioscoId == kioscoId)
                    .Where(v => v.Fecha >= desde)
                    .Where(v => v.Fecha < hasta)
                    .Order("id", Ordering.Ascending)
                    .Range(i, i + 999)
                    .Get();
                var lote = res.Models ?? new List<Venta>();
                filas.AddRange(lote);
                if (lote.Count < 1000) break;
            }
            if (!filas.Any()) return new Datos(filas, new(), new(), new(), new());

            // hora de cada renglón, como texto (ver TextoAUtc)
            var horas = new Dictionary<long, DateTime>();
            try
            {
                for (int i = 0; ; i += 1000)
                {
                    var res = await _supaClient.From<VentaHora>()
                        .Select("id, hora_txt:hora_venta::text")
                        .Where(v => v.KioscoId == kioscoId)
                        .Where(v => v.Fecha >= desde)
                        .Where(v => v.Fecha < hasta)
                        .Order("id", Ordering.Ascending)
                        .Range(i, i + 999)
                        .Get();
                    var lote = res.Models ?? new List<VentaHora>();
                    foreach (var h in lote)
                        if (TextoAUtc(h.HoraTexto) is DateTime utc) horas[h.Id] = utc;
                    if (lote.Count < 1000) break;
                }
            }
            catch { /* sin la columna hora_venta: las ventas quedan "sin hora" */ }

            // 2) qué movimientos son pagos de deuda (tienen un renglón negativo, aunque sea de otro día)
            var movs = filas.Select(f => f.NumeroUnicoIdentificatorioMovimiento).Distinct().ToList();
            var movsPago = new HashSet<long>(filas.Where(f => f.Total < 0).Select(f => f.NumeroUnicoIdentificatorioMovimiento));
            foreach (var tanda in movs.Where(m => !movsPago.Contains(m)).Chunk(150))
            {
                var res = await _supaClient.From<Venta>()
                    .Select("id, numero_unico_identificatorio_movimiento")
                    .Filter("numero_unico_identificatorio_movimiento", Operator.In, tanda.Cast<object>().ToList())
                    .Filter("total", Operator.LessThan, 0)
                    .Get();
                foreach (var v in res.Models ?? new()) movsPago.Add(v.NumeroUnicoIdentificatorioMovimiento);
            }

            // 3) nombres de formas de pago y usuarios
            var formas = (await _supaClient.From<FormaPago>().Where(f => f.KioscoId == kioscoId).Get()).Models
                         .ToDictionary(f => f.Id, f => f.Nombre ?? "Sin nombre");
            var usuarios = (await _supaClient.From<Usuario>().Select("id, username, rol, id_kiosco").Where(u => u.IdKiosco == kioscoId).Get()).Models
                           .ToDictionary(u => u.Id, u => u);

            return new Datos(filas, horas, movsPago, formas, usuarios);
        }

        /// <summary>Tipo de cada renglón de ventas (null = no cuenta: equilibrio de fiados o ajuste por aumento).</summary>
        private static string? Tipo(Venta f, bool fiado, HashSet<long> movsPago)
        {
            if (movsPago.Contains(f.NumeroUnicoIdentificatorioMovimiento))
                return f.Total >= 0 ? null : (fiado ? "descontado" : "cobro");
            if (f.EsAjustePrecio == true) return null;
            return fiado ? "fiado" : "venta";
        }

        // ===================== POR TURNO =====================
        /// <summary>
        /// Recaudación del día separada por los turnos fijos del negocio, según la HORA de cada venta
        /// (el turno noche cuenta entero para el día en que empezó). minutosUtc: diferencia del equipo con UTC (Argentina 180).
        /// </summary>
        public async Task<List<FilaTurno>> ObtenerPorTurnoAsync(DateTime fecha, long kioscoId, List<Turno> turnos, int minutosUtc)
        {
            var dia = fecha.Date;
            // se trae también el día siguiente: la madrugada es parte del turno noche de hoy
            var d = await CargarDatosAsync(kioscoId, dia, dia.AddDays(2));
            var filasTurno = turnos.Where(t => t.Activo).Select(t => new FilaTurno { Nombre = t.Nombre, Rango = t.Rango(), TurnoId = t.Id }).ToList();
            var fuera = new FilaTurno { Nombre = "Fuera de los turnos", Rango = "" };
            var sinHora = new FilaTurno { Nombre = "Sin hora", Rango = "ventas viejas" };
            var ventasPorFila = new Dictionary<FilaTurno, HashSet<long>>();

            foreach (var f in d.Filas)
            {
                string forma = d.Formas.TryGetValue(f.FormaPagoId, out var n) ? n : "Sin especificar";
                var tipo = Tipo(f, EsFiado(forma), d.MovsPago);
                if (tipo == null) continue;

                FilaTurno fila;
                if (d.Horas.TryGetValue(f.Id, out var utc))
                {
                    var local = utc.AddMinutes(-minutosUtc);
                    var (turno, jornada) = AsistenciaService.Ubicar(local, turnos);
                    if (jornada != dia) continue;
                    fila = turno == null ? fuera : filasTurno.First(x => x.TurnoId == turno.Id);
                }
                else
                {
                    if (f.Fecha.Date != dia) continue;
                    fila = sinHora;
                }

                decimal monto = Math.Abs(f.Total);
                switch (tipo)
                {
                    case "venta": fila.Cobrado += monto; break;
                    case "fiado": fila.Fiado += monto; break;
                    case "cobro": fila.CobrosDeudas += monto; break;
                    case "descontado": fila.Descontado += monto; break;
                }

                string quien = f.UsuarioOperadorId is Guid g && d.Usuarios.TryGetValue(g, out var u) ? (u.Username ?? "?") : "Sin usuario";
                if (!fila.Empleados.TryGetValue(quien, out var et)) fila.Empleados[quien] = et = new EmpleadoTurno { Nombre = quien };

                if (tipo is "venta" or "fiado")
                {
                    if (!ventasPorFila.TryGetValue(fila, out var set)) ventasPorFila[fila] = set = new HashSet<long>();
                    set.Add(f.NumeroUnicoIdentificatorioMovimiento);

                    fila.PorEmpleado[quien] = fila.PorEmpleado.GetValueOrDefault(quien) + monto;
                    et.Movimientos.Add(f.NumeroUnicoIdentificatorioMovimiento);
                    if (tipo == "venta") et.Cobrado += monto; else et.Fiado += monto;
                }
                if (tipo == "cobro") et.CobrosDeudas += monto;
                if (tipo is "venta" or "cobro" or "fiado")
                    et.PorMedio[forma] = et.PorMedio.GetValueOrDefault(forma) + monto;
                if (tipo is "venta" or "cobro")
                    fila.PorMedio[forma] = fila.PorMedio.GetValueOrDefault(forma) + monto;
            }

            foreach (var kv in ventasPorFila) kv.Key.CantidadVentas = kv.Value.Count;
            var lista = filasTurno.ToList();
            if (fuera.TotalVendido + fuera.CobrosDeudas > 0) lista.Add(fuera);
            if (sinHora.TotalVendido + sinHora.CobrosDeudas > 0) lista.Add(sinHora);
            return lista;
        }

        /// <summary>Trae los productos de las ventas del empleado, con el precio al que se vendió cada uno.</summary>
        public async Task CargarProductosAsync(FilaEmpleado emp, long kioscoId)
        {
            if (emp.ProductosCargados || !emp.Ventas.Any()) { emp.ProductosCargados = true; return; }

            var porMov = emp.Ventas.ToDictionary(t => t.Movimiento);
            var detalles = new List<VentaDetalle>();
            var varios = new List<ProductoVarioVenta>();
            foreach (var tanda in porMov.Keys.Chunk(150))
            {
                var ids = tanda.Cast<object>().ToList();
                var resDet = await _supaClient.From<VentaDetalle>()
                    .Select("id, producto_id, cantidad, precio_unitario, subtotal, created_at, numero_unico_identificatorio_movimiento")
                    .Filter("numero_unico_identificatorio_movimiento", Operator.In, ids)
                    .Get();
                detalles.AddRange(resDet.Models ?? new());

                var resVar = await _supaClient.From<ProductoVarioVenta>()
                    .Where(v => v.KioscoId == kioscoId)
                    .Filter("numero_unico_identificatorio_movimiento", Operator.In, ids)
                    .Get();
                varios.AddRange(resVar.Models ?? new());
            }

            // nombres y unidad de los productos
            var productos = new Dictionary<long, Producto>();
            foreach (var tanda in detalles.Select(d => d.ProductoId).Distinct().Chunk(150))
            {
                var res = await _supaClient.From<Producto>()
                    .Select("id, nombre, unidad")
                    .Filter("id", Operator.In, tanda.Cast<object>().ToList())
                    .Get();
                foreach (var p in res.Models ?? new()) productos[p.Id] = p;
            }

            foreach (var d in detalles)
            {
                if (!porMov.TryGetValue(d.NumeroUnicoIdentificatorioMovimiento, out var t)) continue;
                productos.TryGetValue(d.ProductoId, out var p);
                t.Items.Add(new ItemVendido
                {
                    ProductoId = d.ProductoId,
                    Nombre = p?.Nombre ?? $"Producto #{d.ProductoId}",
                    Unidad = p?.UnidadMedida,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                });
            }
            foreach (var v in varios)
            {
                if (!porMov.TryGetValue(v.NumeroUnicoIdentificatorioMovimiento, out var t)) continue;
                t.Items.Add(new ItemVendido
                {
                    Nombre = string.IsNullOrWhiteSpace(v.NombreProducto) ? "Varios" : v.NombreProducto!,
                    Cantidad = v.Cantidad,
                    PrecioUnitario = v.PrecioUnitario,
                    Subtotal = v.Subtotal,
                    EsVario = true
                });
            }

            emp.Ventas = emp.Ventas.OrderByDescending(t => t.HoraUtc ?? DateTime.MinValue).ThenByDescending(t => t.Movimiento).ToList();
            emp.ProductosCargados = true;
        }

        // ---------- métodos anteriores (se dejan por compatibilidad) ----------

        /// <summary>Obtiene el total recaudado para un día específico y un kiosco determinado.</summary>
        public async Task<decimal> ObtenerRecaudacionPorDia(DateTime fechaSeleccionada, long kioscoId)
            => (await ObtenerResumenDelDia(fechaSeleccionada, kioscoId)).TotalRecaudado;

        public async Task<List<RecaudacionPorFormaPagoDto>> ObtenerRecaudacionPorFormaPago(DateTime fecha, long kioscoId)
            => (await ObtenerResumenDelDia(fecha, kioscoId)).Medios
                .Select(m => new RecaudacionPorFormaPagoDto { FormaPago = m.Nombre, Total = m.Total })
                .ToList();
    }
}
