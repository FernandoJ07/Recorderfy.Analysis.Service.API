# Pruebas Unitarias - Recorderfy Analysis Service

## Descripción General

Este proyecto contiene las **pruebas unitarias** para el microservicio **Recorderfy.Analysis.Service.API**, que es responsable del análisis cognitivo de pacientes mediante procesamiento de imágenes y descripciones utilizando IA (Gemini API).

## Objetivo

Asegurar la calidad y correctitud del código mediante pruebas unitarias que validan:
- La lógica de negocio del servicio de análisis cognitivo
- Los endpoints del controlador de análisis
- Las operaciones de repositorio con la base de datos

## Frameworks Utilizados

- **xUnit 2.6.2**: Framework de pruebas unitarias para .NET
- **Moq 4.20.70**: Biblioteca para crear objetos mock de interfaces y clases
- **EntityFrameworkCore.InMemory 8.0.0**: Proveedor de base de datos en memoria para pruebas de repositorio
- **.NET 8.0**: Framework target del proyecto

## Estructura del Proyecto

```
Recorderfy.Analysis.Service.Test/
├── Controllers/
│   └── AnalisisControllerTests.cs      # 9 pruebas del controlador API
├── Services/
│   └── AnalisisServiceTests.cs          # 6 pruebas de lógica de negocio
├── Repositories/
│   └── AnalisisRepositoryTests.cs       # 11 pruebas de acceso a datos
├── Helpers/
│   └── TestAppDbContext.cs              # DbContext personalizado para pruebas
└── Recorderfy.Analysis.Service.Test.csproj
```

## Pruebas Implementadas

### **Total: 26 Pruebas**

#### **AnalisisServiceTests (6 pruebas)**

Valida la lógica de negocio del servicio de análisis cognitivo:

1. `RealizarAnalisisAsync_PrimeraEvaluacion_DebeCrearLineaBase`
   - Verifica que la primera evaluación de un paciente cree una línea base

2. `RealizarAnalisisAsync_ConLineaBaseExistente_DebeCompararConBaseline`
   - Valida que evaluaciones posteriores se comparen con la línea base

3. `RealizarAnalisisAsync_DebeGuardarAnalisisEnRepositorio`
   - Asegura que el análisis se guarde correctamente en la base de datos

4. `RealizarAnalisisMultipleAsync_DebeCrearLineaBaseConPromedio`
   - Verifica creación de línea base con múltiples cuestionarios

5. `ProcesarEvaluacionCompletaAsync_DebeCrearEvaluacionYAnalisis`
   - Valida el procesamiento de una evaluación completa

6. `ObtenerHistorialPacienteAsync_DebeRetornarHistorialOrdenado`
   - Verifica recuperación del historial de análisis del paciente

#### **AnalisisControllerTests (9 pruebas)**

Valida los endpoints HTTP del controlador:

1. `AnalizarDescripcion_ConDatosValidos_DebeRetornarAnalisis`
   - Verifica análisis de descripción con datos válidos

2. `AnalizarDescripcion_ConLineaBase_DebeIncluirComparacion`
   - Valida que incluya comparación con línea base

3. `ObtenerHistorialPaciente_DebeRetornarListaDeAnalisis`
   - Verifica recuperación del historial del paciente

4. `ObtenerHistorialPaciente_PacienteSinAnalisis_DebeRetornarListaVacia`
   - Valida comportamiento con paciente sin análisis

5. `ObtenerAnalisis_ConIdValido_DebeRetornarAnalisis`
   - Verifica recuperación de análisis por ID

6. `ObtenerAnalisis_ConIdInvalido_DebeRetornarNotFound`
   - Valida manejo de ID inexistente

7. `ObtenerLineaBase_ConLineaBaseExistente_DebeRetornarLineaBase`
   - Verifica recuperación de línea base activa

8. `ObtenerLineaBase_SinLineaBase_DebeRetornarNotFound`
   - Valida comportamiento sin línea base

9. `ProcesarEvaluacionCompleta_DebeCrearEvaluacion`
   - Verifica procesamiento de evaluación completa

#### **AnalisisRepositoryTests (11 pruebas)**

Valida las operaciones de acceso a datos:

1. `CrearAnalisisAsync_DebeAgregarAnalisisABaseDeDatos`
   - Verifica inserción de análisis

2. `ObtenerPorIdAsync_ConIdValido_DebeRetornarAnalisis`
   - Valida recuperación por ID

3. `ObtenerPorIdAsync_ConIdInvalido_DebeRetornarNull`
   - Verifica comportamiento con ID inexistente

4. `ObtenerPorPacienteAsync_DebeRetornarAnalisisDelPaciente`
   - Valida filtrado por paciente

