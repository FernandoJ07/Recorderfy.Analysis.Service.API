using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Recorderfy.Analisys.Service.Model.Entities
{
    [Table("linea_base")]
    public class LineaBase
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("paciente_id")]
        public Guid PacienteId { get; set; }

        [Required]
        [Column("score_global_inicial")]
        public float ScoreGlobalInicial { get; set; }

        [Required]
        [Column("fecha_establecimiento")]
        public DateTime FechaEstablecimiento { get; set; }

        [Column("cantidad_evaluaciones")]
        public int CantidadEvaluaciones { get; set; }

        [Column("ultima_evaluacion")]
        public DateTime? UltimaEvaluacion { get; set; }

        [Column("activa")]
        public bool Activa { get; set; }

        [Column("notas")]
        [MaxLength(1000)]
        public string Notas { get; set; }

        [JsonIgnore] // Evitar ciclo de serialización - usar DTOs para incluir análisis
        public virtual ICollection<AnalisisCognitivo> Analisis { get; set; }

        public LineaBase()
        {
            Id = Guid.NewGuid();
            FechaEstablecimiento = DateTime.UtcNow;
            Activa = true;
            CantidadEvaluaciones = 0;
            Analisis = new List<AnalisisCognitivo>();
        }
    }
}
