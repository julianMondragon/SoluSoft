using System;
using System.ComponentModel.DataAnnotations;

namespace TAS360.Models.ViewModel
{
    public class PlantelViewModel
    {
        public int Id { get; set; } // Solo requerido para edición

        [Required(ErrorMessage = "El nombre del plantel es obligatorio.")]
        [Display(Name = "Nombre del plantel")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Display(Name = "Teléfono")]
        [StringLength(20)]
        [RegularExpression(@"^\d+$", ErrorMessage = "Solo se permiten números.")]
        public string Telefono { get; set; }

        [Display(Name = "Dirección")]
        [StringLength(200)]
        public string Direccion { get; set; }

        [Display(Name = "Administrador")]
        [StringLength(100)]
        public string Administrador { get; set; }

        [Display(Name = "Director")]
        [StringLength(100)]
        public string Director { get; set; }

        [Display(Name = "Fecha de Registro")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaHoraRegistro { get; set; } = DateTime.Now;
    }
}
