using System.Collections.Generic;

namespace TAS360.Models.ViewModel
{
    /// <summary>
    /// Representa el resultado de una búsqueda de eventos en el dispositivo Hikvision.
    /// </summary>
    public class HikvisionEventResult
    {
        /// <summary>
        /// Lista de eventos devueltos por el dispositivo.
        /// </summary>
        public List<HikvisionEventViewModel> Events { get; set; } = new List<HikvisionEventViewModel>();

        /// <summary>
        /// Número de eventos devueltos en la consulta.
        /// </summary>
        public int NumOfMatches { get; set; }

        /// <summary>
        /// Número total de eventos almacenados que cumplen la condición de búsqueda.
        /// </summary>
        public int TotalMatches { get; set; }
    }

    /// <summary>
    /// Modelo de vista que encapsula un evento proveniente del dispositivo Hikvision.
    /// </summary>
    public class HikvisionEventViewModel
    {
        /// <summary>
        /// Identificador del empleado asociado al evento.
        /// </summary>
        public string EmployeeNo { get; set; }

        /// <summary>
        /// Nombre de la persona asociada al evento.
        /// </summary>
        public string PersonName { get; set; }

        /// <summary>
        /// Número de tarjeta o credencial asociado al evento.
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// Nombre o identificador del acceso en el que ocurrió el evento.
        /// </summary>
        public string DoorName { get; set; }

        /// <summary>
        /// Tipo general del evento reportado por el dispositivo.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Descripción amigable del evento.
        /// </summary>
        public string EventDescription { get; set; }

        /// <summary>
        /// Código combinado del evento (mayor-menor) para referencia rápida.
        /// </summary>
        public string EventCode { get; set; }

        /// <summary>
        /// Fecha y hora del evento representado en formato ISO 8601.
        /// </summary>
        public string EventTime { get; set; }
    }
}
