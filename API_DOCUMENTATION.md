# ?? API de Análisis Cognitivo - Documentación

## ?? Descripción General
API REST para el análisis cognitivo de pacientes mediante la descripción de imágenes, utilizada para la detección temprana de deterioro cognitivo y Alzheimer.

**Base URL:** `http://localhost:5100` o `https://localhost:5101`

**Tecnologías:**
- .NET 8
- PostgreSQL
- Google Gemini AI
- Entity Framework Core

---

## ?? Endpoints Disponibles

### 1. ?? Analizar Descripción de Imagen (Principal)

Realiza un análisis cognitivo basado en la descripción que un paciente hace de una imagen.

**Endpoint:** `POST /api/analisis/analizar`

#### Request Body:
```json
{
  "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "imagenId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "descripcionPaciente": "Veo una mujer en la cocina preparando comida...",
  "descripcionReal": "Una mujer de mediana edad en una cocina moderna preparando el desayuno. Hay una cafetera, tostadora y varios utensilios en la encimera."
}
```

#### Campos del Request:
| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `pacienteId` | GUID | ? | Identificador único del paciente |
| `imagenId` | GUID | ? | Identificador único de la imagen |
| `descripcionPaciente` | string | ? | Descripción de la imagen dada por el paciente (máx. 2000 caracteres) |
| `descripcionReal` | string | ? | Descripción real/correcta de la imagen (máx. 2000 caracteres) |

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": {
    "analisisId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imagenId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "scoreSemantico": 85.5,
    "scoreObjetos": 90.0,
    "scoreAcciones": 80.0,
    "falsosObjetos": 2,
    "tiempoRespuestaSeg": 15.5,
    "coherenciaLinguistica": 88.0,
    "scoreGlobal": 86.3,
    "observaciones": "El paciente mostró buena capacidad de descripción general...",
    "comparacionConBaseline": {
      "diferenciaScore": -3.2,
      "deterioroDetectado": false,
      "nivelCambio": "estable"
    },
    "esLineaBase": true,
    "fechaAnalisis": "2024-01-15T10:30:00Z",
    "mensaje": "Línea base establecida correctamente - Primera evaluación del paciente"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### Campos del Response:
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `analisisId` | GUID | ID único del análisis generado |
| `scoreSemantico` | float | Score de precisión semántica (0-100) |
| `scoreObjetos` | float | Score de identificación de objetos (0-100) |
| `scoreAcciones` | float | Score de descripción de acciones (0-100) |
| `falsosObjetos` | int | Cantidad de objetos mencionados incorrectamente |
| `tiempoRespuestaSeg` | float | Tiempo de respuesta en segundos |
| `coherenciaLinguistica` | float | Score de coherencia del discurso (0-100) |
| `scoreGlobal` | float | Score global del análisis (0-100) |
| `observaciones` | string | Observaciones detalladas del análisis |
| `comparacionConBaseline` | object | Comparación con línea base del paciente |
| `esLineaBase` | boolean | Indica si este análisis es la línea base |
| `fechaAnalisis` | datetime | Fecha y hora del análisis |
| `mensaje` | string | Mensaje descriptivo del resultado |

#### Niveles de Cambio (comparacionConBaseline.nivelCambio):
- `"estable"` - Sin deterioro significativo
- `"leve"` - Deterioro cognitivo leve
- `"moderado"` - Deterioro cognitivo moderado  
- `"severo"` - Deterioro cognitivo severo

---

### 2. ?? Evaluación Completa (Múltiples Preguntas)

Procesa una evaluación completa con múltiples preguntas/imágenes en una sola sesión.

**Endpoint:** `POST /api/analisis/evaluacion-completa`

