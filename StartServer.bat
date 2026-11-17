@echo off
chcp 65001 > nul
echo =========================================================================
echo    سامانه مدیریت نظرسنجی و ارزیابی سازمانی (Enterprise Survey System)
echo    ASP.NET Framework 4.8 / EF 6.2.0 / SQL Server
echo =========================================================================
echo.
echo در حال راه‌اندازی سرور محلی بر روی پورت 5055 ...
echo آدرس پنل مدیریت: http://localhost:5055/Admin
echo.

if exist "C:\Program Files\IIS Express\iisexpress.exe" (
    "C:\Program Files\IIS Express\iisexpress.exe" /path:"%~dp0EnterpriseSurveySystem" /port:5055 /clr:v4.0
) else (
    echo [خطا] IIS Express یافت نشد. لطفاً پروژه را در Visual Studio باز فرمایید.
    pause
)
