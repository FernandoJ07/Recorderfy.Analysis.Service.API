# 🧠 API de Análisis Cognitivo - Recorderfy

Sistema de análisis cognitivo para detección temprana de deterioro mediante evaluación de descripciones de imágenes.

---

## 📚 Documentación Principal

### ⭐ Archivos a Utilizar (Actualizados - Nov 2025)

| Archivo | Propósito | Estado |
|---------|-----------|--------|
| **database_setup_actualizado.sql** | Script completo de base de datos | ✅ USAR ESTE |
| **USO_API_POSTMAN.md** | Guía de uso de la API | ✅ Actualizado |
| **COMANDOS_UTILES.md** | Comandos y consultas SQL | ✅ Actualizado |
| **Recorderfy_Analisis_API.postman_collection.json** | Colección Postman | ✅ Actualizado |
| **RESUMEN_CAMBIOS_SIMPLIFICACION.md** | Changelog técnico | ✅ Actualizado |

### ❌ Archivos Obsoletos (No Usar)

| Archivo | Estado |
|---------|--------|
| database_setup.sql | ❌ Versión antigua |
| migration_evaluacion_completa.sql | ❌ Versión antigua |
| DATOS_MOCK.md | ❌ Ya no se usan mocks |

---

## 🚀 Inicio Rápido

### 1. Configurar Base de Datos
```powershell
# Crear base de datos si no existe
psql -U postgres -h localhost -c "CREATE DATABASE \"PRUEBA\";"

# Ejecutar script de setup
psql -U postgres -h localhost -d PRUEBA -f "database_setup_actualizado.sql"
```

### 2. Iniciar el Servicio
```powershell
cd "Recorderfy.Analysis.Service.API"
dotnet run
```

### 3. Probar con Postman
1. Importar `Recorderfy_Analisis_API.postman_collection.json`
2. Ejecutar "Health Check"
3. Ejecutar "Primera Evaluación"

---

## 📋 Flujo de Uso

### Primera Evaluación (Línea Base) ✅
```http
POST /api/analisis/analizar
Content-Type: application/json

{
  "pacienteId": "550e8400-e29b-41d4-a716-446655440000",
  "imagenId": "770e8400-e29b-41d4-a716-446655440002",
  "descripcionPaciente": "Veo una persona con un perro",
  "descripcionReal": "Una persona sentada en un banco con un perro marrón"
}
```

**Resultado:**
- ✅ Sistema detecta: Primera evaluación
- ✅ Crea línea base automáticamente
- ✅ Retorna `esLineaBase: true`

### Evaluaciones de Seguimiento ✅
```http
POST /api/analisis/analizar
(mismo formato)
```

**Resultado:**
- ✅ Sistema detecta: Análisis previo existe
- ✅ Compara con línea base
- ✅ Retorna `esLineaBase: false`
- ✅ Indica si hay deterioro

---

## 🔑 Campos del Request

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `pacienteId` | GUID | ✅ | ID único del paciente |
| `imagenId` | GUID | ✅ | ID único de la imagen |
| `descripcionPaciente` | string | ✅ | Lo que describe el paciente (máx 2000 chars) |
| `descripcionReal` | string | ✅ | Descripción correcta de la imagen (máx 2000 chars) |

### ⚠️ Campos NO Requeridos (Automáticos)
- ❌ `esLineaBase` - El sistema lo detecta automáticamente
- ❌ `tiempoRespuestaSeg` - Se calcula desde Gemini

---

## 🎯 Características Principales

### Detección Automática de Línea Base
- ✅ **Primera evaluación del paciente** → Se marca como línea base
- ✅ **Evaluaciones posteriores** → Se comparan con la línea base
- ✅ **Una línea base activa por paciente** → Se desactivan automáticamente las anteriores

### Niveles de Deterioro
- **estable**: Sin cambios significativos
- **leve**: Deterioro leve (diferencia 10-20 puntos)
- **moderado**: Deterioro moderado (diferencia 20-30 puntos)
- **severo**: Deterioro severo (diferencia > 30 puntos)

