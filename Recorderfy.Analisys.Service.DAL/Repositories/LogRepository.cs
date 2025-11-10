using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Recorderfy.Analisys.Service.DAL.Data;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analisys.Service.DAL.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly ApplicationDbContext _context;

        public LogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(string nivel, string componente, string mensaje, 
            string excepcion = null, string datosAdicionales = null, 
            Guid? usuarioId = null, string endpoint = null)
        {
            try
            {
                var log = new LogSistema
                {
                    Nivel = nivel.ToUpper(),
                    Componente = componente,
                    Mensaje = mensaje,
                    Excepcion = excepcion,
                    DatosAdicionales = datosAdicionales,
                    UsuarioId = usuarioId,
                    Endpoint = endpoint
                };

                await _context.LogsSistema.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Si falla el logging en BD, al menos escribir en consola
                Console.WriteLine($"[ERROR LOG] {DateTime.UtcNow} - {nivel} - {componente}: {mensaje}");
                Console.WriteLine($"[ERROR LOG EXCEPTION] {ex.Message}");
            }
        }

        public async Task<List<LogSistema>> ObtenerLogsAsync(int cantidad = 100, string nivel = null)
        {
            var query = _context.LogsSistema.AsQueryable();

            if (!string.IsNullOrEmpty(nivel))
            {
                query = query.Where(l => l.Nivel == nivel.ToUpper());
            }

            return await query
                .OrderByDescending(l => l.FechaRegistro)
                .Take(cantidad)
                .ToListAsync();
        }

        public async Task<List<LogSistema>> ObtenerLogsPorFechaAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.LogsSistema
                .Where(l => l.FechaRegistro >= fechaInicio && l.FechaRegistro <= fechaFin)
                .OrderByDescending(l => l.FechaRegistro)
                .ToListAsync();
        }

        public async Task<List<LogSistema>> ObtenerErroresRecientesAsync(int horas = 24)
        {
            var fechaLimite = DateTime.UtcNow.AddHours(-horas);
            
            return await _context.LogsSistema
                .Where(l => l.Nivel == "ERROR" && l.FechaRegistro >= fechaLimite)
                .OrderByDescending(l => l.FechaRegistro)
                .ToListAsync();
        }

        public async Task LimpiarLogsAntiguosAsync(int diasRetencion = 30)
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-diasRetencion);
            
            var logsAntiguos = await _context.LogsSistema
                .Where(l => l.FechaRegistro < fechaLimite)
                .ToListAsync();

            if (logsAntiguos.Any())
            {
                _context.LogsSistema.RemoveRange(logsAntiguos);
                await _context.SaveChangesAsync();
            }
        }
    }
}
