# Authentication Module

## Overview
ValiantXP uses **passwordless authentication**. Users authenticate via a one-time password (OTP) delivered to their chosen channel, with optional TOTP-based MFA as a second factor. There are no stored passwords.

---

## OTP Channels
OTP delivery is channel-agnostic, controlled by the `OtpChannel` enum:

| Channel | Enum Value | Transport |
|---|---|---|
| Email | `OtpChannel.Email` | SMTP / Email provider |
| WhatsApp | `OtpChannel.WhatsApp` | WhatsApp Business API |

The user selects their preferred channel at request time. New channels can be added by:
1. Adding a value to `OtpChannel`
2. Implementing `IOtpSender` for that channel
3. Registering it in DI with a key matching the enum value

---

## Standard OTP Login Flow

```
POST /api/auth/otp/request
  Body:  { "contact": "user@example.com", "channel": "Email" }

  → RequestOtpCommandHandler:
      1. Generate 6-digit OTP
      2. Hash with HMAC-SHA256
      3. Store OtpCode entity (expires in 10 minutes)
      4. Resolve IOtpSender by OtpChannel
      5. Send OTP to contact

  Response: { "message": "OTP sent successfully" }
```

```
POST /api/auth/otp/verify
  Body:  { "contact": "user@example.com", "otp": "123456" }

  → VerifyOtpCommandHandler:
      1. Look up OtpCode by contact
      2. Validate: hash match + not expired
      3. If user doesn't exist → auto-register (unified login/register)
      4. If user.IsMfaEnabled = true:
           → Return { "mfaRequired": true, "tempToken": "..." }
      5. If user.IsMfaEnabled = false:
           → Generate JWT (access) + Refresh Token
           → Return { "accessToken": "...", "refreshToken": "..." }
```

---

## MFA Flow (TOTP)

```
POST /api/auth/mfa/setup
  [Authorize]
  → Returns: { "secret": "BASE32SECRET", "qrCodeUri": "otpauth://totp/..." }
  → User scans QR in Google Authenticator / Authy

POST /api/auth/mfa/enable
  [Authorize]
  Body: { "totp": "654321" }
  → Validates TOTP against secret
  → Sets user.IsMfaEnabled = true

POST /api/auth/mfa/verify
  Body: { "tempToken": "...", "totp": "654321" }
  → Validates tempToken + TOTP
  → Returns { "accessToken": "...", "refreshToken": "..." }
```

---

## Token Refresh

```
POST /api/auth/refresh
  Body: { "refreshToken": "..." }
  → Validates refresh token (not expired, not revoked)
  → Rotates: revokes old token, issues new access + refresh token pair
  → Returns { "accessToken": "...", "refreshToken": "..." }
```

---

## JWT Claims

| Claim | Value | Description |
|---|---|---|
| `sub` | `Guid` | User ID |
| `email` | `string` | User email |
| `unique_name` | `string` | Username |
| `jti` | `Guid` | Token ID (for revocation) |
| `role` | `string` | User role |

---

## Security Details

| Aspect | Detail |
|---|---|
| OTP format | 6 numeric digits |
| OTP expiry | 10 minutes |
| OTP storage | HMAC-SHA256 hash only (plaintext never stored) |
| MFA algorithm | RFC 6238 TOTP (30-second window) |
| MFA compatibility | Google Authenticator, Authy, Microsoft Authenticator |
| Refresh token | Cryptographically random, slides on each use |
| JWT signing | HS256 (symmetric), key from Key Vault in production |
