using FluentAssertions;
using System.Threading.Tasks;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Identity;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class InMemoryPendingLinkServiceTests
{
    private readonly InMemoryPendingLinkService _sut = new();

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyToken()
    {
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "sub_123", "test@example.com", true);
        var token = await _sut.CreateAsync(claims);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ValidToken_ReturnsClaims()
    {
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "sub_123", "test@example.com", true);
        var token = await _sut.CreateAsync(claims);

        var result = await _sut.ValidateAndConsumeAsync(token);

        result.Should().NotBeNull();
        result!.ExternalId.Should().Be("sub_123");
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_OneTimeUse_SecondCallReturnsNull()
    {
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "sub_123", "test@example.com", true);
        var token = await _sut.CreateAsync(claims);

        await _sut.ValidateAndConsumeAsync(token); // consume
        var second = await _sut.ValidateAndConsumeAsync(token); // should be null

        second.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_UnknownToken_ReturnsNull()
    {
        var result = await _sut.ValidateAndConsumeAsync("unknown-token-xyz");
        result.Should().BeNull();
    }
}
