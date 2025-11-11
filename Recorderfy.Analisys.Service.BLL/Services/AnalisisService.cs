using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.DTOs;
using Recorderfy.Analisys.Service.Model.Entities;

namespace Recorderfy.Analisys.Service.BLL.Services
{
    public class AnalisisService : IAnalisisService
    {
        private readonly IAnalisisRepository _analisisRepository;
        private readonly IGeminiService _geminiService;
        private readonly ILogRepository _logRepository;

        public AnalisisService(
            IAnalisisRepository analisisRepository,
            IGeminiService geminiService,
            ILogRepository logRepository)
        {
            _analisisRepository = analisisRepository;
            _geminiService = geminiService;
            _logRepository = logRepository;
        }

        // MÉTODO PRINCIPAL
        public async Task<AnalisisResponse> RealizarAnalisisAsync(AnalisisRequest request)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                    $"Iniciando análisis para paciente {request.PacienteId}, imagen {request.ImagenId}");

                // Verificar si el paciente tiene análisis previos para determinar si es línea base
                var lineaBaseActiva = await _analisisRepository.ObtenerLineaBaseActivaAsync(request.PacienteId);
                bool esLineaBase = lineaBaseActiva == null;

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                    $"Línea base: {(esLineaBase ? "Primera evaluación del paciente" : $"Seguimiento basado en línea base {lineaBaseActiva.Id}")}");

                float? scoreBaselinePrevio = null;
                if (!esLineaBase)
                {
                    scoreBaselinePrevio = lineaBaseActiva?.ScoreGlobalInicial;
                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                        $"Score de línea base para comparación: {scoreBaselinePrevio}");
                }

                // Crear metadata simple con la información recibida
                var metadataJson = JsonSerializer.Serialize(new
                {
                    imagenId = request.ImagenId,
                    fechaAnalisis = DateTime.UtcNow,
                    fuente = "api_externa"
                });

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                    $"Enviando análisis a Gemini - Descripción paciente: {request.DescripcionPaciente.Substring(0, Math.Min(50, request.DescripcionPaciente.Length))}...");

                // Analizar con Gemini
                var analisisGemini = await _geminiService.AnalizarConGeminiAsync(
                    request.DescripcionPaciente,
                    request.DescripcionReal,
                    metadataJson,
                    scoreBaselinePrevio
                );

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                    $"Análisis Gemini completado - Score global: {analisisGemini.score_global}");

                // Crear el registro del análisis cognitivo
                var analisisCognitivo = new AnalisisCognitivo
                {
                    PacienteId = request.PacienteId,
                    ImagenId = request.ImagenId,
                    DescripcionPaciente = request.DescripcionPaciente,
                    DescripcionReal = request.DescripcionReal,
                    MetadataImagen = metadataJson,
                    ScoreSemantico = analisisGemini.score_semantico,
                    ScoreObjetos = analisisGemini.score_objetos,
                    ScoreAcciones = analisisGemini.score_acciones,
                    FalsosObjetos = analisisGemini.falsos_objetos,
                    TiempoRespuestaSeg = analisisGemini.tiempo_respuesta_seg,
                    CoherenciaLinguistica = analisisGemini.coherencia_linguistica,
                    ScoreGlobal = analisisGemini.score_global,
                    Observaciones = analisisGemini.observaciones,
                    DiferenciaScore = analisisGemini.comparacion_con_baseline?.diferencia_score,
                    DeterioroDetectado = analisisGemini.comparacion_con_baseline?.deterioro_detectado,
                    NivelCambio = analisisGemini.comparacion_con_baseline?.nivel_cambio,
                    EsLineaBase = esLineaBase,
                    LineaBaseId = lineaBaseActiva?.Id,
                    RespuestaLlmCompleta = JsonSerializer.Serialize(analisisGemini)
                };

                // Si es línea base, crear nueva línea base
                if (esLineaBase)
                {
                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                        $"Creando nueva línea base para paciente {request.PacienteId} con score {analisisGemini.score_global}");

                    var nuevaLineaBase = new LineaBase
                    {
                        PacienteId = request.PacienteId,
                        ScoreGlobalInicial = analisisGemini.score_global,
                        Notas = "Línea base establecida automáticamente - Primera evaluación del paciente"
                    };

                    var lineaBaseCreada = await _analisisRepository.CrearLineaBaseAsync(nuevaLineaBase);
                    analisisCognitivo.LineaBaseId = lineaBaseCreada.Id;

                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                        $"Línea base creada exitosamente con ID: {lineaBaseCreada.Id}");
                }

                // Guardar el análisis
                var analisisGuardado = await _analisisRepository.CrearAnalisisAsync(analisisCognitivo);

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisis",
                    $"Análisis guardado exitosamente con ID: {analisisGuardado.Id}");

                return new AnalisisResponse
                {
                    AnalisisId = analisisGuardado.Id,
                    PacienteId = analisisGuardado.PacienteId,
                    ImagenId = analisisGuardado.ImagenId,
                    ScoreSemantico = analisisGuardado.ScoreSemantico,
                    ScoreObjetos = analisisGuardado.ScoreObjetos,
                    ScoreAcciones = analisisGuardado.ScoreAcciones,
                    FalsosObjetos = analisisGuardado.FalsosObjetos,
                    TiempoRespuestaSeg = analisisGuardado.TiempoRespuestaSeg,
                    CoherenciaLinguistica = analisisGuardado.CoherenciaLinguistica,
                    ScoreGlobal = analisisGuardado.ScoreGlobal,
                    Observaciones = analisisGuardado.Observaciones,
                    ComparacionConBaseline = new ComparacionBaselineDto
                    {
                        DiferenciaScore = analisisGuardado.DiferenciaScore,
                        DeterioroDetectado = analisisGuardado.DeterioroDetectado ?? false,
                        NivelCambio = analisisGuardado.NivelCambio ?? "estable"
                    },
                    EsLineaBase = analisisGuardado.EsLineaBase,
                    FechaAnalisis = analisisGuardado.FechaAnalisis,
                    Mensaje = analisisGuardado.EsLineaBase
                        ? "Línea base establecida correctamente - Primera evaluación del paciente"
                        : analisisGuardado.DeterioroDetectado == true
                            ? $"Deterioro cognitivo detectado - Nivel: {analisisGuardado.NivelCambio}"
                            : "Análisis de seguimiento completado - Sin signos de deterioro significativo"
                };
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisService.RealizarAnalisis",
                    $"Error en análisis: {ex.Message}", ex.ToString(), null, request.PacienteId);
                throw;
            }
        }

        // MÉTODO PARA ANÁLISIS MÚLTIPLE (usado en evaluaciones completas)
        public async Task<List<AnalisisResponse>> RealizarAnalisisMultipleAsync(List<AnalisisRequest> requests, Guid pacienteId)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Iniciando análisis de cuestionario completo para paciente {pacienteId} - {requests.Count} preguntas");

                // Verificar si el paciente tiene análisis previos para determinar si es línea base
                var lineaBaseActiva = await _analisisRepository.ObtenerLineaBaseActivaAsync(pacienteId);
                bool esLineaBase = lineaBaseActiva == null;

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Línea base: {(esLineaBase ? "Primera evaluación del paciente - se creará línea base" : $"Seguimiento basado en línea base {lineaBaseActiva.Id}")}");

                float? scoreBaselinePrevio = null;
                if (!esLineaBase)
                {
                    scoreBaselinePrevio = lineaBaseActiva?.ScoreGlobalInicial;
                }

                // ========== NUEVA LÓGICA: UNA SOLA LLAMADA A GEMINI ==========
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Enviando cuestionario completo a Gemini ({requests.Count} preguntas en una sola llamada)");

                // Analizar el cuestionario completo con Gemini en UNA sola llamada
                var analisisCuestionario = await _geminiService.AnalizarCuestionarioCompletoAsync(
                    requests,
                    scoreBaselinePrevio
                );

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Análisis de Gemini completado - Score promedio: {analisisCuestionario.resumen_general.score_global_promedio}");

                // Verificar que Gemini devolvió resultados para todas las preguntas
                if (analisisCuestionario.resultados_preguntas.Count != requests.Count)
                {
                    throw new Exception($"Gemini devolvió {analisisCuestionario.resultados_preguntas.Count} resultados pero se enviaron {requests.Count} preguntas");
                }

                var resultados = new List<AnalisisResponse>();
                var analisisGuardados = new List<AnalisisCognitivo>();
                float sumaScoresGlobal = 0;

                // Procesar cada resultado de pregunta individual
                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    var resultadoPregunta = analisisCuestionario.resultados_preguntas[i];

                    // Crear metadata simple con la información recibida
                    var metadataJson = JsonSerializer.Serialize(new
                    {
                        imagenId = request.ImagenId,
                        fechaAnalisis = DateTime.UtcNow,
                        fuente = "api_externa",
                        numero_pregunta = i + 1
                    });

                    // Crear el registro del análisis cognitivo
                    var analisisCognitivo = new AnalisisCognitivo
                    {
                        PacienteId = pacienteId,
                        ImagenId = request.ImagenId,
                        DescripcionPaciente = request.DescripcionPaciente,
                        DescripcionReal = request.DescripcionReal,
                        MetadataImagen = metadataJson,
                        ScoreSemantico = resultadoPregunta.score_semantico,
                        ScoreObjetos = resultadoPregunta.score_objetos,
                        ScoreAcciones = resultadoPregunta.score_acciones,
                        FalsosObjetos = resultadoPregunta.falsos_objetos,
                        TiempoRespuestaSeg = analisisCuestionario.resumen_general.tiempo_respuesta_promedio_seg,
                        CoherenciaLinguistica = resultadoPregunta.coherencia_linguistica,
                        ScoreGlobal = resultadoPregunta.score_global,
                        Observaciones = resultadoPregunta.observaciones,
                        DiferenciaScore = analisisCuestionario.comparacion_con_baseline?.diferencia_score,
                        DeterioroDetectado = analisisCuestionario.comparacion_con_baseline?.deterioro_detectado,
                        NivelCambio = analisisCuestionario.comparacion_con_baseline?.nivel_cambio,
                        EsLineaBase = esLineaBase,
                        LineaBaseId = lineaBaseActiva?.Id,
                        RespuestaLlmCompleta = JsonSerializer.Serialize(analisisCuestionario)
                    };

                    analisisGuardados.Add(analisisCognitivo);
                    sumaScoresGlobal += resultadoPregunta.score_global;
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Todos los análisis individuales preparados exitosamente");

                // Si es línea base, crear nueva línea base con el promedio de todos los análisis
                if (esLineaBase && analisisGuardados.Count > 0)
                {
                    float scoreGlobalPromedio = sumaScoresGlobal / analisisGuardados.Count;

                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                        $"Creando nueva línea base para paciente {pacienteId} con score promedio {scoreGlobalPromedio} basado en {analisisGuardados.Count} análisis");

                    var nuevaLineaBase = new LineaBase
                    {
                        PacienteId = pacienteId,
                        ScoreGlobalInicial = scoreGlobalPromedio,
                        Notas = $"Línea base establecida automáticamente - Primera evaluación del paciente con {analisisGuardados.Count} análisis"
                    };

                    var lineaBaseCreada = await _analisisRepository.CrearLineaBaseAsync(nuevaLineaBase);

                    // Actualizar todos los análisis con el ID de la línea base creada
                    foreach (var analisis in analisisGuardados)
                    {
                        analisis.LineaBaseId = lineaBaseCreada.Id;
                    }

                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                        $"Línea base creada exitosamente con ID: {lineaBaseCreada.Id}");
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Guardando {analisisGuardados.Count} análisis en la base de datos");

                // Guardar todos los análisis
                foreach (var analisis in analisisGuardados)
                {
                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                        $"Guardando análisis para ImagenId: {analisis.ImagenId}");

                    var analisisGuardado = await _analisisRepository.CrearAnalisisAsync(analisis);

                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                        $"Análisis guardado exitosamente - Id: {analisisGuardado.Id}, ImagenId: {analisisGuardado.ImagenId}");

                    resultados.Add(new AnalisisResponse
                    {
                        AnalisisId = analisisGuardado.Id,
                        PacienteId = analisisGuardado.PacienteId,
                        ImagenId = analisisGuardado.ImagenId,
                        ScoreSemantico = analisisGuardado.ScoreSemantico,
                        ScoreObjetos = analisisGuardado.ScoreObjetos,
                        ScoreAcciones = analisisGuardado.ScoreAcciones,
                        FalsosObjetos = analisisGuardado.FalsosObjetos,
                        TiempoRespuestaSeg = analisisGuardado.TiempoRespuestaSeg,
                        CoherenciaLinguistica = analisisGuardado.CoherenciaLinguistica,
                        ScoreGlobal = analisisGuardado.ScoreGlobal,
                        Observaciones = analisisGuardado.Observaciones,
                        ComparacionConBaseline = new ComparacionBaselineDto
                        {
                            DiferenciaScore = analisisGuardado.DiferenciaScore,
                            DeterioroDetectado = analisisGuardado.DeterioroDetectado ?? false,
                            NivelCambio = analisisGuardado.NivelCambio ?? "estable"
                        },
                        EsLineaBase = analisisGuardado.EsLineaBase,
                        FechaAnalisis = analisisGuardado.FechaAnalisis,
                        Mensaje = analisisGuardado.EsLineaBase
                            ? $"Línea base establecida correctamente - Evaluación con {analisisGuardados.Count} análisis"
                            : analisisGuardado.DeterioroDetectado == true
                                ? $"Deterioro cognitivo detectado - Nivel: {analisisGuardado.NivelCambio}"
                                : "Análisis de seguimiento completado - Sin signos de deterioro significativo"
                    });
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.RealizarAnalisisMultiple",
                    $"Análisis múltiple completado exitosamente - {resultados.Count} análisis guardados");

                return resultados;
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisService.RealizarAnalisisMultiple",
                    $"Error en análisis múltiple: {ex.Message}", ex.ToString(), null, pacienteId);
                throw;
            }
        }

        // Obtener análisis por ID
        public async Task<AnalisisResponse> ObtenerAnalisisPorIdAsync(int id)
        {
            var analisis = await _analisisRepository.ObtenerPorIdAsync(Guid.Parse(id.ToString()));
            if (analisis == null) return null;

            return MapearAResponse(analisis);
        }

        // Historial paciente
        public async Task<List<AnalisisResponse>> ObtenerHistorialPacienteAsync(int pacienteId)
        {
            var analisis = await _analisisRepository.ObtenerPorPacienteAsync(Guid.Parse(pacienteId.ToString()));
            return analisis.ConvertAll(MapearAResponse);
        }

        private AnalisisResponse MapearAResponse(AnalisisCognitivo analisis)
        {
            return new AnalisisResponse
            {
                AnalisisId = analisis.Id,
                PacienteId = analisis.PacienteId,
                ImagenId = analisis.ImagenId,
                ScoreSemantico = analisis.ScoreSemantico,
                ScoreObjetos = analisis.ScoreObjetos,
                ScoreAcciones = analisis.ScoreAcciones,
                FalsosObjetos = analisis.FalsosObjetos,
                TiempoRespuestaSeg = analisis.TiempoRespuestaSeg,
                CoherenciaLinguistica = analisis.CoherenciaLinguistica,
                ScoreGlobal = analisis.ScoreGlobal,
                Observaciones = analisis.Observaciones,
                ComparacionConBaseline = new ComparacionBaselineDto
                {
                    DiferenciaScore = analisis.DiferenciaScore,
                    DeterioroDetectado = analisis.DeterioroDetectado ?? false,
                    NivelCambio = analisis.NivelCambio ?? "estable"
                },
                EsLineaBase = analisis.EsLineaBase,
                FechaAnalisis = analisis.FechaAnalisis,
                Mensaje = analisis.EsLineaBase
                    ? "Línea base establecida correctamente"
                    : analisis.DeterioroDetectado == true
                        ? $"Deterioro cognitivo detectado - Nivel: {analisis.NivelCambio}"
                        : "Análisis completado - Sin signos de deterioro significativo"
            };
        }

        // ==================== MÉTODOS PARA EVALUACIONES COMPLETAS ====================

        /// <summary>
        /// Procesa una evaluación completa con múltiples preguntas
        /// </summary>
        public async Task<EvaluacionCompletaResponse> ProcesarEvaluacionCompletaAsync(EvaluacionCompletaRequest request)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Iniciando evaluación completa para paciente {request.IdPaciente} - {request.Preguntas.Count} preguntas");

                var pacienteId = Guid.Parse(request.IdPaciente);
                var cuidadorId = Guid.Parse(request.IdCuidador);

                // Determinar si es línea base (primera evaluación del paciente)
                var lineaBaseActiva = await _analisisRepository.ObtenerLineaBaseActivaAsync(pacienteId);
                bool esLineaBase = lineaBaseActiva == null;

                // Convertir las preguntas en AnalisisRequest
                var analisisRequests = request.Preguntas.Select(p => new AnalisisRequest
                {
                    PacienteId = pacienteId,
                    ImagenId = Guid.Parse(p.IdPicture),
                    DescripcionPaciente = p.PacienteRespuesta,
                    DescripcionReal = p.DescripcionReal
                }).ToList();

                // Usar el método de análisis múltiple que maneja la línea base correctamente
                var analisisResponses = await RealizarAnalisisMultipleAsync(analisisRequests, pacienteId);

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Análisis múltiple completado - {analisisResponses.Count} respuestas recibidas");

                // Construir los resultados individuales desde las respuestas
                var resultadosIndividuales = new List<ResultadoPregunta>();
                float sumaScoresGlobal = 0;
                float sumaScoresSemantico = 0;
                float sumaScoresObjetos = 0;
                float sumaScoresAcciones = 0;
                float sumaCoherencia = 0;
                int totalDeteriorosDetectados = 0;

                // Iterar sobre las respuestas de análisis (que ya están en el orden correcto)
                for (int i = 0; i < analisisResponses.Count && i < request.Preguntas.Count; i++)
                {
                    var analisisResponse = analisisResponses[i];
                    var pregunta = request.Preguntas[i];

                    await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                        $"Procesando resultado {i + 1}/{analisisResponses.Count} - ImagenId: {analisisResponse.ImagenId}, IdPicture: {pregunta.IdPicture}");

                    resultadosIndividuales.Add(new ResultadoPregunta
                    {
                        IdPicture = pregunta.IdPicture,
                        AnalisisId = analisisResponse.AnalisisId,
                        ImagenUrl = pregunta.ImagenUrl,
                        DescripcionReal = pregunta.DescripcionReal,
                        PacienteRespuesta = pregunta.PacienteRespuesta,
                        ScoreGlobal = analisisResponse.ScoreGlobal,
                        ScoreSemantico = analisisResponse.ScoreSemantico,
                        ScoreObjetos = analisisResponse.ScoreObjetos,
                        ScoreAcciones = analisisResponse.ScoreAcciones,
                        FalsosObjetos = analisisResponse.FalsosObjetos,
                        CoherenciaLinguistica = analisisResponse.CoherenciaLinguistica,
                        Observaciones = analisisResponse.Observaciones,
                        NivelCambio = analisisResponse.ComparacionConBaseline?.NivelCambio ?? "estable",
                        DeterioroDetectado = analisisResponse.ComparacionConBaseline?.DeterioroDetectado ?? false
                    });

                    // Acumular estadísticas
                    sumaScoresGlobal += analisisResponse.ScoreGlobal;
                    sumaScoresSemantico += analisisResponse.ScoreSemantico;
                    sumaScoresObjetos += analisisResponse.ScoreObjetos;
                    sumaScoresAcciones += analisisResponse.ScoreAcciones;
                    sumaCoherencia += analisisResponse.CoherenciaLinguistica;

                    if (analisisResponse.ComparacionConBaseline?.DeterioroDetectado == true)
                    {
                        totalDeteriorosDetectados++;
                    }
                }

                int totalProcesadas = resultadosIndividuales.Count;
                if (totalProcesadas == 0)
                {
                    throw new Exception("No se pudo procesar ninguna pregunta de la evaluación");
                }

                // Calcular promedios
                float scoreGlobalPromedio = sumaScoresGlobal / totalProcesadas;
                float scoreSemanticoPromedio = sumaScoresSemantico / totalProcesadas;
                float scoreObjetosPromedio = sumaScoresObjetos / totalProcesadas;
                float scoreAccionesPromedio = sumaScoresAcciones / totalProcesadas;
                float coherenciaPromedio = sumaCoherencia / totalProcesadas;

                // Determinar nivel de deterioro general
                bool deterioroDetectado = totalDeteriorosDetectados > (totalProcesadas / 2); // Más del 50%
                string nivelDeterioroGeneral = DeterminarNivelDeterioroGeneral(scoreGlobalPromedio, deterioroDetectado);

                float? diferenciaConLineaBase = null;
                Guid? lineaBaseId = null;

                // Obtener la línea base después del procesamiento (puede haberse creado)
                if (esLineaBase)
                {
                    var nuevaLineaBase = await _analisisRepository.ObtenerLineaBaseActivaAsync(pacienteId);
                    lineaBaseId = nuevaLineaBase?.Id;
                }
                else
                {
                    lineaBaseId = lineaBaseActiva?.Id;
                    if (lineaBaseActiva != null)
                    {
                        diferenciaConLineaBase = scoreGlobalPromedio - lineaBaseActiva.ScoreGlobalInicial;
                    }
                }

                // Generar observaciones y recomendaciones
                string observaciones = GenerarObservacionesGenerales(resultadosIndividuales, scoreGlobalPromedio, deterioroDetectado);
                string recomendaciones = GenerarRecomendacionesMedicas(nivelDeterioroGeneral, scoreGlobalPromedio, deterioroDetectado);

                // Crear registro de evaluación completa
                var evaluacionCompleta = new EvaluacionCompleta
                {
                    PacienteId = pacienteId,
                    CuidadorId = cuidadorId,
                    FechaEvaluacion = request.FechaRealizacion,
                    TotalPreguntas = request.Preguntas.Count,
                    PreguntasProcesadas = totalProcesadas,
                    ScoreGlobalPromedio = scoreGlobalPromedio,
                    ScoreSemanticoPromedio = scoreSemanticoPromedio,
                    ScoreObjetosPromedio = scoreObjetosPromedio,
                    ScoreAccionesPromedio = scoreAccionesPromedio,
                    CoherenciaPromedio = coherenciaPromedio,
                    TiempoRespuestaPromedio = 30.0f,
                    DeterioroDetectado = deterioroDetectado,
                    NivelDeterioroGeneral = nivelDeterioroGeneral,
                    DiferenciaConLineaBase = diferenciaConLineaBase,
                    ObservacionesGenerales = observaciones,
                    RecomendacionesMedicas = recomendaciones,
                    EsLineaBase = esLineaBase,
                    LineaBaseId = lineaBaseId
                };

                // Guardar evaluación completa en base de datos
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Guardando evaluación completa en base de datos");

                var evaluacionGuardada = await _analisisRepository.CrearEvaluacionCompletaAsync(evaluacionCompleta);

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Evaluación completa guardada con ID: {evaluacionGuardada.Id}");

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Evaluación completa finalizada - Score promedio: {scoreGlobalPromedio:F2}");

                return new EvaluacionCompletaResponse
                {
                    EvaluacionId = evaluacionGuardada.Id,
                    PacienteId = pacienteId,
                    CuidadorId = cuidadorId,
                    FechaEvaluacion = request.FechaRealizacion,
                    TotalPreguntas = request.Preguntas.Count,
                    PreguntasProcesadas = totalProcesadas,
                    ScoreGlobalPromedio = scoreGlobalPromedio,
                    ScoreSemanticoPromedio = scoreSemanticoPromedio,
                    ScoreObjetosPromedio = scoreObjetosPromedio,
                    ScoreAccionesPromedio = scoreAccionesPromedio,
                    CoherenciaPromedio = coherenciaPromedio,
                    TiempoRespuestaPromedio = 30.0f,
                    DeterioroDetectado = deterioroDetectado,
                    NivelDeterioroGeneral = nivelDeterioroGeneral,
                    DiferenciaConLineaBase = diferenciaConLineaBase,
                    ResultadosIndividuales = resultadosIndividuales,
                    ObservacionesGenerales = observaciones,
                    RecomendacionesMedicas = recomendaciones,
                    EsLineaBase = esLineaBase,
                    LineaBaseId = lineaBaseId,
                    FechaProcesamiento = evaluacionGuardada.FechaCreacion
                };
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisService.ProcesarEvaluacionCompleta", ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Determina el nivel de deterioro general basado en el score promedio
        /// </summary>
        private string DeterminarNivelDeterioroGeneral(float scorePromedio, bool deterioroDetectado)
        {
            if (!deterioroDetectado || scorePromedio >= 80)
                return "estable";

            if (scorePromedio >= 70)
                return "leve";

            if (scorePromedio >= 50)
                return "moderado";

            return "severo";
        }

        /// <summary>
        /// Genera observaciones generales de la evaluación
        /// </summary>
        private string GenerarObservacionesGenerales(List<ResultadoPregunta> resultados, float scorePromedio, bool deterioroDetectado)
        {
            var observaciones = new System.Text.StringBuilder();
            observaciones.AppendLine($"Evaluación completada con {resultados.Count} preguntas.");
            observaciones.AppendLine($"Score global promedio: {scorePromedio:F2}/100");

            int preguntasConDeterioro = resultados.Count(r => r.DeterioroDetectado);
            if (preguntasConDeterioro > 0)
            {
                observaciones.AppendLine($"Se detectó deterioro en {preguntasConDeterioro} de {resultados.Count} preguntas.");
            }

            var scoresOrdenados = resultados.OrderBy(r => r.ScoreGlobal).ToList();
            if (scoresOrdenados.Any())
            {
                observaciones.AppendLine($"Mejor desempeño: Pregunta {scoresOrdenados.Last().IdPicture} ({scoresOrdenados.Last().ScoreGlobal:F2})");
                observaciones.AppendLine($"Mayor dificultad: Pregunta {scoresOrdenados.First().IdPicture} ({scoresOrdenados.First().ScoreGlobal:F2})");
            }

            return observaciones.ToString();
        }

        /// <summary>
        /// Genera recomendaciones médicas basadas en el análisis
        /// </summary>
        private string GenerarRecomendacionesMedicas(string nivelDeterioro, float scorePromedio, bool deterioroDetectado)
        {
            var recomendaciones = new System.Text.StringBuilder();

            switch (nivelDeterioro)
            {
                case "estable":
                    recomendaciones.AppendLine("✅ Función cognitiva estable.");
                    recomendaciones.AppendLine("• Continuar con evaluaciones periódicas de seguimiento.");
                    recomendaciones.AppendLine("• Mantener actividades de estimulación cognitiva.");
                    break;

                case "leve":
                    recomendaciones.AppendLine("⚠️ Deterioro cognitivo leve detectado.");
                    recomendaciones.AppendLine("• Recomendar consulta con neurólogo.");
                    recomendaciones.AppendLine("• Incrementar frecuencia de evaluaciones (mensual).");
                    recomendaciones.AppendLine("• Iniciar terapia de estimulación cognitiva.");
                    break;

                case "moderado":
                    recomendaciones.AppendLine("⚠️ Deterioro cognitivo moderado detectado.");
                    recomendaciones.AppendLine("• URGENTE: Derivación a especialista en neurología.");
                    recomendaciones.AppendLine("• Evaluación neuropsicológica completa recomendada.");
                    recomendaciones.AppendLine("• Considerar estudios de imagen cerebral.");
                    recomendaciones.AppendLine("• Iniciar plan de intervención terapéutica.");
                    break;

                case "severo":
                    recomendaciones.AppendLine("🚨 Deterioro cognitivo severo detectado.");
                    recomendaciones.AppendLine("• URGENTE: Consulta neurológica inmediata.");
                    recomendaciones.AppendLine("• Evaluación para posible diagnóstico de demencia.");
                    recomendaciones.AppendLine("• Considerar estudios complementarios (TAC/MRI).");
                    recomendaciones.AppendLine("• Evaluación de capacidades para actividades diarias.");
                    recomendaciones.AppendLine("• Apoyo para familia y cuidadores.");
                    break;
            }

            return recomendaciones.ToString();
        }

        /// <summary>
        /// Obtiene una evaluación completa por ID
        /// </summary>
        public async Task<EvaluacionCompletaResponse> ObtenerEvaluacionCompletaPorIdAsync(Guid evaluacionId)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionCompletaPorId",
                    $"Obteniendo evaluación completa con ID: {evaluacionId}");

                var evaluacion = await _analisisRepository.ObtenerEvaluacionCompletaPorIdAsync(evaluacionId);

                if (evaluacion == null)
                {
                    await _logRepository.RegistrarAsync("WARNING", "AnalisisService.ObtenerEvaluacionCompletaPorId",
                        $"No se encontró evaluación con ID: {evaluacionId}");
                    return null;
                }

                // Obtener los análisis individuales que pertenecen a esta evaluación
                // Los análisis tienen la misma fecha y paciente que la evaluación
                var analisisIndividuales = await _analisisRepository.ObtenerPorPacienteAsync(evaluacion.PacienteId);

                // Filtrar análisis del mismo día de la evaluación
                var analisisDelDia = analisisIndividuales
                    .Where(a => a.FechaAnalisis.Date == evaluacion.FechaEvaluacion.Date)
                    .OrderBy(a => a.FechaAnalisis)
                    .ToList();

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionCompletaPorId",
                    $"Se encontraron {analisisDelDia.Count} análisis individuales para la evaluación");

                // Construir los resultados individuales
                var resultadosIndividuales = analisisDelDia.Select(a => new ResultadoPregunta
                {
                    IdPicture = a.ImagenId.ToString(),
                    AnalisisId = a.Id,
                    ImagenUrl = "", // No está almacenada en el análisis
                    DescripcionReal = a.DescripcionReal,
                    PacienteRespuesta = a.DescripcionPaciente,
                    ScoreGlobal = a.ScoreGlobal,
                    ScoreSemantico = a.ScoreSemantico,
                    ScoreObjetos = a.ScoreObjetos,
                    ScoreAcciones = a.ScoreAcciones,
                    FalsosObjetos = a.FalsosObjetos,
                    CoherenciaLinguistica = a.CoherenciaLinguistica,
                    Observaciones = a.Observaciones,
                    NivelCambio = a.NivelCambio ?? "estable",
                    DeterioroDetectado = a.DeterioroDetectado ?? false
                }).ToList();

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionCompletaPorId",
                    $"Evaluación completa obtenida exitosamente");

                return new EvaluacionCompletaResponse
                {
                    EvaluacionId = evaluacion.Id,
                    PacienteId = evaluacion.PacienteId,
                    CuidadorId = evaluacion.CuidadorId,
                    FechaEvaluacion = evaluacion.FechaEvaluacion,
                    TotalPreguntas = evaluacion.TotalPreguntas,
                    PreguntasProcesadas = evaluacion.PreguntasProcesadas,
                    ScoreGlobalPromedio = evaluacion.ScoreGlobalPromedio,
                    ScoreSemanticoPromedio = evaluacion.ScoreSemanticoPromedio,
                    ScoreObjetosPromedio = evaluacion.ScoreObjetosPromedio,
                    ScoreAccionesPromedio = evaluacion.ScoreAccionesPromedio,
                    CoherenciaPromedio = evaluacion.CoherenciaPromedio,
                    TiempoRespuestaPromedio = evaluacion.TiempoRespuestaPromedio,
                    DeterioroDetectado = evaluacion.DeterioroDetectado,
                    NivelDeterioroGeneral = evaluacion.NivelDeterioroGeneral,
                    DiferenciaConLineaBase = evaluacion.DiferenciaConLineaBase,
                    ResultadosIndividuales = resultadosIndividuales,
                    ObservacionesGenerales = evaluacion.ObservacionesGenerales,
                    RecomendacionesMedicas = evaluacion.RecomendacionesMedicas,
                    EsLineaBase = evaluacion.EsLineaBase,
                    LineaBaseId = evaluacion.LineaBaseId,
                    FechaProcesamiento = evaluacion.FechaCreacion
                };
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisService.ObtenerEvaluacionCompletaPorId",
                    $"Error al obtener evaluación: {ex.Message}", ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las evaluaciones de un paciente
        /// </summary>
        public async Task<List<EvaluacionCompletaResponse>> ObtenerEvaluacionesPorPacienteAsync(Guid pacienteId)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionesPorPaciente",
                    $"Obteniendo evaluaciones completas para paciente: {pacienteId}");

                var evaluaciones = await _analisisRepository.ObtenerEvaluacionesPorPacienteAsync(pacienteId);

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionesPorPaciente",
                    $"Se encontraron {evaluaciones.Count} evaluaciones para el paciente");

                if (!evaluaciones.Any())
                {
                    return new List<EvaluacionCompletaResponse>();
                }

                // Obtener todos los análisis del paciente para poder construir los detalles
                var todosLosAnalisis = await _analisisRepository.ObtenerPorPacienteAsync(pacienteId);

                var respuestas = new List<EvaluacionCompletaResponse>();

                foreach (var evaluacion in evaluaciones)
                {
                    // Filtrar análisis del mismo día de la evaluación
                    var analisisDelDia = todosLosAnalisis
                        .Where(a => a.FechaAnalisis.Date == evaluacion.FechaEvaluacion.Date)
                        .OrderBy(a => a.FechaAnalisis)
                        .ToList();

                    // Construir los resultados individuales
                    var resultadosIndividuales = analisisDelDia.Select(a => new ResultadoPregunta
                    {
                        IdPicture = a.ImagenId.ToString(),
                        AnalisisId = a.Id,
                        ImagenUrl = "", // No está almacenada en el análisis
                        DescripcionReal = a.DescripcionReal,
                        PacienteRespuesta = a.DescripcionPaciente,
                        ScoreGlobal = a.ScoreGlobal,
                        ScoreSemantico = a.ScoreSemantico,
                        ScoreObjetos = a.ScoreObjetos,
                        ScoreAcciones = a.ScoreAcciones,
                        FalsosObjetos = a.FalsosObjetos,
                        CoherenciaLinguistica = a.CoherenciaLinguistica,
                        Observaciones = a.Observaciones,
                        NivelCambio = a.NivelCambio ?? "estable",
                        DeterioroDetectado = a.DeterioroDetectado ?? false
                    }).ToList();

                    respuestas.Add(new EvaluacionCompletaResponse
                    {
                        EvaluacionId = evaluacion.Id,
                        PacienteId = evaluacion.PacienteId,
                        CuidadorId = evaluacion.CuidadorId,
                        FechaEvaluacion = evaluacion.FechaEvaluacion,
                        TotalPreguntas = evaluacion.TotalPreguntas,
                        PreguntasProcesadas = evaluacion.PreguntasProcesadas,
                        ScoreGlobalPromedio = evaluacion.ScoreGlobalPromedio,
                        ScoreSemanticoPromedio = evaluacion.ScoreSemanticoPromedio,
                        ScoreObjetosPromedio = evaluacion.ScoreObjetosPromedio,
                        ScoreAccionesPromedio = evaluacion.ScoreAccionesPromedio,
                        CoherenciaPromedio = evaluacion.CoherenciaPromedio,
                        TiempoRespuestaPromedio = evaluacion.TiempoRespuestaPromedio,
                        DeterioroDetectado = evaluacion.DeterioroDetectado,
                        NivelDeterioroGeneral = evaluacion.NivelDeterioroGeneral,
                        DiferenciaConLineaBase = evaluacion.DiferenciaConLineaBase,
                        ResultadosIndividuales = resultadosIndividuales,
                        ObservacionesGenerales = evaluacion.ObservacionesGenerales,
                        RecomendacionesMedicas = evaluacion.RecomendacionesMedicas,
                        EsLineaBase = evaluacion.EsLineaBase,
                        LineaBaseId = evaluacion.LineaBaseId,
                        FechaProcesamiento = evaluacion.FechaCreacion
                    });
                }

                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ObtenerEvaluacionesPorPaciente",
                    $"Evaluaciones obtenidas exitosamente - Total: {respuestas.Count}");

                return respuestas;
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "AnalisisService.ObtenerEvaluacionesPorPaciente",
                    $"Error al obtener evaluaciones del paciente: {ex.Message}", ex.ToString());
                throw;
            }
        }
    }
}
