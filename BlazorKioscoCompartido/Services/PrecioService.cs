using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorKioscoCompartido.Models;
using Supabase;

namespace BlazorKioscoCompartido.Services
{
    public class PrecioService
    {
        private readonly Supabase.Client _supaClient;

        // Inyectamos el cliente de Supabase por constructor
        public PrecioService(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        /// <summary>
        /// Determina el nombre de la columna de precio activa según el kiosco y la hora actual.
        /// </summary>
        public async Task<string> ObtenerColumnaPrecioActivaAsync(long idNegocio, TimeSpan horaInicioVenta)
        {
            // Nombre por defecto si no hay reglas
            string columnaPrecioActiva = "precio_venta";

            try
            {
                // 1. Buscamos la regla horaria
                var res = await _supaClient
                    .From<HorarioVenta>()
                    .Where(x => x.KioscoId == idNegocio)
                    .Filter(
                        "hora_inicio",
                        Supabase.Postgrest.Constants.Operator.LessThanOrEqual,
                        horaInicioVenta.ToString(@"hh\:mm\:ss")
                    )
                    .Filter(
                        "hora_fin",
                        Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual,
                        horaInicioVenta.ToString(@"hh\:mm\:ss")
                    )
                    .Get();

                var reglaHoraria = res.Models.FirstOrDefault();

                // 2. Si hay regla de tipo LISTA, buscamos la columna correspondiente
                if (reglaHoraria != null && reglaHoraria.TipoAjuste == "LISTA")
                {
                    long idListaABuscar = reglaHoraria.ListaPrecioId;

                    var resLista = await _supaClient.From<ListaPrecio>()
                                   .Where(l => l.KioscoId == idNegocio && l.Id == idListaABuscar)
                                   .Get();

                    var listaConfig = resLista.Models.FirstOrDefault();
                    if (listaConfig != null)
                    {
                        columnaPrecioActiva = listaConfig.NombreColumnaProductos;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PrecioService: {ex.Message}");
                // Retorna "precio_venta" como fallback seguro ante fallos
            }

            return columnaPrecioActiva;
        }

        public async Task<string> ObtenerNombreColumnaAsync(long idNegocio, string nombre_columna_en_tabla_productos)
        {
            string nombreColumna = "";
            try
            {
                // Ejecutamos el equivalente de tu query SQL en Supabase
                var res = await _supaClient.From<ListaPrecio>()
                    .Where(l => l.KioscoId == idNegocio && l.NombreColumnaProductos == nombre_columna_en_tabla_productos)
                    .Get();

                var lista = res.Models.FirstOrDefault();
                if (lista != null && !string.IsNullOrEmpty(lista.NombreLista))
                {
                    nombreColumna = lista.NombreLista; // Asignamos el nombre real (ej: "Noche")
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el nombre de la lista: {ex.Message}");
            }
            return nombreColumna;
        }



        }
}
