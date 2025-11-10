using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recorderfy.Analisys.Service.Model.Entities
{
    [Table("log_sistema")]
    public class LogSistema
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("nivel")]
        [MaxLength(20)]
        public string Nivel { get; set; } // INFO, WARNING, ERROR, DEBUG

        [Required]
        [Column("componente")]
        [MaxLength(200)]
        public string Componente { get; set; }

        [Required]
        [Column("mensaje")]
        [MaxLength(5000)]
        public string Mensaje { get; set; }

        [Column("excepcion")]
        public string Excepcion { get; set; }

        [Column("datos_adicionales")]
        public string DatosAdicionales { get; set; } // JSON

        [Required]
        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_id")]
        public Guid? UsuarioId { get; set; }

        [Column("endpoint")]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        public LogSistema()
        {
            Id = Guid.NewGuid();
            FechaRegistro = DateTime.UtcNow;
        }
    }
}
