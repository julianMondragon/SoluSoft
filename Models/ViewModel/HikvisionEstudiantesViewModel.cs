using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class HikvisionEstudiantesViewModel
    {
        public int id { get; set; }

        public int IdEscuela { get; set; }

        public string Nombre { get; set; }

        [StringLength(200)]
        [Display(Name = "Nombre Plantel")]
        public string NombrePlantel { get; set; }

        public string Grado { get; set; }

        public string CorreoPersonal { get; set; }

        //public string CorreoTutor { get; set; }

        public string TelefonoPersonal { get; set; }

        //public string TelefonoTutor { get; set; }

        public string IdExterno { get; set; }

        public DateTime FechaHoraRegistro { get; set; }
    }
}