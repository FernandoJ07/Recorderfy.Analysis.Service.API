using Microsoft.EntityFrameworkCore;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analisys.Service.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AnalisisCognitivo> AnalisisCognitivo { get; set; }
        public DbSet<LineaBase> LineasBase { get; set; }
        public DbSet<LogSistema> LogsSistema { get; set; }
        public DbSet<EvaluacionCompleta> EvaluacionesCompletas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de AnalisisCognitivo
            modelBuilder.Entity<AnalisisCognitivo>(entity =>
            {
                entity.ToTable("analisis_cognitivo");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.DescripcionPaciente)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.DescripcionReal)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(5000);

                entity.Property(e => e.NivelCambio)
                    .HasMaxLength(50);

                entity.Property(e => e.MetadataImagen)
                    .HasColumnType("jsonb");

                entity.Property(e => e.RespuestaLlmCompleta)
                    .HasColumnType("jsonb");

                // Índices
                entity.HasIndex(e => e.PacienteId)
                    .HasDatabaseName("idx_analisis_paciente");

                entity.HasIndex(e => e.FechaAnalisis)
                    .HasDatabaseName("idx_analisis_fecha");

                // Relación con LineaBase
                entity.HasOne(e => e.LineaBase)
                    .WithMany(lb => lb.Analisis)
                    .HasForeignKey(e => e.LineaBaseId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de LineaBase
            modelBuilder.Entity<LineaBase>(entity =>
            {
                entity.ToTable("linea_base");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Notas)
                    .HasMaxLength(1000);

                // Índices
                entity.HasIndex(e => e.PacienteId)
                    .HasDatabaseName("idx_linea_base_paciente");

                entity.HasIndex(e => new { e.PacienteId, e.Activa })
                    .HasDatabaseName("idx_linea_base_activa");
            });

            // Configuración de LogSistema
            modelBuilder.Entity<LogSistema>(entity =>
            {
                entity.ToTable("log_sistema");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nivel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Componente)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Mensaje)
                    .IsRequired()
                    .HasMaxLength(5000);

                entity.Property(e => e.Endpoint)
                    .HasMaxLength(500);

                entity.Property(e => e.DatosAdicionales)
                    .HasColumnType("jsonb");

                // Índices
                entity.HasIndex(e => e.FechaRegistro)
                    .HasDatabaseName("idx_log_fecha");

                entity.HasIndex(e => e.Nivel)
                    .HasDatabaseName("idx_log_nivel");
            });

            // Configuración de EvaluacionCompleta
            modelBuilder.Entity<EvaluacionCompleta>(entity =>
            {
                entity.ToTable("evaluacion_completa");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NivelDeterioroGeneral)
                    .HasMaxLength(50);

                entity.Property(e => e.ObservacionesGenerales)
                    .HasColumnType("text");

                entity.Property(e => e.RecomendacionesMedicas)
                    .HasColumnType("text");

                // Índices
                entity.HasIndex(e => e.PacienteId)
                    .HasDatabaseName("idx_evaluacion_paciente");

                entity.HasIndex(e => e.CuidadorId)
                    .HasDatabaseName("idx_evaluacion_cuidador");

                entity.HasIndex(e => e.FechaEvaluacion)
                    .HasDatabaseName("idx_evaluacion_fecha");

                // Relación con LineaBase
                entity.HasOne(e => e.LineaBase)
                    .WithMany()
                    .HasForeignKey(e => e.LineaBaseId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
