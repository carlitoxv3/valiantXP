using OtpNet;
using System;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;

namespace ValiantXP.Infrastructure.Identity;

public class MfaService : IMfaService
{
    private const string Issuer = "ValiantXP";

    public MfaSetupDto GenerateMfaSetup(string target)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);
        
        var label = Uri.EscapeDataString(target);
        var issuerEscaped = Uri.EscapeDataString(Issuer);
        var qrCodeUri = $"otpauth://totp/{issuerEscaped}:{label}?secret={base32Secret}&issuer={issuerEscaped}";

        return new MfaSetupDto
        {
            Secret = base32Secret,
            QrCodeUri = qrCodeUri
        };
    }

    public bool VerifyMfaCode(string secret, string code)
    {
        try
        {
            var bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
