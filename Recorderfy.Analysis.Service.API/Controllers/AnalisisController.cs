using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.DTOs;

namespace Recorderfy.Analysis.Service.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalisisController : ControllerBase
    {
        private readonly IAnalisisService _analisisService;
        private readonly IAnalisisRepository _analisisRepository;
        private readonly ILogRepository _logRepository;
        private readonly IGeminiService _geminiService;

        public AnalisisController(
            IAnalisisService analisisService,
            IAnalisisRepository analisisRepository,
            ILogRepository logRepository,
            IGeminiService geminiService)
        {
            _analisisService = analisisService;
            _analisisRepository = analisisRepository;
            _logRepository = logRepository;
            _geminiService = geminiService;
        }

        /// <summary>
        /// Endpoint principal: Analiza la descripción de una imagen dada por un paciente
        /// </summary>
        [HttpPost("analizar")]
        public async Task<IActionResult> AnalizarDescripcion([FromBody] AnalisisRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await _logRepository.RegistrarAsync("WARNING", "AnalisisController.AnalizarDescripcion",
                        $"Request inválido recibido para paciente {request?.PacienteId}", 
                        $"Errores: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}", 
                        null, request?.PacienteId, "/api/analisis/analizar");
                    return BadRequest(new { 
                        success = false, 
                        message = "Datos de entrada inválidos",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                        timestamp = DateTime.UtcNow 
                    });
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.AnalizarDescripcion",
                    $"Request recibido - Paciente: {request.PacienteId}, Imagen: {request.ImagenId}", 
                    null, null, request.PacienteId, "/api/analisis/analizar");

                var resultado = await _analisisService.RealizarAnalisisAsync(request);

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.AnalizarDescripcion",
                    $"Análisis completado exitosamente - ID: {resultado.AnalisisId}, Score: {resultado.ScoreGlobal}, EsLineaBase: {resultado.EsLineaBase}", 
                    null, null, request.PacienteId, "/api/analisis/analizar");

                return Ok(new
                {
                    success = true,
                    data = resultado,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.AnalizarDescripcion",
                    $"Error al procesar solicitud de análisis para paciente {request?.PacienteId}", 
                    ex.ToString(), ex.StackTrace, request?.PacienteId, "/api/analisis/analizar");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al procesar el análisis cognitivo",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene el historial de análisis de un paciente
        /// </summary>
        [HttpGet("paciente/{pacienteId}")]
        public async Task<IActionResult> ObtenerHistorialPaciente(Guid pacienteId)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerHistorialPaciente",
                    $"Consultando historial del paciente {pacienteId}", null, null, pacienteId, "/api/analisis/paciente/{id}");

                var analisis = await _analisisRepository.ObtenerPorPacienteAsync(pacienteId);

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerHistorialPaciente",
                    $"Historial obtenido - Total análisis: {analisis.Count}", null, null, pacienteId, "/api/analisis/paciente/{id}");

                return Ok(new
                {
                    success = true,
                    data = analisis,
                    count = analisis.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerHistorialPaciente",
                    $"Error al obtener historial del paciente {pacienteId}", ex.ToString(), ex.StackTrace, pacienteId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener el historial del paciente",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene un análisis específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerAnalisis(Guid id)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerAnalisis",
                    $"Consultando análisis {id}", null, null, null, "/api/analisis/{id}");

                var analisis = await _analisisRepository.ObtenerPorIdAsync(id);

                if (analisis == null)
                {
                    await _logRepository.RegistrarAsync("WARNING", "AnalisisController.ObtenerAnalisis",
                        $"Análisis {id} no encontrado", null, null, null, "/api/analisis/{id}");

                    return NotFound(new
                    {
                        success = false,
                        message = $"Análisis con ID {id} no encontrado",
                        timestamp = DateTime.UtcNow
                    });
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerAnalisis",
                    $"Análisis {id} obtenido exitosamente - Paciente: {analisis.PacienteId}", 
                    null, null, analisis.PacienteId, "/api/analisis/{id}");

                return Ok(new
                {
                    success = true,
                    data = analisis,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerAnalisis",
                    $"Error al obtener análisis {id}", ex.ToString(), ex.StackTrace);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener el análisis",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene todos los análisis donde se detectó deterioro
        /// </summary>
        [HttpGet("deterioro")]
        public async Task<IActionResult> ObtenerAnalisisConDeterioro()
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerAnalisisConDeterioro",
                    "Consultando análisis con deterioro detectado", null, null, null, "/api/analisis/deterioro");

                var analisis = await _analisisRepository.ObtenerAnalisisConDeterioroAsync();

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerAnalisisConDeterioro",
                    $"Análisis con deterioro obtenidos - Total: {analisis.Count}", null, null, null, "/api/analisis/deterioro");

                return Ok(new
                {
                    success = true,
                    data = analisis,
                    count = analisis.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerAnalisisConDeterioro",
                    "Error al obtener análisis con deterioro", ex.ToString(), ex.StackTrace);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener los análisis con deterioro",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene la línea base activa de un paciente
        /// </summary>
        [HttpGet("linea-base/{pacienteId}")]
        public async Task<IActionResult> ObtenerLineaBase(Guid pacienteId)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerLineaBase",
                    $"Consultando línea base activa del paciente {pacienteId}", null, null, pacienteId, "/api/analisis/linea-base/{id}");

                var lineaBase = await _analisisRepository.ObtenerLineaBaseActivaAsync(pacienteId);

                if (lineaBase == null)
                {
                    await _logRepository.RegistrarAsync("WARNING", "AnalisisController.ObtenerLineaBase",
                        $"No se encontró línea base activa para el paciente {pacienteId}", null, null, pacienteId, "/api/analisis/linea-base/{id}");

                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró línea base activa para el paciente {pacienteId}",
                        timestamp = DateTime.UtcNow
                    });
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ObtenerLineaBase",
                    $"Línea base obtenida - ID: {lineaBase.Id}, Score inicial: {lineaBase.ScoreGlobalInicial}", 
                    null, null, pacienteId, "/api/analisis/linea-base/{id}");

                return Ok(new
                {
                    success = true,
                    data = lineaBase,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerLineaBase",
                    $"Error al obtener línea base del paciente {pacienteId}", ex.ToString(), ex.StackTrace, pacienteId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener la línea base",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Health check del servicio
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "Microservicio de Análisis Cognitivo",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// TEST: Prueba de conexión con Gemini AI
        /// </summary>
        [HttpPost("test-gemini")]
        public async Task<IActionResult> TestGemini()
        {
            try
            {
                //await _logRepository.RegistrarAsync("INFO", "AnalisisController.TestGemini",
                //    "Iniciando prueba de conexión con Gemini", null, null, null, "/api/analisis/test-gemini");

                var descripcionPaciente = "Veo una mujer en una cocina preparando el desayuno. Hay una cafetera sobre la mesa y ella está usando una tostadora.";
                var descripcionReal = "Una mujer de mediana edad en una cocina moderna preparando el desayuno. En la encimera hay una cafetera, una tostadora, platos, tazas y utensilios de cocina.";
                var metadata = "{\"test\": true, \"fecha\": \"2024-01-15\"}";

                var resultado = await _geminiService.AnalizarConGeminiAsync(
                    descripcionPaciente,
                    descripcionReal,
                    metadata,
                    null
                );

                //await _logRepository.RegistrarAsync("INFO", "AnalisisController.TestGemini",
                //    $"Prueba completada exitosamente - Score global: {resultado.score_global}");

                return Ok(new
                {
                    success = true,
                    message = "Conexión con Gemini AI exitosa",
                    data = new
                    {
                        scoreGlobal = resultado.score_global,
                        scoreSemantico = resultado.score_semantico,
                        scoreObjetos = resultado.score_objetos,
                        scoreAcciones = resultado.score_acciones,
                        falsosObjetos = resultado.falsos_objetos,
                        coherenciaLinguistica = resultado.coherencia_linguistica,
                        observaciones = resultado.observaciones,
                        comparacionBaseline = resultado.comparacion_con_baseline
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                //await _logRepository.RegistrarAsync("ERROR", "AnalisisController.TestGemini",
                //    $"Error en prueba de Gemini: {ex.Message}", ex.ToString());

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al conectar con Gemini AI",
                    error = ex.Message,
                    detalle = ex.InnerException?.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ==================== ENDPOINTS PARA EVALUACIONES COMPLETAS ====================

        /// <summary>
        /// Procesa una evaluación completa con múltiples preguntas
        /// </summary>
        [HttpPost("evaluacion-completa")]
        public async Task<IActionResult> ProcesarEvaluacionCompleta([FromBody] EvaluacionCompletaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await _logRepository.RegistrarAsync("WARNING", "AnalisisController.ProcesarEvaluacionCompleta",
                        "Request de evaluación completa inválido", null, null, null, "/api/analisis/evaluacion-completa");
                    return BadRequest(ModelState);
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisController.ProcesarEvaluacionCompleta",
                    $"Evaluación completa recibida - Paciente: {request.IdPaciente}, Preguntas: {request.Preguntas.Count}",
                    null, null, Guid.Parse(request.IdPaciente), "/api/analisis/evaluacion-completa");

                var resultado = await _analisisService.ProcesarEvaluacionCompletaAsync(request);

                return Ok(new
                {
                    success = true,
                    data = resultado,
                    message = $"Evaluación completada exitosamente. Score promedio: {resultado.ScoreGlobalPromedio:F2}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ProcesarEvaluacionCompleta",
                    "Error al procesar evaluación completa", ex.ToString(), null, null, "/api/analisis/evaluacion-completa");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al procesar la evaluación completa",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene una evaluación completa por ID
        /// </summary>
        [HttpGet("evaluacion-completa/{evaluacionId}")]
        public async Task<IActionResult> ObtenerEvaluacionCompleta(Guid evaluacionId)
        {
            try
            {
                var evaluacion = await _analisisService.ObtenerEvaluacionCompletaPorIdAsync(evaluacionId);

                return Ok(new
                {
                    success = true,
                    data = evaluacion,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new
                {
                    success = false,
                    message = "Funcionalidad en desarrollo"
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerEvaluacionCompleta",
                    $"Error al obtener evaluación {evaluacionId}", ex.ToString());

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener la evaluación completa",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene todas las evaluaciones completas de un paciente
        /// </summary>
        [HttpGet("evaluacion-completa/paciente/{pacienteId}")]
        public async Task<IActionResult> ObtenerEvaluacionesPorPaciente(Guid pacienteId)
        {
            try
            {
                var evaluaciones = await _analisisService.ObtenerEvaluacionesPorPacienteAsync(pacienteId);

                return Ok(new
                {
                    success = true,
                    data = evaluaciones,
                    count = evaluaciones.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new
                {
                    success = false,
                    message = "Funcionalidad en desarrollo"
                });
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisController.ObtenerEvaluacionesPorPaciente",
                    $"Error al obtener evaluaciones del paciente {pacienteId}", ex.ToString());

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener las evaluaciones del paciente",
                    error = ex.Message
                });
            }
        }
    }
}
