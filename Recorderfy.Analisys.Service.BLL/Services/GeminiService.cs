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
