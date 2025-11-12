using Microsoft.EntityFrameworkCore;
using Recorderfy.Analisys.Service.DAL.Data;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Repositories;
using Recorderfy.Analisys.Service.Model.Entities;
using Recorderfy.Analysis.Service.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Recorderfy.Analysis.Service.Test.Repositories
{
    public class AnalisisRepositoryTests : IDisposable
    {
        private readonly TestAppDbContext _context;
        private readonly IAnalisisRepository _repository;

        public AnalisisRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestAppDbContext(options);
            _repository = new AnalisisRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CrearAnalisisAsync_DebeAgregarAnalisisABaseDeDatos()
        {
            // Arrange
            var analisis = new AnalisisCognitivo
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Veo una cocina",
                DescripcionReal = "Cocina moderna",
                ScoreSemantico = 85.0f,
                ScoreObjetos = 80.0f,
                ScoreAcciones = 90.0f,
                FalsosObjetos = 0,
                TiempoRespuestaSeg = 25.5f,
                CoherenciaLinguistica = 88.0f,
                ScoreGlobal = 85.5f,
                Observaciones = "Buen desempeño",
                EsLineaBase = true
            };

            // Act
            var resultado = await _repository.CrearAnalisisAsync(analisis);

            // Assert
            Assert.NotNull(resultado);
            Assert.NotEqual(Guid.Empty, resultado.Id);

            var analisisEnDb = await _context.AnalisisCognitivo.FindAsync(resultado.Id);
            Assert.NotNull(analisisEnDb);
            Assert.Equal(85.5f, analisisEnDb.ScoreGlobal);
        }

        [Fact]
        public async Task ObtenerPorIdAsync_ConIdValido_DebeRetornarAnalisis()
        {
            // Arrange
            var analisisId = Guid.NewGuid();
            var analisis = new AnalisisCognitivo
            {
                Id = analisisId,
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test",
                DescripcionReal = "Test",
                ScoreGlobal = 80.0f,
                EsLineaBase = true
            };

            await _context.AnalisisCognitivo.AddAsync(analisis);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerPorIdAsync(analisisId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(analisisId, resultado.Id);
            Assert.Equal(80.0f, resultado.ScoreGlobal);
        }

        [Fact]
        public async Task ObtenerPorIdAsync_ConIdInvalido_DebeRetornarNull()
        {
            // Arrange
            var analisisId = Guid.NewGuid();

            // Act
            var resultado = await _repository.ObtenerPorIdAsync(analisisId);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerPorPacienteAsync_DebeRetornarAnalisisDelPaciente()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var otroPacienteId = Guid.NewGuid();

            var analisis1 = new AnalisisCognitivo
            {
                PacienteId = pacienteId,
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test1",
                DescripcionReal = "Test1",
                ScoreGlobal = 85.0f,
                FechaAnalisis = DateTime.UtcNow.AddDays(-2),
                EsLineaBase = true
            };

            var analisis2 = new AnalisisCognitivo
            {
                PacienteId = pacienteId,
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test2",
                DescripcionReal = "Test2",
                ScoreGlobal = 80.0f,
                FechaAnalisis = DateTime.UtcNow.AddDays(-1),
                EsLineaBase = false
            };

            var analisisOtroPaciente = new AnalisisCognitivo
            {
                PacienteId = otroPacienteId,
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test3",
                DescripcionReal = "Test3",
                ScoreGlobal = 75.0f,
                EsLineaBase = true
            };

            await _context.AnalisisCognitivo.AddRangeAsync(analisis1, analisis2, analisisOtroPaciente);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerPorPacienteAsync(pacienteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count);
            Assert.All(resultado, a => Assert.Equal(pacienteId, a.PacienteId));
            // Verificar que están ordenados por fecha descendente
            Assert.True(resultado[0].FechaAnalisis >= resultado[1].FechaAnalisis);
        }

        [Fact]
        public async Task CrearLineaBaseAsync_DebeDesactivarLineasBaseAnteriores()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();

            var lineaBaseAntigua = new LineaBase
            {
                PacienteId = pacienteId,
                ScoreGlobalInicial = 80.0f,
                Activa = true,
                FechaEstablecimiento = DateTime.UtcNow.AddMonths(-1)
            };

            await _context.LineasBase.AddAsync(lineaBaseAntigua);
            await _context.SaveChangesAsync();

            var nuevaLineaBase = new LineaBase
            {
                PacienteId = pacienteId,
                ScoreGlobalInicial = 85.0f,
                Activa = true,
                Notas = "Nueva línea base"
            };

            // Act
            var resultado = await _repository.CrearLineaBaseAsync(nuevaLineaBase);

            // Assert
            Assert.NotNull(resultado);
            Assert.NotEqual(Guid.Empty, resultado.Id);

            // Verificar que la antigua está desactivada
            var lineaBaseAntiguaActualizada = await _context.LineasBase.FindAsync(lineaBaseAntigua.Id);
            Assert.False(lineaBaseAntiguaActualizada.Activa);

            // Verificar que la nueva está activa
            Assert.True(resultado.Activa);
        }

        [Fact]
        public async Task ObtenerLineaBaseActivaAsync_DebeRetornarLineaBaseActiva()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();

            var lineaBaseInactiva = new LineaBase
            {
                PacienteId = pacienteId,
                ScoreGlobalInicial = 75.0f,
                Activa = false,
                FechaEstablecimiento = DateTime.UtcNow.AddMonths(-2)
            };

            var lineaBaseActiva = new LineaBase
            {
                PacienteId = pacienteId,
                ScoreGlobalInicial = 85.0f,
                Activa = true,
                FechaEstablecimiento = DateTime.UtcNow.AddMonths(-1)
            };

            await _context.LineasBase.AddRangeAsync(lineaBaseInactiva, lineaBaseActiva);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerLineaBaseActivaAsync(pacienteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.Activa);
            Assert.Equal(85.0f, resultado.ScoreGlobalInicial);
        }

        [Fact]
        public async Task ObtenerLineaBaseActivaAsync_SinLineaBase_DebeRetornarNull()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();

            // Act
            var resultado = await _repository.ObtenerLineaBaseActivaAsync(pacienteId);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerAnalisisConDeterioroAsync_DebeRetornarSoloConDeterioro()
        {
            // Arrange
            var analisisConDeterioro = new AnalisisCognitivo
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test",
                DescripcionReal = "Test",
                ScoreGlobal = 60.0f,
                DeterioroDetectado = true,
                NivelCambio = "moderado",
                EsLineaBase = false
            };

            var analisisSinDeterioro = new AnalisisCognitivo
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test",
                DescripcionReal = "Test",
                ScoreGlobal = 85.0f,
                DeterioroDetectado = false,
                NivelCambio = "estable",
                EsLineaBase = false
            };

            await _context.AnalisisCognitivo.AddRangeAsync(analisisConDeterioro, analisisSinDeterioro);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerAnalisisConDeterioroAsync();

            // Assert
            Assert.NotNull(resultado);
            Assert.Single(resultado);
            Assert.All(resultado, a => Assert.True(a.DeterioroDetectado));
        }

        [Fact]
        public async Task CrearEvaluacionCompletaAsync_DebeAgregarEvaluacion()
        {
            // Arrange
            var evaluacion = new EvaluacionCompleta
            {
                PacienteId = Guid.NewGuid(),
                CuidadorId = Guid.NewGuid(),
                FechaEvaluacion = DateTime.UtcNow,
                TotalPreguntas = 5,
                PreguntasProcesadas = 5,
                ScoreGlobalPromedio = 82.5f,
                DeterioroDetectado = false,
                NivelDeterioroGeneral = "estable",
                EsLineaBase = true
            };

            // Act
            var resultado = await _repository.CrearEvaluacionCompletaAsync(evaluacion);

            // Assert
            Assert.NotNull(resultado);
            Assert.NotEqual(Guid.Empty, resultado.Id);

            var evaluacionEnDb = await _context.EvaluacionesCompletas.FindAsync(resultado.Id);
            Assert.NotNull(evaluacionEnDb);
            Assert.Equal(82.5f, evaluacionEnDb.ScoreGlobalPromedio);
        }

        [Fact]
        public async Task ObtenerEvaluacionesPorPacienteAsync_DebeRetornarEvaluacionesDelPaciente()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var otroPacienteId = Guid.NewGuid();

            var evaluacion1 = new EvaluacionCompleta
            {
                PacienteId = pacienteId,
                CuidadorId = Guid.NewGuid(),
                FechaEvaluacion = DateTime.UtcNow.AddDays(-2),
                TotalPreguntas = 5,
                ScoreGlobalPromedio = 85.0f,
                EsLineaBase = true
            };

            var evaluacion2 = new EvaluacionCompleta
            {
                PacienteId = pacienteId,
                CuidadorId = Guid.NewGuid(),
                FechaEvaluacion = DateTime.UtcNow.AddDays(-1),
                TotalPreguntas = 5,
                ScoreGlobalPromedio = 80.0f,
                EsLineaBase = false
            };

            var evaluacionOtroPaciente = new EvaluacionCompleta
            {
                PacienteId = otroPacienteId,
                CuidadorId = Guid.NewGuid(),
                FechaEvaluacion = DateTime.UtcNow,
                TotalPreguntas = 5,
                ScoreGlobalPromedio = 75.0f,
                EsLineaBase = true
            };

            await _context.EvaluacionesCompletas.AddRangeAsync(evaluacion1, evaluacion2, evaluacionOtroPaciente);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerEvaluacionesPorPacienteAsync(pacienteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count);
            Assert.All(resultado, e => Assert.Equal(pacienteId, e.PacienteId));
            // Verificar orden descendente por fecha
            Assert.True(resultado[0].FechaEvaluacion >= resultado[1].FechaEvaluacion);
        }
    }
}
