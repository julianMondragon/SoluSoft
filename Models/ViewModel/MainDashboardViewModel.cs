using DocumentFormat.OpenXml.Office2013.Word;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TAS360.Models.ViewModel
{
    public class MainDashboardViewModel
    {
        public MainDashboardViewModel() 
        {
            DeviceInfo = new DeviceInfo();
            NetworkInterfaces = new List<NetworkInterface>();
            Personas = new List<UserInfo>();

        }
        public DeviceInfo DeviceInfo { get; set; }
        public int CapacidadEvento { get; set; } // Eventos almacenados
        public int MaxEventos { get; set; }      // Capacidad máxima
        public string RawXML { get; set; }       // Por si quieres mostrar el XML crudo
        public List<NetworkInterface> NetworkInterfaces { get; set; } //Lista de interfaces de red
        public List<UserInfo> Personas { get; set; } //Lista de personas
        public int NumPersonas { get; set; }
        public int TotalPersonas { get; set; }
    }

    public class DeviceInfo
    {
        public string DeviceName { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public string Model { get; set; }
    }

    public class NetworkInterface
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public string ConnectionType { get; set; }
        public string LinkStatus { get; set; }
        public string WirelessStatus { get; set; }
    }
 
    public class UserInfoSearch
    {
        public string searchID { get; set; }
        public string responseStatusStr { get; set; }
        public int numOfMatches { get; set; }
        public int totalMatches { get; set; }
        public List<UserInfo> UserInfo { get; set; }
    }

    public class UserInfo
    {
        public string employeeNo { get; set; }
        public string name { get; set; }
        public string userType { get; set; }
        public ValidPeriod valid { get; set; }
        public string password { get; set; }
        public string belongGroup { get; set; }
        public string doorRight { get; set; }
    }

    public class ValidPeriod
    {
        public bool enable { get; set; }
        public string beginTime { get; set; }
        public string endTime { get; set; }
        public string timeType { get; set; }
    }

}