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

                var resultadosIndividuales = new List<ResultadoPregunta>();
                float sumaScoresGlobal = 0;
                float sumaScoresSemantico = 0;
                float sumaScoresObjetos = 0;
                float sumaScoresAcciones = 0;
                float sumaCoherencia = 0;
                int totalDeteriorosDetectados = 0;

                // Procesar cada pregunta
                foreach (var pregunta in request.Preguntas)
                {
                    try
                    {
                        // Crear metadata simple con la información recibida
                        var metadataJson = JsonSerializer.Serialize(new
                        {
                            categoria = "evaluacion_personalizada",
                            fuente = "cuidador",
                            url = pregunta.ImagenUrl,
                            imagenId = pregunta.IdPicture
                        });

                        float? scoreBaselinePrevio = null;
                        if (!esLineaBase && lineaBaseActiva != null)
                        {
                            scoreBaselinePrevio = lineaBaseActiva.ScoreGlobalInicial;
                        }

                        // Análisis con Gemini
                        var analisisGemini = await _geminiService.AnalizarConGeminiAsync(
                            pregunta.PacienteRespuesta,
                            pregunta.DescripcionReal,
                            metadataJson,
                            scoreBaselinePrevio
                        );

                        // Guardar análisis individual
                        var analisisCognitivo = new AnalisisCognitivo
                        {
                            PacienteId = pacienteId,
                            ImagenId = Guid.Parse(pregunta.IdPicture),
                            DescripcionPaciente = pregunta.PacienteRespuesta,
                            DescripcionReal = pregunta.DescripcionReal,
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

                        var analisisGuardado = await _analisisRepository.CrearAnalisisAsync(analisisCognitivo);

                        // Agregar a resultados
                        resultadosIndividuales.Add(new ResultadoPregunta
                        {
                            IdPicture = pregunta.IdPicture,
                            AnalisisId = analisisGuardado.Id,
                            ImagenUrl = pregunta.ImagenUrl,
                            DescripcionReal = pregunta.DescripcionReal,
                            PacienteRespuesta = pregunta.PacienteRespuesta,
                            ScoreGlobal = analisisGemini.score_global,
                            ScoreSemantico = analisisGemini.score_semantico,
                            ScoreObjetos = analisisGemini.score_objetos,
                            ScoreAcciones = analisisGemini.score_acciones,
                            FalsosObjetos = analisisGemini.falsos_objetos,
                            CoherenciaLinguistica = analisisGemini.coherencia_linguistica,
                            Observaciones = analisisGemini.observaciones,
                            NivelCambio = analisisGemini.comparacion_con_baseline?.nivel_cambio ?? "estable",
                            DeterioroDetectado = analisisGemini.comparacion_con_baseline?.deterioro_detectado ?? false
                        });

                        // Acumular estadísticas
                        sumaScoresGlobal += analisisGemini.score_global;
                        sumaScoresSemantico += analisisGemini.score_semantico;
                        sumaScoresObjetos += analisisGemini.score_objetos;
                        sumaScoresAcciones += analisisGemini.score_acciones;
                        sumaCoherencia += analisisGemini.coherencia_linguistica;

                        if (analisisGemini.comparacion_con_baseline?.deterioro_detectado == true)
                        {
                            totalDeteriorosDetectados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logRepository.RegistrarAsync("ERROR", "AnalisisService.ProcesarEvaluacionCompleta",
                            $"Error procesando pregunta {pregunta.IdPicture}: {ex.Message}");
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
                if (!esLineaBase && lineaBaseActiva != null)
                {
                    diferenciaConLineaBase = scoreGlobalPromedio - lineaBaseActiva.ScoreGlobalInicial;
                }

                // Crear línea base si es la primera evaluación
                Guid? lineaBaseId = lineaBaseActiva?.Id;
                if (esLineaBase)
                {
                    var nuevaLineaBase = new LineaBase
                    {
                        PacienteId = pacienteId,
                        ScoreGlobalInicial = scoreGlobalPromedio,
                        Notas = $"Línea base establecida con evaluación completa de {totalProcesadas} preguntas"
                    };
                    var lineaBaseCreada = await _analisisRepository.CrearLineaBaseAsync(nuevaLineaBase);
                    lineaBaseId = lineaBaseCreada.Id;
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

                // Guardar en base de datos (necesitaremos crear el repositorio)
                // Por ahora, retornamos el response directamente
                
                await _logRepository.RegistrarAsync("INFO", "AnalisisService.ProcesarEvaluacionCompleta",
                    $"Evaluación completa finalizada - Score promedio: {scoreGlobalPromedio:F2}");

                return new EvaluacionCompletaResponse
                {
                    EvaluacionId = evaluacionCompleta.Id,
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
                    FechaProcesamiento = DateTime.UtcNow
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
            // TODO: Implementar cuando se cree el repositorio de evaluaciones
            throw new NotImplementedException("Método en desarrollo - Requiere repositorio de evaluaciones completas");
        }

        /// <summary>
        /// Obtiene todas las evaluaciones de un paciente
        /// </summary>
        public async Task<List<EvaluacionCompletaResponse>> ObtenerEvaluacionesPorPacienteAsync(Guid pacienteId)
        {
            // TODO: Implementar cuando se cree el repositorio de evaluaciones
            throw new NotImplementedException("Método en desarrollo - Requiere repositorio de evaluaciones completas");
        }
    }
}
