using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Models.DTO;
using BlazorKioscoCompartido.Models.Vistas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Services.Reportes
{
    public class ReporteRecaudacionDiariaService
    {
        private readonly Supabase.Client _supaClient;

        public ReporteRecaudacionDiariaService(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        /// <summary>
        /// Obtiene el total recaudado para un día específico y un kiosco determinado.
        /// </summary>
        public async Task<decimal> ObtenerRecaudacionPorDia(DateTime fechaSeleccionada, long kioscoId)
        {
            try
            {
                // Definimos el inicio del día (00:00:00) y el final (23:59:59)
                DateTime inicioDia = fechaSeleccionada.Date;
                DateTime finDia = fechaSeleccionada.Date.AddDays(1).AddTicks(-1);

                // Hacemos la consulta a Supabase emulando tu consulta SQL
                var response = await _supaClient.From<Venta>()
                    .Where(v => v.KioscoId == kioscoId)
                    .Where(v => v.Fecha >= inicioDia)
                    .Where(v => v.Fecha <= finDia)
                    .Where(v => v.Pagado == true)
                    .Get();

                // Sumamos el total de las ventas recuperadas
                if (response.Models != null && response.Models.Count > 0)
                {
                    return response.Models.Sum(v => v.Total);
                }

                return 0m;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular recaudación: {ex.Message}");
                return 0m;
            }
        }
        public async Task<List<RecaudacionPorFormaPagoDto>> ObtenerRecaudacionPorFormaPago(DateTime fecha, long kioscoId)
        {
            try
            {
                DateTime fechaInicio = fecha.Date;
                DateTime fechaFin = fecha.Date.AddDays(1).AddTicks(-1);



                // Si usás PostgREST / Supabase C# Client directo uniendo tablas:
                var respuesta = await _supaClient
                    .From<ResumenFormaPago>()
                    .Where(x => x.KioscoId == kioscoId)
                    .Where(x => x.Fecha >= fechaInicio.Date)
                    .Where(x => x.Fecha <= fechaFin.Date)
                    .Get();

                // Agrupamos en C# por el nombre del medio de pago
                var resultado = respuesta.Models
                    .GroupBy(v => v.FormaPago ?? "Sin Especificar")
                    .Select(g => new RecaudacionPorFormaPagoDto
                    {
                        FormaPago = g.Key,
                        Total = g.Sum(x => x.TotalRecaudado)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener desglose de pago: {ex.Message}");
                return new List<RecaudacionPorFormaPagoDto>();
            }
        }
    }
}
