using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Auth.Commands;
using ValiantXP.Domain.Enums;

namespace ValiantXP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("otp/request")]
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

    [HttpPost("otp/verify")]
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

    [HttpPost("mfa/setup")]
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

    [HttpPost("mfa/enable")]
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

    [HttpPost("mfa/verify")]
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

    [HttpPost("refresh")]
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
