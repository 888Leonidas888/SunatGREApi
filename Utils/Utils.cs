using System.Globalization;
using System.Text.RegularExpressions;

namespace SunatGreApi.Utils
{
    public static class SunatHelper
    {
        /// <summary>
        /// Obtiene el valor de un patrón en un texto.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <param name="pattron">El patrón a buscar.</param>
        /// <returns>El valor del patrón si se encuentra, de lo contrario, una cadena vacía.</returns>
        public static string GetPatron(string texto, string pattron)
        {
            Match match = Regex.Match(texto, pattron);

            if (match.Success)
                return match.Value;
            return string.Empty;
        }

        /// <summary>
        /// Obtiene el número de partida de una guía de remisión.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <returns>El número de partida si se encuentra, de lo contrario, una cadena vacía.</returns>
        public static string GetPartida(string texto)
        {
            // Buscamos patrones como B8175, B9525, etc. 
            string pattron = @"\b[A-Za-z]\d{4}\b"; 
            return GetPatron(texto, pattron);
        }

        /// <summary>
        /// Obtiene el número de orden de compra de una guía de remisión.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <returns>El número de orden de compra si se encuentra, de lo contrario, una cadena vacía.</returns>
        public static string GetOrdenCompra(string texto)
        {
            string pattron = @"[0-2]{3}-[0-9]{6}"; //Ejemplo: 100-185350
            return GetPatron(texto, pattron);
        }

        /// <summary>
        /// Obtiene el número de rollos de una guía de remisión.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <returns>El número de rollos si se encuentra, de lo contrario, 0.</returns>
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

        /// <summary>
        /// Obtiene el peso bruto de una guía de remisión.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <returns>El peso bruto si se encuentra, de lo contrario, 0.</returns>   
        public static double GetPesoBruto(string texto)
        {
            var patron = @"\s?(?:-?PB|P.Bruto)\s*[\.\:]?\s?([0-9]+(?:[.,][0-9]+)?)";
            Match m = Regex.Match(texto, patron, RegexOptions.IgnoreCase);

            if (m.Success)
            {
                var valor = m.Groups[1].Value.Replace(',', '.'); // Normaliza coma a punto
                //Console.WriteLine(valor);

                if (double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double peso))
                    return peso;
            }
            return 0;
        }

        /// <summary>
        /// Obtiene el nombre comercial de una guía de remisión.
        /// </summary>
        /// <param name="texto">El texto de la guía de remisión.</param>
        /// <returns>El nombre comercial si se encuentra, de lo contrario, una cadena vacía.</returns>
        public static string GetNombreComercial(string texto){
            string pattron = @"^.*(?=\s*Color\s*:)";
            return GetPatron(texto, pattron);
        }
    }
}
