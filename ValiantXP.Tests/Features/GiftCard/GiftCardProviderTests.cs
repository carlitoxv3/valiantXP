using System;
using FluentAssertions;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Tests.Features.GiftCard;

/// <summary>
/// Tests for GiftCardProvider and GiftCard entities.
/// Validates domain invariants: IsActive flag, IsAvailable computed property,
/// and basic object initialization.
/// </summary>
public class GiftCardProviderTests
{
    // ── GiftCardProvider entity ──

    [Fact]
    public void GiftCardProvider_WithActiveState_HasIsActiveTrue()
    {
        // Arrange / Act
        var provider = new GiftCardProvider { Id = Guid.NewGuid(), Name = "Amazon", IsActive = true };

        // Assert
        provider.IsActive.Should().BeTrue();
        provider.Name.Should().Be("Amazon");
    }

    [Fact]
    public void GiftCardProvider_WithInactiveState_HasIsActiveFalse()
    {
        // Arrange / Act
        var provider = new GiftCardProvider { Id = Guid.NewGuid(), Name = "Sodexo", IsActive = false };

        // Assert
        provider.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GiftCardProvider_DefaultIsActive_IsTrue()
    {
        // Arrange / Act — default value per entity definition
        var provider = new GiftCardProvider { Name = "Netflix" };

        // Assert
        provider.IsActive.Should().BeTrue("IsActive defaults to true per GiftCardProvider entity definition");
    }

    [Fact]
    public void GiftCardProvider_WithCampaignId_IsScopedToThatCampaign()
    {
        // Arrange / Act
        var campaignId = Guid.NewGuid();
        var provider = new GiftCardProvider { Id = Guid.NewGuid(), Name = "Spotify", CampaignId = campaignId };

        // Assert
        provider.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public void GiftCardProvider_WithNullCampaignId_IsGlobalProvider()
    {
        // Arrange / Act
        var provider = new GiftCardProvider { Id = Guid.NewGuid(), Name = "Global Provider", CampaignId = null };

        // Assert
        provider.CampaignId.Should().BeNull("null CampaignId means the provider is globally available");
    }

    // ── GiftCard entity ──

    [Fact]
    public void GiftCard_WithNullAssignedToUserId_IsAvailableTrue()
    {
        // Arrange / Act
        var card = new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(),
            Code = "ABC-123",
            AssignedToUserId = null
        };

        // Assert
        card.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void GiftCard_WithAssignedToUserId_IsAvailableFalse()
    {
        // Arrange / Act
        var card = new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(),
            Code = "ABC-123",
            AssignedToUserId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow
        };

        // Assert
        card.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void GiftCard_IsAvailable_DependsOnlyOnAssignedToUserId()
    {
        // Arrange — card with a UserPrizeId but no AssignedToUserId (edge case: shouldn't happen, but test invariant)
        var card = new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(),
            Code = "EDGE-CASE",
            UserPrizeId = Guid.NewGuid(), // linked to a prize record
            AssignedToUserId = null         // not yet assigned to a user
        };

        // Assert — IsAvailable is computed purely from AssignedToUserId
        card.IsAvailable.Should().BeTrue("IsAvailable is `AssignedToUserId is null` regardless of UserPrizeId");
    }

    [Fact]
    public void GiftCard_Code_IsStoredCorrectly()
    {
        // Arrange / Act
        const string expectedCode = "AMZN-XRAY-9900";
        var card = new Domain.Entities.GiftCard { Id = Guid.NewGuid(), Code = expectedCode };

        // Assert
        card.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void GiftCard_RedeemUrl_IsOptional()
    {
        // Arrange / Act
        var card = new Domain.Entities.GiftCard { Id = Guid.NewGuid(), Code = "OPT-URL" };

        // Assert
        card.RedeemUrl.Should().BeNull("RedeemUrl is optional for providers that don't use a URL");
    }
}
