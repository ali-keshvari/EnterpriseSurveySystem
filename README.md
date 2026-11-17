# سامانه جامع مدیریت نظرسنجی و ارزیابی سازمانی (Enterprise Survey & Questionnaire System)

سامانه سازمانی توزیع پرسش‌نامه، مدیریت لینک‌های یکبارمصرف و پایش زمانی پاسخ‌دهی طراحی‌شده بر پایه **ASP.NET Framework 4.8 MVC**، **Entity Framework 6.2.0**، **SQL Server** و سیستم طراحی مدرن **Stitch (RTL-Native)**.

---

## ۱. گزارش فازهای اجرایی سیستم چندعاملی (Multi-Agent Architecture)

### 🔹 فاز اول: عامل زیرساخت و محیط (.NET & Infra Agent)
- بررسی موفقیت‌آمیز نسخه .NET Framework 4.8 در رجیستری ویندوز (`Release: 533325`، نسخه `4.8.09037`).
- اعتبارسنجی ابزارهای توسعه، MSBuild نسخه ۱۷.۱۴ Enterprise و دسترسی به Reference Assemblies فریم‌ورک ۴.۸.
- استقرار پکیج‌های رسمی `EntityFramework 6.2.0`، `Microsoft.AspNet.Mvc 5.2.9` و `Newtonsoft.Json 13.0.3`.
- ذخیره‌سازی و بومی‌سازی **۱۰۰٪ محلی** تمامی فایل‌های استاتیک بدون وابستگی به CDN خارجی:
  - فایل جاوااسکریپت Tailwind در مسیر `Scripts/tailwind.min.js`
  - فونت استاندارد وزیرمتن در مسیر `Content/fonts/vazirmatn.woff2` و استایل `vazirmatn.css`
  - آیکون‌های Material Symbols در مسیر `Content/fonts/material-symbols-outlined.woff2` و استایل `material-symbols.css`
  - فایل استایل یکپارچه سامانه در مسیر `Content/site.css`
- پیکربندی و راه‌اندازی سرور وب IIS Express روی پورت ۵۰۵۵.

### 🔹 فاز دوم: عامل پایگاه داده و معماری (Database & Architecture Agent - Database-First)
- معماری کاملاً **Database-First**؛ مدل داده‌ای در SQL Server از طریق اسکریپت ساخت دیتابیس (`CreateDatabase.sql`) مدیریت می‌شود و Code-First Initializer غیرفعال شده است (`Database.SetInitializer<SurveyDbContext>(null)`).
- **عدم وجود هیچ‌گونه داده پیش‌فرض یا تستی (Zero Dummy Data)**: پایگاه داده با جداول کاملاً پاک و آماده استفاده عملیاتی تحویل شده است.
- جداول طراحی‌شده:
  1. `Questionnaires`: عنوان، توضیحات، تعداد هدف مخاطبان، محدودیت زمانی (دقیقه)، زمان ایجاد.
  2. `Questions`: متن سوال، نوع (Testy, MultiSelect, Boolean, Select, Tashrihi)، گزینه‌ها (JSON)، ترتیب و اجباری بودن.
  3. `RespondentTokens`: شناسه توکن یکتا (GUID)، وضعیت استفاده (`IsUsed`)، تاریخ‌های بازگشایی اولیه (`OpenedAt`)، انقضا (`ExpiresAt`) و ثبت نهایی (`CompletedAt`).
  4. `Answers`: ثبت پاسخ‌های مرتبط با هر توکن و سوال.
  5. `AuditLogs`: رویدادنگاری تفصیلی با مهر زمانی دقیق، آدرس IP و نوع عملیات (`PageOpened`, `FormSubmitted`, `TokenExpired`, `AccessDenied`, `TokensGenerated`).
