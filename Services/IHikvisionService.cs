using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS360.Models.ViewModel;

namespace TAS360.Services
{
    /// <summary>
    /// Define las operaciones principales disponibles para interactuar con dispositivos Hikvision
    /// a través de ISAPI, de forma que puedan inyectarse y reemplazarse fácilmente en escenarios de pruebas.
    /// </summary>
    public interface IHikvisionService
    {
        /// <summary>
        /// Ejecuta una llamada genérica al endpoint ISAPI especificado y devuelve la respuesta cruda.
        /// </summary>
        /// <param name="url">Dirección completa del recurso ISAPI que se desea consumir.</param>
        /// <param name="user">Nombre de usuario con permisos en el dispositivo Hikvision.</param>
        /// <param name="pass">Contraseña correspondiente al usuario indicado.</param>
        /// <returns>Contenido de la respuesta retornada por el dispositivo, en formato de texto.</returns>
        Task<string> ConsumeISAPI(string url, string user, string pass);

        /// <summary>
        /// Obtiene la información general del dispositivo Hikvision remoto.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario autorizado a consultar el dispositivo.</param>
        /// <param name="pass">Contraseña asociada al usuario autorizado.</param>
        /// <returns>Respuesta en texto plano con los metadatos del dispositivo.</returns>
        Task<string> GetDeviceInfo(string apiServer, string user, string pass);

        /// <summary>
        /// Recupera el listado de interfaces de red disponibles en el dispositivo Hikvision.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos para consultar la configuración de red.</param>
        /// <param name="pass">Contraseña asociada al usuario con permisos.</param>
        /// <returns>Respuesta cruda que describe las interfaces de red configuradas.</returns>
        Task<string> GetNetworkInterfaces(string apiServer, string user, string pass);

        /// <summary>
        /// Obtiene la información de personas registrada en el dispositivo Hikvision de manera paginada.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos para consultar la base de personas.</param>
        /// <param name="password">Contraseña del usuario autorizado.</param>
        /// <returns>Una tupla con la lista de personas, el número de registros totales y la página actual.</returns>
        Task<(List<UserInfo>, int, int)> GetPeopleInfoAsync(string apiServer, string user, string password);

        /// <summary>
        /// Crea un nuevo usuario dentro del dispositivo Hikvision con los datos proporcionados.
        /// </summary>
        /// <param name="model">Información del usuario que se registrará en el dispositivo.</param>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="usuario">Usuario administrador o con permisos de escritura.</param>
        /// <param name="password">Contraseña del usuario administrador.</param>
        /// <returns><c>true</c> si el usuario se creó correctamente; de lo contrario, <c>false</c>.</returns>
        Task<bool> CrearUsuarioDispositivoAsync(HikvisionUserViewModel model, string apiServer, string usuario, string password);

        /// <summary>
        /// Recupera la información de un usuario del dispositivo Hikvision a partir de su identificador interno.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos de lectura.</param>
        /// <param name="password">Contraseña del usuario con permisos de lectura.</param>
        /// <param name="employeeNo">Identificador del usuario dentro del dispositivo Hikvision.</param>
        /// <returns>Modelo con los datos del usuario recuperado.</returns>
        Task<UserInfo> ObtenerUsuarioPorIdAsync(string apiServer, string user, string password, string employeeNo);

        /// <summary>
        /// Actualiza la información de un usuario existente en el dispositivo Hikvision.
        /// </summary>
        /// <param name="model">Datos del usuario que serán actualizados.</param>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="usuario">Usuario con permisos de modificación.</param>
        /// <param name="password">Contraseña del usuario con permisos de modificación.</param>
        /// <returns><c>true</c> si la actualización se realizó correctamente; de lo contrario, <c>false</c>.</returns>
        Task<bool> EditarUsuarioDispositivoAsync(HikvisionUserViewModel model, string apiServer, string usuario, string password);

        /// <summary>
        /// Elimina un usuario del dispositivo Hikvision con base en su identificador interno.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="usuario">Usuario con privilegios para eliminar registros.</param>
        /// <param name="password">Contraseña del usuario con privilegios.</param>
        /// <param name="employeeNo">Identificador del usuario que será eliminado.</param>
        /// <returns><c>true</c> si el usuario se eliminó exitosamente; de lo contrario, <c>false</c>.</returns>
        Task<bool> EliminarUsuarioDispositivoAsync(string apiServer, string usuario, string password, string employeeNo);

        /// <summary>
        /// Verifica el estado de conectividad del dispositivo Hikvision y mide la latencia de respuesta.
        /// </summary>
        /// <param name="baseUrl">Dirección base del dispositivo que se desea validar.</param>
        /// <param name="user">Usuario con permisos para autenticar contra el dispositivo.</param>
        /// <param name="pass">Contraseña asociada al usuario autenticado.</param>
        /// <param name="timeoutMs">Tiempo máximo de espera en milisegundos para la operación.</param>
        /// <returns>Resultado detallado del chequeo que indica disponibilidad, código de estado y errores.</returns>
        Task<DeviceCheckResult> CheckStatusAsync(string baseUrl, string user, string pass, int timeoutMs = 2500);

        /// <summary>
        /// Recupera el historial de eventos registrado por el dispositivo Hikvision dentro del intervalo indicado.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos para consultar el historial de eventos.</param>
        /// <param name="pass">Contraseña asociada al usuario autorizado.</param>
        /// <param name="start">Fecha y hora inicial (incluida) para acotar la búsqueda.</param>
        /// <param name="end">Fecha y hora final (incluida) para acotar la búsqueda.</param>
        /// <param name="maxResults">Cantidad máxima de eventos a recuperar.</param>
        /// <returns>Listado de eventos convertidos a un modelo de presentación.</returns>
        Task<IReadOnlyCollection<HikvisionEventViewModel>> GetEventsAsync(string apiServer, string user, string pass, DateTime start, DateTime end, int maxResults = 100);
    }
}