### Logs Automáticos
Todos los eventos se registran en la tabla `log_sistema`:
- ✅ Requests recibidos
- ✅ Análisis completados
- ✅ Líneas base creadas
- ✅ Errores y advertencias
- ✅ Llamadas a Gemini API

---

## 🗄️ Estructura de Base de Datos

### Tablas Principales

#### `linea_base`
Almacena las líneas base de cada paciente (una activa por paciente).

#### `analisis_cognitivo`
Todos los análisis individuales realizados. Primera evaluación tiene `es_linea_base = TRUE`.

#### `log_sistema`
Logs del sistema para auditoría y debugging.

#### `evaluacion_completa`
Evaluaciones con múltiples preguntas/imágenes en una sesión.

### Vistas Útiles
- `v_pacientes_con_deterioro` - Resumen de pacientes con deterioro
- `v_historial_paciente` - Historial completo por paciente
- `v_estadisticas_sistema` - Estadísticas generales
- `v_resumen_evaluaciones_paciente` - Resumen de evaluaciones completas

---

## 🔧 Configuración

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=PRUEBA;Username=postgres;Password=admin;"
  },
  "Gemini": {
    "ApiKey": "YOUR_API_KEY",
    "ApiUrl": "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"
  },
  "Urls": "http://localhost:5100;https://localhost:5101"
}
```

---

## 📖 Endpoints Disponibles

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/analisis/health` | Health check del servicio |
| POST | `/api/analisis/analizar` | Realizar análisis cognitivo |
| GET | `/api/analisis/{id}` | Obtener análisis por ID |
| GET | `/api/analisis/paciente/{pacienteId}` | Historial del paciente |
| GET | `/api/analisis/linea-base/{pacienteId}` | Línea base del paciente |
| GET | `/api/analisis/deterioro` | Análisis con deterioro |

---

## 🧪 Testing

### Con Postman
```
1. Importar colección
2. Configurar base_url = http://localhost:5100
3. Ejecutar requests en orden
```

### Con cURL
```powershell
# Health Check
curl http://localhost:5100/api/analisis/health

# Primera Evaluación
curl -X POST http://localhost:5100/api/analisis/analizar `
  -H "Content-Type: application/json" `
  -d '{"pacienteId":"550e8400-e29b-41d4-a716-446655440000","imagenId":"770e8400-e29b-41d4-a716-446655440002","descripcionPaciente":"Test","descripcionReal":"Test real"}'
```

---

## 🐛 Troubleshooting

### Error común: Puerto ocupado
```powershell
netstat -ano | findstr :5100
taskkill /PID <PID> /F
```

### Error común: Base de datos no existe
```powershell
psql -U postgres -h localhost -c "CREATE DATABASE \"PRUEBA\";"
```

### Error común: Tablas no existen
```powershell
psql -U postgres -h localhost -d PRUEBA -f "database_setup_actualizado.sql"
```

---

## 📊 Consultas SQL Útiles

### Ver análisis recientes
```sql
SELECT * FROM v_historial_paciente LIMIT 10;
```

### Ver líneas base activas
```sql
SELECT * FROM linea_base WHERE activa = TRUE;
```

### Ver logs de errores
```sql
SELECT * FROM log_sistema WHERE nivel = 'ERROR' ORDER BY fecha_registro DESC LIMIT 20;
```

---

## 🔗 Referencias

Para más información detallada, consulta:

1. **USO_API_POSTMAN.md** - Guía completa con ejemplos
2. **COMANDOS_UTILES.md** - Comandos y consultas SQL
3. **RESUMEN_CAMBIOS_SIMPLIFICACION.md** - Detalles técnicos de cambios

---

## 📝 Versión

**Versión:** 2.0  
**Fecha:** Noviembre 2025  
**Cambios principales:**
- ✅ Detección automática de línea base
- ✅ Eliminación de datos mock
- ✅ Logs mejorados
- ✅ API simplificada (4 campos en request)
- ✅ Base de datos actualizada
