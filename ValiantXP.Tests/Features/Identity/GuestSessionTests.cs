using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Repositories;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class GuestSessionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GuestSessionRepository _repo;

    public GuestSessionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repo = new GuestSessionRepository(_context);
    }

    private GuestSession CreateSession(int ttlMinutes = 60, bool expired = false)
    {
        var offset = expired ? -1 : ttlMinutes;
        return new GuestSession
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            ChannelHint = IdentityProvider.EmailOtp,
            TtlMinutes = ttlMinutes,
            ExpiresAt = DateTime.UtcNow.AddMinutes(offset)
        };
    }

    // D3.1 — CreateSession produces valid token and correct ExpiresAt
    [Fact]
    public async Task CreateSession_ReturnsToken_WithCorrectExpiry()
    {
        // Arrange
        var session = CreateSession(ttlMinutes: 30);

        // Act
        await _repo.AddAsync(session);
        await _context.SaveChangesAsync();

        var found = await _repo.FindByTokenAsync(session.Token);

        // Assert
        found.Should().NotBeNull();
        found!.Token.Should().Be(session.Token);
        found.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    // D3.2 — IsExpired returns true when ExpiresAt is in the past
    [Fact]
    public void GuestSession_IsExpired_WhenPastTTL()
    {
        var session = CreateSession(expired: true);
        session.IsExpired.Should().BeTrue();
    }

    // D3.3 — IsConverted is true when ConvertedToUserId is set
    [Fact]
    public void GuestSession_IsConverted_AfterConversion()
    {
        var session = CreateSession();
        session.IsConverted.Should().BeFalse();

        session.ConvertedToUserId = Guid.NewGuid();
        session.ConvertedAt = DateTime.UtcNow;

        session.IsConverted.Should().BeTrue();
    }

    // D3.4 — Expired session is detected
    [Fact]
    public async Task Convert_ExpiredSession_IsExpiredIsTrue()
    {
        // Arrange
        var session = CreateSession(expired: true);
        await _repo.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var found = await _repo.FindByTokenAsync(session.Token);

        // Assert — controller logic would return 400 based on IsExpired
        found!.IsExpired.Should().BeTrue("session TTL has passed");
        found.IsConverted.Should().BeFalse();
    }

    // D3.5 — Already converted session is detected
    [Fact]
    public async Task Convert_AlreadyConverted_IsConvertedIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = CreateSession();
        session.ConvertedToUserId = userId;
        session.ConvertedAt = DateTime.UtcNow;

        await _repo.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var found = await _repo.FindByTokenAsync(session.Token);

        // Assert
        found!.IsConverted.Should().BeTrue("session was already converted");
    }

    public void Dispose() => _context.Dispose();
}
