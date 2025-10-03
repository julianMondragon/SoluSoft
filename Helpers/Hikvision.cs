using System;
using System.Collections.Generic;
using System.Linq;
using TAS360.Models.ViewModel;

namespace TAS360.Helpers
{
    public class Hikvision
    {
        // Texto por "major"
        private static readonly Dictionary<int, string> MajorText = new Dictionary<int, string>
        {
            { 5, "Control de acceso" },
            { 3, "Sistema/Operación" },
            // agrega otros majors si los usas
        };

        // Diccionario de "minor" por major
        // ⚠️ Nota: los códigos minor varían por firmware. Te dejo mapeos comunes y/o placeholders.
        // Ve ajustando conforme observes en tus respuestas y en la guía ISAPI.
        private static readonly Dictionary<int, Dictionary<int, string>> MinorTextByMajor =
            new Dictionary<int, Dictionary<int, string>>
        {
            // Major 5 = Access Control
            { 5, new Dictionary<int, string>
                {
                    // ✅ Éxitos (ejemplos frecuentes)
                    { 1,  "Acceso concedido (desconocido)" },
                    { 7,  "Acceso por tarjeta válido" },      // ajusta si tu equipo lo usa así
                    { 39, "Acceso por rostro válido" },       // ajuste común en Face series
                    { 75, "Acceso válido" },                  // usado en varios firmwares
                    // ❌ Fallos típicos
                    { 112, "Falló autenticación huella" },
                    { 113, "Falló autenticación rostro" },
                    { 114, "Falló autenticación tarjeta" },
                    { 204, "Duración no válida" },            // “valid period” expirado/no vigente
                    // Agrega aquí los minors que veas en tus JSON
                }
            },

            // Major 3 = Sistema/Operación (ej. “Remoto: inicio de sesión”)
            { 3, new Dictionary<int, string>
                {
                    { 1,   "Inicio de sesión" },
                    { 2,   "Cierre de sesión" },
                    { 112, "Operación remota" },
                    // agrega según lo que observes
                }
            },
        };

        // Traducción “bonita” de VerifyMode cuando viene en el JSON (face/finger/card/…)
        private static readonly Dictionary<string, string> VerifyModeText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "face",                 "rostro" },
            { "fingerprint",          "huella" },
            { "card",                 "tarjeta" },
            { "password",             "contraseña" },
            { "faceOrFpOrCardOrPw",   "rostro/huella/tarjeta/contraseña" },
            { "invalid",              "método inválido" },
        };

        public static string GetMajorText(int major)
            => MajorText.TryGetValue(major, out var t) ? t : $"Major {major}";

        public static string GetMinorText(int major, int minor)
        {
            if (MinorTextByMajor.TryGetValue(major, out var minors) &&
                minors.TryGetValue(minor, out var txt))
                return txt;

            // Si no hay mapeo exacto, devolvemos código legible
            return $"Minor {minor}";
        }

        public static string GetVerifyModeText(string verifyMode)
        {
            if (string.IsNullOrWhiteSpace(verifyMode)) return "";
            return VerifyModeText.TryGetValue(verifyMode, out var t) ? t : verifyMode;
        }

        /// <summary>
        /// Devuelve una descripción amigable combinando major/minor + verifyMode + tarjeta/persona
        /// </summary>
        public static string Describe(InfoItem e)
        {
            if (e == null) return "";

            var majorTxt = GetMajorText(e.major);
            var minorTxt = GetMinorText(e.major, (int)e.minor);
            var vmTxt = GetVerifyModeText(e.currentVerifyMode);

            // Ejemplos de frases:
            // - "Acceso por rostro válido (Julian Mondragon, emp 117)"
            // - "Falló autenticación huella (emp 117)"
            // - "Duración no válida (tarjeta: 123456)"
            var actor = !string.IsNullOrWhiteSpace(e.name) ? e.name
                       : !string.IsNullOrWhiteSpace(e.employeeNo) ? $"emp {e.employeeNo}"
                       : !string.IsNullOrWhiteSpace(e.cardNo.ToString()) ? $"tarjeta {e.cardNo}"
                       : "-";

            // Construcción base
            string action = minorTxt;

            // Si no tenemos minor mapeado, intenta una frase con verify mode
            if (action.StartsWith("Minor "))
            {
                if (!string.IsNullOrEmpty(vmTxt))
                    action = $"Intento de acceso por {vmTxt} (código {e.minor})";
                else
                    action = $"Evento {majorTxt} (código {e.minor})";
            }
            else
            {
                // Si tenemos minor “genérico” como “Acceso válido”, añade el verify mode si existe
                if (!string.IsNullOrEmpty(vmTxt) &&
                    (action.Contains("Acceso") || action.Contains("autenticación")))
                {
                    // Evita duplicar palabras si ya dice "rostro" o "huella" en el texto
                    //if (!action.Contains(vmTxt, StringComparison.OrdinalIgnoreCase))
                    //    action = $"{action} por {vmTxt}";
                }
            }

            return actor == "-" ? action : $"{action} ({actor})";
        }
    }
}