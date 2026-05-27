namespace BlazorKioscos_v1.Services
{
    public class FunctionAux
    {

        public string GenerarCodigoNegocio()
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Sin '0', 'O', '1', 'I' para evitar confusiones
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
