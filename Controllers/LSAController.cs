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
using Newtonsoft.Json;

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
        [HttpGet]
        public ActionResult ReportHis(DateTime? fecha)
        {
            string path = Server.MapPath("~/ReportesSLAHistorico/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var fechasConArchivo = new HashSet<string>();
            var archivos = Directory.GetFiles(path, "*.pdf");

            foreach (var archivo in archivos)
            {
                string nombre = Path.GetFileNameWithoutExtension(archivo);
                fechasConArchivo.Add(nombre); // nombre = "010125"
            }

            ViewBag.FechasDisponibles = fechasConArchivo;

            return View();
        }
        /// <summary>
        /// Abre y devuelve el archivo PDF del reporte correspondiente a la fecha 
        /// seleccionada, la cual se recibe como parámetro. El nombre del archivo 
        /// se construye con base en esa fecha y se busca en la carpeta 
        /// ~/ReportesSLAHistorico/.
        /// Cabe mencionar que es necesario programar un nueno metodo, para que
        /// se genere el reporte y se guarde directamente en el servidor.
        /// </summary>
        /// <param name="fecha">Fecha del reporte a visualizar.</param>
        /// <returns></returns>
        public ActionResult VerReportePDF(DateTime fecha)
        {
            string fileName = fecha.ToString("ddMMyy") + ".pdf";
            string filePath = Server.MapPath("~/ReportesSLAHistorico/" + fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("Archivo no encontrado: " + fileName);
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", fileName);
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
                        Status_Actual = item.Status_Actual,
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