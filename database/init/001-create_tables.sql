-- ============================================
-- Base mínima para el Microservicio Cognitivo
-- Versión simplificada - Noviembre 2025
-- ============================================

-- Habilitar extensión UUID (solo una vez)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================
-- Tabla: linea_base
-- ============================================
CREATE TABLE IF NOT EXISTS linea_base (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    paciente_id UUID NOT NULL,
    score_global_inicial REAL NOT NULL CHECK (score_global_inicial >= 0 AND score_global_inicial <= 100),
    fecha_establecimiento TIMESTAMP NOT NULL DEFAULT NOW(),
    cantidad_evaluaciones INTEGER NOT NULL DEFAULT 0 CHECK (cantidad_evaluaciones >= 0),
    ultima_evaluacion TIMESTAMP,
    activa BOOLEAN NOT NULL DEFAULT TRUE,
    notas VARCHAR(1000)
);

CREATE INDEX IF NOT EXISTS idx_linea_base_paciente ON linea_base(paciente_id);
CREATE INDEX IF NOT EXISTS idx_linea_base_activa ON linea_base(paciente_id, activa);

-- ============================================
-- Tabla: analisis_cognitivo
-- ============================================
CREATE TABLE IF NOT EXISTS analisis_cognitivo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    paciente_id UUID NOT NULL,
    imagen_id UUID NOT NULL,
    descripcion_paciente VARCHAR(2000) NOT NULL,
    descripcion_real VARCHAR(2000) NOT NULL,
    metadata_imagen JSONB,
    
    score_semantico REAL NOT NULL CHECK (score_semantico BETWEEN 0 AND 100),
    score_objetos REAL NOT NULL CHECK (score_objetos BETWEEN 0 AND 100),
    score_acciones REAL NOT NULL CHECK (score_acciones BETWEEN 0 AND 100),
    falsos_objetos INTEGER NOT NULL CHECK (falsos_objetos >= 0),
    tiempo_respuesta_seg REAL NOT NULL CHECK (tiempo_respuesta_seg >= 0),
    coherencia_linguistica REAL NOT NULL CHECK (coherencia_linguistica BETWEEN 0 AND 100),
    score_global REAL NOT NULL CHECK (score_global BETWEEN 0 AND 100),
    
    observaciones VARCHAR(5000),
    diferencia_score REAL,
    deterioro_detectado BOOLEAN,
    nivel_cambio VARCHAR(50) CHECK (nivel_cambio IN ('estable', 'leve', 'moderado', 'severo')),
    
    es_linea_base BOOLEAN NOT NULL DEFAULT FALSE,
    linea_base_id UUID REFERENCES linea_base(id) ON DELETE SET NULL,
    
    fecha_analisis TIMESTAMP NOT NULL DEFAULT NOW(),
    respuesta_llm_completa JSONB
);

CREATE INDEX IF NOT EXISTS idx_analisis_paciente ON analisis_cognitivo(paciente_id);
CREATE INDEX IF NOT EXISTS idx_analisis_fecha ON analisis_cognitivo(fecha_analisis DESC);

-- ============================================
-- Tabla: log_sistema
-- ============================================
CREATE TABLE IF NOT EXISTS log_sistema (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nivel VARCHAR(20) NOT NULL CHECK (nivel IN ('INFO', 'WARNING', 'ERROR', 'DEBUG', 'CRITICAL')),
    componente VARCHAR(200) NOT NULL,
    mensaje VARCHAR(5000) NOT NULL,
    excepcion TEXT,
    datos_adicionales JSONB,
    fecha_registro TIMESTAMP NOT NULL DEFAULT NOW(),
    usuario_id UUID,
    endpoint VARCHAR(500)
);

CREATE INDEX IF NOT EXISTS idx_log_fecha ON log_sistema(fecha_registro DESC);
CREATE INDEX IF NOT EXISTS idx_log_nivel ON log_sistema(nivel);

-- ============================================
-- Tabla: evaluacion_completa
-- ============================================
CREATE TABLE IF NOT EXISTS evaluacion_completa (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    paciente_id UUID NOT NULL,
    cuidador_id UUID NOT NULL,
    fecha_evaluacion TIMESTAMP NOT NULL DEFAULT NOW(),
    total_preguntas INTEGER NOT NULL CHECK (total_preguntas > 0),
    preguntas_procesadas INTEGER NOT NULL DEFAULT 0 CHECK (preguntas_procesadas >= 0),
    
    score_global_promedio REAL NOT NULL CHECK (score_global_promedio BETWEEN 0 AND 100),
    score_semantico_promedio REAL NOT NULL CHECK (score_semantico_promedio BETWEEN 0 AND 100),
    score_objetos_promedio REAL NOT NULL CHECK (score_objetos_promedio BETWEEN 0 AND 100),
    score_acciones_promedio REAL NOT NULL CHECK (score_acciones_promedio BETWEEN 0 AND 100),
    coherencia_promedio REAL NOT NULL CHECK (coherencia_promedio BETWEEN 0 AND 100),
    tiempo_respuesta_promedio REAL,
    
    deterioro_detectado BOOLEAN DEFAULT FALSE,
    nivel_deterioro_general VARCHAR(50),
    diferencia_con_linea_base REAL,
    
    observaciones_generales TEXT,
    recomendaciones_medicas TEXT,
    
    es_linea_base BOOLEAN NOT NULL DEFAULT FALSE,
    linea_base_id UUID REFERENCES linea_base(id) ON DELETE SET NULL,
    
    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_evaluacion_paciente ON evaluacion_completa(paciente_id);
CREATE INDEX IF NOT EXISTS idx_evaluacion_fecha ON evaluacion_completa(fecha_evaluacion DESC);

-- ============================================
-- Confirmación
-- ============================================
SELECT 'Esquema mínimo creado correctamente' AS status, NOW() AS fecha;