5. `CrearLineaBaseAsync_DebeDesactivarLineasBaseAnteriores`
   - Verifica desactivación de líneas base anteriores

6.  `ObtenerLineaBaseActivaAsync_DebeRetornarLineaBaseActiva`
   - Valida recuperación de línea base activa

7.  `ObtenerLineaBaseActivaAsync_SinLineaBase_DebeRetornarNull`
   - Verifica comportamiento sin línea base

8.  `ActualizarLineaBaseAsync_DebeModificarLineaBase`
   - Valida actualización de línea base

9.  `ObtenerAnalisisConDeterioroAsync_DebeRetornarSoloConDeterioro`
   - Verifica filtrado de análisis con deterioro

10.  `CrearEvaluacionCompletaAsync_DebeAgregarEvaluacion`
    - Valida inserción de evaluación completa

11.  `ObtenerEvaluacionesPorPacienteAsync_DebeRetornarEvaluacionesDelPaciente`
    - Verifica recuperación de evaluaciones por paciente

## Ejecución de las Pruebas

### Ejecutar todas las pruebas:

```powershell
cd Recorderfy.Analysis.Service.API
dotnet test Recorderfy.Analysis.Service.Test/Recorderfy.Analysis.Service.Test.csproj
```

### Ejecutar con salida detallada:

```powershell
dotnet test Recorderfy.Analysis.Service.Test/Recorderfy.Analysis.Service.Test.csproj --verbosity normal
```

### Ejecutar una prueba específica:

```powershell
dotnet test --filter "FullyQualifiedName~AnalisisServiceTests.RealizarAnalisisAsync_PrimeraEvaluacion_DebeCrearLineaBase"
```

## Resultados de las Pruebas

```
Resumen de pruebas: 
  Total: 25 
  Exitosas: 25 
  Fallidas: 0 
  Omitidas: 0 
  Duración: ~5.8s
```

## Configuración Especial

### TestAppDbContext

Para resolver problemas de compatibilidad con **EntityFrameworkCore InMemory**, se creó un `TestAppDbContext` personalizado que configura las propiedades string requeridas como opcionales:

```csharp
public class TestAppDbContext : ApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar propiedades opcionales para InMemory database
        modelBuilder.Entity<AnalisisCognitivo>()
            .Property(a => a.MetadataImagen)
            .IsRequired(false);
        // ... más configuraciones
    }
}
```

Esto permite que las pruebas de repositorio funcionen correctamente sin necesidad de inicializar todas las propiedades string en cada test.

## Patrón de Prueba AAA

Todas las pruebas siguen el patrón **AAA (Arrange-Act-Assert)**:

```csharp
[Fact]
public async Task RealizarAnalisisAsync_PrimeraEvaluacion_DebeCrearLineaBase()
{
    // Arrange - Preparar datos y mocks
    var request = new AnalisisCognitivoRequest { ... };
    _mockRepository.Setup(...);

    // Act - Ejecutar el método a probar
    var resultado = await _service.RealizarAnalisisAsync(request);

    // Assert - Verificar resultados
    Assert.NotNull(resultado);
    _mockRepository.Verify(...);
}
```

## 🔍 Uso de Mocks

### Servicios Mockeados:

- **IAnalisisRepository**: Simula operaciones de base de datos
- **IGeminiService**: Simula llamadas a la API de Gemini
- **ILogRepository**: Simula registro de logs

### Ejemplo de Mock con Moq:

```csharp
_mockGemini.Setup(g => g.AnalizarDescripcionAsync(
    It.IsAny<string>(), 
    It.IsAny<string>(), 
    It.IsAny<string>(), 
    It.IsAny<int>(), 
    It.IsAny<string>()))
.ReturnsAsync(new GeminiAnalisisResponse { ... });
```

## Entidades Probadas

- **AnalisisCognitivo**: Análisis individual de un paciente
- **LineaBase**: Línea base cognitiva del paciente
- **EvaluacionCompleta**: Sesión de múltiples cuestionarios

## Cobertura de Código

Las pruebas cubren:
-  Creación de análisis cognitivos
-  Comparación con línea base
-  Gestión de líneas base
-  Procesamiento de evaluaciones completas
-  Detección de deterioro cognitivo
-  Manejo de errores y casos límite
-  Validación de datos

## Autores

Desarrollado para el proyecto **Recorderfy** - Sistema de análisis cognitivo para pacientes con deterioro cognitivo.

## Última Actualización

Fecha: 2025

---

## Estado del Proyecto

 **Todas las pruebas pasando** - El servicio de análisis está completamente probado y validado.
