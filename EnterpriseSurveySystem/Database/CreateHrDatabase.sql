USE [HrEnterpriseDb];
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'prj')
BEGIN
    EXEC('CREATE SCHEMA [prj]');
END
GO

IF OBJECT_ID('[prj].[GetTopDepartment]', 'FN') IS NULL
BEGIN
    EXEC('CREATE FUNCTION [prj].[GetTopDepartment](@DepartmentId INT) RETURNS INT AS BEGIN RETURN @DepartmentId; END');
END
GO

IF OBJECT_ID('[prj].[PersonPosition]', 'U') IS NOT NULL DROP TABLE [prj].[PersonPosition];
IF OBJECT_ID('[prj].[Person]', 'U') IS NOT NULL DROP TABLE [prj].[Person];
IF OBJECT_ID('[prj].[Position]', 'U') IS NOT NULL DROP TABLE [prj].[Position];
IF OBJECT_ID('[prj].[Department]', 'U') IS NOT NULL DROP TABLE [prj].[Department];
GO

CREATE TABLE [prj].[Department]
(
    [DepartmentId] [int] NOT NULL PRIMARY KEY,
    [DepHdrId] [int] NOT NULL DEFAULT(0),
    [DepNameFa] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NOT NULL,
    [DepNameEn] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [DepLevel] [int] NOT NULL DEFAULT(1),
    [IsActive] [bit] NOT NULL DEFAULT(1),
    [DomainId] [int] NOT NULL DEFAULT(1)
);
GO

CREATE TABLE [prj].[Position]
(
    [PositionId] [int] NOT NULL PRIMARY KEY,
    [PositionNameFa] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [PositionNameEn] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [ParentId] [int] NOT NULL DEFAULT(0),
    [DepartmentId] [int] NOT NULL,
    [IsActive] [bit] NOT NULL DEFAULT(1),
    [PositionLevelId] [int] NOT NULL DEFAULT(1),
    [TopDepartmentId] AS ([prj].[GetTopDepartment]([DepartmentId])),
    [BonusCategoryId] [int] NOT NULL CONSTRAINT [DF_Position_BonusCategoryId] DEFAULT ((0)),
    [IsSupport] [bit] NOT NULL CONSTRAINT [DF_Position_IsSuport] DEFAULT ((1))
);
GO

CREATE TABLE [prj].[Person]
(
    [PersonId] [int] NOT NULL PRIMARY KEY,
    [FirstName] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NOT NULL,
    [LastName] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NOT NULL,
    [FirstNameEn] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [LastNameEn] [nvarchar] (55) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [FatherName] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [loginName] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [Email] [nvarchar] (76) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [RoleId] [int] NOT NULL DEFAULT(1),
    [PersonelCode] [int] NOT NULL,
    [Gender] [bit] NOT NULL DEFAULT(1),
    [IsActive] [bit] NOT NULL DEFAULT(1),
    [EducGrade] [nvarchar] (43) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [EducField] [nvarchar] (71) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [Maried] [bit] NOT NULL DEFAULT(0),
    [moblie] [nvarchar] (42) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [Telephone] [nchar] (11) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [bdateG] [datetime] NOT NULL DEFAULT(GETDATE()),
    [ShenId] [nvarchar] (42) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [EmpType] [nvarchar] (41) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [ContType] [nvarchar] (46) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [PassKey] [nvarchar] (94) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [Address] [nvarchar] (600) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [NationalCode] [nvarchar] (41) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [ChangePassDate] [datetime] NULL,
    [SignName] [nvarchar] (44) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [CDUserId] [int] NOT NULL DEFAULT(1),
    [FullName] AS (([FirstName]+' ')+[LastName]),
    [IsRuleAccepted] [bit] NOT NULL CONSTRAINT [DF__Person__IsRuleAc__5A92848A] DEFAULT ((0)),
    [TypeId] [int] NULL,
    [PersonDoApprisal] [bit] NOT NULL CONSTRAINT [DF__Person__PersonDo__34F02690] DEFAULT ((0)),
    [MobileConfirmCode] [int] NOT NULL CONSTRAINT [DF__Person__MobileCo__1F94A6D6] DEFAULT ((0)),
    [MobileConfirmFlag] [bit] NOT NULL CONSTRAINT [DF__Person__MobileCo__2088CB0F] DEFAULT ((0)),
    [MobileCloseFlag] [bit] NOT NULL CONSTRAINT [DF__Person__MobileCl__217CEF48] DEFAULT ((0)),
    [HasAccess] [bit] NOT NULL CONSTRAINT [DF__Person__HasAcces__2CFC1E22] DEFAULT ((0)),
    [DateEs] [date] NULL,
    [StopCooperationReasonId] [int] NULL CONSTRAINT [DF__Person__StopCoop__5DDFFD9E] DEFAULT ((0)),
    [LastWorkingDay] [date] NULL,
    [UnitId] [int] NULL CONSTRAINT [DF__Person__UnitId__648CFB2D] DEFAULT ((0)),
    [InactiveDate] [date] NULL,
    [Description] [nvarchar] (250) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [PlaceOfBirth] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [HistoryInAbidi] [int] NULL,
    [HistoryOutsideAbidi] [int] NULL,
    [EssentialContactPhoneNumber] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [MarriageDate] [datetime] NULL,
    [CostCenter] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [PostalCode] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [InsuranceNumber] [nvarchar] (40) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [BankAccountNumber] [nvarchar] (40) COLLATE SQL_Latin1_General_CP1256_CI_AS NULL,
    [TotalRecords] AS (isnull([HistoryInAbidi],(0))+isnull([HistoryOutsideAbidi],(0))),
    [HRBPTypeId] [int] NOT NULL CONSTRAINT [DF_Person_HRBPTypeId] DEFAULT ((0)),
    [AbidiLocationId] [int] NOT NULL CONSTRAINT [DF__Person__AbidiLoc__6E0F08F5] DEFAULT ((0)),
    [CanLoginWithOTP] [bit] NOT NULL CONSTRAINT [DF_Person_CanLoginWithOTP] DEFAULT ((0))
);
GO

