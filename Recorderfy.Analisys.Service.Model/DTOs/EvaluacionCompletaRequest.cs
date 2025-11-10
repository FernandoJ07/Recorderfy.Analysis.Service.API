using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    /// <summary>
    /// Request para una evaluación completa con múltiples preguntas/imágenes
    /// </summary>
    public class EvaluacionCompletaRequest
    {
        [Required]
        public string IdPaciente { get; set; }

        [Required]
        public string IdCuidador { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe incluir al menos una pregunta")]
        public List<PreguntaEvaluacion> Preguntas { get; set; }

        [Required]
        public DateTime FechaRealizacion { get; set; }

        public int Puntaje { get; set; } = 0;
    }

    /// <summary>
    /// Representa una pregunta individual dentro de la evaluación
    /// </summary>
    public class PreguntaEvaluacion
    {
        [Required]
        public string IdPicture { get; set; }

        [Required]
        public string ImagenUrl { get; set; }

        [Required]
        public string DescripcionReal { get; set; }

        [Required]
        public string PacienteRespuesta { get; set; }
    }
}
