using BlazorKioscos_v1.Models;

namespace BlazorKioscos_v1.Services
{
    public class AppState
    {
        public int KioscoId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string?NombreNegocio { get; set; }  // Valor inicial
       
    }
}