CREATE TABLE [prj].[PersonPosition]
(
    [PersonPositionId] [int] NOT NULL PRIMARY KEY,
    [PersonId] [int] NOT NULL,
    [PositionId] [int] NOT NULL,
    [StartDate] [datetime] NOT NULL DEFAULT(GETDATE()),
    [FinishDate] [datetime] NULL,
    [IsLast] [bit] NOT NULL DEFAULT(1)
);
GO

-- Sample Organization Data for Testing & Verification
INSERT INTO [prj].[Department] ([DepartmentId], [DepHdrId], [DepNameFa], [DepNameEn], [DepLevel], [IsActive], [DomainId])
VALUES
(1, 0, N'مدیریت منابع انسانی', 'Human Resources', 1, 1, 1),
(2, 0, N'فناوری اطلاعات و ارتباطات', 'Information Technology', 1, 1, 1),
(3, 0, N'مالی و حسابداری', 'Finance & Accounting', 1, 1, 1),
(4, 0, N'تولید و عملیات', 'Production & Operations', 1, 1, 1),
(5, 0, N'واحدهای غیرفعال آزمایشی', 'Inactive Unit', 1, 0, 1);

INSERT INTO [prj].[Position] ([PositionId], [PositionNameFa], [PositionNameEn], [ParentId], [DepartmentId], [IsActive], [PositionLevelId])
VALUES
(101, N'مدیر منابع انسانی', 'HR Director', 0, 1, 1, 1),
(102, N'کارشناس ارشد جذب و استخدام', 'Senior Talent Acquisition Specialist', 101, 1, 1, 2),
(201, N'مدیر فناوری اطلاعات', 'IT Director', 0, 2, 1, 1),
(202, N'توسعه‌دهنده ارشد نرم‌افزار', 'Senior Software Engineer', 201, 2, 1, 2),
(203, N'کارشناس امنیت شبکه و سرور', 'Network Security Specialist', 201, 2, 1, 2),
(301, N'رئیس حسابداری مالی', 'Chief Accountant', 0, 3, 1, 1),
(302, N'کارشناس حسابداری و حقوق دستمزد', 'Payroll Specialist', 301, 3, 1, 2),
(401, N'مدیر برنامه‌ریزی تولید', 'Production Planning Manager', 0, 4, 1, 1),
(999, N'پست غیرفعال', 'Inactive Position', 0, 1, 0, 1);

INSERT INTO [prj].[Person] ([PersonId], [FirstName], [LastName], [PersonelCode], [IsActive], [moblie], [Email], [bdateG])
VALUES
(1001, N'علی', N'کشوری', 10452, 1, N'09123456789', N'ali.keshvari@company.com', '1990-05-15'),
(1002, N'رضا', N'محمدی', 10453, 1, N'09121112233', N'reza.mohammadi@company.com', '1988-08-20'),
(1003, N'سارا', N'احمدی', 10454, 1, N'09122223344', N'sara.ahmadi@company.com', '1995-02-10'),
(1004, N'مهدی', N'حسینی', 10455, 1, N'09123334455', N'mehdi.hosseini@company.com', '1992-11-05'),
(1005, N'مریم', N'کریمی', 10456, 1, N'09124445566', N'maryam.karimi@company.com', '1994-04-12'),
(1006, N'امیر', N'صادقی', 10457, 1, N'09125556677', N'amir.sadeghi@company.com', '1991-09-25'),
(1007, N'زهرا', N'مرادی', 10458, 1, N'09126667788', N'zahra.moradi@company.com', '1993-01-30'),
(1008, N'حسین', N'قاسمی', 10459, 1, N'09127778899', N'hossein.ghasemi@company.com', '1989-07-14'),
(1009, N'کارمند', N'غیرفعال سابق', 99999, 0, N'09120000000', N'inactive@company.com', '1985-01-01');

INSERT INTO [prj].[PersonPosition] ([PersonPositionId], [PersonId], [PositionId], [StartDate], [FinishDate], [IsLast])
VALUES
(1, 1001, 202, '2022-01-01', NULL, 1), -- علی کشوری: توسعه‌دهنده ارشد نرم‌افزار (IT)
(2, 1002, 201, '2020-05-01', NULL, 1), -- رضا محمدی: مدیر IT (IT)
(3, 1003, 101, '2021-03-01', NULL, 1), -- سارا احمدی: مدیر منابع انسانی (HR)
(4, 1004, 102, '2023-02-01', NULL, 1), -- مهدی حسینی: کارشناس جذب (HR)
(5, 1005, 301, '2019-10-01', NULL, 1), -- مریم کریمی: رئیس حسابداری (مالی)
(6, 1006, 302, '2021-06-01', NULL, 1), -- امیر صادقی: کارشناس حسابداری (مالی)
(7, 1007, 401, '2020-01-01', NULL, 1), -- زهرا مرادی: مدیر تولید (تولید)
(8, 1008, 203, '2022-08-01', NULL, 1), -- حسین قاسمی: امنیت شبکه (IT)
(9, 1001, 203, '2020-01-01', '2021-12-31', 0), -- سابقه قبلی علی کشوری که IsLast = 0 است (باید فیلتر شود!)
(10, 1009, 102, '2018-01-01', NULL, 1); -- کارمند غیرفعال (IsActive = 0) (باید فیلتر شود!)
GO
