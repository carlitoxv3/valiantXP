namespace ValiantXP.Domain.Enums;

public enum IdentityProvider
{
    // OAuth 2.0 / OIDC — globally unique sub/id
    Google    = 1,
    Spotify   = 2,
    Twitch    = 3,
    Apple     = 4,
    Microsoft = 5,

    // Meta / Business-scoped (cannot cross-link automatically)
    Facebook  = 10,
    Instagram = 11,
    WhatsApp  = 12,

    // Messaging (globally unique but no verifiable email claim)
    Telegram  = 20,

    // Our own OTP flows
    EmailOtp  = 30,
    SmsOtp    = 31,

    // Future
    Kiosk     = 40,
    PosDevice = 41,
}
