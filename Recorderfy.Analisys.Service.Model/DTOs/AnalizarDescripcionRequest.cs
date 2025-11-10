using System;
using System.ComponentModel.DataAnnotations;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    public class AnalizarDescripcionRequest
    {
        [Required(ErrorMessage = "El ID del paciente es obligatorio")]
        public Guid PacienteId { get; set; }

        [Required(ErrorMessage = "El ID de la imagen es obligatorio")]
        public Guid ImagenId { get; set; }

        [Required(ErrorMessage = "La descripción del paciente es obligatoria")]
        [MaxLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
        public string DescripcionPaciente { get; set; }

        public float TiempoRespuestaSeg { get; set; }

        public bool EsLineaBase { get; set; } = false;
    }
}
