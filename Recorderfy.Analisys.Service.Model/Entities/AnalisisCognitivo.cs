using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Recorderfy.Analisys.Service.Model.Entities
{
    [Table("analisis_cognitivo")]
    public class AnalisisCognitivo
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("paciente_id")]
        public Guid PacienteId { get; set; }

        [Required]
        [Column("imagen_id")]
        public Guid ImagenId { get; set; }

        [Required]
        [Column("descripcion_paciente")]
        [MaxLength(2000)]
        public string DescripcionPaciente { get; set; }

        [Required]
        [Column("descripcion_real")]
        [MaxLength(2000)]
        public string DescripcionReal { get; set; }

        [Column("metadata_imagen")]
        public string MetadataImagen { get; set; } // JSON

        [Required]
        [Column("score_semantico")]
        public float ScoreSemantico { get; set; }

        [Required]
        [Column("score_objetos")]
        public float ScoreObjetos { get; set; }

        [Required]
        [Column("score_acciones")]
        public float ScoreAcciones { get; set; }

        [Required]
        [Column("falsos_objetos")]
        public int FalsosObjetos { get; set; }

        [Required]
        [Column("tiempo_respuesta_seg")]
        public float TiempoRespuestaSeg { get; set; }

        [Required]
        [Column("coherencia_linguistica")]
        public float CoherenciaLinguistica { get; set; }

        [Required]
        [Column("score_global")]
        public float ScoreGlobal { get; set; }

        [Column("observaciones")]
        [MaxLength(5000)]
        public string Observaciones { get; set; }

        [Column("diferencia_score")]
        public float? DiferenciaScore { get; set; }

        [Column("deterioro_detectado")]
        public bool? DeterioroDetectado { get; set; }

        [Column("nivel_cambio")]
        [MaxLength(50)]
        public string NivelCambio { get; set; } // estable | leve | moderado | severo

        [Required]
        [Column("es_linea_base")]
        public bool EsLineaBase { get; set; }

        [Column("linea_base_id")]
        public Guid? LineaBaseId { get; set; }

        [ForeignKey("LineaBaseId")]
        [JsonIgnore] // Evitar ciclo de serialización
        public virtual LineaBase LineaBase { get; set; }

        [Required]
        [Column("fecha_analisis")]
        public DateTime FechaAnalisis { get; set; }

        [Column("respuesta_llm_completa")]
        public string RespuestaLlmCompleta { get; set; } // JSON completo del LLM

        public AnalisisCognitivo()
        {
            Id = Guid.NewGuid();
            FechaAnalisis = DateTime.UtcNow;
        }
    }
}
