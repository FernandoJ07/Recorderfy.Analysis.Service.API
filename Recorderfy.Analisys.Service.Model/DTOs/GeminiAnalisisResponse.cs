namespace Recorderfy.Analisys.Service.Model.DTOs
{
    public class GeminiAnalisisResponse
    {
        public float score_semantico { get; set; }
        public float score_objetos { get; set; }
        public float score_acciones { get; set; }
        public int falsos_objetos { get; set; }
        public float tiempo_respuesta_seg { get; set; }
        public float coherencia_linguistica { get; set; }
        public float score_global { get; set; }
        public string observaciones { get; set; }
        public ComparacionBaseline comparacion_con_baseline { get; set; }
    }

    public class ComparacionBaseline
    {
        public float diferencia_score { get; set; }
        public bool deterioro_detectado { get; set; }
        public string nivel_cambio { get; set; }
    }
}
