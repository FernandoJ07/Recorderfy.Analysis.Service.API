using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.DTOs;
using Recorderfy.Analisys.Service.Model.Entities;
using Recorderfy.Analysis.Service.API.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Recorderfy.Analysis.Service.Test.Controllers
{
    public class AnalisisControllerTests
    {
        private readonly Mock<IAnalisisService> _mockAnalisisService;
        private readonly Mock<IAnalisisRepository> _mockRepository;
        private readonly Mock<ILogRepository> _mockLogRepository;
        private readonly Mock<IGeminiService> _mockGeminiService;
        private readonly AnalisisController _controller;

        public AnalisisControllerTests()
        {
            _mockAnalisisService = new Mock<IAnalisisService>();
            _mockRepository = new Mock<IAnalisisRepository>();
            _mockLogRepository = new Mock<ILogRepository>();
            _mockGeminiService = new Mock<IGeminiService>();

            _controller = new AnalisisController(
                _mockAnalisisService.Object,
                _mockRepository.Object,
                _mockLogRepository.Object,
                _mockGeminiService.Object
            );

            // Configurar HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task AnalizarDescripcion_ConRequestValido_DebeRetornarOk()
        {
            // Arrange
            var request = new AnalisisRequest
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Veo una cocina",
                DescripcionReal = "Cocina moderna"
            };

            var analisisResponse = new AnalisisResponse
            {
                AnalisisId = Guid.NewGuid(),
                PacienteId = request.PacienteId,
                ImagenId = request.ImagenId,
                ScoreGlobal = 85.5f,
                ScoreSemantico = 85.0f,
                ScoreObjetos = 80.0f,
                ScoreAcciones = 90.0f,
                FalsosObjetos = 0,
                CoherenciaLinguistica = 88.0f,
                EsLineaBase = true,
                Mensaje = "Línea base establecida correctamente"
            };

            _mockAnalisisService.Setup(s => s.RealizarAnalisisAsync(It.IsAny<AnalisisRequest>()))
                .ReturnsAsync(analisisResponse);

            // Act
            var resultado = await _controller.AnalizarDescripcion(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockAnalisisService.Verify(s => s.RealizarAnalisisAsync(It.IsAny<AnalisisRequest>()), Times.Once);
        }

        [Fact]
        public async Task AnalizarDescripcion_ConError_DebeRetornarServerError()
        {
            // Arrange
            var request = new AnalisisRequest
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test",
                DescripcionReal = "Test"
            };

            _mockAnalisisService.Setup(s => s.RealizarAnalisisAsync(It.IsAny<AnalisisRequest>()))
                .ThrowsAsync(new Exception("Error de prueba"));

            // Act
            var resultado = await _controller.AnalizarDescripcion(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerHistorialPaciente_DebeRetornarAnalisisDelPaciente()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var analisis = new List<AnalisisCognitivo>
            {
                new AnalisisCognitivo
                {
                    Id = Guid.NewGuid(),
                    PacienteId = pacienteId,
                    ScoreGlobal = 85.0f,
                    EsLineaBase = true
                },
                new AnalisisCognitivo
                {
                    Id = Guid.NewGuid(),
                    PacienteId = pacienteId,
                    ScoreGlobal = 80.0f,
                    EsLineaBase = false
                }
            };

            _mockRepository.Setup(r => r.ObtenerPorPacienteAsync(pacienteId))
                .ReturnsAsync(analisis);

            // Act
            var resultado = await _controller.ObtenerHistorialPaciente(pacienteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockRepository.Verify(r => r.ObtenerPorPacienteAsync(pacienteId), Times.Once);
        }

        [Fact]
        public async Task ObtenerAnalisis_ConIdValido_DebeRetornarAnalisis()
        {
            // Arrange
            var analisisId = Guid.NewGuid();
            var analisis = new AnalisisCognitivo
            {
                Id = analisisId,
                PacienteId = Guid.NewGuid(),
                ScoreGlobal = 85.0f,
                EsLineaBase = true
            };

            _mockRepository.Setup(r => r.ObtenerPorIdAsync(analisisId))
                .ReturnsAsync(analisis);

            // Act
            var resultado = await _controller.ObtenerAnalisis(analisisId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockRepository.Verify(r => r.ObtenerPorIdAsync(analisisId), Times.Once);
        }

        [Fact]
        public async Task ObtenerAnalisis_ConIdInvalido_DebeRetornarNotFound()
        {
            // Arrange
            var analisisId = Guid.NewGuid();

            _mockRepository.Setup(r => r.ObtenerPorIdAsync(analisisId))
                .ReturnsAsync((AnalisisCognitivo?)null);

            // Act
            var resultado = await _controller.ObtenerAnalisis(analisisId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }

        [Fact]
        public async Task ObtenerAnalisisConDeterioro_DebeRetornarAnalisisConDeterioro()
        {
            // Arrange
            var analisisConDeterioro = new List<AnalisisCognitivo>
            {
                new AnalisisCognitivo
                {
                    Id = Guid.NewGuid(),
                    PacienteId = Guid.NewGuid(),
                    ScoreGlobal = 60.0f,
                    DeterioroDetectado = true,
                    NivelCambio = "moderado"
                }
            };

            _mockRepository.Setup(r => r.ObtenerAnalisisConDeterioroAsync())
                .ReturnsAsync(analisisConDeterioro);

            // Act
            var resultado = await _controller.ObtenerAnalisisConDeterioro();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockRepository.Verify(r => r.ObtenerAnalisisConDeterioroAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerLineaBase_ConLineaBaseActiva_DebeRetornarLineaBase()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var lineaBase = new LineaBase
            {
                Id = Guid.NewGuid(),
                PacienteId = pacienteId,
                ScoreGlobalInicial = 85.0f,
                Activa = true
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync(lineaBase);

            // Act
            var resultado = await _controller.ObtenerLineaBase(pacienteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockRepository.Verify(r => r.ObtenerLineaBaseActivaAsync(pacienteId), Times.Once);
        }

        [Fact]
        public async Task ObtenerLineaBase_SinLineaBase_DebeRetornarNotFound()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync((LineaBase?)null);

            // Act
            var resultado = await _controller.ObtenerLineaBase(pacienteId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }

        [Fact]
        public async Task ProcesarEvaluacionCompleta_ConRequestValido_DebeRetornarOk()
        {
            // Arrange
            var request = new EvaluacionCompletaRequest
            {
                IdPaciente = Guid.NewGuid().ToString(),
                IdCuidador = Guid.NewGuid().ToString(),
                FechaRealizacion = DateTime.UtcNow,
                Preguntas = new List<PreguntaEvaluacion>
                {
                    new PreguntaEvaluacion
                    {
                        IdPicture = Guid.NewGuid().ToString(),
                        ImagenUrl = "http://test.com/img.jpg",
                        DescripcionReal = "Cocina",
                        PacienteRespuesta = "Veo una cocina"
                    }
                }
            };

            var evaluacionResponse = new EvaluacionCompletaResponse
            {
                EvaluacionId = Guid.NewGuid(),
                PacienteId = Guid.Parse(request.IdPaciente),
                CuidadorId = Guid.Parse(request.IdCuidador),
                TotalPreguntas = 1,
                PreguntasProcesadas = 1,
                ScoreGlobalPromedio = 85.0f,
                EsLineaBase = true
            };

            _mockAnalisisService.Setup(s => s.ProcesarEvaluacionCompletaAsync(It.IsAny<EvaluacionCompletaRequest>()))
                .ReturnsAsync(evaluacionResponse);

            // Act
            var resultado = await _controller.ProcesarEvaluacionCompleta(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
            
            _mockAnalisisService.Verify(s => s.ProcesarEvaluacionCompletaAsync(It.IsAny<EvaluacionCompletaRequest>()), Times.Once);
        }

        [Fact]
        public void Health_DebeRetornarOkConEstadoHealthy()
        {
            // Act
            var resultado = _controller.Health();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.NotNull(okResult.Value);
        }
    }
}
