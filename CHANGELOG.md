# Registro de Cambios - ValiantXP

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/)
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
