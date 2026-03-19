using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class DocumentSectionViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Título de sección")]
        public string SectionTitle { get; set; }

        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Display(Name = "Mostrar título de sección")]
        public bool ShowSectionTitle { get; set; }

        [Display(Name = "Mostrar descripción")]
        public bool ShowDescription { get; set; }

        [Display(Name = "Mostrar imagen")]
        public bool ShowImage { get; set; }

        [Display(Name = "Ruta de imagen")]
        public string ImagePath { get; set; }

        [Display(Name = "Nombre de imagen")]
        public string ImageName { get; set; }

        [Display(Name = "Orden")]
        public int Order { get; set; }
    }
}