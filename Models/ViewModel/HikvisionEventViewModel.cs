using System;

namespace TAS360.Models.ViewModel
{
    /// <summary>
    /// Representa un evento generado por un dispositivo Hikvision a través del API de control de acceso.
    /// </summary>
    public class HikvisionEventViewModel
    {
        /// <summary>
        /// Identificador del empleado reportado por el dispositivo.
        /// </summary>
        public string EmployeeNo { get; set; }

        /// <summary>
        /// Nombre completo de la persona asociada al evento.
        /// </summary>
        public string PersonName { get; set; }

        /// <summary>
        /// Número de tarjeta leído por el dispositivo, cuando aplica.
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// Tipo principal del evento reportado (ej. acceso, alarma, etc.).
        /// </summary>
        public string MajorEventType { get; set; }

        /// <summary>
        /// Clasificación específica del evento dentro del tipo principal.
        /// </summary>
        public string MinorEventType { get; set; }

        /// <summary>
        /// Fecha y hora en la que el evento fue generado por el dispositivo.
        /// </summary>
        public DateTime? EventTime { get; set; }

        /// <summary>
        /// Texto descriptivo adicional asociado al evento (puerta, lector, etc.).
        /// </summary>
        public string SourceName { get; set; }
    }
}
