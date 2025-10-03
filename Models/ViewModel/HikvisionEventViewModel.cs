using System;
using System.Collections.Generic;

namespace TAS360.Models.ViewModel
{
    
    public class AcsEventEnvelope
    {
        public HikvisionEventViewModel AcsEvent { get; set; }
    }

    
    public class HikvisionEventViewModel
    {
        public HikvisionEventViewModel() { InfoList = new List<InfoItem>(); }

        public string searchID { get; set; }
        public int totalMatches { get; set; }
        public string responseStatusStrg { get; set; }
        public int numOfMatches { get; set; }
        public List<InfoItem> InfoList { get; set; }
    }

    
    public class InfoItem
    {
        public int major { get; set; }
        public int? minor { get; set; }              // <- hazlo nullable para detectar ausencia
        public string time { get; set; }
        public string serialNo { get; set; }
        public int? cardType { get; set; }
        public string currentVerifyMode { get; set; }
        public string mask { get; set; }
        public int? cardReaderNo { get; set; }
        public int? doorNo { get; set; }

        // Si tu equipo los entrega, inclúyelos:
        public string name { get; set; }
        public string employeeNo { get; set; }
        public string cardNo { get; set; }
    }
}

