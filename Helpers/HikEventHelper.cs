using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using TAS360.Models.ViewModel;

namespace TAS360.Helpers
{
    public static class HikEventHelper
    {
        private static readonly Dictionary<int, string> MajorText = new Dictionary<int, string>
        {
            { 5, "Control de acceso" },
            { 3, "Sistema/Operación" },
        };

        // Defaults “genéricos” (se pueden sobreescribir por JSON)
        private static Dictionary<int, Dictionary<int, string>> _minorTextByMajor =
            new Dictionary<int, Dictionary<int, string>>
            {
                { 5, new Dictionary<int, string>
                    {
                        { 7,   "Acceso por tarjeta válido" },
                        { 39,  "Acceso por rostro válido" },
                        { 75,  "Acceso válido" },
                        { 112, "Falló autenticación huella" },
                        { 113, "Falló autenticación rostro" },
                        { 114, "Falló autenticación tarjeta" },
                        { 204, "Duración no válida" }
                    }
                },
                { 3, new Dictionary<int, string>
                    {
                        { 1,   "Inicio de sesión" },
                        { 2,   "Cierre de sesión" },
                        { 112, "Operación remota" }
                    }
                }
            };

        private static readonly Dictionary<string, string> VerifyModeText =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "face",               "rostro" },
                { "fingerprint",        "huella" },
                { "card",               "tarjeta" },
                { "password",           "contraseña" },
                { "faceOrFpOrCardOrPw", "rostro/huella/tarjeta/contraseña" },
                { "invalid",            "método inválido" },
            };

        private static bool _customLoaded = false;

        /// Carga App_Data/hik_minor_map.json y mezcla con los defaults
        private static void EnsureCustomMapLoaded()
        {
            if (_customLoaded) return;
            try
            {
                var path = HttpContext.Current?.Server.MapPath("~/App_Data/hik_minor_map.json");
                if (path != null && File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var custom = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

                    if (custom != null)
                    {
                        foreach (var majorKV in custom)
                        {
                            if (!int.TryParse(majorKV.Key, out var majorInt)) continue;

                            if (!_minorTextByMajor.ContainsKey(majorInt))
                                _minorTextByMajor[majorInt] = new Dictionary<int, string>();

                            foreach (var minorKV in majorKV.Value)
                            {
                                if (!int.TryParse(minorKV.Key, out var minorInt)) continue;
                                _minorTextByMajor[majorInt][minorInt] = minorKV.Value; // override/add
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silencioso: si hay error de lectura, seguimos con defaults
            }
            _customLoaded = true;
        }

        public static string GetMajorText(int major)
        {
            EnsureCustomMapLoaded();
            return MajorText.TryGetValue(major, out var t) ? t : $"Major {major}";
        }

        public static string GetMinorText(int major, int minor)
        {
            EnsureCustomMapLoaded();

            if (_minorTextByMajor.TryGetValue(major, out var minors) &&
                minors.TryGetValue(minor, out var txt))
                return txt;

            return $"Minor {minor}";
        }

        public static string GetVerifyModeText(string verifyMode)
        {
            if (string.IsNullOrWhiteSpace(verifyMode)) return "";
            return VerifyModeText.TryGetValue(verifyMode, out var t) ? t : verifyMode;
        }

        /// Descripción amigable (respetando tu firmware)
        public static string Describe(InfoItem e)
        {
            if (e == null) return "";

            var majorTxt = GetMajorText(e.major);
            var minorCode = e.minor ?? -1;
            var minorTxt = GetMinorText(e.major, minorCode);
            var vmTxt = GetVerifyModeText(e.currentVerifyMode);

            var actor = !string.IsNullOrWhiteSpace(e.name) ? e.name
                      : !string.IsNullOrWhiteSpace(e.employeeNo) ? $"emp {e.employeeNo}"
                      : !string.IsNullOrWhiteSpace(e.cardNo) ? $"tarjeta {e.cardNo}"
                      : "-";

            string action = minorTxt;

            // Si no hay mapeo exacto, apóyate en verify mode
            if (action.StartsWith("Minor "))
            {
                action = !string.IsNullOrEmpty(vmTxt)
                    ? $"Intento de acceso por {vmTxt} (código {minorCode})"
                    : $"Evento {majorTxt} (código {minorCode})";
            }
            else if (!string.IsNullOrEmpty(vmTxt) &&
                     (action.Contains("Acceso") || action.Contains("autenticación")))
            {
                //if (!action.Contains(vmTxt, StringComparison.OrdinalIgnoreCase))
                //    action = $"{action} por {vmTxt}";
            }

            return actor == "-" ? action : $"{action} ({actor})";
        }

        /// Hora formateada como el dashboard (usa el offset que manda el equipo)
        public static string FormatTime(string timeIso)
        {
            if (string.IsNullOrWhiteSpace(timeIso)) return "-";
            if (DateTimeOffset.TryParse(timeIso, out var dto))
                return dto.ToString("yyyy-MM-dd HH:mm:ss"); // mismo formato del dashboard
            return timeIso;
        }

        /// Tooltip de depuración (para cazar minors/verifyMode reales)
        public static string DebugTip(InfoItem e)
        {
            var vm = string.IsNullOrEmpty(e.currentVerifyMode) ? "-" : e.currentVerifyMode;
            var m = e.minor.HasValue ? e.minor.Value.ToString() : "-";
            return $"major={e.major}, minor={m}, verifyMode={vm}";
        }
    }
}
