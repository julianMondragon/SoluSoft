namespace TAS360.Services
{
    /// <summary>
    /// Representa el resultado del chequeo de disponibilidad realizado sobre un dispositivo Hikvision.
    /// </summary>
    public sealed class DeviceCheckResult
    {
        /// <summary>
        /// Indica si el dispositivo respondió satisfactoriamente dentro del tiempo de espera configurado.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Código de estado HTTP obtenido durante la verificación, si la respuesta incluyó uno.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Tiempo en milisegundos que tardó el dispositivo en responder a la solicitud de verificación.
        /// </summary>
        public long LatencyMs { get; set; }

        /// <summary>
        /// Mensaje descriptivo del error producido durante la verificación, si esta falló.
        /// </summary>
        public string Error { get; set; }
    }
}
