using System.Threading.Tasks;
using Recorderfy.Analisys.Service.Model.DTOs;

namespace Recorderfy.Analisys.Service.BLL.Interfaces
{
    public interface IGeminiService
    {
        Task<GeminiAnalisisResponse> AnalizarConGeminiAsync(
            string descripcionPaciente, 
            string descripcionReal, 
            string metadataJson,
            float? scoreBaselinePrevio = null);

        /// <summary>
        /// Analiza un cuestionario completo (múltiples preguntas) en una sola llamada
        /// </summary>
        Task<GeminiCuestionarioResponse> AnalizarCuestionarioCompletoAsync(
            List<AnalisisRequest> preguntas,
            float? scoreBaselinePrevio = null);
    }
}
