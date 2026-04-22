using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel;
using DocumentFormat.OpenXml.Presentation;
using TAS360.StorProc;
using System.Configuration;

namespace TAS360.Controllers
{
    public class UserController : Controller
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
                        Tu cuenta ha sido creada exitosamente
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
                            <span class='label'>Rol:</span> {{Rol}}
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
        // GET: User/Index
        [AuthorizeUser(idOperacion: 17)]
        public ActionResult Index()
        {
            List<ListUsuarioViewModel> lst;
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                lst = (from u in db.User
                       join r in db.Roll on u.id_Roll equals r.id
                       select new ListUsuarioViewModel
                       {
                           id = u.id,
                           nombre = u.nombre,
                           email = u.email,
                           Rolid = u.id_Roll,
                           RolidName = r.nombre
                       }).ToList();
            }
            return View(lst);
        }

        // GET: User/Edit/5
        [HttpGet]
        [AuthorizeUser(idOperacion: 18)]
        public ActionResult Edit(int id)
        {

            ListUsuarioViewModel model = new ListUsuarioViewModel();
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                var userToEdit = db.User.Find(id);
                if (userToEdit != null)
                {
                    model.id = userToEdit.id;
                    model.nombre = userToEdit.nombre;
                    model.email = userToEdit.email;
                    model.Rolid = (int)userToEdit.id_Roll;
                }                
            }
            GetRoles();
            return View(model);
        }

        // POST: User/Edit/5
        [HttpPost]
        [AuthorizeUser(idOperacion: 18)]
        public ActionResult Edit(ListUsuarioViewModel model, string confirmPassword)
        {
            try
            {
                using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                {
                    string path = Server.MapPath("~/Logs/Usuarios/");
                    Log oLog = new Log(path);
                    var usr = db.User.FirstOrDefault(x => x.email == model.email && x.id != model.id);
                    if (usr != null)
                    {
                        ModelState.AddModelError("email", "Este correo ya exite !!!");
                        GetRoles();
                        return View(model);
                    }
                    
                    var userToEdit = db.User.Find(model.id);
                    if (userToEdit != null)
                    {
                        oLog.Add($"Usuario con ID {userToEdit.id} editado por {((User)Session["User"]).nombre}");
                        oLog.Add($"Nombre anterior: {userToEdit.nombre}");
                        oLog.Add($"Email anterior: {userToEdit.email}");
                        userToEdit.nombre = model.nombre;
                        userToEdit.email = model.email;
                        userToEdit.id_Roll = model.Rolid;


                        if (!string.IsNullOrEmpty(model.password))
                        {
                            if (model.password != confirmPassword)
                            {
                                ModelState.AddModelError("confirmPassword", "Las contraseñas no coinciden");
                                ModelState.AddModelError("Password", "Las contraseñas no coinciden");
                                GetRoles();
                                return View(model);
                            }


                            string passencripted = ComputeSha256Hash(model.password);
                            userToEdit.password = passencripted;

                            oLog.Add($"Nuevo passencripted: {passencripted}");
                            oLog.Add($"Nuevo password: {model.password}");
                        }

                        // Guardar log de la edición del usuario                       
                        oLog.Add($"Nuevo nombre: {userToEdit.nombre}");
                        oLog.Add($"Nuevo email: {userToEdit.email}");
                        oLog.Add($"Nuevo Rol: {model.Rolid} "); 
                        oLog.Add($"--------------------------------");
                    }
                    else
                    {
                        ViewBag.ExceptionMessage = "ID no encontrado";
                        GetRoles();
                        return View(model);
                    }

                    db.Entry(userToEdit).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = ex.Message;
                GetRoles();
                return View(model);
            }
        }

        // GET: User/Create
        [AuthorizeUser(idOperacion: 20)]
        public ActionResult Create()
        {
            ListUsuarioViewModel usuario = new ListUsuarioViewModel();
            GetRoles();
            return View(usuario);

        }

        /// <summary>
        /// Devuelve a la vista una lista de los Roles del sistema 
        /// </summary>
        private void GetRoles()
        {

            List<SelectListItem> Roles = new List<SelectListItem>();
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                var aux = (from s in db.Roll select s);
                if (aux != null && aux.Any())
                {
                    foreach (var a in aux)
                    {
                        Roles.Add(new SelectListItem
                        {
                            Text = a.nombre,
                            Value = a.id.ToString()

                        });
                    }
                }
            }
            ViewBag.Roles = Roles;
        }

        // POST: User/Create
        [HttpPost]
        [AuthorizeUser(idOperacion: 20)]
        public ActionResult Create(ListUsuarioViewModel model)
        {
            try
            {
                
                if (ModelState.IsValid)
                {
                    // Obtener el usuario actual
                    User user = (User)Session["User"];
                    if (user == null)
                    {
                        ViewBag.InfoMessage = "Inicia sesión para crear un usuario";
                        GetRoles();
                        return View(model);
                    }

                    if (model.password != model.confirmPassword)
                    {
                        ModelState.AddModelError("confirmPassword", "Las contraseñas no coinciden");
                        ModelState.AddModelError("Password", "Las contraseñas no coinciden");
                        GetRoles();
                        return View(model);
                    }


                    

                    string passencripted = ComputeSha256Hash(model.password);
                    using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                    {
                        var usr = db.User.FirstOrDefault(x => x.email == model.email);
                        if (usr != null)
                        {
                            ModelState.AddModelError("email", "Este correo ya exite !!!");
                            GetRoles();
                            return View(model);
                        }
                        //if (model.Rolid == 0)
                        //{
                        //    ModelState.AddModelError("Rolid", "Seleccione un Rol");
                        //    //return View(model);
                        //}
                        //if (!ModelState.IsValid)
                        //{
                          
                        //}GetRoles();


                        User newUser = new User
                        {
                            createdAt = DateTime.Now,
                            nombre = model.nombre,
                            email = model.email,
                            password = passencripted,
                            id_Roll = model. Rolid,

                        };


                        db.User.Add(newUser);
                        db.SaveChanges();

                        User newUserAdded = db.User.FirstOrDefault(u => u.email == model.email);
                        if(newUserAdded != null)
                        {
                            sendEmailNewUser( newUserAdded.id , user.id);
                        }

                        string path = Server.MapPath("~/Logs/Usuarios/");
                        Log oLog = new Log(path);
                        oLog.Add($"Se agrega nuevo usuario por id user: {user.id} con nombre: {user.nombre}");
                        oLog.Add($"Nombre: {model.nombre}");
                        oLog.Add($"Email: {model.email}");
                        oLog.Add($"Roll: {model.Rolid}");
                    }



                    return RedirectToAction("Index");
                }
                else
                {
                    GetRoles();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = ex.Message;
                GetRoles(); 
                return View(model);
            }
        }

        // GET: User/Delete/5
        [HttpGet]
        [AuthorizeUser(idOperacion: 19)]
        public ActionResult Delete(int id)
        {
            try
            {
                using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                {
                    var UserToDelete = db.User.Find(id);
                    if (UserToDelete != null)
                    {
                        db.User.Remove(UserToDelete);
                        db.SaveChanges();

                        // Guardar log de la eliminación del usuario
                        string path = Server.MapPath("~/Logs/Usuarios/");
                        Log oLog = new Log(path);
                        oLog.Add($"Usuario con ID {id} eliminado por {((User)Session["User"]).nombre}");
                    }
                    else
                    {
                        return HttpNotFound();
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = ex.Message;
                return View();
            }
        }
        /// <summary>
        /// Metodo para cifrar la contraseña
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Metodo que envia correo al crear un nuevo usuario. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id_subjet"></param>
        public void sendEmailNewUser(int id_subjet, int id_from)
        {
            try
            {
                //logs
                string path = Server.MapPath("~/Logs/Emails/");
                Log oLog = new Log(path);
                string destinatario , origen;
                using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                {
                    var subjet = (from u in db.User where u.id == id_subjet select u).FirstOrDefault();
                    destinatario = subjet.email;
                    var from = (from u in db.User where u.id == id_from select u).FirstOrDefault();
                    origen = from.email;
                    //Remplaza el contenido del mensaje. 
                    contenidoHtml = contenidoHtml.Replace("{usuarioName}", subjet.nombre)
                             .Replace("{Rol}", GetRoles((int)subjet.id_Roll))
                             .Replace("{email}", subjet.email.ToString())
                             .Replace("{password}", "Alfer3z");
                }

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

                // Crear el mensaje de correo
                MailMessage mensaje = new MailMessage
                {
                    From = new MailAddress("contacto@negmon.com"),
                    Subject = "Bienvenido a NEGMON",
                    Body = contenidoHtml,
                    IsBodyHtml = true 
                };

                // Añadir destinatario
                mensaje.To.Add(destinatario);
                // Añadir en copia (CC)
                //mensaje.CC.Add(origen);
                
                if (origen != "jmondragon@negmon.com")
                {
                    mensaje.CC.Add("jmondragon@negmon.com");
                }
                
                // Enviar el correo
                clienteSmtp.Send(mensaje);
                oLog.Add("---------------------------");
                oLog.Add($"Correo enviado exitosamente a {destinatario} sobre la creacion de su nuevo usuario.");
                //Devuelve un mensaje exitoso a la vista 
                ViewBag.InfoMessage = $"Correo enviado exitosamente a {destinatario} sobre la creacion de su nuevo usuario.";
            }
            catch (SmtpException smtpEx)
            {
                //logs
                string path = Server.MapPath("~/Logs/Emails/");
                Log oLog = new Log(path);
                oLog.Add($"SMTP Error al enviar el correo: {smtpEx.Message}  Status Code: {smtpEx.StatusCode}");
                ViewBag.ExceptionMessage = "SMTP Error al enviar el correo: " + smtpEx.Message + " Status Code: " + smtpEx.StatusCode;
                if (smtpEx.InnerException != null)
                {
                    oLog.Add(" Inner Exception: " + smtpEx.InnerException.Message);
                    ViewBag.ExceptionMessage += " Inner Exception: " + smtpEx.InnerException.Message;
                }
            }
            catch (Exception ex)
            {
                //logs
                string path = Server.MapPath("~/Logs/Emails/");
                Log oLog = new Log(path);
                oLog.Add($"Exception al enviar el correo: {ex.Message}");
                ViewBag.ExceptionMessage = "Exception al enviar el correo: " + ex.Message;
            }
        }

        /// <summary>
        /// Devuelve a la vista una lista de los Roles del sistema 
        /// </summary>
        private string GetRoles(int id)
        {
            
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                var aux = (from s in db.Roll select s);
                if (aux != null && aux.Any())
                {
                    foreach (var a in aux)
                    {
                        if (a.id == id)
                            return a.nombre;
                       
                    }
                }
            }
            return "Sin Roll";
        }
}
}
