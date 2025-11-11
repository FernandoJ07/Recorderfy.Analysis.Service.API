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

                var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                {
                    Content = content
                };
                request.Headers.Add("x-goog-api-key", _apiKey);

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logRepository.RegistrarAsync("ERROR", "GeminiService",
                        $"Error en llamada a Gemini API: {response.StatusCode}", responseContent);
                    throw new Exception($"Error al llamar a Gemini API: {response.StatusCode}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent);
                var responseText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;

                if (string.IsNullOrWhiteSpace(responseText))
                    throw new Exception("Respuesta vacía de Gemini API");

                responseText = SanitizarJsonGemini(responseText);

                await _logRepository.RegistrarAsync("DEBUG", "GeminiService", $"JSON limpio recibido: {responseText}");

                GeminiAnalisisResponse analisisResponse;
                try
                {
                    analisisResponse = JsonSerializer.Deserialize<GeminiAnalisisResponse>(
                        responseText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        });
                }
                catch (JsonException ex)
                {
                    await _logRepository.RegistrarAsync("ERROR", "GeminiService",
                        $"Error al deserializar JSON: {ex.Message}", responseText);
                    throw new Exception("El JSON devuelto por Gemini está incompleto o mal formado.", ex);
                }

                await _logRepository.RegistrarAsync("INFO", "GeminiService",
                    "Análisis completado exitosamente",
                    JsonSerializer.Serialize(analisisResponse));

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
        ? $@"
SCORE BASELINE PREVIO: {scoreBaselinePrevio.Value:F2}
Debes calcular la diferencia con este baseline y determinar si hay deterioro."
        : @"
PRIMERA EVALUACIÓN (LÍNEA BASE): No hay datos previos. En comparacion_con_baseline usa:
- diferencia_score: 0.0
- deterioro_detectado: false
- nivel_cambio: ""estable""";

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
4. falsos_objetos: Cantidad de objetos mencionados que NO están en la imagen
5. tiempo_respuesta_seg: Estimado de tiempo de respuesta (usa 30.0 si no conoces)
6. coherencia_linguistica (0-100): Fluidez, gramática y coherencia del discurso
7. score_global (0-100): Promedio ponderado de todos los scores

INTERPRETACIÓN DE DETERIORO:
- diferencia_score entre -5 y 5: estable
- diferencia_score entre 5 y 15 O entre -15 y -5: leve
- diferencia_score entre 15 y 30 O entre -30 y -15: moderado
- diferencia_score mayor a 30 O menor a -30: severo

FORMATO DE RESPUESTA - DEVUELVE EXACTAMENTE ESTE JSON (reemplaza los valores):
{{
  ""score_semantico"": 85.5,
  ""score_objetos"": 90.0,
  ""score_acciones"": 80.0,
  ""falsos_objetos"": 2,
  ""tiempo_respuesta_seg"": 30.0,
  ""coherencia_linguistica"": 88.0,
  ""score_global"": 85.0,
  ""observaciones"": ""Descripción detallada de los hallazgos principales"",
  ""comparacion_con_baseline"": {{
    ""diferencia_score"": 0.0,
    ""deterioro_detectado"": false,
    ""nivel_cambio"": ""estable""
  }}
}}

REGLAS CRÍTICAS:
1. Devuelve ÚNICAMENTE el objeto JSON, sin texto adicional antes o después
2. NO uses comillas simples, solo comillas dobles
3. TODOS los números deben tener al menos un decimal (85.0, no 85)
4. NO dejes campos vacíos o undefined
5. El campo nivel_cambio debe ser EXACTAMENTE uno de estos: ""estable"", ""leve"", ""moderado"", ""severo""
6. NO agregues comentarios dentro del JSON
7. Asegúrate que todas las llaves {{ }} estén balanceadas

Responde AHORA con el JSON:";
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

        private string SanitizarJsonGemini(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "{}";

            texto = texto.Trim();

            // Quitar etiquetas Markdown
            if (texto.StartsWith("```json")) texto = texto[7..];
            if (texto.StartsWith("```")) texto = texto[3..];
            if (texto.EndsWith("```")) texto = texto[..^3];

            // Eliminar saltos, escapes y caracteres rotos
            texto = texto
                .Replace("\\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\\\"", "\"")
                .Trim();

            // Extraer bloque JSON (entre llaves)
            int start = texto.IndexOf('{');
            int end = texto.LastIndexOf('}');
            if (start >= 0 && end > start)
                texto = texto.Substring(start, end - start + 1);

            // Corregir números tipo "8." -> "8.0"
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"(\d+)\.(\s*[,}])", "$1.0$2");

            // Corregir cadenas sin comillas finales
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @":\s*""([^""]*)$", ": \"$1\"");

            // Verificar que termina con llave
            if (!texto.EndsWith("}"))
                texto += "}";

            // Asegurar apertura
            if (!texto.StartsWith("{"))
                texto = "{" + texto;

            return texto;
        }
    }

}
