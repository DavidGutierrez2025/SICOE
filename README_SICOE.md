# SICOE - Documentación del Proyecto

## 📋 Índice de Documentación

Este repositorio contiene la documentación completa del proyecto **SICOE** (Sistema de Integración y Control de Operaciones Electrónicas).

### 📄 Documentos Principales

1. **[RESUMEN_EJECUTIVO_SICOE.md](./RESUMEN_EJECUTIVO_SICOE.md)**
   - Visión general del proyecto
   - Funcionalidades principales
   - Plan de implementación resumido
   - Valor entregado y ROI
   - **Recomendado para**: Stakeholders, Gerentes, Inversores

2. **[PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md](./PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md)** ⭐ **NUEVO**
   - Plan de trabajo con Clean Architecture
   - Estructura de proyecto detallada
   - Principios SOLID aplicados
   - Casos de uso y CQRS
   - Plan de trabajo replanteado (8 fases)
   - **Recomendado para**: Tech Leads, Arquitectos, Desarrolladores

3. **[DECISIONES_APROBADAS_SICOE.md](./DECISIONES_APROBADAS_SICOE.md)** ⭐ **NUEVO - APROBADO**
   - Decisiones aprobadas por el equipo
   - Respuestas de validación
   - Estrategia de evolución
   - **Recomendado para**: Todo el equipo

4. **[ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md](./ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md)** ⭐ **NUEVO**
   - Estrategia para evolucionar a distribuida
   - Bounded contexts
   - Abstracciones necesarias
   - **Recomendado para**: Arquitectos, Tech Leads

5. **[DECISIONES_FINALES_ARQUITECTURA_COLAS.md](./DECISIONES_FINALES_ARQUITECTURA_COLAS.md)**
   - Decisiones consolidadas: Arquitectura y Colas de Mensajes
   - Comparación detallada de opciones
   - Recomendaciones con justificación
   - Preguntas para validación
   - **Recomendado para**: Arquitectos, Tech Leads, Decision Makers, Stakeholders

6. **[ANALISIS_ARQUITECTURA_Y_COLAS_SICOE.md](./ANALISIS_ARQUITECTURA_Y_COLAS_SICOE.md)**
   - Análisis comparativo detallado: Monolítica vs Distribuida
   - Análisis completo de colas de mensajes
   - Arquitectura propuesta
   - **Recomendado para**: Arquitectos, Tech Leads (análisis profundo)

7. **[ANALISIS_Y_PLAN_TRABAJO_SICOE.md](./ANALISIS_Y_PLAN_TRABAJO_SICOE.md)**
   - Análisis detallado de requerimientos (versión inicial)
   - Arquitectura propuesta (versión inicial)
   - Plan de trabajo fase por fase
   - **Nota**: Ver versión actualizada en PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md
   - **Recomendado para**: Referencia histórica

8. **[CONSIDERACIONES_TECNICAS_SICOE.md](./CONSIDERACIONES_TECNICAS_SICOE.md)**
   - Detalles técnicos de integración con SAT
   - Modelo de base de datos
   - Arquitectura Azure detallada
   - Seguridad y encriptación
   - Performance y optimización
   - **Recomendado para**: Desarrolladores, Arquitectos, DevOps

9. **[CONFIGURACION_ALMACENAMIENTO_ARCHIVOS_SICOE.md](./CONFIGURACION_ALMACENAMIENTO_ARCHIVOS_SICOE.md)** ⭐ **NUEVO**
   - Configuración de almacenamiento de archivos
   - Patrón homologado con GEDINET (sistema de archivos + BD)
   - Implementación de ArchivoService
   - Estructura de carpetas
   - **Recomendado para**: Desarrolladores

10. **[ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md](./ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md)** ⭐ **NUEVO**
    - Estructura completa de directorios con Clean Architecture
    - Aplicación de principios SOLID en cada capa
    - Ejemplos de código por capa
    - Configuración de Dependency Injection
    - Checklist SOLID por capa
    - Paquetes NuGet requeridos
    - **Recomendado para**: Desarrolladores, Arquitectos

---

## 🎯 Inicio Rápido

