using System;
using System.Collections.Generic;

namespace TAS360.Models.ViewModel
{
    /// <summary>
    /// Representa un evento generado por un dispositivo Hikvision a través del API de control de acceso.
    /// </summary>
    public class HikvisionEventViewModel
    {
        public HikvisionEventViewModel() 
        { 
            InfoList = new List<InfoList>();
        }    
        public string serchID { get; set; }
        public int totalMatches { get; set; }
        public string responseStatusStrg { get; set; }
        public int numOfMatches { get; set; }
        public List<InfoList> InfoList { get; set; }
    }
    public class InfoList
    {
        public int major { get; set; }
        public int minor { get; set; }
        public DateTime time { get; set; }
        public int serialNo { get; set; }
        public int cardType { get; set; }
        public string currentVerifyMode { get; set; }
        public string mask { get; set; }
        public int cardReaderNo { get; set; }
        public int doorNo { get; set; }
        public string remoteHostAddr { get;set; }
    }
}
