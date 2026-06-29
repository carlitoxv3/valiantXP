using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.Features.Auth.Commands;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Auth;

public class RequestOtpCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOtpCodeRepository> _mockOtpCodeRepo;
    private readonly Mock<IEmailOtpSender> _mockEmailSender;
    private readonly Mock<IWhatsAppOtpSender> _mockWhatsAppSender;
    private readonly RequestOtpCommandHandler _handler;

    public RequestOtpCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOtpCodeRepo = new Mock<IOtpCodeRepository>();
        _mockEmailSender = new Mock<IEmailOtpSender>();
        _mockWhatsAppSender = new Mock<IWhatsAppOtpSender>();

        _mockUnitOfWork.Setup(u => u.OtpCodes).Returns(_mockOtpCodeRepo.Object);

        _handler = new RequestOtpCommandHandler(
            _mockUnitOfWork.Object,
            _mockEmailSender.Object,
            _mockWhatsAppSender.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidEmailRequest_ShouldGenerateOtpSaveItAndSendViaEmail()
    {
        // Arrange
        var target = "user@example.com";
        var command = new RequestOtpCommand(target, OtpChannel.Email);

        OtpCode? savedOtp = null;
        _mockOtpCodeRepo
            .Setup(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()))
            .Callback<OtpCode, CancellationToken>((otp, _) => savedOtp = otp)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailSender
            .Setup(s => s.SendOtpAsync(target, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        savedOtp.Should().NotBeNull();
        savedOtp!.Target.Should().Be(target);
        savedOtp.Code.Should().HaveLength(6);
        int.TryParse(savedOtp.Code, out _).Should().BeTrue();
        savedOtp.Channel.Should().Be(OtpChannel.Email);
        savedOtp.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        savedOtp.IsUsed.Should().BeFalse();

        _mockOtpCodeRepo.Verify(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailSender.Verify(s => s.SendOtpAsync(target, savedOtp.Code, It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppSender.Verify(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidWhatsAppRequest_ShouldGenerateOtpSaveItAndSendViaWhatsApp()
    {
        // Arrange
        var target = "+123456789";
        var command = new RequestOtpCommand(target, OtpChannel.WhatsApp);

        OtpCode? savedOtp = null;
        _mockOtpCodeRepo
            .Setup(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()))
            .Callback<OtpCode, CancellationToken>((otp, _) => savedOtp = otp)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockWhatsAppSender
            .Setup(s => s.SendOtpAsync(target, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        savedOtp.Should().NotBeNull();
        savedOtp!.Target.Should().Be(target);
        savedOtp.Code.Should().HaveLength(6);
        savedOtp.Channel.Should().Be(OtpChannel.WhatsApp);

        _mockOtpCodeRepo.Verify(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppSender.Verify(s => s.SendOtpAsync(target, savedOtp.Code, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailSender.Verify(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyTarget_ShouldReturnFailure(string? invalidTarget)
    {
        // Arrange
        var command = new RequestOtpCommand(invalidTarget!, OtpChannel.Email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Target (email or phone) is required.");

        _mockOtpCodeRepo.Verify(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSenderFails_ShouldReturnFailureButSaveOtp()
    {
        // Arrange
        var target = "user@example.com";
        var command = new RequestOtpCommand(target, OtpChannel.Email);

        OtpCode? savedOtp = null;
        _mockOtpCodeRepo
            .Setup(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()))
            .Callback<OtpCode, CancellationToken>((otp, _) => savedOtp = otp)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailSender
            .Setup(s => s.SendOtpAsync(target, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP configuration error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to send OTP: SMTP configuration error");

        _mockOtpCodeRepo.Verify(r => r.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailSender.Verify(s => s.SendOtpAsync(target, savedOtp!.Code, It.IsAny<CancellationToken>()), Times.Once);
    }
}
