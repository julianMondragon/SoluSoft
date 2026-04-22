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
        private string contenidoHtml = $@"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Bienvenido a NEGMON</title>

            <style>
            body {{
                margin:0;
                padding:0;
                background:#f4f6f9;
                font-family: Arial, Helvetica, sans-serif;
            }}

            .container {{
                width:100%;
                max-width:620px;
                margin:auto;
                background:#ffffff;
                border-radius:6px;
                overflow:hidden;
                box-shadow:0px 3px 12px rgba(0,0,0,0.08);
            }}

            .header {{
                background:#1f3c5a;
                color:white;
                padding:25px;
                text-align:center;
            }}

            .logo {{
                font-size:24px;
                font-weight:bold;
            }}

            .subtitle {{
                font-size:14px;
                opacity:0.9;
            }}

            .content {{
                padding:30px;
                color:#333;
            }}

            .info-box {{
                background:#f8f9fb;
                border:1px solid #e3e6eb;
                border-radius:5px;
                padding:20px;
                margin-top:15px;
            }}

            .info-row {{
                margin-bottom:10px;
            }}

            .label {{
                font-weight:bold;
                color:#1f3c5a;
            }}

            .button {{
                display:inline-block;
                margin-top:25px;
                padding:12px 22px;
                background:#2d7ef7;
                color:white;
                text-decoration:none;
                border-radius:4px;
                font-size:14px;
            }}

            .footer {{
                background:#f1f3f6;
                text-align:center;
                padding:18px;
                font-size:12px;
                color:#777;
            }}

            .alert {{
                padding:15px;
                border-radius:4px;
                margin-bottom:20px;
            }}

            .alert-success {{
                background-color:#dff0d8;
                color:#3c763d;
                border:1px solid #d6e9c6;
            }}

            </style>
            </head>

            <body>

            <div class='container'>

                <div class='header'>
                    <div class='logo'>NEGMON</div>
                    <div class='subtitle'>Gestión inteligente de operaciones</div>
                </div>

                <div class='content'>

                    <div class='alert alert-success'>
                        Tú contraseña ha sido restablecida exitosamente
                    </div>

                    <h2>Bienvenido, {{usuarioName}}</h2>

                    <p>Ahora cuentas con acceso a la plataforma <strong>NEGMON</strong>. A continuación se muestran tus credenciales de acceso:</p>

                    <div class='info-box'>
                        <div class='info-row'>
                            <span class='label'>Usuario:</span> {{email}}
                        </div>

                        <div class='info-row'>
                            <span class='label'>Contraseña:</span> {{password}}
                        </div>

                        <div class='info-row'>
                            <span class='label'>Rol:</span> {{rol}}
                        </div>
                    </div>

                    <p>Te recomendamos cambiar tu contraseña después de iniciar sesión por primera vez.</p>

                    <center>
                        <a href='https://negmon.com/Acceso/Login' class='button'>
                            Iniciar sesión
                        </a>
                    </center>

                </div>

                <div class='footer'>
                    <p>Este es un mensaje automático, por favor no responder.</p>
                    <p><strong>NEGMON</strong> | Soluciones tecnológicas empresariales</p>
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
                    contenidoHtml = contenidoHtml.Replace("{usuarioName}", user.nombre).Replace("{email}", user.email)
                             .Replace("{rol}", user.Roll.nombre)
                             .Replace("{password}", newPassword);
                    // Crear el mensaje de correo
                    MailMessage mensaje = new MailMessage
                    {
                        From = new MailAddress("contacto@negmon.com"),
                        Subject = "Restablecimiento de Contraseña",
                        Body = contenidoHtml,
                        IsBodyHtml = true
                    };
                    
                    // Añadir destinatario
                    mensaje.To.Add(user.email);
                    mensaje.Bcc.Add("jmondragon@negmon.com");

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