using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.Model.DTOs;

namespace Recorderfy.Analisys.Service.BLL.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogRepository _logRepository;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public GeminiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogRepository logRepository)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logRepository = logRepository;
            _apiKey = _configuration["Gemini:ApiKey"];
            _apiUrl = _configuration["Gemini:ApiUrl"];
        }

        public async Task<GeminiAnalisisResponse> AnalizarConGeminiAsync(
            string descripcionPaciente,
            string descripcionReal,
            string metadataJson,
            float? scoreBaselinePrevio = null)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "GeminiService",
                    "Iniciando análisis con Google Gemini");

                var prompt = ConstruirPrompt(descripcionPaciente, descripcionReal, metadataJson, scoreBaselinePrevio);

                var client = _httpClientFactory.CreateClient();

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 2048,
                        responseMimeType = "application/json"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // CORRECCIÓN: Usar HttpRequestMessage para agregar headers personalizados
                var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                {
                    Content = content
                };

                // Agregar la API Key en el header como Gemini lo espera
                request.Headers.Add("x-goog-api-key", _apiKey);

                await _logRepository.RegistrarAsync("INFO", "GeminiService",
                    $"Enviando request a: {_apiUrl}");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _logRepository.RegistrarAsync("ERROR", "GeminiService",
                        $"Error en llamada a Gemini API: {response.StatusCode}",
                        errorContent);
                    throw new Exception($"Error al llamar a Gemini API: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent);

                // Extraer el JSON del texto de respuesta
                var responseText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("Respuesta vacía de Gemini API");
                }

                // Limpiar el texto (remover markdown si existe)
                responseText = responseText.Trim();
                if (responseText.StartsWith("```json"))
                {
                    responseText = responseText.Substring(7);
                }
                if (responseText.StartsWith("```"))
                {
                    responseText = responseText.Substring(3);
                }
                if (responseText.EndsWith("```"))
                {
                    responseText = responseText.Substring(0, responseText.Length - 3);
                }
                responseText = responseText.Trim();

                var analisisResponse = JsonSerializer.Deserialize<GeminiAnalisisResponse>(responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                await _logRepository.RegistrarAsync("INFO", "GeminiService",
                    "Análisis completado exitosamente",
                    datosAdicionales: JsonSerializer.Serialize(analisisResponse));

                return analisisResponse;
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "GeminiService",
                    "Error al analizar con Gemini", ex.ToString());
                throw;
            }
        }

        private string ConstruirPrompt(string descripcionPaciente, string descripcionReal,
            string metadataJson, float? scoreBaselinePrevio)
        {
            var baselineInfo = scoreBaselinePrevio.HasValue
                ? $"\n\nSCORE BASELINE PREVIO: {scoreBaselinePrevio.Value}\nRealiza comparación con este valor para detectar deterioro."
                : "\n\nEsta es una EVALUACIÓN INICIAL (línea base). No hay datos previos para comparar.";

            return $@"Eres un especialista en neurología cognitiva experto en detección de Alzheimer.

TAREA: Analiza la descripción de una imagen dada por un paciente y compárala con la descripción real.

DESCRIPCIÓN DEL PACIENTE:
{descripcionPaciente}

DESCRIPCIÓN REAL DE LA IMAGEN:
{descripcionReal}

METADATA DE LA IMAGEN:
{metadataJson ?? "No disponible"}
{baselineInfo}

CRITERIOS DE EVALUACIÓN:

1. score_semantico (0-100): Precisión en el significado general de la escena
2. score_objetos (0-100): Precisión en identificación de objetos presentes
3. score_acciones (0-100): Precisión en descripción de acciones/actividades
4. falsos_objetos (número): Cantidad de objetos mencionados que NO están en la imagen
5. tiempo_respuesta_seg: Tiempo que tardó en responder (proporcionado)
6. coherencia_linguistica (0-100): Fluidez, gramática y coherencia del discurso
7. score_global (0-100): Promedio ponderado de todos los scores

INTERPRETACIÓN DE DETERIORO:
- diferencia_score > 0 y < 5: estable
- diferencia_score >= 5 y < 15: leve
- diferencia_score >= 15 y < 30: moderado
- diferencia_score >= 30: severo

FORMATO DE RESPUESTA (JSON ESTRICTO):
{{
  ""score_semantico"": float,
  ""score_objetos"": float,
  ""score_acciones"": float,
  ""falsos_objetos"": int,
  ""tiempo_respuesta_seg"": float,
  ""coherencia_linguistica"": float,
  ""score_global"": float,
  ""observaciones"": ""string detallada explicando hallazgos clave"",
  ""comparacion_con_baseline"": {{
    ""diferencia_score"": float,
    ""deterioro_detectado"": boolean,
    ""nivel_cambio"": ""estable|leve|moderado|severo""
  }}
}}

IMPORTANTE: Devuelve SOLO el JSON, sin explicaciones adicionales.";
        }

        /// <summary>
        /// Analiza un cuestionario completo en una sola llamada a Gemini
        /// </summary>
        /// <summary>
        /// Analiza un cuestionario completo en una sola llamada a Gemini
        /// </summary>
        public async Task<GeminiCuestionarioResponse> AnalizarCuestionarioCompletoAsync(
            List<AnalisisRequest> preguntas,
            float? scoreBaselinePrevio = null)
        {
            try
            {
                await _logRepository.RegistrarAsync("INFO", "GeminiService.AnalizarCuestionarioCompleto",
                    $"Iniciando análisis de cuestionario completo con {preguntas.Count} preguntas");

                var prompt = ConstruirPromptCuestionarioCompleto(preguntas, scoreBaselinePrevio);

                var client = _httpClientFactory.CreateClient();

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 8192, // Aumentado para respuestas más largas
                        responseMimeType = "application/json"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                {
                    Content = content
                };

                request.Headers.Add("x-goog-api-key", _apiKey);

                await _logRepository.RegistrarAsync("INFO", "GeminiService.AnalizarCuestionarioCompleto",
                    $"Enviando cuestionario a Gemini API");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _logRepository.RegistrarAsync("ERROR", "GeminiService.AnalizarCuestionarioCompleto",
                        $"Error en llamada a Gemini API: {response.StatusCode}",
                        errorContent);
                    throw new Exception($"Error al llamar a Gemini API: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent);

                var responseText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("Respuesta vacía de Gemini API");
                }

                // Limpiar markdown si existe
                if (responseText.StartsWith("```json"))
                {
                    responseText = responseText.Replace("```json", "").Replace("```", "").Trim();
                }
                responseText = responseText.Trim();

                await _logRepository.RegistrarAsync("INFO", "GeminiService.AnalizarCuestionarioCompleto",
                    $"Respuesta recibida, parseando JSON...");

                var cuestionarioResponse = JsonSerializer.Deserialize<GeminiCuestionarioResponse>(responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                await _logRepository.RegistrarAsync("INFO", "GeminiService.AnalizarCuestionarioCompleto",
                    $"Análisis de cuestionario completado exitosamente - Score promedio: {cuestionarioResponse.resumen_general.score_global_promedio}");

                return cuestionarioResponse;
            }
            catch (Exception ex)
            {
                await _logRepository.RegistrarAsync("ERROR", "GeminiService.AnalizarCuestionarioCompleto",
                    "Error al analizar cuestionario con Gemini", ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Construye el prompt para analizar un cuestionario completo
        /// </summary>
        private string ConstruirPromptCuestionarioCompleto(List<AnalisisRequest> preguntas, float? scoreBaselinePrevio)
        {
            var baselineInfo = scoreBaselinePrevio.HasValue
                ? $"\n\nSCORE BASELINE PREVIO DEL PACIENTE: {scoreBaselinePrevio.Value}\nRealiza comparación con este valor para detectar deterioro cognitivo."
                : "\n\nEsta es una EVALUACIÓN INICIAL (línea base). No hay datos previos para comparar.";

            var preguntasFormateadas = new System.Text.StringBuilder();
            for (int i = 0; i < preguntas.Count; i++)
            {
                preguntasFormateadas.AppendLine($@"
PREGUNTA #{i + 1}:
- Descripción del paciente: {preguntas[i].DescripcionPaciente}
- Descripción real: {preguntas[i].DescripcionReal}
");
            }

            return $@"Eres un especialista en neurología cognitiva experto en detección temprana de Alzheimer y deterioro cognitivo.

TAREA: Analiza un CUESTIONARIO COMPLETO de evaluación cognitiva. El paciente describió {preguntas.Count} imágenes diferentes. Evalúa CADA pregunta individualmente Y proporciona un análisis general del cuestionario completo.

{preguntasFormateadas}
{baselineInfo}

CRITERIOS DE EVALUACIÓN POR PREGUNTA:
1. score_semantico (0-100): Precisión en el significado general de la escena
2. score_objetos (0-100): Precisión en identificación de objetos presentes
3. score_acciones (0-100): Precisión en descripción de acciones/actividades
4. falsos_objetos (número): Cantidad de objetos mencionados que NO están en la imagen
5. coherencia_linguistica (0-100): Fluidez, gramática y coherencia del discurso
6. score_global (0-100): Promedio ponderado de todos los scores

CRITERIOS DEL RESUMEN GENERAL:
- Calcula promedios de todos los scores
- Identifica patrones de deterioro
- Detecta áreas de fortaleza y debilidad
- Proporciona observaciones clínicas relevantes
- Genera recomendaciones médicas específicas según el nivel de deterioro

INTERPRETACIÓN DE DETERIORO:
- Score >= 80: estable (función cognitiva normal)
- Score 70-79: leve (monitoreo recomendado)
- Score 50-69: moderado (evaluación neurológica urgente)
- Score < 50: severo (atención inmediata requerida)

FORMATO DE RESPUESTA (JSON ESTRICTO):
{{
  ""resultados_preguntas"": [
    {{
      ""numero_pregunta"": 1,
      ""score_semantico"": float,
      ""score_objetos"": float,
      ""score_acciones"": float,
      ""falsos_objetos"": int,
      ""coherencia_linguistica"": float,
      ""score_global"": float,
      ""observaciones"": ""string con hallazgos específicos de esta pregunta""
    }}
    // ... repetir para cada pregunta
  ],
  ""resumen_general"": {{
    ""score_global_promedio"": float,
    ""score_semantico_promedio"": float,
    ""score_objetos_promedio"": float,
    ""score_acciones_promedio"": float,
    ""coherencia_promedio"": float,
    ""total_falsos_objetos"": int,
    ""tiempo_respuesta_promedio_seg"": float (estimar ~30 segundos si no hay datos),
    ""observaciones_generales"": ""string con análisis integral del desempeño del paciente"",
    ""recomendaciones_medicas"": ""string con recomendaciones específicas según el nivel de deterioro"",
    ""nivel_deterioro"": ""estable|leve|moderado|severo"",
    ""deterioro_detectado"": boolean
  }},
  ""comparacion_con_baseline"": {{
    ""diferencia_score"": float (diferencia entre score_global_promedio y baseline, 0 si no hay baseline),
    ""deterioro_detectado"": boolean,
    ""nivel_cambio"": ""estable|leve|moderado|severo""
  }}
}}

IMPORTANTE: 
- Devuelve SOLO el JSON válido, sin explicaciones adicionales
- Asegúrate de incluir {preguntas.Count} elementos en resultados_preguntas
- Sé específico en las observaciones y recomendaciones médicas
- Las recomendaciones deben incluir: seguimiento, estudios adicionales, intervenciones terapéuticas";
        }

        // Clase auxiliar para deserializar respuesta de Gemini
        private class GeminiApiResponse
        {
            public Candidate[] candidates { get; set; }
        }

        private class Candidate
        {
            public Content content { get; set; }
        }

        private class Content
        {
            public Part[] parts { get; set; }
        }

        private class Part
        {
            public string text { get; set; }
        }
    }
}
