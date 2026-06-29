# Arquitectura del Sistema - ValiantXP

Este documento detalla la estructura y directrices de diseño arquitectónico para ValiantXP, utilizando una aproximación de **Clean Architecture** (Arquitectura Limpia) con .NET 8, patrón CQRS mediante MediatR, y autenticación JWT personalizada.

---

## 1. Estructura de Proyectos y Namespaces

La solución está organizada en cuatro capas concéntricas (Domain, Application, Infrastructure, API) más un proyecto de pruebas (Tests). Las dependencias fluyen siempre hacia el interior (hacia el Dominio).

`mermaid
graph TD
    API[ValiantXP.API] --> Infrastructure[ValiantXP.Infrastructure]
    API --> Application[ValiantXP.Application]
    Infrastructure --> Application
    Application --> Domain[ValiantXP.Domain]
    Tests[ValiantXP.Tests] --> API
    Tests --> Infrastructure
    Tests --> Application
    Tests --> Domain
`

### 1.1 ValiantXP.Domain
Capa central que contiene las entidades de negocio, reglas de dominio y abstracciones base. No tiene dependencias externas ni de otras capas.
* **ValiantXP.Domain.Entities**: Modelos de datos del dominio (ej. User, RefreshToken, OtpCode).
* **ValiantXP.Domain.Enums**: Enumeraciones seguras (ej. OtpChannel).
* **ValiantXP.Domain.Events**: Eventos de dominio disparados por cambios de estado.
* **ValiantXP.Domain.Interfaces**: Contratos/interfaces de bajo nivel que definen el comportamiento requerido por el dominio (ej. IDynamicStrategy, IRepository<T>).

### 1.2 ValiantXP.Application
Capa de lógica de aplicación que implementa los casos de uso utilizando CQRS con MediatR.
* **ValiantXP.Application.Common**:
  * Exceptions: Excepciones de aplicación (ej. ValidationException).
  * Interfaces: Contratos de servicios de aplicación y persistencia (ej. IApplicationDbContext, ITokenService).
* **ValiantXP.Application.DTOs**: Objetos de transferencia de datos para peticiones y respuestas de API.
* **ValiantXP.Application.Features**: Casos de uso estructurados por carpetas de características (Vertical Slices), agrupados por Commands, Queries, Handlers y Validators (usando FluentValidation).

### 1.3 ValiantXP.Infrastructure
Capa de implementación técnica que provee servicios de persistencia, integración y seguridad.
* **ValiantXP.Infrastructure.Data**:
  * ApplicationDbContext: Implementación de EF Core DbContext.
  * Configurations: Mapeos de EF Core (Fluent API).
* **ValiantXP.Infrastructure.Identity**:
  * Implementación de generación y validación de tokens JWT.
  * Lógica de MFA (TOTP con Otp.NET) y OTP.
* **ValiantXP.Infrastructure.Repositories**: Implementaciones del repositorio genérico y específicos.

### 1.4 ValiantXP.API
Capa de presentación que expone la interfaz REST del sistema.

---

## 2. Flujo de Autenticación Passwordless y MFA

1.  **Solicitud de OTP (/api/auth/otp/request)**: El usuario proporciona su correo o teléfono y elige el canal (Email o WhatsApp). Se genera un código de 6 dígitos con expiración de 5 minutos.
2.  **Verificación de OTP (/api/auth/otp/verify)**: El usuario envía el código recibido.
    *   Si es válido y el usuario no existía, se crea en la base de datos (Registro automático).
    *   Si el usuario tiene MFA habilitado (IsMfaEnabled == true), se retorna un estatus MfaRequired junto con un token de transacción temporal.
    *   Si MFA no está habilitado, se emiten los tokens JWT Access y Refresh.
3.  **Verificación de MFA (/api/auth/mfa/verify)**: El usuario envía el código de 6 dígitos generado en su aplicación de autenticación (Google Authenticator). Al ser válido, se emiten los tokens definitivos.
