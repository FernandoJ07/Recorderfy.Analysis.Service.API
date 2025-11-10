using System;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    public class AnalisisResponse
    {
        public Guid AnalisisId { get; set; }
        public Guid PacienteId { get; set; }
        public Guid ImagenId { get; set; }
        public float ScoreSemantico { get; set; }
        public float ScoreObjetos { get; set; }
        public float ScoreAcciones { get; set; }
        public int FalsosObjetos { get; set; }
        public float TiempoRespuestaSeg { get; set; }
        public float CoherenciaLinguistica { get; set; }
        public float ScoreGlobal { get; set; }
        public string Observaciones { get; set; }
        public ComparacionBaselineDto ComparacionConBaseline { get; set; }
        public bool EsLineaBase { get; set; }
        public DateTime FechaAnalisis { get; set; }
        public string Mensaje { get; set; }
    }

    public class ComparacionBaselineDto
    {
        public float? DiferenciaScore { get; set; }
        public bool DeterioroDetectado { get; set; }
        public string NivelCambio { get; set; } // estable | leve | moderado | severo
    }
}
