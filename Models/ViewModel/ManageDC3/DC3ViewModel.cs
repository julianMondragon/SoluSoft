using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace TAS360.Models.ViewModel.ManageDC3
{
    public class DC3ViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Empresa")]
        public int EmpresaId { get; set; }

        [Required]
        [Display(Name = "Trabajador")]
        public int TrabajadorId { get; set; }

        public int CertificadorId { get; set; }

        [Required]
        [Display(Name = "Curso")]
        public int CursoId { get; set; }

        [Required]
        [Display(Name = "Capacitador")]
        public int CapacitadorId { get; set; }

        [Required]
        [Display(Name = "Fecha de inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Display(Name = "Fecha de fin")]
        [DataType(DataType.Date)]
        public DateTime FechaFin { get; set; }

        [Required]
        [Range(1, 999)]
        [Display(Name = "Duración (horas)")]
        public int DuracionHoras { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; }

        [Required]
        [Display(Name = "Ocupacion")]
        public int OcupacionId { get; set; }

        [StringLength(200)]
        [Display(Name = "Representante de trabajadores")]
        public string RepresentanteTrabajadores { get; set; }

        [Display(Name = "Fecha de emisión")]
        [DataType(DataType.Date)]
        public DateTime FechaEmision { get; set; }

        public string RutaFirmaCapacitador { get; set; }
        public string RutaFirmaRepresentante { get; set; }
        public string RutaFirmaTrabajador { get; set; }

        public string Status { get; set; }

        public string QRUrl { get; set; }
        public string PdfUrl { get; set; }

        public string Folio { get; set; }
        public bool Activo { get; set; }

        public IEnumerable<SelectListItem> Empresas { get; set; }
        public IEnumerable<SelectListItem> Trabajadores { get; set; }
        public IEnumerable<SelectListItem> Cursos { get; set; }
        public IEnumerable<SelectListItem> Capacitadores { get; set; }
        public IEnumerable<SelectListItem> Ocupaciones { get; set; }
        public HttpPostedFileBase FirmaRepresentanteFile { get; set; }
        public HttpPostedFileBase FirmaTrabajadorFile { get; set; }

        public string EmpresaNombre { get; set; }
        public string TrabajadorNombre { get; set; }
        public string CursoNombre { get; set; }
        public string CapacitadorNombre { get; set; }
        public string CapacitadorSTPS { get; set; }
        public string OcupacionNombre { get; set; }
        public string CursoTematica { get; set; }
        public string CURP { get; set; }
        public string RFC { get; set; }

        // Fechas desglosadas para pdf
        public string[] FechaInicioArray { get; set; }
        public string[] FechaFinArray { get; set; }

        // CURP y RFC en arreglo para pdf
        public char[] CURPArray { get; set; }
        public char[] RFCArray { get; set; }
    }
}
