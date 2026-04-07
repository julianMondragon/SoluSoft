using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TAS360.Models.ViewModel.ManageDC3
{
    public class TrabajadorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del trabajador es obligatorio")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La CURP es obligatoria")]
        [StringLength(18, MinimumLength = 18, ErrorMessage = "La CURP debe tener 18 caracteres")]
        [RegularExpression(@"^[A-Z][AEIOU][A-Z]{2}\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])[HM](AS|BC|BS|CC|CL|CM|CS|CH|DF|DG|GT|GR|HG|JC|MC|MN|MS|NT|NL|OC|PL|QT|QR|SP|SL|SR|TC|TS|TL|VZ|YN|ZS|NE)[B-DF-HJ-NP-TV-Z]{3}[A-Z\d]\d$",
                            ErrorMessage = "CURP no válida")]
        public string CURP { get; set; }

        [Required(ErrorMessage = "El puesto es obligatorio")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "El puesto debe tener entre 3 y 120 caracteres")]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una ocupación")]
        [Display(Name = "Ocupación")]
        public int OcupacionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una empresa")]
        [Display(Name = "Empresa")]
        public int EmpresaId { get; set; }

        public int CertificadorId { get; set; }

        public bool Activo { get; set; }

        public List<SelectListItem> Empresas { get; set; }

        public List<SelectListItem> Ocupaciones { get; set; }
    }
}