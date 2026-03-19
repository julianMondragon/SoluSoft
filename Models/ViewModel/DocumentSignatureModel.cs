using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class DocumentSignatureViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Display(Name = "Puesto")]
        public string Position { get; set; }

        [Display(Name = "Mostrar firma")]
        public bool ShowSignature { get; set; }

        [Display(Name = "Ruta de firma")]
        public string SignaturePath { get; set; }

        [Display(Name = "Orden")]
        public int Order { get; set; }
    }
}