#### Request Body:
```json
{
  "idPaciente": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "idCuidador": "8b2d6a91-3456-4321-a1b2-9d8e7f6a5b4c",
  "fechaRealizacion": "2024-01-15T10:00:00Z",
  "puntaje": 0,
  "preguntas": [
    {
      "idPicture": "pic-001",
      "imagenUrl": "https://example.com/images/cocina.jpg",
      "descripcionReal": "Una mujer en la cocina preparando el desayuno",
      "pacienteRespuesta": "Veo una señora cocinando algo"
    },
    {
      "idPicture": "pic-002",
      "imagenUrl": "https://example.com/images/parque.jpg",
      "descripcionReal": "Niños jugando en un parque con columpios",
      "pacienteRespuesta": "Hay niños afuera jugando"
    }
  ]
}
```

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": {
    "evaluacionId": "9d8e7f6a-5b4c-3d2e-1f0a-8c7b6a5d4e3f",
    "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "cuidadorId": "8b2d6a91-3456-4321-a1b2-9d8e7f6a5b4c",
    "fechaEvaluacion": "2024-01-15T10:00:00Z",
    "totalPreguntas": 2,
    "preguntasProcesadas": 2,
    "scoreGlobalPromedio": 83.5,
    "scoreSemanticoPromedio": 85.0,
    "scoreObjetosPromedio": 87.5,
    "scoreAccionesPromedio": 80.0,
    "coherenciaPromedio": 82.0,
    "tiempoRespuestaPromedio": 30.0,
    "deterioroDetectado": false,
    "nivelDeterioroGeneral": "estable",
    "diferenciaConLineaBase": null,
    "resultadosIndividuales": [
      {
        "idPicture": "pic-001",
        "analisisId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "imagenUrl": "https://example.com/images/cocina.jpg",
        "descripcionReal": "Una mujer en la cocina preparando el desayuno",
        "pacienteRespuesta": "Veo una señora cocinando algo",
        "scoreGlobal": 85.0,
        "scoreSemantico": 87.0,
        "scoreObjetos": 88.0,
        "scoreAcciones": 82.0,
        "falsosObjetos": 1,
        "coherenciaLinguistica": 85.0,
        "observaciones": "Buena identificación de la escena principal...",
        "nivelCambio": "estable",
        "deterioroDetectado": false
      },
      {
        "idPicture": "pic-002",
        "analisisId": "8d0f7a89-8536-51ef-c45d-3e18f2g01bf8",
        "imagenUrl": "https://example.com/images/parque.jpg",
        "descripcionReal": "Niños jugando en un parque con columpios",
        "pacienteRespuesta": "Hay niños afuera jugando",
        "scoreGlobal": 82.0,
        "scoreSemantico": 83.0,
        "scoreObjetos": 87.0,
        "scoreAcciones": 78.0,
        "falsosObjetos": 0,
        "coherenciaLinguistica": 79.0,
        "observaciones": "Descripción básica pero correcta...",
        "nivelCambio": "estable",
        "deterioroDetectado": false
      }
    ],
    "observacionesGenerales": "Evaluación completada con 2 preguntas.\nScore global promedio: 83.50/100\nMejor desempeño: Pregunta pic-001 (85.00)\nMayor dificultad: Pregunta pic-002 (82.00)\n",
    "recomendacionesMedicas": "? Función cognitiva estable.\n• Continuar con evaluaciones periódicas de seguimiento.\n• Mantener actividades de estimulación cognitiva.\n",
    "esLineaBase": true,
    "lineaBaseId": "4f3e2d1c-0b9a-8765-4321-1a2b3c4d5e6f",
    "fechaProcesamiento": "2024-01-15T10:35:00Z"
  },
  "message": "Evaluación completada exitosamente. Score promedio: 83.50",
  "timestamp": "2024-01-15T10:35:00Z"
}
```

---

### 3. ?? Obtener Historial de un Paciente

Obtiene todos los análisis realizados a un paciente específico.

**Endpoint:** `GET /api/analisis/paciente/{pacienteId}`

#### Parámetros:
| Parámetro | Tipo | Ubicación | Descripción |
|-----------|------|-----------|-------------|
| `pacienteId` | GUID | URL | ID del paciente |

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "imagenId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "scoreGlobal": 86.3,
      "deterioroDetectado": false,
      "esLineaBase": true,
      "fechaAnalisis": "2024-01-15T10:30:00Z"
    }
  ],
  "count": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 4. ?? Obtener Análisis por ID

Obtiene los detalles completos de un análisis específico.

**Endpoint:** `GET /api/analisis/{id}`

#### Parámetros:
| Parámetro | Tipo | Ubicación | Descripción |
|-----------|------|-----------|-------------|
| `id` | GUID | URL | ID del análisis |

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imagenId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "descripcionPaciente": "Veo una mujer en la cocina...",
    "descripcionReal": "Una mujer de mediana edad en una cocina...",
    "scoreSemantico": 85.5,
    "scoreObjetos": 90.0,
    "scoreAcciones": 80.0,
    "falsosObjetos": 2,
    "tiempoRespuestaSeg": 15.5,
    "coherenciaLinguistica": 88.0,
    "scoreGlobal": 86.3,
    "observaciones": "El paciente mostró buena capacidad...",
    "diferenciaScore": null,
    "deterioroDetectado": false,
    "nivelCambio": "estable",
    "esLineaBase": true,
    "lineaBaseId": null,
    "fechaAnalisis": "2024-01-15T10:30:00Z"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 5. ?? Obtener Análisis con Deterioro

Obtiene todos los análisis donde se detectó deterioro cognitivo.

**Endpoint:** `GET /api/analisis/deterioro`

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "8d0f7a89-8536-51ef-c45d-3e18f2g01bf8",
      "pacienteId": "5gb96f75-6828-5673-c4gd-3d074g77bgb7",
      "scoreGlobal": 65.0,
      "deterioroDetectado": true,
      "nivelCambio": "leve",
      "fechaAnalisis": "2024-01-14T09:15:00Z"
    }
  ],
  "count": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 6. ?? Obtener Línea Base de un Paciente

Obtiene la línea base activa (primera evaluación) de un paciente.

**Endpoint:** `GET /api/analisis/linea-base/{pacienteId}`

#### Parámetros:
| Parámetro | Tipo | Ubicación | Descripción |
|-----------|------|-----------|-------------|
| `pacienteId` | GUID | URL | ID del paciente |

#### Response Exitoso (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "4f3e2d1c-0b9a-8765-4321-1a2b3c4d5e6f",
    "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "scoreGlobalInicial": 86.3,
    "fechaEstablecimiento": "2024-01-15T10:30:00Z",
    "cantidadEvaluaciones": 1,
    "ultimaEvaluacion": "2024-01-15T10:30:00Z",
    "activa": true,
    "notas": "Línea base establecida automáticamente - Primera evaluación del paciente"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### Response cuando no existe (404 Not Found):
```json
{
  "success": false,
  "message": "No se encontró línea base activa para el paciente 3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 7. ?? Health Check

Verifica el estado del servicio.

**Endpoint:** `GET /api/analisis/health`

#### Response Exitoso (200 OK):
```json
{
  "status": "healthy",
  "service": "Microservicio de Análisis Cognitivo",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 8. ?? Obtener Evaluación Completa por ID

Obtiene los detalles de una evaluación completa específica.

**Endpoint:** `GET /api/analisis/evaluacion-completa/{evaluacionId}`

**Estado:** ?? En desarrollo (501 Not Implemented)

---

### 9. ?? Obtener Evaluaciones de un Paciente

Obtiene todas las evaluaciones completas de un paciente.

**Endpoint:** `GET /api/analisis/evaluacion-completa/paciente/{pacienteId}`

**Estado:** ?? En desarrollo (501 Not Implemented)

---

## ? Respuestas de Error

### Error de Validación (400 Bad Request):
```json
{
  "success": false,
  "message": "Datos de entrada inválidos",
  "errors": [
    "El ID del paciente es obligatorio",
    "La descripción del paciente es obligatoria"
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error del Servidor (500 Internal Server Error):
```json
{
  "success": false,
  "message": "Error al procesar el análisis cognitivo",
  "error": "PostgresException: la autentificación password falló para el usuario postgres",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## ?? Configuración

### Variables de Configuración (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=analisis_cognitivo_db;Username=postgres;Password=admin;Include Error Detail=true"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "ApiUrl": "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"
  },
  "Urls": "http://localhost:5100;https://localhost:5101"
}
```

---

## ?? Interpretación de Scores

### Score Global (0-100):
- **90-100**: Excelente - Capacidad cognitiva óptima
- **80-89**: Bueno - Función cognitiva normal
- **70-79**: Regular - Posible deterioro leve
- **50-69**: Bajo - Deterioro moderado
- **0-49**: Muy bajo - Deterioro severo

### Niveles de Deterioro:
| Nivel | Descripción | Acción Recomendada |
|-------|-------------|-------------------|
| `estable` | Sin cambios significativos | Seguimiento periódico |
| `leve` | Deterioro cognitivo leve | Consulta con neurólogo |
| `moderado` | Deterioro cognitivo moderado | Derivación urgente a especialista |
| `severo` | Deterioro cognitivo severo | Consulta neurológica inmediata |

---

## ?? Ejemplos de Uso

### cURL - Analizar Descripción:
```bash
curl -X POST "https://localhost:5101/api/analisis/analizar" \
  -H "Content-Type: application/json" \
  -d '{
    "pacienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imagenId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "descripcionPaciente": "Veo una mujer en la cocina preparando comida",
    "descripcionReal": "Una mujer de mediana edad en una cocina moderna preparando el desayuno"
  }'
```

### JavaScript/Fetch:
```javascript
const response = await fetch('https://localhost:5101/api/analisis/analizar', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    pacienteId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    imagenId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    descripcionPaciente: 'Veo una mujer en la cocina preparando comida',
    descripcionReal: 'Una mujer de mediana edad en una cocina moderna preparando el desayuno'
  })
});

const result = await response.json();
console.log(result);
```

### C# HttpClient:
```csharp
var client = new HttpClient();
var request = new AnalisisRequest
{
    PacienteId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    ImagenId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    DescripcionPaciente = "Veo una mujer en la cocina preparando comida",
    DescripcionReal = "Una mujer de mediana edad en una cocina moderna preparando el desayuno"
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("https://localhost:5101/api/analisis/analizar", content);
var result = await response.Content.ReadAsStringAsync();
```

---

## ?? Notas Importantes

1. **Línea Base**: La primera evaluación de un paciente se establece automáticamente como línea base para futuras comparaciones.

2. **IA Gemini**: Todos los análisis son procesados por Google Gemini AI para evaluación cognitiva avanzada.

3. **Logging**: Todos los requests y errores son registrados en la tabla `log_sistema` de PostgreSQL.

4. **GUID Format**: Todos los IDs deben estar en formato GUID válido (ej: `3fa85f64-5717-4562-b3fc-2c963f66afa6`).

5. **Timestamps**: Todas las fechas están en formato UTC (ISO 8601).

---

## ?? Soporte

Para problemas o preguntas, contactar al equipo de desarrollo o revisar los logs del sistema en la base de datos.

**Última actualización:** Enero 2024
