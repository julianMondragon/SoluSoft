using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class ContactoSSFViewModel
    {

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }


        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        [Display(Name = "Correo")]
        public string Correo { get; set; }


        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }


        [Required(ErrorMessage = "Seleccione un servicio")]
        [Display(Name = "Servicio")]
        public string Servicio { get; set; }


        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [Display(Name = "Mensaje")]
        public string Mensaje { get; set; }

    }
}