- اسکریپت دیتابیس در مسیر [CreateDatabase.sql](file:///d:/Antigravity/EnterpriseSurveySystem/EnterpriseSurveySystem/Database/CreateDatabase.sql) تعبیه شده است.

### 🔹 فاز سوم: عامل تخصصی بک‌اند (Backend MVC Specialist)
- **قانون لینک تک‌بارمصرف (Single-Use Link):**
  - در اولین بازگشایی لینک (`/Survey/Take/{token}`)، وضعیت `OpenedAt` با ساعت دقیق سرور مقداردهی می‌شود.
  - در صورت وجود محدودیت زمانی، زمان انقضا بر پایه فرمول `ExpiresAt = OpenedAt.AddMinutes(TimeLimitMinutes)` محاسبه و به صورت اتمیک در دیتابیس ذخیره می‌گردد.
  - در صورت اتمام مهلت یا ورود مجدد پس از ثبت، رویداد در `AuditLogs` ثبت شده و دسترسی مسدود می‌شود.
- **اعتبارسنجی سمت سرور زمان (Timer Enforcement):**
  - پیش از ثبت نهایی پاسخ‌ها، صحت شرط `DateTime.Now <= ExpiresAt` در تراکنش دیتابیس بررسی می‌شود. در صورت رد زمان، تراکنش لغو شده و وضعیت `TokenExpired` ثبت می‌گردد.
- **داشبورد و تحلیل آماری:**
  - کنترلر مدیریت برای ایجاد پرسش‌نامه، تولید خودکار توکن‌های رمزنگاری‌شده، گزارش‌گیری تفکیکی و خروجی اکسل (CSV).

### 🔹 فاز چهارم: عامل امنیت و یکپارچه‌سازی رابط کاربری (UI Integration & Security Agent)
- ادغام ۵ صفحه طراحی‌شده از پکیج Stitch:
  - **داشبورد و آمار کل:** کارت‌های شاخص‌های کلیدی عملکرد (KPI)، نوار پیشرفت و جدول پرسش‌نامه‌ها.
  - **سازنده فرم (`_1`):** تعریف دینامیک سوالات، گزینه‌ها و تنظیمات اجرایی.
  - **مدیریت مخاطبان (`_2`):** نشان‌های رنگی وضعیت («در انتظار»، «مشاهده شده»، «تکمیل شده»، «منقضی شده»)، کپی آسان لینک و تولید توکن.
  - **گزارش‌های تحلیلی و لاگ‌های زمانی (`_3`):** نمودار توزیع زمان پاسخ‌گویی به دقیقه و ردگیری وقایع امنیتی.
  - **محیط پاسخ‌دهی داوطلب (`_4` و `_5`):** چیدمان کاملاً راست‌به‌چپ (RTL)، تایمر ثانیه‌شمار هماهنگ با سرور و به‌روزرسانی لحظه‌ای نوار پیشرفت.

---

## ۲. ساختار سورس‌کد و مسیر فایل‌ها

```
d:\Antigravity\EnterpriseSurveySystem\
│   EnterpriseSurveySystem.sln          # فایل Solution ویژوال استودیو
│   StartServer.bat                     # اسکریپت اجرای سریع IIS Express
│
└───EnterpriseSurveySystem\
    │   EnterpriseSurveySystem.csproj   # پروژه C# مبتنی بر .NET Framework 4.8
    │   Web.config                      # تنظیمات اتصال به SQL Server و نگاشت پکیج‌ها
    │   Global.asax.cs                  # رجیستری روت‌ها و Seed داده‌های اولیه
    │
    ├───Database\
    │       CreateDatabase.sql          # اسکریپت ساخت دیتابیس و جداول
    │
    ├───Data\
    │       SurveyDbContext.cs          # کانتکست EF 6.2.0
    │       DatabaseInitializer.cs      # سید خودکار پرسش‌نامه تستی و توکن‌ها
    │
    ├───Models\
    │   ├───Entities\                   # موجودیت‌های پایگاه داده
    │   └───ViewModels\                # مدل‌های انتقال داده فرم‌ها و داشبورد
    │
    ├───Services\
    │       ISurveyService.cs           # اینترفیس منطق تجاری نظرسنجی
    │       SurveyService.cs            # پیاده‌سازی سرویس با تراکنش‌های اتمیک
    │       IAuditService.cs / AuditService.cs
    │
    ├───Controllers\
    │       SurveyController.cs         # دریافت و ثبت پاسخ‌های آزمون
    │       AdminController.cs          # مدیریت پرسش‌نامه‌ها، لینک‌ها و لاگ‌ها
    │       HomeController.cs
    │
    └───Views\
        ├───Shared\                     # قالب‌های اصلی و پاسخ‌دهی Stitch
        ├───Admin\                      # صفحات Index, Create, Details, Analytics, AuditLogs
        └───Survey\                     # صفحات Take, Completed, Expired, Invalid
```

---

## ۳. نحوه اجرا و تست زنده

۱. **اجرای مستقیم سرور:** فایل [StartServer.bat](file:///d:/Antigravity/EnterpriseSurveySystem/StartServer.bat) را اجرا نمایید یا در ویژوال استودیو، Solution را باز کرده و F5 بزنید.
۲. **ورود به پنل مدیریت:** مرورگر خود را به آدرس `http://localhost:5055/Admin` هدایت فرمایید.
۳. **تست پاسخ‌دهی زنده:** از جدول مخاطبان، روی دکمه «تست ورود» برای هر توکن کلیک نموده یا از لینک‌های اختصاصی استفاده فرمایید.
