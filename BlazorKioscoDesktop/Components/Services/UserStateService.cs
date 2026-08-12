using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoDesktop.Components.Services
{
    public class UserStateService
    {
        public string? UsuarioActual { get; private set; }
        public List<string> PermisosMenu { get; private set; } = new();

        public event Action? OnChange;

        public void SetUser(string usuario, List<string> permisos)
        {
            UsuarioActual = usuario;
            PermisosMenu = permisos;
            NotifyStateChanged();
        }

        public void ClearUser()
        {
            UsuarioActual = null;
            PermisosMenu.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
