using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Models;

namespace TAS360.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple =false)]
    public class AuthorizeUser : AuthorizeAttribute
    {
        private User oUser;
        private HelpDesk_Entities1 db = new HelpDesk_Entities1();
        private int idOperacion;
        private string nombreOperacionConfigurada;

        public AuthorizeUser(int idOperacion = 0)
        {
            this.idOperacion = idOperacion;
        }

        /// <summary>
        /// Permite autorizar por el nombre estable de la operacion y evita depender
        /// de IDs identity que pueden cambiar entre ambientes.
        /// </summary>
        public AuthorizeUser(string nombreOperacion)
        {
            nombreOperacionConfigurada = nombreOperacion;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            //base.OnAuthorization(filterContext);
            string nombreOperacion = "";
            string nombreModulo = "";

            try
            {              
               
                oUser = (User)HttpContext.Current.Session["User"];
                if (oUser == null)
                {
                    filterContext.Result = new RedirectResult("~/Acceso/Login");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(nombreOperacionConfigurada))
                    {
                        var operacionConfigurada = db.Operacion
                            .FirstOrDefault(x => x.nombre == nombreOperacionConfigurada);

                        if (operacionConfigurada == null)
                        {
                            nombreOperacion = nombreOperacionConfigurada;
                            filterContext.Result = new RedirectResult("~/Error/UnauthorizedOperation?operacion=" +
                                HttpUtility.UrlEncode(nombreOperacion) +
                                "&modulo=Documentos&message=" +
                                HttpUtility.UrlEncode("La operacion no esta registrada en la base de datos."));
                            return;
                        }

                        idOperacion = operacionConfigurada.id;
                    }

                    var MyOperationsList = from Op in db.Roll_Operacion
                                           where Op.id_Roll == oUser.id_Roll && Op.id_Operacion == idOperacion
                                           select Op;
                    if (MyOperationsList.ToList().Count() == 0)
                    {
                        var oOperation = db.Operacion.Find(idOperacion);
                        int? IdModulo = oOperation?.id_Modulo == null ? idOperacion : oOperation?.id_Modulo;
                        nombreOperacion = getNombreDeOperacion(idOperacion);
                        nombreModulo = getNombreModulo(IdModulo);
                        filterContext.Result = new RedirectResult("~/Error/UnauthorizedOperation?operacion=" + nombreOperacion + "?modulo=" + nombreModulo + "?message=No tienes privilegios de acceso, verificalo con el administrador del sistema");
                    }
                }
                
            }
            catch(Exception ex)
            {
                filterContext.Result = new RedirectResult("~/Error/UnauthorizedOperation?operacion=" + nombreOperacion + "?modulo=" + nombreModulo + "?message=" + ex.Message);
            }
            
        }

        private string getNombreDeOperacion(int id)
        {
            var nombre = from op in db.Operacion
                         where op.id == id 
                         select op.nombre;
            String Nombre = "";
            try
            {
                Nombre = nombre.First();
            }
            catch (Exception)
            {

            }

            return Nombre;
        }

        private string getNombreModulo(int? id)
        {
            var nombre = from m in db.Modulo
                         where m.id == id
                         select m.nombre;
            String Nombre = "";
            try
            {
                Nombre = nombre.First();
            }
            catch (Exception)
            {

            }
            return Nombre;
        }
    }
}
