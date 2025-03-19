using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class HikVisionViewModel
    {
        [Required]
        [Display(Name = "API Server")]
        public string APIServer { get; set; }
        [Required]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; }
        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Petición (GET,POST,PUT...)")]
        public string Peticion { get; set; }

        [Display(Name = "Respuesta")]
        public string Respuesta { get; set; }

        [Display(Name = "Historial")]
        public string Historial { get; set; }

        [Display(Name = "Parámetro (Opcional)")]
        [RegularExpression(@"^(\d+|(\{.*\}))$", ErrorMessage = "La petición debe ser un número o un JSON válido.")]
        public string Parametro { get; set; }
    }
}