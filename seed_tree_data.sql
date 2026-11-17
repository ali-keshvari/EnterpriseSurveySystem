-- Clear and rebuild sample HR data with tree hierarchy
DELETE FROM [prj].[PersonPosition];
DELETE FROM [prj].[Person];
DELETE FROM [prj].[Position];
DELETE FROM [prj].[Department];

-- Level 1: Root Departments (DepHdrId = 0, DepLevel = 1)
INSERT INTO [prj].[Department] ([DepartmentId], [DepHdrId], [DepNameFa], [DepNameEn], [DepLevel], [IsActive], [DomainId]) VALUES
(1, 0, N'معاونت منابع انسانی', 'HR Directorate', 1, 1, 1),
(2, 0, N'معاونت فناوری اطلاعات و ارتباطات', 'ICT Directorate', 1, 1, 1),
(3, 0, N'معاونت مالی و اقتصادی', 'Finance Directorate', 1, 1, 1),
(4, 0, N'معاونت تولید و عملیات', 'Operations Directorate', 1, 1, 1);

-- Level 2: Sub-Departments / Managements (DepHdrId points to Level 1)
INSERT INTO [prj].[Department] ([DepartmentId], [DepHdrId], [DepNameFa], [DepNameEn], [DepLevel], [IsActive], [DomainId]) VALUES
(11, 1, N'اداره جذب و استخدام', 'Recruitment Dept', 2, 1, 1),
(12, 1, N'اداره آموزش و توسعه سرمایه انسانی', 'Training & Development Dept', 2, 1, 1),
(21, 2, N'مدیریت مهندسی نرم‌افزار', 'Software Engineering Dept', 2, 1, 1),
(22, 2, N'مدیریت زیرساخت، شبکه و امنیت', 'Network & Security Dept', 2, 1, 1),
(31, 3, N'اداره حسابداری و دفاتر مالی', 'Accounting Dept', 2, 1, 1),
(32, 3, N'اداره خزانه‌داری و حقوق دستمزد', 'Treasury & Payroll Dept', 2, 1, 1),
(41, 4, N'اداره کنترل کیفیت و آزمایشگاه (QC)', 'Quality Control Dept', 2, 1, 1),
(42, 4, N'اداره برنامه‌ریزی و خط تولید', 'Production Planning Dept', 2, 1, 1);

-- Level 3: Units under Software Engineering (DepHdrId = 21, DepLevel = 3)
INSERT INTO [prj].[Department] ([DepartmentId], [DepHdrId], [DepNameFa], [DepNameEn], [DepLevel], [IsActive], [DomainId]) VALUES
(211, 21, N'واحد سامانه‌های وب و پورتال', 'Web & Portal Unit', 3, 1, 1),
(212, 21, N'واحد پایگاه‌داده و معماری سرویس‌ها', 'Database & Architecture Unit', 3, 1, 1);

-- Positions
INSERT INTO [prj].[Position] ([PositionId], [PositionNameFa], [PositionNameEn], [ParentId], [DepartmentId], [IsActive], [PositionLevelId], [BonusCategoryId], [IsSupport]) VALUES
(101, N'معاون منابع انسانی', 'HR Director', 0, 1, 1, 1, 1, 1),
(102, N'کارشناس ارشد جذب و استخدام', 'Senior Recruitment Specialist', 101, 11, 1, 2, 1, 1),
(103, N'کارشناس آموزش و توانمندسازی', 'Training Specialist', 101, 12, 1, 2, 1, 1),
(201, N'معاون فناوری اطلاعات و ارتباطات', 'CIO / IT Director', 0, 2, 1, 1, 1, 0),
(202, N'مدیر فنی مهندسی نرم‌افزار', 'Software Engineering Manager', 201, 21, 1, 2, 1, 0),
(203, N'توسعه‌دهنده ارشد فرانت‌اند و وب', 'Senior Frontend Developer', 202, 211, 1, 3, 1, 0),
(204, N'توسعه‌دهنده ارشد دیتابیس و بک‌اند', 'Senior Backend & DB Developer', 202, 212, 1, 3, 1, 0),
(205, N'مهندس ارشد زیرساخت و امنیت شبکه', 'Senior Network & Security Engineer', 201, 22, 1, 2, 1, 0),
(301, N'معاون مالی و اقتصادی', 'CFO / Finance Director', 0, 3, 1, 1, 1, 1),
(302, N'رئیس حسابداری مالی', 'Chief Accountant', 301, 31, 1, 2, 1, 1),
(303, N'کارشناس امور خزانه‌داری و حقوق', 'Treasury Specialist', 301, 32, 1, 3, 1, 1),
(401, N'معاون تولید و عملیات', 'COO / Operations Director', 0, 4, 1, 1, 1, 0),
(402, N'سرپرست کنترل کیفی و آزمایشگاه', 'QC Supervisor', 401, 41, 1, 2, 1, 0),
(403, N'سرپرست برنامه‌ریزی تولید', 'Production Supervisor', 401, 42, 1, 2, 1, 0);

-- Personnel
INSERT INTO [prj].[Person] ([PersonId], [FirstName], [LastName], [RoleId], [PersonelCode], [Gender], [IsActive], [Maried], [bdateG], [CDUserId], [moblie], [Email]) VALUES
(1001, N'علی', N'کشوری', 1, 10452, 1, 1, 1, '1990-05-15', 1, '09121111111', 'ali.keshvari@example.com'),
(1002, N'رضا', N'محمدی', 2, 10453, 1, 1, 0, '1994-08-20', 2, '09122222222', 'reza.mohammadi@example.com'),
(1003, N'سارا', N'احمدی', 2, 10454, 0, 1, 1, '1992-02-10', 3, '09123333333', 'sara.ahmadi@example.com'),
(1004, N'مهدی', N'حسینی', 2, 10455, 1, 1, 1, '1988-11-25', 4, '09124444444', 'mehdi.hosseini@example.com'),
(1005, N'مریم', N'کریمی', 2, 10456, 0, 1, 0, '1995-07-03', 5, '09125555555', 'maryam.karimi@example.com'),
(1006, N'امیر', N'صادقی', 1, 10457, 1, 1, 1, '1985-03-30', 6, '09126666666', 'amir.sadeghi@example.com'),
(1007, N'زهرا', N'مرادی', 2, 10458, 0, 1, 1, '1991-09-12', 7, '09127777777', 'zahra.moradi@example.com'),
(1008, N'حسین', N'قاسمی', 1, 10459, 1, 1, 1, '1986-12-05', 8, '09128888888', 'hossein.ghasemi@example.com'),
(1009, N'نرگس', N'فلاح', 2, 10460, 0, 1, 0, '1996-04-18', 9, '09129999999', 'narges.fallah@example.com'),
(1010, N'پویا', N'ابراهیمی', 2, 10461, 1, 1, 1, '1993-10-22', 10, '09120000000', 'pouya.ebrahimi@example.com');

-- PersonPosition (IsLast = 1)
INSERT INTO [prj].[PersonPosition] ([PersonPositionId], [PersonId], [PositionId], [StartDate], [IsLast]) VALUES
(1, 1001, 204, '2022-01-01', 1),
(2, 1002, 203, '2022-03-01', 1),
(3, 1003, 202, '2021-06-01', 1),
(4, 1004, 205, '2020-09-01', 1),
(5, 1005, 102, '2023-01-15', 1),
(6, 1006, 101, '2019-04-01', 1),
(7, 1007, 302, '2021-11-01', 1),
(8, 1008, 401, '2018-05-10', 1),
(9, 1009, 402, '2023-05-01', 1),
(10, 1010, 303, '2022-08-15', 1);
