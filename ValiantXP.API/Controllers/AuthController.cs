using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Auth.Commands;
using ValiantXP.Domain.Enums;

namespace ValiantXP.API.Controllers;

/// <summary>Authentication controller — passwordless OTP + MFA flows.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Request a one-time password (OTP) via Email or WhatsApp.</summary>
    /// <remarks>
    /// Generates a 6-digit OTP, stores it hashed, and sends it through the selected channel.
    /// OTP expires in 10 minutes. Rate-limited to 5 requests per minute per IP.
    /// </remarks>
    /// <param name="dto">Contact details and channel selection.</param>
    /// <response code="200">OTP sent successfully.</response>
    /// <response code="400">Invalid channel or request validation failed.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpPost("otp/request")]
    [EnableRateLimiting("OtpPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequestDto dto)
    {
        if (!Enum.TryParse<OtpChannel>(dto.Channel, true, out var channel))
        {
            return BadRequest(new { error = "Invalid channel. Supported channels are: Email, WhatsApp." });
        }

        var command = new RequestOtpCommand(dto.Target, channel);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = "OTP sent successfully." });
    }

    /// <summary>Verify an OTP and authenticate (or auto-register) the user.</summary>
    /// <remarks>
    /// Validates the OTP hash and expiry. If the user does not exist, they are auto-registered (unified login/register).
    /// Returns JWT tokens if MFA is disabled, or a temporary token + mfaRequired flag if MFA is enabled.
    /// </remarks>
    /// <param name="dto">Contact and OTP code.</param>
    /// <response code="200">Authentication successful. Returns tokens or mfaRequired flag.</response>
    /// <response code="400">Invalid or expired OTP.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpPost("otp/verify")]
    [EnableRateLimiting("OtpPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var command = new VerifyOtpCommand(dto.Target, dto.Code, ipAddress);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>Start TOTP MFA setup — returns a secret and QR code URI.</summary>
    /// <remarks>
    /// Scan the returned QR URI with Google Authenticator, Authy, or any RFC 6238–compatible app.
    /// Call /mfa/enable with a valid TOTP code to activate MFA for the account.
    /// </remarks>
    /// <param name="dto">User contact for which to set up MFA.</param>
    /// <response code="200">MFA setup initiated. Returns secret and QR URI.</response>
    /// <response code="400">User not found or MFA already enabled.</response>
    [HttpPost("mfa/setup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetupMfa([FromBody] MfaSetupRequestDto dto)
    {
        var command = new SetupMfaCommand(dto.Target);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>Enable MFA by verifying the first TOTP code after setup.</summary>
    /// <param name="dto">User contact and current TOTP code from authenticator app.</param>
    /// <response code="200">MFA enabled successfully.</response>
    /// <response code="400">Invalid TOTP code or setup not initiated.</response>
    [HttpPost("mfa/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequestDto dto)
    {
        var command = new EnableMfaCommand(dto.Target, dto.Code);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = "MFA enabled successfully." });
    }

    /// <summary>Complete login with a TOTP code when MFA is required.</summary>
    /// <remarks>Use the tempToken returned by /otp/verify when mfaRequired is true.</remarks>
    /// <param name="dto">Temporary token and current TOTP code.</param>
    /// <response code="200">MFA verified. Returns full JWT + refresh token pair.</response>
    /// <response code="400">Invalid or expired TOTP code or tempToken.</response>
    [HttpPost("mfa/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var command = new VerifyMfaCommand(dto.Target, dto.Code, ipAddress);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>Rotate a refresh token and issue a new access + refresh token pair.</summary>
    /// <remarks>Refresh tokens are single-use. Each call invalidates the previous token.</remarks>
    /// <param name="dto">Current refresh token.</param>
    /// <response code="200">New token pair issued.</response>
    /// <response code="400">Refresh token is invalid, expired, or already used.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var command = new RefreshTokenCommand(dto.RefreshToken, ipAddress);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
