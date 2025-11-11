using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorderfy.Analisys.Service.Model.DTOs;

namespace Recorderfy.Analisys.Service.BLL.Interfaces
{
    public interface IAnalisisService
    {
        // Análisis individual (backward compatibility)
        Task<AnalisisResponse> RealizarAnalisisAsync(AnalisisRequest request);

        // Análisis múltiple para línea base
        Task<List<AnalisisResponse>> RealizarAnalisisMultipleAsync(List<AnalisisRequest> requests, Guid pacienteId);

        Task<AnalisisResponse> ObtenerAnalisisPorIdAsync(int id);
        Task<List<AnalisisResponse>> ObtenerHistorialPacienteAsync(int pacienteId);

        // Evaluación completa (múltiples preguntas)
        Task<EvaluacionCompletaResponse> ProcesarEvaluacionCompletaAsync(EvaluacionCompletaRequest request);
        Task<EvaluacionCompletaResponse> ObtenerEvaluacionCompletaPorIdAsync(Guid evaluacionId);
        Task<List<EvaluacionCompletaResponse>> ObtenerEvaluacionesPorPacienteAsync(Guid pacienteId);

    }
}
