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
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Identity;
using Xunit;

namespace ValiantXP.Tests.Features.Auth;

public class VerifyOtpCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOtpCodeRepository> _mockOtpCodeRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IEmailOtpSender> _mockEmailSender;
    private readonly Mock<IWhatsAppOtpSender> _mockWhatsAppSender;
    private readonly Mock<ITokenService> _mockTokenService;
    
    private readonly OtpService _otpService;
    private readonly VerifyOtpCommandHandler _handler;

    public VerifyOtpCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOtpCodeRepo = new Mock<IOtpCodeRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockEmailSender = new Mock<IEmailOtpSender>();
        _mockWhatsAppSender = new Mock<IWhatsAppOtpSender>();
        _mockTokenService = new Mock<ITokenService>();

        _mockUnitOfWork.Setup(u => u.OtpCodes).Returns(_mockOtpCodeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

        _otpService = new OtpService(
            _mockUnitOfWork.Object,
            _mockEmailSender.Object,
            _mockWhatsAppSender.Object
        );

        _handler = new VerifyOtpCommandHandler(_otpService, _mockTokenService.Object);
    }

    [Fact]
    public async Task Handle_WithNoActiveOtp_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OtpCode?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No active OTP request found.");
    }

    [Fact]
    public async Task Handle_WithUsedOtp_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("OTP code has already been used.");
    }

    [Fact]
    public async Task Handle_WithExpiredOtp_ShouldReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("OTP code has expired.");
    }

    [Fact]
    public async Task Handle_WithTooManyAttempts_ShouldMarkUsedAndReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            Attempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Too many invalid verification attempts.");
        otpCode.IsUsed.Should().BeTrue();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidOtpCode_ShouldIncrementAttemptsAndReturnFailure()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "wrongcode", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            Attempts = 1,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid OTP code.");
        otpCode.Attempts.Should().Be(2);
        otpCode.IsUsed.Should().BeFalse();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidOtpAndExistingUser_MfaDisabled_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var target = "user@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            Attempts = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = target,
            UserName = target,
            IsMfaEnabled = false
        };

        var expectedTokens = new TokenResponseDto
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockTokenService
            .Setup(s => s.GenerateTokensAsync(existingUser, "127.0.0.1"))
            .ReturnsAsync(expectedTokens);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsMfaRequired.Should().BeFalse();
        result.Value.Tokens.Should().BeEquivalentTo(expectedTokens);
        otpCode.IsUsed.Should().BeTrue();
        
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidOtpAndNewUser_ShouldAutoRegisterAndReturnSuccessWithTokens()
    {
        // Arrange
        var target = "newuser@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            Attempts = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var expectedTokens = new TokenResponseDto
        {
            AccessToken = "access_token_new",
            RefreshToken = "refresh_token_new",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        // Simulate user not existing
        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? createdUser = null;
        _mockUserRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        _mockTokenService
            .Setup(s => s.GenerateTokensAsync(It.IsAny<User>(), "127.0.0.1"))
            .ReturnsAsync(expectedTokens);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsMfaRequired.Should().BeFalse();
        result.Value.Tokens.Should().BeEquivalentTo(expectedTokens);
        
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(target);
        createdUser.UserName.Should().Be(target);
        createdUser.IsMfaEnabled.Should().BeFalse();

        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidOtpAndExistingUser_MfaEnabled_ShouldFlagMfaRequiredAndNotReturnTokens()
    {
        // Arrange
        var target = "mfauser@example.com";
        var command = new VerifyOtpCommand(target, "123456", "127.0.0.1");
        var otpCode = new OtpCode
        {
            Target = target,
            Code = "123456",
            IsUsed = false,
            Attempts = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var mfaUser = new User
        {
            Id = Guid.NewGuid(),
            Email = target,
            UserName = target,
            IsMfaEnabled = true
        };

        _mockOtpCodeRepo
            .Setup(r => r.GetLatestActiveOtpAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaUser);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsMfaRequired.Should().BeTrue();
        result.Value.Tokens.Should().BeNull();
        
        _mockTokenService.Verify(s => s.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
