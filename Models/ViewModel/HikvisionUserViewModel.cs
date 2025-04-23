using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class HikvisionUserViewModel
    {
        [Required]
        [Display(Name = "Nombre completo")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Número de empleado")]
        public string EmployeeNo { get; set; }

        [Display(Name = "Tipo de usuario")]
        public string UserType { get; set; } = "normal";

        [Display(Name = "Inicio de validez")]
        [DataType(DataType.DateTime)]
        public DateTime BeginTime { get; set; } = DateTime.Now;

        [Display(Name = "Fin de validez")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; } = DateTime.Now.AddYears(1);

        [Display(Name = "Acceso a puerta")]
        public string DoorRight { get; set; } = "1";

        [Display(Name = "Número de habitación")]
        public string RoomNumber { get; set; }

        [Display(Name = "Genero")]
        public string gender { get; set; }

        [Display(Name = "# de Tags")]
        public int numOfCard { get; set; }

        [Display(Name = "# de Huellas")]
        public int numOfFP { get; set; }

        [Display(Name = "# key Facial")]
        public int numOfFace { get; set; }

        [Display(Name = "Es Administrador")]
        public bool isAdmin { get; set; }
    }
}