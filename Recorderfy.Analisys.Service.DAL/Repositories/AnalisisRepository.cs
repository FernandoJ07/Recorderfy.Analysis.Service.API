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
    public class AnalisisRepository : IAnalisisRepository
    {
        private readonly ApplicationDbContext _context;

        public AnalisisRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AnalisisCognitivo> CrearAnalisisAsync(AnalisisCognitivo analisis)
        {
            await _context.AnalisisCognitivo.AddAsync(analisis);
            await _context.SaveChangesAsync();
            return analisis;
        }

        public async Task<AnalisisCognitivo> ObtenerPorIdAsync(Guid id)
        {
            return await _context.AnalisisCognitivo
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<AnalisisCognitivo>> ObtenerPorPacienteAsync(Guid pacienteId)
        {
            return await _context.AnalisisCognitivo
                .AsNoTracking()
                .Where(a => a.PacienteId == pacienteId)
                .OrderByDescending(a => a.FechaAnalisis)
                .ToListAsync();
        }

        public async Task<AnalisisCognitivo> ObtenerLineaBasePorPacienteAsync(Guid pacienteId)
        {
            return await _context.AnalisisCognitivo
                .Where(a => a.PacienteId == pacienteId && a.EsLineaBase)
                .OrderByDescending(a => a.FechaAnalisis)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AnalisisCognitivo>> ObtenerSeguimientosPorPacienteAsync(Guid pacienteId)
        {
            return await _context.AnalisisCognitivo
                .AsNoTracking()
                .Where(a => a.PacienteId == pacienteId && !a.EsLineaBase)
                .OrderByDescending(a => a.FechaAnalisis)
                .ToListAsync();
        }

        public async Task<bool> ExisteLineaBaseAsync(Guid pacienteId)
        {
            return await _context.LineasBase
                .AnyAsync(lb => lb.PacienteId == pacienteId && lb.Activa);
        }

        public async Task<LineaBase> ObtenerLineaBaseActivaAsync(Guid pacienteId)
        {
            return await _context.LineasBase
                .Where(lb => lb.PacienteId == pacienteId && lb.Activa)
                .OrderByDescending(lb => lb.FechaEstablecimiento)
                .FirstOrDefaultAsync();
        }

        public async Task<LineaBase> CrearLineaBaseAsync(LineaBase lineaBase)
        {
            // Desactivar líneas base anteriores del mismo paciente
            var lineasBaseAnteriores = await _context.LineasBase
                .Where(lb => lb.PacienteId == lineaBase.PacienteId && lb.Activa)
                .ToListAsync();

            foreach (var lb in lineasBaseAnteriores)
            {
                lb.Activa = false;
            }

            await _context.LineasBase.AddAsync(lineaBase);
            await _context.SaveChangesAsync();
            return lineaBase;
        }

        public async Task ActualizarLineaBaseAsync(LineaBase lineaBase)
        {
            _context.LineasBase.Update(lineaBase);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AnalisisCognitivo>> ObtenerAnalisisConDeterioroAsync()
        {
            return await _context.AnalisisCognitivo
                .AsNoTracking()
                .Where(a => a.DeterioroDetectado == true)
                .OrderByDescending(a => a.FechaAnalisis)
                .ToListAsync();
        }

        // ==================== MÉTODOS PARA EVALUACIONES COMPLETAS ====================

        public async Task<EvaluacionCompleta> CrearEvaluacionCompletaAsync(EvaluacionCompleta evaluacion)
        {
            await _context.EvaluacionesCompletas.AddAsync(evaluacion);
            await _context.SaveChangesAsync();
            return evaluacion;
        }

        public async Task<EvaluacionCompleta> ObtenerEvaluacionCompletaPorIdAsync(Guid evaluacionId)
        {
            return await _context.EvaluacionesCompletas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == evaluacionId);
        }

        public async Task<List<EvaluacionCompleta>> ObtenerEvaluacionesPorPacienteAsync(Guid pacienteId)
        {
            return await _context.EvaluacionesCompletas
                .AsNoTracking()
                .Where(e => e.PacienteId == pacienteId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .ToListAsync();
        }
    }
}
