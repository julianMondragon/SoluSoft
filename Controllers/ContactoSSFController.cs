using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class ContactoSSFController : Controller
    {
        public ActionResult Index(string servicio, string mensaje)
        {
            var model = new ContactoSSFViewModel();

            model.Servicio = servicio;
            model.Mensaje = mensaje;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarMensaje(ContactoSSFViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {

                string contenidoHtml = $@"
                    <!DOCTYPE html>
                    <html lang='es'>
                    <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Nuevo contacto - NEGMON</title>

                    <style>

                    body{{margin:0;
                        padding:0;
                        background:#f4f6f9;
                        font-family: Arial, Helvetica, sans-serif;
                    }}

                    .container{{width:100%;
                        max-width:620px;
                        margin:auto;
                        background:#ffffff;
                        border-radius:6px;
                        overflow:hidden;
                        box-shadow:0px 3px 12px rgba(0,0,0,0.08);
                    }}

                    .header{{background:#1f3c5a;
                        color:white;
                        padding:25px;
                        text-align:center;
                    }}

                    .logo{{font-size:24px;
                        font-weight:bold;
                        letter-spacing:1px;
                    }}

                    .subtitle{{font-size:14px;
                        opacity:0.9;
                    }}

                    .content{{padding:30px;
                        color:#333;
                    }}

                    .info-box{{background:#f8f9fb;
                        border:1px solid #e3e6eb;
                        border-radius:5px;
                        padding:20px;
                        margin-top:15px;
                    }}

                    .info-row{{margin-bottom:10px;
                    }}

                    .label{{font-weight:bold;
                        color:#1f3c5a;
                    }}

                    .message{{margin-top:20px;
                        padding:15px;
                        background:#f4f6f9;
                        border-left:4px solid #1f3c5a;
                    }}

                    .button{{display:inline-block;
                        margin-top:25px;
                        padding:12px 22px;
                        background:#2d7ef7;
                        color:white;
                        text-decoration:none;
                        border-radius:4px;
                        font-size:14px;
                    }}

                    .footer{{background:#f1f3f6;
                        text-align:center;
                        padding:18px;
                        font-size:12px;
                        color:#777;
                    }}

                    .footer strong{{color:#1f3c5a;
                    }}
                    .alert {{padding: 15px;
                        margin-bottom: 20px;
                        border: 1px solid transparent;
                        border-radius: 4px;
                    }}
                    .alert-success {{color: #3c763d;
                        background-color: #dff0d8;
                        border-color: #d6e9c6;
                    }}
                    </style>

                    </head>

                    <body>

                    <div class='container'>

                        <div class='header'>
                            <div class='logo'>NEGMON</div>
                            <div class='subtitle'>Soluciones tecnológicas empresariales</div>
                        </div>                        
                        <div class='content'>
                        <div class='alert alert-success'>
                                Uno de nuestros asesores se comunicara contigo {model.Nombre}            
                        </div>
                        <h2>Nuevo contacto desde la página web</h2>
                        <p>Se ha recibido una nueva solicitud desde la pagina de contacto de<strong>NEGMON</strong>.</p>
                        <div class='info-box'>

                            <div class='info-row'>
                                <span class='label'>Nombre:</span> {model.Nombre}
                            </div>

                            <div class='info-row'>
                                <span class='label'>Correo:</span> {model.Correo}
                            </div>

                            <div class='info-row'>
                                <span class='label'>Teléfono:</span> {model.Telefono}
                            </div>

                            <div class='info-row'>
                                <span class='label'>Servicio:</span> {model.Servicio}
                            </div>

                        </div>

                        <div class='message'>
                            <strong>Mensaje del cliente:</strong>
                            <p>{model.Mensaje}</p>
                        </div>

                        <center>
                            <a href='mailto:{model.Correo}' class='button'>
                                Responder al cliente
                            </a>
                        </center>
                        </div>

                        <div class='footer'>
                            <p>Este mensaje fue generado automáticamente desde el formulario web.</p>
                            <p><strong>NEGMON</strong> | Soluciones tecnológicas empresariales</p>
                        </div>

                    </div>

                    </body>
                    </html>
                    ";
                SmtpClient clienteSmtp = new SmtpClient(
                    ConfigurationManager.AppSettings["SmtpHost"],
                    int.Parse(ConfigurationManager.AppSettings["SmtpPort"]))
                {
                    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]),
                    Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["SmtpUser"],
                        ConfigurationManager.AppSettings["SmtpPassword"]
                    )
                };


                MailMessage mensaje = new MailMessage
                {
                    From = new MailAddress(ConfigurationManager.AppSettings["SmtpUser"]),
                    Subject = "Nuevo mensaje desde Negmon-Contacto",
                    Body = contenidoHtml,
                    IsBodyHtml = true
                };


                mensaje.To.Add("jmondragon@negmon.com");
                mensaje.To.Add("janegrete@negmon.com");
                mensaje.CC.Add(model.Correo);
                mensaje.BodyEncoding = System.Text.Encoding.UTF8;
                mensaje.SubjectEncoding = System.Text.Encoding.UTF8;

                clienteSmtp.Send(mensaje);


                ViewBag.MensajeExito = "Mensaje enviado correctamente.";

                ModelState.Clear();

                return View("Index", new ContactoSSFViewModel());

            }
            catch (Exception ex)
            {

                ModelState.AddModelError("", "Error al enviar el correo: " + ex.Message);

                return View("Index", model);

            }

        }
    }
}
