# Registro de Cambios - ValiantXP

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/)
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-06-29
### Added
- **Módulo de Códigos (Canje de Código / Codigo)**: Nueva dinámica `CodigoStrategy` que valida y consume códigos promocionales. Un usuario puede canjear un código único; el sistema verifica su existencia, que no haya sido utilizado, y lo marca como consumido (`UsedAt`, `UserId`). La asignación de premios se delega al evento `ChallengeCompletedEvent` siguiendo el patrón PromoHub.
- **Entidad `Code`** con propiedades: `CodeNumber`, `CampaignId`, `UserId`, `UsedAt`, `RemoteIP`.
- **`CodeRepository`** implementa `ICodeRepository` con `GetByCodeNumberAsync` y `BulkInsertAsync`.
- **`CodeConfiguration`** (EF Core Fluent API): tabla `Codes`, índice único en `CodeNumber`, FK a `Campaign` (Restrict) y `User` (SetNull).
- **`DbSet<Code> Codes`** agregado a `ApplicationDbContext`.
- **`ICodeRepository Codes`** agregado a `IUnitOfWork`; implementado en `UnitOfWork` con lazy initialization.
- **Challenge Chaining**: `SubmitChallengeCommandHandler` ahora retorna `NextChallengeId` en el `ChallengeResultDto` cuando el reto se completa exitosamente y `DynamicChallenge.NextChallengeId` está configurado. Los clientes pueden usar este campo para navegar automáticamente al siguiente reto de la cadena.
- **`ChallengeResultDto.NextChallengeId`** (`Guid?`): nuevo campo en el DTO de respuesta.
- **Registro DI**: `CodigoStrategy` e `ICodeRepository` registrados en `DependencyInjection.cs`.
- **Pruebas unitarias** (`CodigoStrategyTests.cs`): 5 casos cubriendo código nulo, vacío, inválido, ya utilizado y válido con trimming.
- **Pruebas de integración** (`ChainingIntegrationTests.cs`): 3 casos verificando que `NextChallengeId` se propaga correctamente en éxito, es null en fallo, y es null cuando no hay cadena configurada.
- Total: **45 pruebas unitarias pasando** (0 fallos).

## [0.1.0] - 2026-06-29
### Added
- Configuración inicial de la solución .NET 8 y estructura de Clean Architecture.
- Módulo de Identidad y Autenticación Passwordless y Omnicanal.
- Entidades User, RefreshToken y OtpCode.
- Repositorio genérico y repositorios específicos (UserRepository, RefreshTokenRepository, OtpCodeRepository) utilizando EF Core.
- Generador de JWT Claims (sub, email, unique_name, jti, ole) y rotación de Refresh Tokens.
- Servicio de generación y verificación de OTP (Email y WhatsApp Mock) utilizando el enum OtpChannel.
- Verificación de MFA (TOTP mediante Otp.NET).
- Controladores /otp/request, /otp/verify, /mfa/setup, /mfa/enable, /mfa/verify, /refresh y /users/me.
