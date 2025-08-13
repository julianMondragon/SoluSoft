using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class HikvisionPuntosAccesoViewModel
    {
        [Key]
        public int id { get; set; }

        [Required]
        [Display(Name = "Plantel")]
        public int idPlantel { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [StringLength(200)]
        [Display(Name = "Nombre Plantel")]
        public string NombrePlantel { get; set; }

        [StringLength(600)]
        [Display(Name = "Ubicación")]
        public string ubicacion { get; set; }

        [Display(Name = "Fecha")]
        public DateTime? FechaHoraInstalacion { get; set; }

        [Display(Name = "Estado")]
        public bool estado { get; set; } = true;

        [Required]
        [StringLength(400)]
        [Display(Name = "APIServer")]
        public string apiServer { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Usuario")]
        public string usuario { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "PassWord")]
        public string password { get; set; }
    }
}