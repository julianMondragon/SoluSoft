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
        }
        public DeviceInfo DeviceInfo { get; set; }
        public int CapacidadEvento { get; set; } // Eventos almacenados
        public int MaxEventos { get; set; }      // Capacidad máxima
        public string RawXML { get; set; }       // Por si quieres mostrar el XML crudo
        public List<NetworkInterface> NetworkInterfaces { get; set; } //Lista de interfaces de red
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
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public string ConnectionType { get; set; }
        public string LinkStatus { get; set; }
        public string WirelessStatus { get; set; }
    }
}