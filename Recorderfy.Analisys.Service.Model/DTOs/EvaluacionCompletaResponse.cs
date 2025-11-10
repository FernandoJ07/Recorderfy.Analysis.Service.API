using System;
using System.Collections.Generic;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    /// <summary>
    /// Respuesta de una evaluación completa procesada
    /// </summary>
    public class EvaluacionCompletaResponse
    {
        public Guid EvaluacionId { get; set; }
        public Guid PacienteId { get; set; }
        public Guid CuidadorId { get; set; }
        public DateTime FechaEvaluacion { get; set; }
        public int TotalPreguntas { get; set; }
        public int PreguntasProcesadas { get; set; }
        
        // Scores agregados de todas las preguntas
        public float ScoreGlobalPromedio { get; set; }
        public float ScoreSemanticoPromedio { get; set; }
        public float ScoreObjetosPromedio { get; set; }
        public float ScoreAccionesPromedio { get; set; }
        public float CoherenciaPromedio { get; set; }
        public float TiempoRespuestaPromedio { get; set; }
        
        // Deterioro general
        public bool DeterioroDetectado { get; set; }
        public string NivelDeterioroGeneral { get; set; } // estable, leve, moderado, severo
        public float? DiferenciaConLineaBase { get; set; }
        
        // Resultados individuales por pregunta
        public List<ResultadoPregunta> ResultadosIndividuales { get; set; }
        
        // Observaciones generales
        public string ObservacionesGenerales { get; set; }
        public string RecomendacionesMedicas { get; set; }
        
        // Línea base
        public bool EsLineaBase { get; set; }
        public Guid? LineaBaseId { get; set; }
        
        public DateTime FechaProcesamiento { get; set; }
    }

    /// <summary>
    /// Resultado del análisis de una pregunta individual
    /// </summary>
    public class ResultadoPregunta
    {
        public string IdPicture { get; set; }
        public Guid AnalisisId { get; set; }
        public string ImagenUrl { get; set; }
        public string DescripcionReal { get; set; }
        public string PacienteRespuesta { get; set; }
        
        // Scores individuales
        public float ScoreGlobal { get; set; }
        public float ScoreSemantico { get; set; }
        public float ScoreObjetos { get; set; }
        public float ScoreAcciones { get; set; }
        public int FalsosObjetos { get; set; }
        public float CoherenciaLinguistica { get; set; }
        
        // Observaciones específicas
        public string Observaciones { get; set; }
        public string NivelCambio { get; set; }
        public bool DeterioroDetectado { get; set; }
    }
}
