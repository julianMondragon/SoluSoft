using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TAS360.Models.Enums
{
    public enum DocumentTypeEnum
    {
        [Display(Name = "Cotización")]
        Cotizacion = 1,

        [Display(Name = "Reporte de Servicio")]
        ReporteServicio = 2,

        [Display(Name = "Requerimiento")]
        Requerimiento = 3,

        [Display(Name = "Documento General")]
        DocumentoGeneral = 4
    }
}