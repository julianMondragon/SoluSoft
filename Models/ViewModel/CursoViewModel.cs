using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TAS360.Models.ViewModel
{
    public class CursoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del curso es obligatorio")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Debe tener entre 5 y 200 caracteres")]
        [Display(Name = "Nombre del curso")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria")]
        [Range(1, 999, ErrorMessage = "Debe ser mayor a 0 horas")]
        [Display(Name = "Duración (horas)")]
        public int DuracionHoras { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un área temática")]
        [Display(Name = "Área temática")]
        public int AreaTematicaId { get; set; }

        public string NombreAreaTematica { get; set; }

        public int CertificadorId { get; set; }

        public List<SelectListItem> AreasTematicas { get; set; }
    }
}