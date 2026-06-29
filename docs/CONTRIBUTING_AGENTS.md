# Guías de Contribución para Agentes de IA - ValiantXP

Este documento define la metodología obligatoria de documentación y desarrollo que deben seguir todos los agentes (Scrum Master, Architect, Developer, Tester, DevOps, Cybersecurity) que contribuyen al repositorio ValiantXP.

---

## 📋 1. Flujo de Trabajo por Tarea

Por cada tarea asignada en 	ask.md, el agente ejecutor debe:
1.  **Analizar**: Leer el rchitecture_spec.md y los requerimientos del módulo en el backlog.
2.  **Codificar**: Escribir código siguiendo los principios SOLID, Clean Architecture y patrones definidos.
3.  **Probar**: Escribir/actualizar unit tests y asegurar que dotnet build y dotnet test pasen limpios.
4.  **Documentar**:
    *   Si introduces nuevos endpoints, servicios o entidades, documenta el cambio en un archivo markdown dentro de docs/.
    *   Actualizar **CHANGELOG.md** agregando los cambios en la sección ### Added, ### Changed o ### Fixed bajo la versión actual.
5.  **Git Commit**: Realizar un commit local con mensajes claros (siguiendo *Conventional Commits*).
    *   Ejemplo: eat: add otp verification handler
    *   Ejemplo: 	est: add unit tests for dynamics engine
    *   Ejemplo: docs: update deployment guidelines

---

## 🗃 2. Estructura de Documentación Requerida

Cada nuevo módulo o característica debe tener su archivo de documentación en la carpeta docs/.
Estructura recomendada:
*   docs/architecture.md - Diagrama de arquitectura física y flujos de MediatR.
*   docs/identity_auth.md - Detalles de flujos de OTP, JWT claims, refresh tokens y MFA (TOTP).
*   docs/dynamics_engine.md - Estructura de estrategias e integración de Trivia, Encuestas, etc.
*   docs/testing.md - Cobertura, frameworks y configuraciones de Testcontainers.
