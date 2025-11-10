using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analisys.Service.DAL.Interfaces
{
    public interface ILogRepository
    {
        Task RegistrarAsync(string nivel, string componente, string mensaje, 
            string excepcion = null, string datosAdicionales = null, 
            Guid? usuarioId = null, string endpoint = null);
        
        Task<List<LogSistema>> ObtenerLogsAsync(int cantidad = 100, string nivel = null);
        Task<List<LogSistema>> ObtenerLogsPorFechaAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<List<LogSistema>> ObtenerErroresRecientesAsync(int horas = 24);
        Task LimpiarLogsAntiguosAsync(int diasRetencion = 30);
    }
}
