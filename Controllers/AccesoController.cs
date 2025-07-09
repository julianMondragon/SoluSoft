using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using TAS360.Models;
using System.Configuration;

namespace TAS360.Controllers
{
    public class AccesoController : Controller
    {
        private string contenidoHtml = @"
                                <!DOCTYPE html>
                                <html lang='es'>
                                <head>
                                    <meta charset='UTF-8'>
                                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                    <title>Actualización de Ticket</title>
                                    <style>
                                        body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }
                                        .container { width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 1px solid #dddddd; border-radius: 5px; overflow: hidden; }
                                        .header { background-color: #4CAF50; color: #ffffff; padding: 20px; text-align: center; }
                                        .content { padding: 20px; }
                                        .footer { background-color: #f1f1f1; color: #888888; padding: 10px; text-align: center; }
                                        .button { display: inline-block; background-color: #4CAF50; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px; }
                                    </style>
                                </head>
                                <body>
                                    <div class='container'>
                                        <div class='header'>
                                            <h1>Recuperación de contraseña</h1>
                                        </div>
                                        <div class='content'>
                                            <p>Estimado/a <strong>{usuarioName}</strong>,</p>
                                            <p> Se envia este correo con el fin de mandar la nueva contraseña. </p>
                                            <ul>                         
                                                <li><strong>Nueva contraseña:</strong> {ultimoComentario}</li>
                                            </ul>
                                        </div>
                                        <div class='footer'>
                                            <p>Este es un mensaje automático, por favor no responda a este correo.</p>
                                            <p>&copy; 2024 HelpDesk PTS</p>
                                        </div>
                                    </div>
                                </body>
                                </html>";
  
        // GET: Acceso
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string user, string pass)
        {
            try
            {
                using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                {
                    // Cifrar la contraseña ingresada por el usuario
                    string hashedPass = ComputeSha256Hash(pass);

                    // Buscar al usuario con el email y la contraseña cifrada
                    var usuario = (from u in db.User where u.email == user && u.password == hashedPass select u).FirstOrDefault();

                    if (usuario == null)
                    {
                        // Si el usuario no existe, mostrar un mensaje de error
                        ViewBag.exception = "Usuario o contraseña no válidos";
                        return View();
                    }

                    // Crear sesión del usuario
                    Session["User"] = usuario;

                    // Registrar log de ingreso
                    string path = Server.MapPath("~/Logs/Login/");
                    Log oLog = new Log(path);
                    oLog.Add("Ingreso " + usuario.nombre);
                    oLog = null;
                }

                // Indicar que el login fue exitoso y se debe limpiar sessionStorage
                TempData["ClearSessionStorage"] = true;

                // Redirigir a la acción que maneja la vista anterior
                return RedirectToAction("RetornarVistaAnterior", "Acceso");
            }
            catch (Exception ex)
            {
                // Registrar log de excepción
                string path = Server.MapPath("~/Logs/Login");
                Log oLog = new Log(path);
                oLog.Add("Excepción en el controlador de acceso");
                oLog.Add(ex.Message);
                oLog = null;
                ViewBag.exception = ex.Message + "\n Por Favor informar al administrador del sistema";
                return View();
            }
        }


        public ActionResult RetornarVistaAnterior()
        {
            // Regresa a la vista anterior utilizando JavaScript
            return View("RetornarVistaAnterior");
        }

        /// <summary>
        /// Metodo para recuperar la contraseña.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult RecuperarContraseña()
        {
            return View();
        }
        
        // Método de recuperación de contraseña
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecuperarContraseña(string email)
        {
            try
            {
                using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                {
                    var user = db.User.FirstOrDefault(u => u.email == email);
                    if (user == null)
                    {
                        ViewBag.Message = "No se encontró ningún usuario con ese correo electrónico.";
                        return View();
                    }

                    // Generar nueva contraseña
                    string newPassword = GenerateRandomPassword(10);
                    string hashedPassword = ComputeSha256Hash(newPassword);

                    // Actualizar la contraseña en la base de datos
                    user.password = hashedPassword;
                    db.SaveChanges();

                    // Configuración del cliente SMTP
                    SmtpClient clienteSmtp = new SmtpClient(
                         ConfigurationManager.AppSettings["SmtpHost"],
                         int.Parse(ConfigurationManager.AppSettings["SmtpPort"]))
                    {
                        EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]),
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(
                             ConfigurationManager.AppSettings["SmtpUser"],
                             ConfigurationManager.AppSettings["SmtpPassword"]
                         ),
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    // Restablecimiento de contraseña
                    string path = Server.MapPath("~/Logs/Restablecimiento/");
                    Log oLog = new Log(path);
                    oLog.Add("Se restauro la contraseña de: " + user.nombre + " la contraseña es:" + newPassword);
                    contenidoHtml = contenidoHtml.Replace("{usuarioName}", user.nombre)
                             .Replace("{ultimoComentario}", newPassword);
                    // Crear el mensaje de correo
                    MailMessage mensaje = new MailMessage
                    {
                        From = new MailAddress("soporte.tas360@pts.mx"),
                        Subject = "Restablecimiento de Contraseña",
                        Body = contenidoHtml,
                        IsBodyHtml = true
                    };
                    
                    // Añadir destinatario
                    mensaje.To.Add(user.email);
                    mensaje.Bcc.Add("julian.mondragon@pts.mx");

                    // Enviar el correo
                    clienteSmtp.Send(mensaje);

                    ViewBag.Message = "Se ha enviado un correo electrónico con tu nueva contraseña.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = "Hubo un error al intentar enviar el correo electrónico: " + ex.Message;
            }

            return View();
        }

        // Método para generar una contraseña aleatoria
        public static string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(validChars[rnd.Next(validChars.Length)]);
            }
            return res.ToString();
        }

        // Método para cifrar con SHA-256
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}