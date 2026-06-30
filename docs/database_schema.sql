IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [Campaigns] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Campaigns] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [FailedAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [ChallengeId] uniqueidentifier NOT NULL,
        [CampaignId] uniqueidentifier NOT NULL,
        [SubmittedValue] nvarchar(256) NULL,
        [RemoteIp] nvarchar(64) NULL,
        [RuleCode] nvarchar(64) NOT NULL,
        [Reason] nvarchar(512) NOT NULL,
        [AttemptedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FailedAttempts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [OtpCodes] (
        [Id] uniqueidentifier NOT NULL,
        [Target] nvarchar(256) NOT NULL,
        [Code] nvarchar(10) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsUsed] bit NOT NULL,
        [Attempts] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OtpCodes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [UserName] nvarchar(256) NOT NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [MfaSecret] nvarchar(128) NULL,
        [IsMfaEnabled] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [DynamicChallenges] (
        [Id] uniqueidentifier NOT NULL,
        [CampaignId] uniqueidentifier NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [ConfigurationJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [NextChallengeId] uniqueidentifier NULL,
        [AntiFraudConfigJson] nvarchar(max) NULL,
        CONSTRAINT [PK_DynamicChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DynamicChallenges_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [Codes] (
        [Id] uniqueidentifier NOT NULL,
        [CodeNumber] nvarchar(100) NOT NULL,
        [CampaignId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [UsedAt] datetime2 NULL,
        [RemoteIP] nvarchar(50) NULL,
        CONSTRAINT [PK_Codes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Codes_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Codes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [Token] nvarchar(256) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsRevoked] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(50) NOT NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(50) NULL,
        [ReplacedByToken] nvarchar(256) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [Prizes] (
        [Id] uniqueidentifier NOT NULL,
        [DynamicChallengeId] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Quantity] int NOT NULL,
        [RemainingQuantity] int NOT NULL,
        [Type] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Prizes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prizes_DynamicChallenges_DynamicChallengeId] FOREIGN KEY ([DynamicChallengeId]) REFERENCES [DynamicChallenges] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [UserChallengeProgresses] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [DynamicChallengeId] uniqueidentifier NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Attempts] int NOT NULL,
        [Score] int NOT NULL,
        [CompletedAt] datetime2 NULL,
        [ReservedPrizeId] uniqueidentifier NULL,
        CONSTRAINT [PK_UserChallengeProgresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserChallengeProgresses_DynamicChallenges_DynamicChallengeId] FOREIGN KEY ([DynamicChallengeId]) REFERENCES [DynamicChallenges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserChallengeProgresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE TABLE [UserPrizes] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PrizeId] uniqueidentifier NOT NULL,
        [AwardedAt] datetime2 NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_UserPrizes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPrizes_Prizes_PrizeId] FOREIGN KEY ([PrizeId]) REFERENCES [Prizes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserPrizes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Codes_CampaignId] ON [Codes] ([CampaignId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Codes_CodeNumber] ON [Codes] ([CodeNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Codes_UserId] ON [Codes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DynamicChallenges_CampaignId] ON [DynamicChallenges] ([CampaignId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FailedAttempts_Ip_Challenge_Date] ON [FailedAttempts] ([RemoteIp], [ChallengeId], [AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FailedAttempts_User_Challenge_Date] ON [FailedAttempts] ([UserId], [ChallengeId], [AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OtpCodes_Target_IsUsed_ExpiresAt] ON [OtpCodes] ([Target], [IsUsed], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Prizes_DynamicChallengeId] ON [Prizes] ([DynamicChallengeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserChallengeProgresses_DynamicChallengeId] ON [UserChallengeProgresses] ([DynamicChallengeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserChallengeProgresses_UserId_DynamicChallengeId] ON [UserChallengeProgresses] ([UserId], [DynamicChallengeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPrizes_Code] ON [UserPrizes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPrizes_PrizeId] ON [UserPrizes] ([PrizeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPrizes_UserId] ON [UserPrizes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL AND [Email] != ''''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630094354_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630094354_InitialCreate', N'8.0.28');
END;
GO

COMMIT;
GO

