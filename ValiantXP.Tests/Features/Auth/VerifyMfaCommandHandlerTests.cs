using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Auth.Commands;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Auth;

public class VerifyMfaCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IMfaService> _mockMfaService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly VerifyMfaCommandHandler _handler;

    public VerifyMfaCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockMfaService = new Mock<IMfaService>();
        _mockTokenService = new Mock<ITokenService>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

        _handler = new VerifyMfaCommandHandler(
            _mockUnitOfWork.Object,
            _mockMfaService.Object,
            _mockTokenService.Object
        );
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var target = "nonexistent@example.com";
        var command = new VerifyMfaCommand(target, "123456", "127.0.0.1");

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found.");
    }

    [Fact]
    public async Task Handle_WithMfaDisabled_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyMfaCommand(target, "123456", "127.0.0.1");
        var user = new User
        {
            Email = target,
            IsMfaEnabled = false,
            MfaSecret = "some_secret"
        };

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("MFA is not enabled for this user.");
    }

    [Fact]
    public async Task Handle_WithEmptyMfaSecret_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyMfaCommand(target, "123456", "127.0.0.1");
        var user = new User
        {
            Email = target,
            IsMfaEnabled = true,
            MfaSecret = "" // Empty secret
        };

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("MFA is not enabled for this user.");
    }

    [Fact]
    public async Task Handle_WithInvalidMfaCode_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyMfaCommand(target, "wrong_code", "127.0.0.1");
        var user = new User
        {
            Email = target,
            IsMfaEnabled = true,
            MfaSecret = "valid_secret"
        };

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMfaService
            .Setup(s => s.VerifyMfaCode(user.MfaSecret, command.Code))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid MFA verification code.");
        
        _mockTokenService.Verify(s => s.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidMfaCode_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var target = "+123456789"; // phone target to coverPhoneNumber check
        var command = new VerifyMfaCommand(target, "123456", "127.0.0.1");
        var user = new User
        {
            PhoneNumber = target,
            IsMfaEnabled = true,
            MfaSecret = "valid_secret"
        };

        var expectedTokens = new TokenResponseDto
        {
            AccessToken = "jwt_access_token",
            RefreshToken = "jwt_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockUserRepo
            .Setup(r => r.GetByPhoneNumberAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMfaService
            .Setup(s => s.VerifyMfaCode(user.MfaSecret, command.Code))
            .Returns(true);

        _mockTokenService
            .Setup(s => s.GenerateTokensAsync(user, "127.0.0.1"))
            .ReturnsAsync(expectedTokens);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        
        _mockTokenService.Verify(s => s.GenerateTokensAsync(user, "127.0.0.1"), Times.Once);
    }
}
