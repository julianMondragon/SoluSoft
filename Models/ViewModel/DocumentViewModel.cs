using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TAS360.Models.Enums;

namespace TAS360.Models.ViewModel
{
    public class DocumentViewModel
    {
        public DocumentViewModel()
        {
            Sections = new List<DocumentSectionViewModel>();
            Items = new List<DocumentItemViewModel>();
            Signatures = new List<DocumentSignatureViewModel>();

            ShowTitle = true;
            ShowSubtitle = true;
            ShowGeneralDescription = true;
            ShowObservations = true;
            ShowNotes = true;
            ShowClientName = true;
            ShowResponsibleName = true;
            ShowResponsiblePosition = true;
            ShowIssueDate = true;
            ShowLocation = true;
            ShowFolio = true;
            ShowItems = true;
            ShowSections = true;
            ShowSignatures = true;

            IssueDate = DateTime.Now;
        }

        public Guid DocumentSessionId { get; set; }

        [Display(Name = "Tipo de documento")]
        [Required(ErrorMessage = "Seleccione un tipo de documento.")]
        public DocumentTypeEnum DocumentType { get; set; }

        [Display(Name = "Folio")]
        [Required(ErrorMessage = "Ingrese un folio.")]
        public string Folio { get; set; }

        [Display(Name = "Título")]
        public string Title { get; set; }

        [Display(Name = "Subtítulo")]
        public string Subtitle { get; set; }

        [Display(Name = "Descripción general")]
        public string GeneralDescription { get; set; }

        [Display(Name = "Observaciones")]
        public string Observations { get; set; }

        [Display(Name = "Notas")]
        public string Notes { get; set; }

        [Display(Name = "Nombre del cliente")]
        public string ClientName { get; set; }

        [Display(Name = "Responsable")]
        public string ResponsibleName { get; set; }

        [Display(Name = "Puesto del responsable")]
        public string ResponsiblePosition { get; set; }

        [Display(Name = "Fecha de emisión")]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Lugar de emisión")]
        public string IssueLocation { get; set; }

        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        [Display(Name = "IVA")]
        public decimal Vat { get; set; }

        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Display(Name = "Mostrar título")]
        public bool ShowTitle { get; set; }

        [Display(Name = "Mostrar subtítulo")]
        public bool ShowSubtitle { get; set; }

        [Display(Name = "Mostrar descripción general")]
        public bool ShowGeneralDescription { get; set; }

        [Display(Name = "Mostrar observaciones")]
        public bool ShowObservations { get; set; }

        [Display(Name = "Mostrar notas")]
        public bool ShowNotes { get; set; }

        [Display(Name = "Mostrar cliente")]
        public bool ShowClientName { get; set; }

        [Display(Name = "Mostrar responsable")]
        public bool ShowResponsibleName { get; set; }

        [Display(Name = "Mostrar puesto del responsable")]
        public bool ShowResponsiblePosition { get; set; }

        [Display(Name = "Mostrar fecha de emisión")]
        public bool ShowIssueDate { get; set; }

        [Display(Name = "Mostrar lugar de emisión")]
        public bool ShowLocation { get; set; }

        [Display(Name = "Mostrar folio")]
        public bool ShowFolio { get; set; }

        [Display(Name = "Mostrar conceptos")]
        public bool ShowItems { get; set; }

        [Display(Name = "Mostrar secciones")]
        public bool ShowSections { get; set; }

        [Display(Name = "Mostrar firmas")]
        public bool ShowSignatures { get; set; }

        public List<DocumentSectionViewModel> Sections { get; set; }
        public List<DocumentItemViewModel> Items { get; set; }
        public List<DocumentSignatureViewModel> Signatures { get; set; }
    }
}