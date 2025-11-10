using System;
using System.ComponentModel.DataAnnotations;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    /// <summary>
    /// Request para realizar un análisis cognitivo
    /// </summary>
    public class AnalisisRequest
    {
        [Required(ErrorMessage = "El ID del paciente es obligatorio")]
        public Guid PacienteId { get; set; }

        [Required(ErrorMessage = "El ID de la imagen es obligatorio")]
        public Guid ImagenId { get; set; }

        [Required(ErrorMessage = "La descripción del paciente es obligatoria")]
        [MaxLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
        public string DescripcionPaciente { get; set; }

        [Required(ErrorMessage = "La descripción real de la imagen es obligatoria")]
        [MaxLength(2000, ErrorMessage = "La descripción real no puede exceder 2000 caracteres")]
        public string DescripcionReal { get; set; }
    }
}