### ⚡ Para Ejecutar el Proyecto
👉 **INICIO RÁPIDO**: **[INICIO_RAPIDO_SICOE.md](./INICIO_RAPIDO_SICOE.md)** ⭐ **NUEVO**  
👉 **INSTRUCCIONES DETALLADAS**: **[INSTRUCCIONES_EJECUCION_VISUAL_STUDIO.md](./INSTRUCCIONES_EJECUCION_VISUAL_STUDIO.md)** ⭐ **NUEVO**

### Para Stakeholders
👉 Comienza con: **[RESUMEN_EJECUTIVO_SICOE.md](./RESUMEN_EJECUTIVO_SICOE.md)**

### Para Arquitectos y Tech Leads
👉 Comienza con: **[DECISIONES_APROBADAS_SICOE.md](./DECISIONES_APROBADAS_SICOE.md)** ⭐ **APROBADO**  
👉 Estrategia de evolución: **[ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md](./ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md)**  
👉 Luego: **[PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md](./PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md)**  
👉 Análisis profundo: **[ANALISIS_ARQUITECTURA_Y_COLAS_SICOE.md](./ANALISIS_ARQUITECTURA_Y_COLAS_SICOE.md)**

### Para Project Managers
👉 Comienza con: **[PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md](./PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md)**

### Para Desarrolladores
👉 Comienza con: **[ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md](./ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md)** ⭐ **NUEVO**  
👉 Luego: **[PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md](./PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md)**  
👉 Detalles técnicos: **[CONSIDERACIONES_TECNICAS_SICOE.md](./CONSIDERACIONES_TECNICAS_SICOE.md)**

---

## 📚 Contexto del Proyecto

### ¿Qué es SICOE?
SICOE es un sistema de **reingeniería completa** del sistema actual GEDINET, enfocado en la **descarga masiva de CFDI** desde el SAT (Servicio de Administración Tributaria) de México, con capacidades de análisis, conciliación y reportes.

### Objetivos Principales
- ✅ Descarga masiva automatizada de CFDI
- ✅ Conciliación automática (solicitado vs. recibido)
- ✅ Análisis fiscal automatizado
- ✅ Reportes especializados
- ✅ Validación contra listas negras
- ✅ Integridad garantizada

