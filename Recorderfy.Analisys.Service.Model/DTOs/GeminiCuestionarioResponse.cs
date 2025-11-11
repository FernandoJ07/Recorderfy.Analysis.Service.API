using System.Collections.Generic;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    /// <summary>
    /// Respuesta de Gemini para análisis de cuestionario completo
    /// </summary>
    public class GeminiCuestionarioResponse
    {
        public List<ResultadoPreguntaGemini> resultados_preguntas { get; set; }
        public ResumenGeneralGemini resumen_general { get; set; }
        public ComparacionBaseline comparacion_con_baseline { get; set; }
    }

    /// <summary>
    /// Resultado individual de cada pregunta analizada por Gemini
    /// </summary>
    public class ResultadoPreguntaGemini
    {
        public int numero_pregunta { get; set; }
        public float score_semantico { get; set; }
        public float score_objetos { get; set; }
        public float score_acciones { get; set; }
        public int falsos_objetos { get; set; }
        public float coherencia_linguistica { get; set; }
        public float score_global { get; set; }
        public string observaciones { get; set; }
    }

    /// <summary>
    /// Resumen general del cuestionario
    /// </summary>
    public class ResumenGeneralGemini
    {
        public float score_global_promedio { get; set; }
        public float score_semantico_promedio { get; set; }
        public float score_objetos_promedio { get; set; }
        public float score_acciones_promedio { get; set; }
        public float coherencia_promedio { get; set; }
        public int total_falsos_objetos { get; set; }
        public float tiempo_respuesta_promedio_seg { get; set; }
        public string observaciones_generales { get; set; }
        public string recomendaciones_medicas { get; set; }
        public string nivel_deterioro { get; set; }
        public bool deterioro_detectado { get; set; }
    }
}
