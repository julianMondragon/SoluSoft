using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models.ViewModel;
using TAS360.StorProc;
using TAS360.Models;
using Microsoft.Ajax.Utilities;
using System.Data.Entity;
using System.IO;
using System.Globalization;

namespace TAS360.Controllers
{
    public class LSAController : Controller
    {
        [HttpGet]
        [AuthorizeUser(idOperacion: 26)]
        public ActionResult Index()
        {
            List<TicketViewModel> tickets = new List<TicketViewModel>();
            using (Models.HelpDesk_Entities1 db = new Models.HelpDesk_Entities1())
            {
                var Tickets = (from s in db.Ticket where s.status != 12 orderby s.CreatedAt descending select s);
                if (Tickets != null && Tickets.Any())
                {
                    foreach (var t in Tickets)
                    {
                        TicketViewModel ticket = new TicketViewModel()
                        {
                            id = t.id,
                            titulo = t.titulo,
                            mensaje = t.mensaje,
                            usuario_name = t.Ticket_User.OrderByDescending(x => x.CreatedAt).FirstOrDefault().User.nombre,
                            categoria_name = t.Categoria.nombre,
                            terminal_name = t.Terminal.Nombre,
                            Subsistema_name = t.Subsistema.Nombre,
                            Status = t.status,
                            Date = t.CreatedAt,
                            Datetobedone = t.CreatedAt.HasValue ? t.CreatedAt.Value.AddDays(15) : DateTime.MinValue
                        };

                        switch (t.Ticket_Record_Status.OrderByDescending(x => x.CreatedAt).FirstOrDefault().Status.descripcion)
                        {
                            case "Pendiente ":
                                ticket.status_name = "Capturado";
                                break;
                            case "Analisis  ":
                                ticket.status_name = "Espera de info";
                                break;
                            case "Correccion":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Pruebas   ":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Implementa":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Pend_Pmx  ":
                                ticket.status_name = "Espera de info";
                                break;
                            case "Cerrado   ":
                                ticket.status_name = "Espera de info";
                                break;
                            default:
                                ticket.status_name = "Undefineded";
                                break;
                        }

                        tickets.Add(ticket);
                    }
                }
            }
            GetSummaryTKs();
            return View(tickets);
        }
       
        public ActionResult SLAReport()
        {
            List<TicketViewModel> tickets = new List<TicketViewModel>();
            using (Models.HelpDesk_Entities1 db = new Models.HelpDesk_Entities1())
            {
                var Tickets = (from s in db.Ticket where s.status != 12 orderby s.CreatedAt descending select s);
                if (Tickets != null && Tickets.Any())
                {
                    foreach (var t in Tickets)
                    {
                        TicketViewModel ticket = new TicketViewModel()
                        {
                            id = t.id,
                            titulo = t.titulo,
                            mensaje = t.mensaje,
                            usuario_name = t.Ticket_User.OrderByDescending(x => x.CreatedAt).FirstOrDefault().User.nombre,
                            categoria_name = t.Categoria.nombre,
                            terminal_name = t.Terminal.Nombre,
                            Subsistema_name = t.Subsistema.Nombre,
                            Status = t.status,
                            Date = t.CreatedAt,
                            Datetobedone = t.CreatedAt.HasValue ? t.CreatedAt.Value.AddDays(15) : DateTime.MinValue
                        };
                        switch (t.Ticket_Record_Status.OrderByDescending(x => x.CreatedAt).FirstOrDefault().Status.descripcion)
                        {
                            case "Pendiente ":
                                ticket.status_name = "Capturado";
                                break;
                            case "Analisis  ":
                                ticket.status_name = "Espera de info";
                                break;
                            case "Correccion":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Pruebas   ":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Implementa":
                                ticket.status_name = "En Proceso";
                                break;
                            case "Pend_Pmx  ":
                                ticket.status_name = "Espera de info";
                                break;
                            case "Cerrado   ":
                                ticket.status_name = "Espera de info";
                                break;
                            default:
                                ticket.status_name = "Undefineded";
                                break;
                        }

                        tickets.Add(ticket);
                    }
                }
            }
            GetSummaryTKs();
            return View(tickets);
        }
        /// <summary>
        /// Se manda a llamar la nueva vista
        /// </summary>
        /// <returns></returns>
        //public ActionResult GenerarReporte(string fecha)
        //{
        //    DateTime fechaFin;
        //    if (!DateTime.TryParse(fecha, out fechaFin))
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Fecha inválida.");
        //    }

