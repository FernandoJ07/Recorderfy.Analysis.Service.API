using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analisys.Service.DAL.Interfaces
{
    public interface IAnalisisRepository
    {
        Task<AnalisisCognitivo> CrearAnalisisAsync(AnalisisCognitivo analisis);
        Task<AnalisisCognitivo> ObtenerPorIdAsync(Guid id);
        Task<List<AnalisisCognitivo>> ObtenerPorPacienteAsync(Guid pacienteId);
        Task<AnalisisCognitivo> ObtenerLineaBasePorPacienteAsync(Guid pacienteId);
        Task<List<AnalisisCognitivo>> ObtenerSeguimientosPorPacienteAsync(Guid pacienteId);
        Task<bool> ExisteLineaBaseAsync(Guid pacienteId);
        Task<LineaBase> ObtenerLineaBaseActivaAsync(Guid pacienteId);
        Task<LineaBase> CrearLineaBaseAsync(LineaBase lineaBase);
        Task ActualizarLineaBaseAsync(LineaBase lineaBase);
        Task<List<AnalisisCognitivo>> ObtenerAnalisisConDeterioroAsync();
    }
}
