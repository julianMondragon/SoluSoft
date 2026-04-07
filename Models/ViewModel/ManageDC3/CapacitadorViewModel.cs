using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel.ManageDC3
{
    public class CapacitadorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, MinimumLength = 5)]
        [Display(Name = "Nombre del capacitador")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El registro STPS es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Registro STPS")]
        public string NumeroRegistroSTPS { get; set; }

        public int CertificadorId { get; set; }

        [Display(Name = "Firma (imagen)")]
        public HttpPostedFileBase FirmaFile { get; set; }

        public string RutaFirmaBase { get; set; }
    }
}