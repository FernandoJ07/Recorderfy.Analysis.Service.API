using Microsoft.EntityFrameworkCore;
using Recorderfy.Analisys.Service.DAL.Data;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analysis.Service.Test.Helpers
{
    /// <summary>
    /// DbContext personalizado para pruebas que ignora propiedades problemáticas para InMemory database
    /// </summary>
    public class TestAppDbContext : ApplicationDbContext
    {
        public TestAppDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar propiedades opcionales para InMemory database
            modelBuilder.Entity<AnalisisCognitivo>()
                .Property(a => a.MetadataImagen)
                .IsRequired(false);

            modelBuilder.Entity<AnalisisCognitivo>()
                .Property(a => a.NivelCambio)
                .IsRequired(false);

            modelBuilder.Entity<AnalisisCognitivo>()
                .Property(a => a.RespuestaLlmCompleta)
                .IsRequired(false);

            modelBuilder.Entity<AnalisisCognitivo>()
                .Property(a => a.Observaciones)
                .IsRequired(false);

            modelBuilder.Entity<LineaBase>()
                .Property(l => l.Notas)
                .IsRequired(false);

            modelBuilder.Entity<EvaluacionCompleta>()
                .Property(e => e.ObservacionesGenerales)
                .IsRequired(false);

            modelBuilder.Entity<EvaluacionCompleta>()
                .Property(e => e.RecomendacionesMedicas)
                .IsRequired(false);

            modelBuilder.Entity<EvaluacionCompleta>()
                .Property(e => e.NivelDeterioroGeneral)
                .IsRequired(false);
        }
    }
}
