-- =========================================================================
-- Enterprise Survey & Questionnaire Management System Database Schema
-- SQL Server 2016+ / LocalDB / Express
-- ASP.NET Framework 4.8 & Entity Framework 6.2.0
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EnterpriseSurveyDb')
BEGIN
    CREATE DATABASE [EnterpriseSurveyDb];
END
GO

USE [EnterpriseSurveyDb];
GO

-- 1. Questionnaires Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Questionnaires]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Questionnaires] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(250) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [TargetRespondentsCount] INT NOT NULL DEFAULT(0),
        [TimeLimitMinutes] INT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT [PK_dbo.Questionnaires] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 2. Questions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Questions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Questions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [QuestionnaireId] INT NOT NULL,
        [QuestionText] NVARCHAR(MAX) NOT NULL,
        [QuestionType] NVARCHAR(50) NOT NULL, -- Testy, Tashrihi, Select, MultiSelect, Boolean
        [Options] NVARCHAR(MAX) NULL,          -- JSON string array e.g. ["عالی", "خوب", "متوسط", "ضعیف"]
        [OrderIndex] INT NOT NULL DEFAULT(1),
        [IsRequired] BIT NOT NULL DEFAULT(1),
        CONSTRAINT [PK_dbo.Questions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_dbo.Questions_dbo.Questionnaires_QuestionnaireId] FOREIGN KEY([QuestionnaireId])
            REFERENCES [dbo].[Questionnaires] ([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Questions_QuestionnaireId] ON [dbo].[Questions]([QuestionnaireId] ASC);
END
GO

-- 3. RespondentTokens Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RespondentTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RespondentTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [QuestionnaireId] INT NOT NULL,
        [TokenGuid] NVARCHAR(64) NOT NULL,
        [IsUsed] BIT NOT NULL DEFAULT(0),
        [OpenedAt] DATETIME NULL,
        [CompletedAt] DATETIME NULL,
        [ExpiresAt] DATETIME NULL,
        [RespondentIdentifier] NVARCHAR(150) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_dbo.RespondentTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_dbo.RespondentTokens_TokenGuid] UNIQUE NONCLUSTERED ([TokenGuid] ASC),
        CONSTRAINT [FK_dbo.RespondentTokens_dbo.Questionnaires_QuestionnaireId] FOREIGN KEY([QuestionnaireId])
            REFERENCES [dbo].[Questionnaires] ([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_RespondentTokens_QuestionnaireId] ON [dbo].[RespondentTokens]([QuestionnaireId] ASC);
    CREATE NONCLUSTERED INDEX [IX_RespondentTokens_TokenGuid] ON [dbo].[RespondentTokens]([TokenGuid] ASC);
END
GO

-- 4. Answers Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Answers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Answers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [TokenId] INT NOT NULL,
        [QuestionId] INT NOT NULL,
        [AnswerValue] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT [PK_dbo.Answers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_dbo.Answers_dbo.RespondentTokens_TokenId] FOREIGN KEY([TokenId])
            REFERENCES [dbo].[RespondentTokens] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_dbo.Answers_dbo.Questions_QuestionId] FOREIGN KEY([QuestionId])
            REFERENCES [dbo].[Questions] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Answers_TokenId] ON [dbo].[Answers]([TokenId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Answers_QuestionId] ON [dbo].[Answers]([QuestionId] ASC);
END
GO

-- 5. AuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [TokenId] INT NULL,
        [QuestionnaireId] INT NULL,
        [ActionType] NVARCHAR(50) NOT NULL, -- PageOpened, FormSubmitted, TokenExpired, AccessDenied, TokensGenerated
        [Timestamp] DATETIME NOT NULL DEFAULT(GETDATE()),
        [IPAddress] NVARCHAR(50) NULL,
        [Details] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_dbo.AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_dbo.AuditLogs_dbo.RespondentTokens_TokenId] FOREIGN KEY([TokenId])
            REFERENCES [dbo].[RespondentTokens] ([Id]) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX [IX_AuditLogs_TokenId] ON [dbo].[AuditLogs]([TokenId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_QuestionnaireId] ON [dbo].[AuditLogs]([QuestionnaireId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp] ON [dbo].[AuditLogs]([Timestamp] DESC);
END
GO
