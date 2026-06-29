# ValiantXP - Core MVP

Sistema backend multi-tenant rediseñado desde cero (*from scratch*) en .NET 8 LTS para la gestión de promociones, dinámicas de engagement y programas de lealtad.

---

## 🚀 Tecnologías y Herramientas

*   **Framework**: .NET 8 LTS (C#)
*   **Base de Datos**: Microsoft SQL Server (vía Entity Framework Core)
*   **Autenticación**: JWT Personalizado + OTP Omnicanal (Email/WhatsApp) + MFA (TOTP)
*   **Patrones**: Clean Architecture, CQRS (MediatR), Repository Pattern, Strategy Pattern (Dynamics)
*   **Contenedores**: Docker / Docker Compose
*   **Orquestación**: Azure Container Apps (ACA) / Cloud Agnostic

---

## 🏛 Arquitectura del Proyecto

El proyecto sigue los principios de **Clean Architecture**, dividiéndose en 4 capas principales:

`
ValiantXP.sln
├── src/
│   ├── ValiantXP.Domain/       # Entidades, Enums, Interfaces de Dominio, Excepciones
│   ├── ValiantXP.Application/  # Casos de uso (CQRS), DTOs, Handlers de MediatR, Validaciones
│   ├── ValiantXP.Infrastructure/ # DbContext, Repositorios, Servicios de JWT/OTP/MFA
│   ├── ValiantXP.API/          # Controladores, Middlewares, Configuración de dependencias
│   └── ValiantXP.Tests/        # Pruebas unitarias e integración (xUnit, Moq)
`

Para mayor detalle de las interfaces y decisiones de diseño, consulta [docs/architecture.md](docs/architecture.md).

---

## 🛠 Ejecución Local y Docker

### Prerrequisitos
*   .NET 8 SDK
*   Docker & Docker Compose

### Pasos para iniciar la API
1.  Navegar a la carpeta raíz del proyecto.
2.  Levantar la base de datos SQL Server y la API:
    `ash
    docker-compose up --build
    `
3.  La API estará disponible en http://localhost:5000 (con Swagger expuesto en /swagger).

---

## 📚 Documentación

*   [docs/CONTRIBUTING_AGENTS.md](docs/CONTRIBUTING_AGENTS.md) - Guías de contribución y metodología para subagentes de desarrollo.
*   [docs/architecture.md](docs/architecture.md) - Especificaciones detalladas de la arquitectura y flujos.
*   [CHANGELOG.md](CHANGELOG.md) - Registro de cambios y versiones de ValiantXP.
