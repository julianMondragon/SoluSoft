using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
namespace TAS360.Models.ViewModel
{
    public class TutorEstudianteViewModel
    {
        public int Id { get; set; }
        public int Tutor { get; set; }
        public string N_Tutor { get; set; }
        public string N_Estudiante { get; set; }
        public int Estudiante { get; set; }
        public DateTime? fechaHora { get; set; }
    }
}
