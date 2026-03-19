using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class DocumentItemViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Concepto")]
        public string Concept { get; set; }

        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Display(Name = "Cantidad")]
        public decimal Quantity { get; set; }

        [Display(Name = "Precio unitario")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Importe")]
        public decimal Amount
        {
            get { return Quantity * UnitPrice; }
        }

        [Display(Name = "Orden")]
        public int Order { get; set; }
    }
}