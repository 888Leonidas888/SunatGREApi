using System.Globalization;
using System.Text.RegularExpressions;

namespace SunatGreApi.Utils
{
    public static class Utils
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
            string pattron = @"[A-Za-z]{1}[0-9]{4}"; //Ejemplo: B8466
            return GetPatron(texto, pattron);
        }

        public static string GetOrdenCompra(string texto)
        {
            string pattron = @"[0-2]{3}-[0-9]{6}"; //Ejemplo: 100-185350
            return GetPatron(texto, pattron);
        }

        public static int GetRollos(string texto)
        {
            var patron = @"\brollos?\b\s*:\s*(\d+)";
            var m = Regex.Match(texto, patron, RegexOptions.IgnoreCase);

            if (m.Success)
            {
                if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero))
                    return numero;
            }
            return 0;
        }
    }

}
