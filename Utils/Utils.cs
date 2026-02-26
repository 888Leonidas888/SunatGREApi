using System.Globalization;
using System.Text.RegularExpressions;

namespace SunatGreApi.Utils
{
    public static class SunatHelper
    {
        public static string GetPatron(string texto, string pattron)
        {
            Match match = Regex.Match(texto, pattron);

            if (match.Success)
                return match.Value;
            return string.Empty;
        }
        public static string GetPartida(string texto)
        {
            // Buscamos patrones como B8175, B9525, etc. 
            string pattron = @"\b[A-Za-z]\d{4}\b"; 
            return GetPatron(texto, pattron);
        }

        public static string GetOrdenCompra(string texto)
        {
            string pattron = @"[0-2]{3}-[0-9]{6}"; //Ejemplo: 100-185350
            return GetPatron(texto, pattron);
        }

        public static int GetRollos(string texto)
        {
            // Buscamos "Rollos: 123" o "Rollos: 123" o "Rollos 123"
            var patron = @"\brollos?\b\s*:\s*(\d+)";
            var m = Regex.Match(texto, patron, RegexOptions.IgnoreCase);

            if (m.Success)
            {
                if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero))
                    return numero;
            }
            return 0;
        }

        public static double GetPesoBruto(string texto)
        {
            // Buscamos "P.Bruto : 1712.550"
            var patron = @"\bP\.Bruto\b[:\s]+([\d.]+)";
            var m = Regex.Match(texto, patron, RegexOptions.IgnoreCase);

            if (m.Success)
            {
                if (double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double peso))
                    return peso;
            }
            return 0;
        }
    }
}
