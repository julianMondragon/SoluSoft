using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
        public class EmpresaViewModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres")]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El RFC es obligatorio")]
            [RegularExpression(
                    @"^([A-ZÑ&]{3,4})\d{6}[A-Z0-9]{3}$",
                    ErrorMessage = "RFC no válido"
                )]
            [Display(Name = "RFC")]
            public string RFC { get; set; }

            [Required(ErrorMessage = "La dirección es obligatoria")]
            [StringLength(200, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 200 caracteres")]
            [Display(Name = "Dirección")]
            public string Direccion { get; set; }

            [Required(ErrorMessage = "Débe crearlo un certificador")]
            public int CertificadorId { get; set; }
        }
    
}