### Stack Tecnológico
- **Backend**: .NET Core (C#)
- **Frontend**: Razor Pages
- **Base de Datos**: Azure SQL Database
- **Cloud**: Azure (App Service, SQL Database, Monitor)
- **Almacenamiento**: Sistema de archivos + BD (homologado con GEDINET)
- **Cola de Mensajes**: Hangfire (Fase 1)

### 🔒 Seguridad FIEL
- **Política de Seguridad**: Ver [POLITICA_SEGURIDAD_FIEL_SICOE.md](./POLITICA_SEGURIDAD_FIEL_SICOE.md)
- **La FIEL NUNCA se almacena**: El usuario captura su FIEL en cada sesión
- **La FIEL permanece en el navegador**: Control total del usuario sobre su certificado
- **Solo se almacenan tokens SAT**: Encriptados y con expiración automática

### 🔌 Integración con SAT

#### Servicios Web SOAP
- **Autenticación**: Servicio de autenticación con tokens WRAP
- **Solicitud de Descarga**: Solicitud de descarga masiva de CFDI
- **Verificación**: Verificación del estado de solicitudes
- **Descarga de Paquetes**: Descarga de paquetes específicos con SOAPAction correcto

#### Componentes Principales
- **`SatService`**: Servicio principal que orquesta las operaciones con el SAT
- **`SatSoapMessageBuilder`**: Construcción de mensajes SOAP con firma digital
- **`SatSoapHttpClient`**: Cliente HTTP para comunicación SOAP
- **`SatSoapResponseParser`**: Parser de respuestas XML del SAT
- **`XmlSignatureService`**: Servicio de firma digital XML (WS-Security)
- **`FielCertificateProvider`**: Proveedor de certificados FIEL desde el navegador

#### Especificaciones Técnicas
- **SOAPAction Descarga**: `http://DescargaMasivaTerceros.sat.gob.mx/IDescargaMasivaTercerosService/Descargar`
- **Elemento Raíz**: `PeticionDescargaMasivaTercerosEntrada`
- **Firma Digital**: RSA-SHA1 con C14N Normal (canonicalización)
- **Formato Respuesta**: `RespuestaDescargaMasivaTercerosSalida` con elemento `<Paquete>` (base64)

#### Códigos de Estado SAT
- **5000**: Solicitud de descarga recibida con éxito
- **5004**: No se encontró la información
- **5007**: No existe el paquete solicitado (paquetes tienen 72 horas de vida útil)
- **5008**: Máximo de descargas permitidas (máximo 2 descargas por paquete)
- **300-305**: Errores de autenticación, firma o certificado

#### Restricciones Importantes
- ⚠️ Los paquetes tienen **72 horas de vida útil** desde su creación
- ⚠️ Cada paquete solo puede descargarse **máximo 2 veces**
- ⚠️ Los tokens SAT tienen expiración y deben renovarse periódicamente

---

## 🗂️ Estructura de Documentos

```
SICOE/
├── README_SICOE.md                                    # Este archivo (índice)
├── RESUMEN_EJECUTIVO_SICOE.md                         # Resumen para stakeholders
├── PLAN_TRABAJO_CLEAN_ARCHITECTURE_SICOE.md          # ⭐ Plan con Clean Architecture
├── ANALISIS_ARQUITECTURA_Y_COLAS_SICOE.md            # ⭐ Análisis arquitectura y MQ
├── ANALISIS_Y_PLAN_TRABAJO_SICOE.md                  # Plan inicial (referencia)
├── CONSIDERACIONES_TECNICAS_SICOE.md                # Detalles técnicos
└── context/                                           # Documentos de referencia
    ├── Requerimientos del SISTEMA SICOE.txt
    ├── Aplicando clean Architecture con typescript/   # Referencias Clean Architecture
    ├── Arquitecturas distribuidas/                    # Referencias arquitecturas
    ├── Buenas prácticas/                             # Buenas prácticas
    ├── Buenas prácticas y principios de diseño/     # SOLID y principios
    ├── 0_UR_Ls_WS_Descarga_Masiva_V1_5_VF_33e2cca681.pdf
    ├── 1_WS_Solicitud_Descarga_Masiva_V1_5_VF_89183c42e9.pdf
    └── 2_WS_Verificacion_de_Descarga_Masiva_V1_5_VF_5e53cc2bb5.pdf
```

---

## 🚀 Próximos Pasos

### Decisiones Arquitectónicas ✅ **APROBADAS**
- ✅ **Arquitectura**: Monolito Modular con Clean Architecture
- ✅ **Cola de Mensajes**: Hangfire (Fase 1) → Azure Service Bus (Fase 2, si es necesario)
- ✅ **Principios**: Clean Architecture + SOLID + CQRS
- ✅ **Almacenamiento**: Sistema de archivos + BD (homologado con GEDINET)
- ✅ **Plataforma**: Azure (App Service + SQL Database)
- ✅ **Nota**: No se justifica arquitectura distribuida en este momento. En el futuro se puede revisar.

👉 **Ver decisiones aprobadas**: **[DECISIONES_APROBADAS_SICOE.md](./DECISIONES_APROBADAS_SICOE.md)**  
👉 **Estrategia de evolución**: **[ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md](./ESTRATEGIA_EVOLUCION_DISTRIBUIDA_SICOE.md)**

### Fase 1: Setup y Arquitectura Base (Semanas 1-2)
1. ✅ **Estructura de directorios definida**: Ver [ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md](./ESTRUCTURA_DIRECTORIOS_CLEAN_ARCHITECTURE_SICOE.md)
2. ⏳ Crear estructura de proyectos con Clean Architecture
3. ⏳ Configurar base de datos y Entity Framework
4. ⏳ Configurar Dependency Injection
5. ⏳ Configurar Hangfire para background jobs
6. ⏳ Configurar logging y monitoreo

### Decisiones Pendientes
- [ ] Confirmar ambiente de pruebas del SAT
- [ ] Validar restricciones de seguridad/compliance
- [ ] Confirmar volumen estimado de CFDI
- [ ] Aprobar presupuesto de infraestructura Azure
- ✅ **Decisión**: No se justifica arquitectura distribuida en este momento
- [ ] Definir fechas límite críticas
- [ ] Capacitación del equipo en Clean Architecture

---

## 📞 Contacto y Soporte

Para preguntas sobre este proyecto, consultar:
- **Documentación técnica**: Ver [CONSIDERACIONES_TECNICAS_SICOE.md](./CONSIDERACIONES_TECNICAS_SICOE.md)
- **Plan de trabajo**: Ver [ANALISIS_Y_PLAN_TRABAJO_SICOE.md](./ANALISIS_Y_PLAN_TRABAJO_SICOE.md)
- **Integración SAT**: Ver sección "🔌 Integración con SAT" más arriba

## 🔧 Archivos de Configuración Clave

### Integración SAT
- **`src/SICOE.Infrastructure/Services/Sat/SatService.cs`**: Servicio principal de integración
- **`src/SICOE.Infrastructure/Services/Sat/Soap/SatSoapMessageBuilder.cs`**: Construcción de mensajes SOAP
- **`src/SICOE.Infrastructure/Services/Sat/Soap/SatSoapResponseParser.cs`**: Parser de respuestas
- **`src/SICOE.Application/Interfaces/Services/ISatService.cs`**: Interface del servicio SAT

### Frontend
- **`src/SICOE.Razor/wwwroot/js/descarga-handler.js`**: Manejo de UI para descargas
- **`src/SICOE.Razor/Pages/Index.cshtml`**: Página principal de solicitudes

---

## 📝 Notas de Versión

### Versión 2.2 (2025-01-22) - CORRECCIONES INTEGRACIÓN SAT ⭐ **ACTUALIZACIÓN**
- ✅ **SOAPAction corregido**: Cambiado a `http://DescargaMasivaTerceros.sat.gob.mx/IDescargaMasivaTercerosService/Descargar`
- ✅ **Elemento raíz XML corregido**: Cambiado a `PeticionDescargaMasivaTercerosEntrada`
- ✅ **Parser de respuesta mejorado**: Soporte para `RespuestaDescargaMasivaTercerosSalida` con elemento `<Paquete>`
- ✅ **UI - Columna Estado corregida**: Muestra el estado real (Completada, En Proceso, etc.) en lugar del número de documentos
- ✅ **UI - Botón descarga deshabilitado**: Deshabilitado para estados "Rechazada", "Cancelada" y "Vencida"
- ✅ **Manejo de IdsPaquetes**: Parser robusto que soporta múltiples formatos de respuesta del SAT
- ✅ **Logging mejorado**: Logs detallados para debugging de respuestas del SAT
- ✅ **Códigos de error SAT**: Manejo completo de códigos 5000, 5004, 5007, 5008, etc.

### Versión 2.1 (2025-01-16) - ESTRUCTURA DE DIRECTORIOS
- ✅ **Estructura de directorios completa**: Clean Architecture + SOLID
- ✅ **Ejemplos de código**: Por cada capa con principios SOLID aplicados
- ✅ **Configuración DI**: Program.cs completo con todas las dependencias
- ✅ **Checklist SOLID**: Validación por capa

### Versión 2.0 (2025-01-16) - ACTUALIZACIÓN PRINCIPAL
- ✅ **Clean Architecture aplicada**: Estructura de proyectos replanteada
- ✅ **Análisis arquitectura**: Monolítica vs Distribuida evaluada
- ✅ **Análisis colas de mensajes**: Hangfire vs SQS evaluado
- ✅ **Plan de trabajo actualizado**: 8 fases con Clean Architecture
- ✅ **Recomendaciones finales**: Monolito Modular + Hangfire

### Versión 1.0 (2025-01-16)
- ✅ Análisis inicial de requerimientos completado
- ✅ Plan de trabajo de 8 fases definido
- ✅ Arquitectura técnica propuesta
- ✅ Documentación técnica inicial

---

**Última actualización**: 2025-01-22  
**Estado del Proyecto**: Desarrollo - Integración con SAT en progreso

