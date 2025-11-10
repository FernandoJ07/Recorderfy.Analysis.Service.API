using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recorderfy.Analisys.Service.Model.Entities
{
    /// <summary>
    /// Entidad que representa una evaluación completa (sesión de múltiples preguntas)
    /// </summary>
    [Table("evaluacion_completa")]
    public class EvaluacionCompleta
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("paciente_id")]
        public Guid PacienteId { get; set; }

        [Required]
        [Column("cuidador_id")]
        public Guid CuidadorId { get; set; }

        [Required]
        [Column("fecha_evaluacion")]
        public DateTime FechaEvaluacion { get; set; }

        [Column("total_preguntas")]
        public int TotalPreguntas { get; set; }

        [Column("preguntas_procesadas")]
        public int PreguntasProcesadas { get; set; }

        // Scores agregados
        [Column("score_global_promedio")]
        public float ScoreGlobalPromedio { get; set; }

        [Column("score_semantico_promedio")]
        public float ScoreSemanticoPromedio { get; set; }

        [Column("score_objetos_promedio")]
        public float ScoreObjetosPromedio { get; set; }

        [Column("score_acciones_promedio")]
        public float ScoreAccionesPromedio { get; set; }

        [Column("coherencia_promedio")]
        public float CoherenciaPromedio { get; set; }

        [Column("tiempo_respuesta_promedio")]
        public float TiempoRespuestaPromedio { get; set; }

        // Deterioro general
        [Column("deterioro_detectado")]
        public bool DeterioroDetectado { get; set; }

        [Column("nivel_deterioro_general")]
        [MaxLength(50)]
        public string NivelDeterioroGeneral { get; set; }

        [Column("diferencia_con_linea_base")]
        public float? DiferenciaConLineaBase { get; set; }

        // Observaciones
        [Column("observaciones_generales")]
        public string ObservacionesGenerales { get; set; }

        [Column("recomendaciones_medicas")]
        public string RecomendacionesMedicas { get; set; }

        // Relación con línea base
        [Column("es_linea_base")]
        public bool EsLineaBase { get; set; }

        [Column("linea_base_id")]
        public Guid? LineaBaseId { get; set; }

        [ForeignKey("LineaBaseId")]
        public LineaBase LineaBase { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
    }
}
