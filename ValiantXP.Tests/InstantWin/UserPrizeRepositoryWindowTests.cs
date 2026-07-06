using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Repositories;

namespace ValiantXP.Tests.InstantWin;

public class UserPrizeRepositoryWindowTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly UserPrizeRepository _repo;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _prizeId = Guid.NewGuid();
    private readonly DateTime _now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public UserPrizeRepositoryWindowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _repo = new UserPrizeRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private UserPrize MakeUserPrize(Guid userId, Guid prizeId, DateTime awardedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PrizeId = prizeId,
            Code = $"VXP-{Guid.NewGuid():N}"[..12],
            AwardedAt = awardedAt,
            PrizeType = PrizeType.Points
        };

    // --- GetAwardCountInWindowAsync ---

    [Fact]
    public async Task GetAwardCountInWindowAsync_ReturnsCorrectCount()
    {
        var windowStart = _now.AddHours(-24);
        await _db.UserPrizes.AddRangeAsync(
            MakeUserPrize(_userId, _prizeId, _now.AddHours(-10)), // inside window
            MakeUserPrize(_userId, _prizeId, _now.AddHours(-5)),  // inside window
            MakeUserPrize(_userId, _prizeId, _now.AddHours(-30))  // OUTSIDE window
        );
        await _db.SaveChangesAsync();

        var count = await _repo.GetAwardCountInWindowAsync(_prizeId, windowStart, CancellationToken.None);
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetAwardCountInWindowAsync_ExcludesOtherPrizes()
    {
        var otherPrize = Guid.NewGuid();
        var windowStart = _now.AddHours(-24);
        await _db.UserPrizes.AddRangeAsync(
            MakeUserPrize(_userId, _prizeId, _now.AddHours(-5)),   // our prize
            MakeUserPrize(_userId, otherPrize, _now.AddHours(-5)) // other prize
        );
        await _db.SaveChangesAsync();

        var count = await _repo.GetAwardCountInWindowAsync(_prizeId, windowStart, CancellationToken.None);
        count.Should().Be(1);
    }

    // --- GetUserAwardCountInWindowAsync ---

    [Fact]
    public async Task GetUserAwardCountInWindowAsync_ReturnsCorrectCount()
    {
        var otherUser = Guid.NewGuid();
        var windowStart = _now.AddHours(-24);
        await _db.UserPrizes.AddRangeAsync(
            MakeUserPrize(_userId, _prizeId, _now.AddHours(-5)),    // our user, in window
            MakeUserPrize(otherUser, _prizeId, _now.AddHours(-5)) // other user — should NOT count
        );
        await _db.SaveChangesAsync();

        var count = await _repo.GetUserAwardCountInWindowAsync(_userId, _prizeId, windowStart, CancellationToken.None);
        count.Should().Be(1);
    }

    // --- UserAlreadyWonAsync ---

    [Fact]
    public async Task UserAlreadyWonAsync_WhenUserHasWon_ReturnsTrue()
    {
        await _db.UserPrizes.AddAsync(MakeUserPrize(_userId, _prizeId, _now));
        await _db.SaveChangesAsync();

        var result = await _repo.UserAlreadyWonAsync(_userId, _prizeId, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserAlreadyWonAsync_WhenUserHasNotWon_ReturnsFalse()
    {
        var result = await _repo.UserAlreadyWonAsync(_userId, _prizeId, CancellationToken.None);
        result.Should().BeFalse();
    }
}
