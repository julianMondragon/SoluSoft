using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TAS360.Models.ViewModel.Documentos
{
    public class DocumentoListItemViewModel
    {
        public int Id { get; set; }
        public string Folio { get; set; }
        public string TipoDocumento { get; set; }
        public string Estado { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DocumentoEditViewModel
    {
        public DocumentoEditViewModel()
        {
            Secciones = new List<DocumentoSeccionViewModel>();
            Conceptos = new List<DocumentoConceptoViewModel>();
            Firmas = new List<DocumentoFirmaViewModel>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "El folio es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Folio")]
        public string Folio { get; set; }

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        [StringLength(30)]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumento { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(250)]
        [Display(Name = "Título")]
        public string Titulo { get; set; }

        [StringLength(2000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de emisión es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de emisión")]
        public DateTime? FechaEmision { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; }

        public IList<DocumentoSeccionViewModel> Secciones { get; set; }
        public IList<DocumentoConceptoViewModel> Conceptos { get; set; }
        public IList<DocumentoFirmaViewModel> Firmas { get; set; }

        public IEnumerable<SelectListItem> TiposDocumento
        {
            get
            {
                return new[]
                {
                    new SelectListItem { Value = "ReporteServicio", Text = "Reporte de servicio" },
                    new SelectListItem { Value = "Cotizacion", Text = "Cotización" },
                    new SelectListItem { Value = "Requerimiento", Text = "Requerimiento" },
                    new SelectListItem { Value = "DocumentoGeneral", Text = "Documento general" }
                };
            }
        }

        public IEnumerable<SelectListItem> EstadosDocumento
        {
            get
            {
                return new[]
                {
                    new SelectListItem { Value = "Borrador", Text = "Borrador" },
                    new SelectListItem { Value = "Generado", Text = "Generado" },
                    new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
                };
            }
        }
    }

    public class DocumentoSeccionViewModel
    {
        public DocumentoSeccionViewModel()
        {
            Imagenes = new List<DocumentoImagenViewModel>();
        }

        public int Id { get; set; }
        public int DocumentoId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
        public int Orden { get; set; }

        [StringLength(250)]
        public string Titulo { get; set; }

        [AllowHtml]
        public string Contenido { get; set; }

        public IList<DocumentoImagenViewModel> Imagenes { get; set; }
    }

    public class DocumentoImagenViewModel
    {
        public int Id { get; set; }
        public int DocumentoSeccionId { get; set; }
        public int Orden { get; set; }
        public string NombreArchivo { get; set; }
        public string MimeType { get; set; }
        public string RutaArchivo { get; set; }
        public string TextoAlternativo { get; set; }
    }

    public class DocumentoConceptoViewModel
    {
        public int Id { get; set; }
        public int DocumentoId { get; set; }

        [Range(0, int.MaxValue)]
        public int Orden { get; set; }

        [StringLength(50)]
        public string Clave { get; set; }

        [Required(ErrorMessage = "La descripción del concepto es obligatoria.")]
        [StringLength(1000)]
        public string Descripcion { get; set; }

        [StringLength(30)]
        public string Unidad { get; set; }

        [Range(typeof(decimal), "0", "99999999999999.9999")]
        public decimal Cantidad { get; set; }

        [Range(typeof(decimal), "0", "99999999999999.9999")]
        [Display(Name = "Precio unitario")]
        public decimal PrecioUnitario { get; set; }

        public decimal Importe { get { return Cantidad * PrecioUnitario; } }
    }

    public class DocumentoFirmaViewModel
    {
        public int Id { get; set; }
        public int DocumentoId { get; set; }

        [Required(ErrorMessage = "El tipo de firma es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Tipo de firma")]
        public string TipoFirma { get; set; }

        [Required(ErrorMessage = "El nombre del firmante es obligatorio.")]
        [StringLength(200)]
        [Display(Name = "Nombre del firmante")]
        public string NombreFirmante { get; set; }

        [StringLength(150)]
        [Display(Name = "Cargo")]
        public string CargoFirmante { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha de firma")]
        public DateTime? FechaFirma { get; set; }
    }
}