        //    DateTime fechaInicio = fechaFin.AddDays(-30);

        //    // Aquí deberías obtener los datos de la base de datos dentro del rango de fechas
        //    var reportes = ObtenerReportes(fechaInicio, fechaFin);

        //    // Retornar la vista con los datos del reporte
        //    return View("ReporteView", reportes);
        //}

        //private List<ReporteModel> ObtenerReportes(DateTime inicio, DateTime fin)
        //{
        //    using (var db = new MiContexto())  // Asegúrate de usar tu contexto de base de datos
        //    {
        //        return db.Reportes
        //                 .Where(r => r.Fecha >= inicio && r.Fecha <= fin)
        //                 .ToList();
        //    }
        //}

        [HttpGet]
        [AuthorizeUser(idOperacion: 27)]
        public ActionResult PrintSLAReport()
        {
            return new ActionAsPdf($"SLAReport/")
            {
                FileName = $"Reporte_de_SLA_{DateTime.Now.Date.ToShortDateString()}.pdf"
            };
        }
        [HttpGet]
        [AuthorizeUser(idOperacion: 27)]
        public ActionResult PrintSLAReportHis(string fecha)
        {
            if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaReporte))
            {
                return new HttpStatusCodeResult(400, "Fecha inválida");
            }
            string nombreArchivo = $"Reporte_de_SLA_{fechaReporte:yyyy_MM_dd}.pdf";
            string rutaCarpeta = Server.MapPath("~/ReportesSLA/");
            string rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);
            if (!System.IO.File.Exists(rutaArchivo))
            {
                var pdf = new ActionAsPdf("ReportePeriodico", new { fechaSeleccionada = fechaReporte.ToString("yyyy-MM-dd") })
                {
                    FileName = nombreArchivo
                };
                byte[] pdfBytes = pdf.BuildFile(ControllerContext);
                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);
                System.IO.File.WriteAllBytes(rutaArchivo, pdfBytes);
            }
            return File(rutaArchivo, "application/pdf", nombreArchivo);
        }
        //[HttpGet]
        //[AuthorizeUser(idOperacion: 27)]
        //public ActionResult ReportHis()
        //{
        //    return View();
        //}
        [HttpGet]
        [AuthorizeUser(idOperacion: 27)]
        public ActionResult ReportHis(DateTime? fecha)
        {
            return View();
        }
        public ActionResult ReportePeriodico(string fechaSeleccionada)
        {
            DateTime fechaReporte;
            if (!DateTime.TryParseExact(fechaSeleccionada, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaReporte))
            {
                return new HttpStatusCodeResult(400, "Fecha inválida");
            }
            DateTime fechaInicio, fechaFin;
            if (fechaReporte.Day <= 15)
            {
                fechaInicio = new DateTime(fechaReporte.Year, fechaReporte.Month, 1);
                fechaFin = new DateTime(fechaReporte.Year, fechaReporte.Month, 15, 23, 59, 59);
            }
            else
            {
                fechaInicio = new DateTime(fechaReporte.Year, fechaReporte.Month, 16);
                fechaFin = new DateTime(fechaReporte.Year, fechaReporte.Month, DateTime.DaysInMonth(fechaReporte.Year, fechaReporte.Month), 23, 59, 59);
            }
            List<TicketViewModel> tickets = new List<TicketViewModel>();
            using (Models.HelpDesk_Entities1 db = new Models.HelpDesk_Entities1())
            {
                var Tickets = db.Ticket
                                .Where(s => s.status != 12 && s.CreatedAt >= fechaInicio && s.CreatedAt <= fechaFin)
                                .OrderByDescending(s => s.CreatedAt);

                foreach (var t in Tickets)
                {
                    var ticket = new TicketViewModel()
                    {
                        id = t.id,
                        titulo = t.titulo,
                        mensaje = t.mensaje,
                        usuario_name = t.Ticket_User.OrderByDescending(x => x.CreatedAt).FirstOrDefault().User.nombre,
                        categoria_name = t.Categoria.nombre,
                        terminal_name = t.Terminal.Nombre,
                        Subsistema_name = t.Subsistema.Nombre,
                        Status = t.status,
                        Date = t.CreatedAt,
                        Datetobedone = t.CreatedAt.HasValue ? t.CreatedAt.Value.AddDays(15) : DateTime.MinValue
                    };
                    string descripcionStatus = t.Ticket_Record_Status.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.Status?.descripcion?.Trim();
                    switch (descripcionStatus)
                    {
                        case "Pendiente":
                            ticket.status_name = "Capturado";
                            break;
                        case "Analisis":
                        case "Pend_Pmx":
                        case "Cerrado":
                            ticket.status_name = "Espera de info";
                            break;
                        case "Correccion":
                        case "Pruebas":
                        case "Implementa":
                            ticket.status_name = "En Proceso";
                            break;
                        default:
                            ticket.status_name = "Undefineded";
                            break;
                    }
                    tickets.Add(ticket);
                }
            }
            ViewBag.FechaSeleccionada = fechaReporte;
            GetSummaryTKs();
            return View(tickets);
        }

        [HttpGet]
        public JsonResult GetTicktsByStatus()
        {
            LSA_Reportes Tickets = new LSA_Reportes();
            List<G_TicketsByStatusViewModel> listTksbyStatus = Tickets.GetTicktsByStatus();

            return Json(listTksbyStatus, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetTicketsByCategoria()
        {
            LSA_Reportes Tickets = new LSA_Reportes();
            List<G_TicketsByCategoriaViewModel> listTksbyCategoria = Tickets.GetTicketsByCategoria();

            return Json(listTksbyCategoria, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetTicketsByTerminal()
        {
            LSA_Reportes Tickets = new LSA_Reportes();
            List<G_TicketsByTerminalViewModel> listTksbyCategoria = Tickets.GetTicketsByTerminal();

            return Json(listTksbyCategoria, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetTicketsByTerminalOnLastMonth()
        {
            LSA_Reportes Tickets = new LSA_Reportes();
            List<G_TicketsByTerminalViewModel> listTksbyCategoria = Tickets.GetTicketsByTerminalOnLastMonth();
            return Json(listTksbyCategoria, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetTicketsModificados()
        {
            var resultado = new List<object>();
            using (var context = new Models.HelpDesk_Entities1())
            {
                var data = context.Database.SqlQuery<G_TicketsModificadosViewModel>("EXEC ptstools_Jmondragon.SP_GetTicketsByModifi").ToList();
                foreach (var item in data)
                {
                    resultado.Add(new
                    {
                        id_ticket = item.id_ticket_editado,
                        Nombre_Usuario = item.Nombre_Usuario,
                        masReciente = item.Mas_Reciente.ToString("dd/MM/yyyy HH:mm:ss"),
                        masAntiguo = item.Mas_Antiguo.ToString("dd/MM/yyyy HH:mm:ss"),
                        Cantidad_modificaciones = item.Cantidad_modificaciones
                    });
                }
            }
            return Json(resultado, JsonRequestBehavior.AllowGet);
        }
        private void GetSummaryTKs()
        {
            int tksopen = 0;
            int tksclose = 0;
            List<TicketViewModel> tickets = new List<TicketViewModel>();
            using (Models.HelpDesk_Entities1 db = new Models.HelpDesk_Entities1())
            {
                var Tickets = (from s in db.Ticket select s);
                if (Tickets != null && Tickets.Any())
                {
                    foreach (var t in Tickets)
                    {
                        if (t.status == 12)
                            tksclose++;
                        else
                            tksopen++;

                    }
                }
            }
            ViewBag.tksopen = tksopen;
            ViewBag.tksclose = tksclose;
        }
    }
}