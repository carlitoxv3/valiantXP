using ValiantXP.Application.DTOs;

namespace ValiantXP.Application.Common.Interfaces;

public interface IMfaService
{
    MfaSetupDto GenerateMfaSetup(string target);
    bool VerifyMfaCode(string secret, string code);
}
