using System;

namespace ValiantXP.Application.DTOs;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RequestOtpRequestDto
{
    public string Target { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    public string Target { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyOtpResponseDto
{
    public bool IsMfaRequired { get; set; }
    public TokenResponseDto? Tokens { get; set; }
}

public class VerifyMfaRequestDto
{
    public string Target { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class EnableMfaRequestDto
{
    public string Target { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class MfaSetupRequestDto
{
    public string Target { get; set; } = string.Empty;
}

public class MfaSetupDto
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
}
