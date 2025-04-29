using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class G_TicketsModificadosViewModel
    {
        public int id_ticket_editado { get; set; }
        public int Cantidad_modificaciones { get; set; }
        public DateTime Mas_Antiguo { get; set; }
        public DateTime Mas_Reciente { get; set; }
    }
}