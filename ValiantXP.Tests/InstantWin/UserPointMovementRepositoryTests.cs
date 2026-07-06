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

public class UserPointMovementRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly UserPointMovementRepository _repo;
    private readonly Guid _userId = Guid.NewGuid();

    public UserPointMovementRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _repo = new UserPointMovementRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetTotalPointsAsync_SumsActiveMovements()
    {
        var m1 = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 100, Source = "Trivia", Description = "Test", CreatedAt = DateTime.UtcNow };
        var m2 = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 200, Source = "Code", Description = "Test", CreatedAt = DateTime.UtcNow };
        await _db.UserPointMovements.AddRangeAsync(m1, m2);
        await _db.SaveChangesAsync();

        var total = await _repo.GetTotalPointsAsync(_userId, CancellationToken.None);
        total.Should().Be(300);
    }

    [Fact]
    public async Task GetTotalPointsAsync_ExcludesExpiredMovements()
    {
        var active = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 100, Source = "Trivia", Description = "Test", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow };
        var expired = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 500, Source = "Trivia", Description = "Test", ExpiresAt = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow };
        await _db.UserPointMovements.AddRangeAsync(active, expired);
        await _db.SaveChangesAsync();

        var total = await _repo.GetTotalPointsAsync(_userId, CancellationToken.None);
        total.Should().Be(100); // only the non-expired
    }

    [Fact]
    public async Task GetTotalPointsAsync_ExcludesOtherUsers()
    {
        var myMovement = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 100, Source = "Trivia", Description = "Test", CreatedAt = DateTime.UtcNow };
        var otherUser = Guid.NewGuid();
        var otherMovement = new UserPointMovement { Id = Guid.NewGuid(), UserId = otherUser, Points = 9999, Source = "Trivia", Description = "Test", CreatedAt = DateTime.UtcNow };
        await _db.UserPointMovements.AddRangeAsync(myMovement, otherMovement);
        await _db.SaveChangesAsync();

        var total = await _repo.GetTotalPointsAsync(_userId, CancellationToken.None);
        total.Should().Be(100);
    }

    [Fact]
    public async Task GetTotalPointsAsync_NoMovements_ReturnsZero()
    {
        var total = await _repo.GetTotalPointsAsync(Guid.NewGuid(), CancellationToken.None);
        total.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_AddsMovementToContext()
    {
        var movement = new UserPointMovement { Id = Guid.NewGuid(), UserId = _userId, Points = 50, Source = "Rally", Description = "Test", CreatedAt = DateTime.UtcNow };
        await _repo.AddAsync(movement, CancellationToken.None);
        await _db.SaveChangesAsync();

        var count = await _db.UserPointMovements.CountAsync();
        count.Should().Be(1);
    }
}
