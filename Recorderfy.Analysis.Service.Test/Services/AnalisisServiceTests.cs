using Moq;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.BLL.Services;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.DTOs;
using Recorderfy.Analisys.Service.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Recorderfy.Analysis.Service.Test.Services
{
    public class AnalisisServiceTests
    {
        private readonly Mock<IAnalisisRepository> _mockRepository;
        private readonly Mock<IGeminiService> _mockGeminiService;
        private readonly Mock<ILogRepository> _mockLogRepository;
        private readonly IAnalisisService _analisisService;

        public AnalisisServiceTests()
        {
            _mockRepository = new Mock<IAnalisisRepository>();
            _mockGeminiService = new Mock<IGeminiService>();
            _mockLogRepository = new Mock<ILogRepository>();
            
            _analisisService = new AnalisisService(
                _mockRepository.Object,
                _mockGeminiService.Object,
                _mockLogRepository.Object
            );
        }

        [Fact]
        public async Task RealizarAnalisisAsync_PrimeraEvaluacion_DebeCrearLineaBase()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var imagenId = Guid.NewGuid();
            
            var request = new AnalisisRequest
            {
                PacienteId = pacienteId,
                ImagenId = imagenId,
                DescripcionPaciente = "Veo una cocina con una mujer preparando comida",
                DescripcionReal = "Una mujer en una cocina moderna preparando el desayuno"
            };

            var geminiResponse = new GeminiAnalisisResponse
            {
                score_semantico = 85.0f,
                score_objetos = 80.0f,
                score_acciones = 90.0f,
                falsos_objetos = 0,
                tiempo_respuesta_seg = 25.5f,
                coherencia_linguistica = 88.0f,
                score_global = 85.5f,
                observaciones = "Buen desempeño cognitivo",
                comparacion_con_baseline = new ComparacionBaseline
                {
                    diferencia_score = 0,
                    deterioro_detectado = false,
                    nivel_cambio = "estable"
                }
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync((LineaBase?)null);

            _mockGeminiService.Setup(g => g.AnalizarConGeminiAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(geminiResponse);

            _mockRepository.Setup(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()))
                .ReturnsAsync((LineaBase lb) =>
                {
                    lb.Id = Guid.NewGuid();
                    return lb;
                });

            _mockRepository.Setup(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()))
                .ReturnsAsync((AnalisisCognitivo a) =>
                {
                    a.Id = Guid.NewGuid();
                    return a;
                });

            // Act
            var resultado = await _analisisService.RealizarAnalisisAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.EsLineaBase);
            Assert.Equal(85.5f, resultado.ScoreGlobal);
            Assert.Equal(pacienteId, resultado.PacienteId);
            Assert.Equal(imagenId, resultado.ImagenId);
            
            _mockRepository.Verify(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()), Times.Once);
            _mockRepository.Verify(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()), Times.Once);
        }

        [Fact]
        public async Task RealizarAnalisisAsync_ConLineaBaseExistente_DebeCompararConBaseline()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var imagenId = Guid.NewGuid();
            var lineaBaseId = Guid.NewGuid();

            var request = new AnalisisRequest
            {
                PacienteId = pacienteId,
                ImagenId = imagenId,
                DescripcionPaciente = "Veo una cocina",
                DescripcionReal = "Una mujer en una cocina moderna"
            };

            var lineaBaseActiva = new LineaBase
            {
                Id = lineaBaseId,
                PacienteId = pacienteId,
                ScoreGlobalInicial = 85.0f,
                Activa = true
            };

            var geminiResponse = new GeminiAnalisisResponse
            {
                score_semantico = 70.0f,
                score_objetos = 65.0f,
                score_acciones = 75.0f,
                falsos_objetos = 2,
                tiempo_respuesta_seg = 30.0f,
                coherencia_linguistica = 72.0f,
                score_global = 70.0f,
                observaciones = "Deterioro cognitivo leve detectado",
                comparacion_con_baseline = new ComparacionBaseline
                {
                    diferencia_score = -15.0f,
                    deterioro_detectado = true,
                    nivel_cambio = "moderado"
                }
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync(lineaBaseActiva);

            _mockGeminiService.Setup(g => g.AnalizarConGeminiAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 85.0f))
                .ReturnsAsync(geminiResponse);

            _mockRepository.Setup(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()))
                .ReturnsAsync((AnalisisCognitivo a) =>
                {
                    a.Id = Guid.NewGuid();
                    return a;
                });

            // Act
            var resultado = await _analisisService.RealizarAnalisisAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.False(resultado.EsLineaBase);
            Assert.Equal(70.0f, resultado.ScoreGlobal);
            Assert.True(resultado.ComparacionConBaseline.DeterioroDetectado);
            Assert.Equal("moderado", resultado.ComparacionConBaseline.NivelCambio);
            
            _mockRepository.Verify(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()), Times.Never);
            _mockRepository.Verify(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()), Times.Once);
        }

        [Fact]
        public async Task RealizarAnalisisMultipleAsync_DebeCrearLineaBaseConPromedio()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var requests = new List<AnalisisRequest>
            {
                new AnalisisRequest
                {
                    PacienteId = pacienteId,
                    ImagenId = Guid.NewGuid(),
                    DescripcionPaciente = "Cocina",
                    DescripcionReal = "Cocina moderna"
                },
                new AnalisisRequest
                {
                    PacienteId = pacienteId,
                    ImagenId = Guid.NewGuid(),
                    DescripcionPaciente = "Jardín",
                    DescripcionReal = "Jardín con flores"
                }
            };

            var cuestionarioResponse = new GeminiCuestionarioResponse
            {
                resultados_preguntas = new List<ResultadoPreguntaGemini>
                {
                    new ResultadoPreguntaGemini
                    {
                        numero_pregunta = 1,
                        score_global = 85.0f,
                        score_semantico = 85.0f,
                        score_objetos = 80.0f,
                        score_acciones = 90.0f,
                        coherencia_linguistica = 85.0f,
                        falsos_objetos = 0,
                        observaciones = "Bien"
                    },
                    new ResultadoPreguntaGemini
                    {
                        numero_pregunta = 2,
                        score_global = 75.0f,
                        score_semantico = 75.0f,
                        score_objetos = 70.0f,
                        score_acciones = 80.0f,
                        coherencia_linguistica = 75.0f,
                        falsos_objetos = 1,
                        observaciones = "Aceptable"
                    }
                },
                resumen_general = new ResumenGeneralGemini
                {
                    score_global_promedio = 80.0f,
                    tiempo_respuesta_promedio_seg = 30.0f,
                    deterioro_detectado = false,
                    nivel_deterioro = "estable"
                },
                comparacion_con_baseline = new ComparacionBaseline
                {
                    diferencia_score = 0,
                    deterioro_detectado = false,
                    nivel_cambio = "estable"
                }
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync((LineaBase?)null);

            _mockGeminiService.Setup(g => g.AnalizarCuestionarioCompletoAsync(
                    It.IsAny<List<AnalisisRequest>>(), null))
                .ReturnsAsync(cuestionarioResponse);

            _mockRepository.Setup(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()))
                .ReturnsAsync((LineaBase lb) =>
                {
                    lb.Id = Guid.NewGuid();
                    return lb;
                });

            _mockRepository.Setup(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()))
                .ReturnsAsync((AnalisisCognitivo a) =>
                {
                    a.Id = Guid.NewGuid();
                    return a;
                });

            // Act
            var resultados = await _analisisService.RealizarAnalisisMultipleAsync(requests, pacienteId);

            // Assert
            Assert.NotNull(resultados);
            Assert.Equal(2, resultados.Count);
            Assert.All(resultados, r => Assert.True(r.EsLineaBase));
            
            _mockRepository.Verify(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()), Times.Once);
            _mockRepository.Verify(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RealizarAnalisisAsync_ConError_DebeLanzarExcepcion()
        {
            // Arrange
            var request = new AnalisisRequest
            {
                PacienteId = Guid.NewGuid(),
                ImagenId = Guid.NewGuid(),
                DescripcionPaciente = "Test",
                DescripcionReal = "Test"
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _analisisService.RealizarAnalisisAsync(request));
            
            // Verificar que se registró el error (sin verificar todos los parámetros opcionales)
            _mockLogRepository.Verify(
                l => l.RegistrarAsync(
                    It.Is<string>(s => s == "ERROR"),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcesarEvaluacionCompletaAsync_DebeCrearEvaluacionYAnalisis()
        {
            // Arrange
            var pacienteId = Guid.NewGuid();
            var cuidadorId = Guid.NewGuid();

            var request = new EvaluacionCompletaRequest
            {
                IdPaciente = pacienteId.ToString(),
                IdCuidador = cuidadorId.ToString(),
                FechaRealizacion = DateTime.UtcNow,
                Preguntas = new List<PreguntaEvaluacion>
                {
                    new PreguntaEvaluacion
                    {
                        IdPicture = Guid.NewGuid().ToString(),
                        ImagenUrl = "http://test.com/img1.jpg",
                        DescripcionReal = "Cocina",
                        PacienteRespuesta = "Veo una cocina"
                    }
                }
            };

            var cuestionarioResponse = new GeminiCuestionarioResponse
            {
                resultados_preguntas = new List<ResultadoPreguntaGemini>
                {
                    new ResultadoPreguntaGemini
                    {
                        numero_pregunta = 1,
                        score_global = 85.0f,
                        score_semantico = 85.0f,
                        score_objetos = 80.0f,
                        score_acciones = 90.0f,
                        coherencia_linguistica = 85.0f,
                        falsos_objetos = 0,
                        observaciones = "Buen desempeño"
                    }
                },
                resumen_general = new ResumenGeneralGemini
                {
                    score_global_promedio = 85.0f,
                    tiempo_respuesta_promedio_seg = 30.0f,
                    deterioro_detectado = false,
                    nivel_deterioro = "estable"
                },
                comparacion_con_baseline = new ComparacionBaseline
                {
                    diferencia_score = 0,
                    deterioro_detectado = false,
                    nivel_cambio = "estable"
                }
            };

            _mockRepository.Setup(r => r.ObtenerLineaBaseActivaAsync(pacienteId))
                .ReturnsAsync((LineaBase?)null);

            _mockGeminiService.Setup(g => g.AnalizarCuestionarioCompletoAsync(
                    It.IsAny<List<AnalisisRequest>>(), null))
                .ReturnsAsync(cuestionarioResponse);

            _mockRepository.Setup(r => r.CrearLineaBaseAsync(It.IsAny<LineaBase>()))
                .ReturnsAsync((LineaBase lb) =>
                {
                    lb.Id = Guid.NewGuid();
                    return lb;
                });

            _mockRepository.Setup(r => r.CrearAnalisisAsync(It.IsAny<AnalisisCognitivo>()))
                .ReturnsAsync((AnalisisCognitivo a) =>
                {
                    a.Id = Guid.NewGuid();
                    return a;
                });

            _mockRepository.Setup(r => r.CrearEvaluacionCompletaAsync(It.IsAny<EvaluacionCompleta>()))
                .ReturnsAsync((EvaluacionCompleta e) =>
                {
                    e.Id = Guid.NewGuid();
                    return e;
                });

            // Act
            var resultado = await _analisisService.ProcesarEvaluacionCompletaAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(pacienteId, resultado.PacienteId);
            Assert.Equal(cuidadorId, resultado.CuidadorId);
            Assert.Equal(1, resultado.TotalPreguntas);
            Assert.Equal(1, resultado.PreguntasProcesadas);
            Assert.Equal(85.0f, resultado.ScoreGlobalPromedio);
            Assert.True(resultado.EsLineaBase);
            
            _mockRepository.Verify(r => r.CrearEvaluacionCompletaAsync(It.IsAny<EvaluacionCompleta>()), Times.Once);
        }
    }
}
