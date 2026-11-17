import os
import docx
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import qn, nsdecls

def create_element(name):
    return OxmlElement(name)

def set_cell_rtl(cell):
    tcPr = cell._tc.get_or_add_tcPr()
    tcBidi = parse_xml(r'<w:tcBidi xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>')
    tcPr.append(tcBidi)

def set_cell_background(cell, hex_color):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>')
    tcPr.append(shd)

def set_paragraph_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT):
    p.alignment = align
    pPr = p._p.get_or_add_pPr()
    bidi = OxmlElement('w:bidi')
    bidi.set(qn('w:val'), '1')
    pPr.append(bidi)

def add_rtl_heading(doc, text, level):
    h = doc.add_heading(level=level)
    set_paragraph_rtl(h, WD_ALIGN_PARAGRAPH.RIGHT)
    run = h.add_run(text)
    run.font.name = 'Vazirmatn'
    rPr = run._r.get_or_add_rPr()
    rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} w:ascii="Vazirmatn" w:hAnsi="Vazirmatn" w:cs="Vazirmatn"/>')
    rPr.append(rFonts)
    
    if level == 1:
        run.font.size = Pt(17)
        run.font.color.rgb = RGBColor(79, 70, 229) # Indigo 600
        run.bold = True
        h.paragraph_format.space_before = Pt(18)
        h.paragraph_format.space_after = Pt(8)
    elif level == 2:
        run.font.size = Pt(13.5)
        run.font.color.rgb = RGBColor(30, 41, 59) # Slate 800
        run.bold = True
        h.paragraph_format.space_before = Pt(14)
        h.paragraph_format.space_after = Pt(6)
    elif level == 3:
        run.font.size = Pt(11.5)
        run.font.color.rgb = RGBColor(71, 85, 105) # Slate 600
        run.bold = True
        h.paragraph_format.space_before = Pt(10)
        h.paragraph_format.space_after = Pt(4)
    return h

def add_rtl_p(doc, text="", bold=False, italic=False, color=None, size_pt=10.5, space_after=6, line_spacing=1.25):
    p = doc.add_paragraph()
    set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
    p.paragraph_format.space_after = Pt(space_after)
    p.paragraph_format.line_spacing = line_spacing
    if text:
        run = p.add_run(text)
        run.font.name = 'Vazirmatn'
        rPr = run._r.get_or_add_rPr()
        rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} w:ascii="Vazirmatn" w:hAnsi="Vazirmatn" w:cs="Vazirmatn"/>')
        rPr.append(rFonts)
        run.font.size = Pt(size_pt)
        run.bold = bold
        run.italic = italic
        if color:
            run.font.color.rgb = color
    return p

def append_rtl_run(p, text, bold=False, italic=False, color=None, size_pt=10.5, font_name='Vazirmatn'):
    run = p.add_run(text)
    run.font.name = font_name
    rPr = run._r.get_or_add_rPr()
    rFonts = parse_xml(f'<w:rFonts {nsdecls("w")} w:ascii="{font_name}" w:hAnsi="{font_name}" w:cs="{font_name}"/>')
    rPr.append(rFonts)
    run.font.size = Pt(size_pt)
    run.bold = bold
    run.italic = italic
    if color:
        run.font.color.rgb = color
    return run

def add_code_block(doc, code_text):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    set_cell_background(cell, "F1F5F9") # Slate 100
    
    # Border
    tcPr = cell._tc.get_or_add_tcPr()
    tcBorders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:top w:val="single" w:sz="6" w:space="0" w:color="CBD5E1"/>'
        f'<w:left w:val="single" w:sz="6" w:space="0" w:color="CBD5E1"/>'
        f'<w:bottom w:val="single" w:sz="6" w:space="0" w:color="CBD5E1"/>'
        f'<w:right w:val="single" w:sz="24" w:space="0" w:color="4F46E5"/>' # Accent right border
        f'</w:tcBorders>'
    )
    tcPr.append(tcBorders)
    
    p = cell.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT # Code in LTR
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.line_spacing = 1.15
    run = p.add_run(code_text)
    run.font.name = 'Consolas'
    run.font.size = Pt(9.5)
    run.font.color.rgb = RGBColor(15, 23, 42)
    doc.add_paragraph().paragraph_format.space_after = Pt(4)

def add_callout(doc, title, text, box_type="info"):
    colors = {
        "info": ("EEF2FF", "4F46E5", "نکته مهم: "),
        "warning": ("FFFBEB", "D97706", "هشدار امنیتی: "),
        "success": ("ECFDF5", "059669", "توصیه اجرایی: ")
    }
    bg_color, border_color, prefix = colors.get(box_type, colors["info"])
    
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    set_cell_rtl(cell)
    set_cell_background(cell, bg_color)
    
    tcPr = cell._tc.get_or_add_tcPr()
    tcBorders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:top w:val="none"/>'
        f'<w:left w:val="none"/>'
        f'<w:bottom w:val="none"/>'
        f'<w:right w:val="single" w:sz="24" w:space="0" w:color="{border_color}"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(tcBorders)
    
    p = cell.paragraphs[0]
    set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.line_spacing = 1.25
    
    append_rtl_run(p, prefix + title + " - ", bold=True, color=RGBColor(15, 23, 42), size_pt=10)
    append_rtl_run(p, text, bold=False, color=RGBColor(51, 65, 85), size_pt=9.5)
    doc.add_paragraph().paragraph_format.space_after = Pt(4)

print("Starting document creation...")
doc = Document()

# Page Margins
sections = doc.sections
for s in sections:
    s.top_margin = Inches(0.9)
    s.bottom_margin = Inches(0.9)
    s.left_margin = Inches(0.9)
    s.right_margin = Inches(0.9)

# ----------------- COVER / TITLE SECTION -----------------
title_p = doc.add_paragraph()
set_paragraph_rtl(title_p, WD_ALIGN_PARAGRAPH.CENTER)
title_p.paragraph_format.space_before = Pt(36)
title_p.paragraph_format.space_after = Pt(8)
run_title = append_rtl_run(title_p, "راهنمای جامع استقرار و اجرای سامانه نظرسنجی سازمانی", bold=True, size_pt=22, color=RGBColor(79, 70, 229))

sub_p = doc.add_paragraph()
set_paragraph_rtl(sub_p, WD_ALIGN_PARAGRAPH.CENTER)
sub_p.paragraph_format.space_after = Pt(24)
append_rtl_run(sub_p, "مستندات گام‌به‌گام راه‌اندازی بر روی سرور عملیاتی و سیستم‌های کلاینت (Production & Staging)", bold=False, size_pt=12.5, color=RGBColor(100, 116, 139))

# Metadata Table
meta_tbl = doc.add_table(rows=4, cols=2)
meta_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
meta_data = [
    ("نام سامانه:", "سامانه مدیریت نظرسنجی و ارزیابی عملکرد سازمانی (پرسش‌نما)"),
    ("فناوری‌های هسته:", "ASP.NET MVC 5.2.9 (.NET Framework 4.8) | Entity Framework 6.2.0 | SQL Server"),
    ("معماری پایگاه داده:", "Database-First (اسکریپت خالص SQL بدون داده اولیه / Zero Mock Data)"),
    ("نسخه مستند و تاریخ نگارش:", "نسخه ۱.۰ سازمانی | آبان‌ماه ۱۴۰۵")
]

tblPr = meta_tbl._tbl.tblPr
tblPr.append(parse_xml(r'<w:bidiVisual xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>'))

for idx, (label, val) in enumerate(meta_data):
    row = meta_tbl.rows[idx]
    
    cell_lbl = row.cells[0]
    set_cell_rtl(cell_lbl)
    set_cell_background(cell_lbl, "F8FAFC")
    p_lbl = cell_lbl.paragraphs[0]
    set_paragraph_rtl(p_lbl, WD_ALIGN_PARAGRAPH.RIGHT)
    append_rtl_run(p_lbl, label, bold=True, size_pt=9.5, color=RGBColor(30, 41, 59))
    
    cell_val = row.cells[1]
    set_cell_rtl(cell_val)
    set_cell_background(cell_val, "FFFFFF")
    p_val = cell_val.paragraphs[0]
    set_paragraph_rtl(p_val, WD_ALIGN_PARAGRAPH.RIGHT)
    append_rtl_run(p_val, val, bold=False, size_pt=9.5, color=RGBColor(51, 65, 85))

doc.add_page_break()

# ----------------- SECTION 1: REQUIREMENTS -----------------
add_rtl_heading(doc, "۱. پیش‌نیازهای سخت‌افزاری و نرم‌افزاری سرور", level=1)

p = add_rtl_p(doc, "برای میزبانی و استقرار بدون اختلال این سامانه بر روی سرور یا رایانه جدید، ملزومات زیر بایستی از پیش تأمین و فعال گردند:")

req_tbl = doc.add_table(rows=6, cols=3)
req_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
req_tbl._tbl.tblPr.append(parse_xml(r'<w:bidiVisual xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>'))

headers = ["عنصر زیرساختی", "حداقل نسخه مورد نیاز", "توضیحات و نیازمندی‌های ویژه"]
for c_idx, h_text in enumerate(headers):
    c = req_tbl.cell(0, c_idx)
    set_cell_rtl(c)
    set_cell_background(c, "4F46E5")
    p = c.paragraphs[0]
    set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
    append_rtl_run(p, h_text, bold=True, size_pt=10, color=RGBColor(255, 255, 255))

req_rows = [
    ("سیستم عامل سرور", "Windows Server 2016 یا بالاتر (یا Windows 10/11 Pro/Enterprise)", "معماری 64-Bit، دسترسی Administrator"),
    (".NET Framework", ".NET Framework 4.8 یا 4.8.1", "شامل Developer Pack و Runtime"),
    ("پایگاه داده (RDBMS)", "Microsoft SQL Server 2016+", "نسخه‌های Enterprise، Standard، Express یا LocalDB"),
    ("وب‌سرور (Web Server)", "IIS (Internet Information Services) 10.0+", "فعال بودن ماژول ASP.NET 4.8 و ویژگی‌های HTTP Redirection"),
    ("سخت‌افزار حداقلی", "پردازنده ۲ هسته‌ای، ۴ گیگابایت رم، ۱۰ گیگابایت فضای دیسک", "SSD NVMe برای عملکرد سریع‌تر پایگاه داده پیشنهاد می‌شود")
]

for r_idx, (col1, col2, col3) in enumerate(req_rows, start=1):
    row = req_tbl.rows[r_idx]
    bg = "F8FAFC" if r_idx % 2 == 1 else "FFFFFF"
    for c_idx, val in enumerate([col1, col2, col3]):
        c = row.cells[c_idx]
        set_cell_rtl(c)
        set_cell_background(c, bg)
        p = c.paragraphs[0]
        set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
        append_rtl_run(p, val, bold=(c_idx==0), size_pt=9.5, color=RGBColor(15, 23, 42))

add_callout(doc, "بررسی خودکار پیش‌نیازها", "جهت بررسی سریع وضعیت نصب .NET Framework 4.8 بر روی سرور، دستور زیر را در PowerShell اجرا فرمایید:", "info")
add_code_block(doc, 'Get-ItemProperty "HKLM:\\SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full" | Select-Object Release, Version')

# ----------------- SECTION 2: DATABASE SETUP -----------------
add_rtl_heading(doc, "۲. آماده‌سازی و راه‌اندازی پایگاه داده (معماری Database-First)", level=1)

add_rtl_p(doc, "این سامانه بر پایه معماری خالص دیتابیس-محور (Database-First) توسعه داده شده است؛ به این معنا که ساختار جداول به صورت کامل و دقیق در اسکریپت SQL پیاده‌سازی شده و هیچ‌گونه وابستگی به Migrations خودکار EF وجود ندارد و هیچ دیتای فرضی یا آزمایشی در جداول قرار ندارد.")

add_rtl_heading(doc, "مراحل ساخت دیتابیس در SQL Server:", level=2)

steps_db = [
    "۱. وارد نرم‌افزار SQL Server Management Studio (SSMS) شوید و با دسترسی مدیریتی (Windows Authentication یا sa) به دیتابیس سرور متصل شوید.",
    "۲. از منوی File -> Open -> File، اسکریپت ساخت پایگاه داده را از مسیر زیر در پروژه باز کنید:",
    "   EnterpriseSurveySystem\\Database\\CreateDatabase.sql",
    "۳. بر روی کلید Execute (یا فشردن کلید F5) کلیک فرمایید تا پایگاه داده EnterpriseSurveyDb به همراه ۵ جدول اصلی، کلیدهای خارجی، شاخص‌های یکتایی و روابط آبشاری ایجاد شوند.",
    "۴. جهت اطمینان از سلامت ساختار، دستور زیر را اجرا فرمایید تا اطمینان حاصل شود تمام جداول ۵گانه در حالت پاک و خالی ایجاد شده‌اند:"
]

for s in steps_db:
    add_rtl_p(doc, s, size_pt=10, space_after=4)

add_code_block(doc, '''USE [EnterpriseSurveyDb];
GO
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
-- خروجی مورد انتظار: Questionnaires, Questions, RespondentTokens, Answers, AuditLogs''')

add_rtl_heading(doc, "تشریح جداول ۵گانه سیستم:", level=2)

tbl_struct = doc.add_table(rows=6, cols=3)
tbl_struct.alignment = WD_TABLE_ALIGNMENT.CENTER
tbl_struct._tbl.tblPr.append(parse_xml(r'<w:bidiVisual xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>'))

struct_headers = ["نام جدول", "وظیفه و نقش تجاری", "فیلدهای کلیدی"]
for c_idx, h_text in enumerate(struct_headers):
    c = tbl_struct.cell(0, c_idx)
    set_cell_rtl(c)
    set_cell_background(c, "312E81") # Indigo 900
    p = c.paragraphs[0]
    set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
    append_rtl_run(p, h_text, bold=True, size_pt=10, color=RGBColor(255, 255, 255))

struct_data = [
    ("Questionnaires", "تعریف شناسنامه پرسش‌نامه‌های سازمانی", "Id, Title, TargetRespondentsCount, TimeLimitMinutes, CreatedAt"),
    ("Questions", "بانک سوالات وابسته به هر پرسش‌نامه", "Id, QuestionnaireId, QuestionText, QuestionType, Options, OrderIndex, IsRequired"),
    ("RespondentTokens", "لینک‌های یکبارمصرف اختصاصی مخاطبان", "Id, TokenGuid (یکتا و ایندکس), IsUsed, OpenedAt, CompletedAt, ExpiresAt, RowVersion"),
    ("Answers", "پاسخ‌های ثبت‌شده داوطلبان به تفکیک سوال", "Id, TokenId, QuestionId, AnswerValue, CreatedAt"),
    ("AuditLogs", "ثبت وقایع زمانی، امنیتی و آی‌پی کاربران", "Id, TokenId, QuestionnaireId, ActionType, Timestamp, IPAddress, Details")
]

for r_idx, (col1, col2, col3) in enumerate(struct_data, start=1):
    row = tbl_struct.rows[r_idx]
    bg = "F8FAFC" if r_idx % 2 == 1 else "FFFFFF"
    for c_idx, val in enumerate([col1, col2, col3]):
        c = row.cells[c_idx]
        set_cell_rtl(c)
        set_cell_background(c, bg)
        p = c.paragraphs[0]
        set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
        append_rtl_run(p, val, bold=(c_idx==0), size_pt=9.5, color=RGBColor(15, 23, 42))

# ----------------- SECTION 3: CONFIGURATION -----------------
add_rtl_heading(doc, "۳. پیکربندی رشته اتصال در Web.config", level=1)

add_rtl_p(doc, "فایل Web.config در ریشه پروژه قرار دارد. برای اتصال سامانه به دیتابیس سرور مقصد، بخش connectionStrings را متناسب با محیط سرور ویرایش فرمایید:")

add_rtl_heading(doc, "حالت اول: اتصال با احراز هویت ویندوز (Windows Authentication / Integrated Security)", level=2)
add_code_block(doc, '''<connectionStrings>
  <add name="EnterpriseSurveyConnection" 
       connectionString="Data Source=.;Initial Catalog=EnterpriseSurveyDb;Integrated Security=True;MultipleActiveResultSets=True;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>''')

add_rtl_heading(doc, "حالت دوم: اتصال از طریق سرور مجزا یا کاربر اختصاصی (SQL Server Authentication)", level=2)
add_code_block(doc, '''<connectionStrings>
  <add name="EnterpriseSurveyConnection" 
       connectionString="Data Source=192.168.1.100;Initial Catalog=EnterpriseSurveyDb;User ID=survey_user;Password=YourStrongPassword#2026;MultipleActiveResultSets=True;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>''')

add_callout(doc, "مجوز دسترسی کاربر پایگاه داده", "کاربر دیتابیس بایستی حداقل دارای رول‌های db_datareader و db_datawriter بر روی پایگاه داده EnterpriseSurveyDb باشد.", "warning")

# ----------------- SECTION 4: DEPLOYMENT METHODS -----------------
add_rtl_heading(doc, "۴. روش‌های استقرار و اجرای سامانه بر روی سرور", level=1)

add_rtl_heading(doc, "روش الف: استقرار دائمی و عملیاتی بر روی وب‌سرور IIS (پیشنهادی جهت سرور اصلی)", level=2)

iis_steps = [
    "۱. فعال‌سازی رول‌های IIS: در سرور ویندوز، وارد Server Manager شوید و از بخش Add Roles and Features، رول Web Server (IIS) را به همراه موارد زیر فعال نمایید:",
    "   • .NET Framework 4.8 Features -> ASP.NET 4.8",
    "   • Web Server -> Application Development -> ASP.NET 4.8, .NET Extensibility 4.8, ISAPI Extensions, ISAPI Filters",
    "   • Common HTTP Features -> Default Document, Directory Browsing, Static Content",
    "۲. کپی سورس‌کد: کل پوشه EnterpriseSurveySystem را در مسیر دائمی سرور کپی نمایید (مثال: C:\\inetpub\\wwwroot\\EnterpriseSurveySystem).",
    "۳. اعطای دسترسی به پوشه: بر روی پوشه پروژه کلیک‌راست کرده، به تب Security رفته و به گروه‌های IIS_IUSRS و IUSR دسترسی Read & Execute و Write (جهت ثبت لاگ در صورت نیاز) اعطا فرمایید.",
    "۴. ایجاد Application Pool در IIS:",
    "   • وارد IIS Manager شوید.",
    "   • در منوی سمت چپ بر روی Application Pools کلیک‌راست کرده و Add Application Pool را برگزینید.",
    "   • نام آن را SurveyAppPool قرار داده، .NET CLR Version را بر روی v4.0.30319 و Managed Pipeline Mode را بر روی Integrated تنظیم کنید.",
    "۵. ایجاد وب‌سایت (Website):",
    "   • بر روی Sites کلیک‌راست کرده و Add Website را بزنید.",
    "   • Site Name: EnterpriseSurveySystem",
    "   • Application Pool: SurveyAppPool (انتخاب پولی که در مرحله قبل ساختید)",
    "   • Physical path: مسیر فیزیکی پروژه (مثال: C:\\inetpub\\wwwroot\\EnterpriseSurveySystem)",
    "   • Port: پورت مورد نظر (مثلاً پورت 80 یا پورت اختصاصی مانند 5055)",
    "۶. پیکربندی فایروال سرور (Windows Defender Firewall):",
    "   • یک Inbound Rule جدید برای پورت انتخابی (TCP) ایجاد کنید تا دسترسی کلاینت‌ها در شبکه درون‌سازمانی برقرار شود."
]

for s in iis_steps:
    add_rtl_p(doc, s, size_pt=9.5, space_after=3)

add_rtl_heading(doc, "روش ب: اجرای سریع تستی/آزمایشی با IIS Express (بدون نیاز به کانفیگ IIS کامل)", level=2)
add_rtl_p(doc, "یک فایل اجرایی آماده به نام StartServer.bat در ریشه پوشه پروژه تعبیه شده است. با دو بار کلیک بر روی این فایل، سرور لوکال بر روی پورت 5055 بلافاصله راه‌اندازی می‌گردد:")

add_code_block(doc, '''D:\\Antigravity\\EnterpriseSurveySystem\\StartServer.bat''')
add_rtl_p(doc, "این روش برای تست سریع سناریوها در سیستم توسعه‌دهنده یا نمایش به کارفرما بسیار سریع و ایده‌آل است.")

add_rtl_heading(doc, "روش ج: استقرار از طریق Publish در Visual Studio 2022", level=2)
add_rtl_p(doc, "در صورتی که سورس‌کد را در ویژوال استودیو باز نموده‌اید:", size_pt=9.5)
vs_steps = [
    "۱. فایل EnterpriseSurveySystem.sln را در Visual Studio باز فرمایید.",
    "۲. بر روی پروژه کلیک‌راست کرده و گزینه Publish را انتخاب نمایید.",
    "۳. مقصد (Target) را Folder انتخاب کرده و یک پوشه خروجی مشخص کنید.",
    "۴. دکمه Publish را کلیک کنید تا تمام فایل‌های کامپایل‌شده (DLLها، ویوها و استایل‌ها) در پوشه مقصد قرار گیرند و سپس آن پوشه را به سرور منتقل نمایید."
]
for s in vs_steps:
    add_rtl_p(doc, s, size_pt=9.5, space_after=2)

# ----------------- SECTION 5: VERIFICATION -----------------
add_rtl_heading(doc, "۵. آزمون اعتبارسنجی و تست صحت کارکرد سامانه پس از استقرار", level=1)

add_rtl_p(doc, "پس از استقرار سرور، مراحل زیر را جهت حصول اطمینان کامل از کارکرد سامانه تست نمایید:")

test_cases = [
    ("ورود به داشبورد مدیریت", "مرورگر را باز کرده و به آدرس http://<Server-IP>:<Port>/Admin مراجعه فرمایید. داشبورد با آمار اولیه، کارت‌های KPI و جدول خالی پرسش‌نامه‌ها به درستی نمایش می‌یابد.", "HTTP 200"),
    ("تعریف پرسش‌نامه جدید", "از دکمه «تعریف پرسش‌نامه جدید» (/Admin/Create) استفاده کرده، یک پرسش‌نامه آزمایشی با محدودیت زمانی ۵ دقیقه و تعداد مخاطبان ۱۰ نفر ایجاد نمایید.", "تولید خودکار توکن‌ها"),
    ("مدیریت لینک‌های اختصاصی", "وارد صفحه جزئیات پرسش‌نامه (/Admin/Details/ID) شوید. ۱۰ توکن یکتا همراه با کلید «کپی لینک» و «تست ورود» تولید شده و نشان وضعیت همگی «در انتظار» است.", "تأیید لینک‌های یکتا"),
    ("تست آزمون داوطلب و تایمر", "یکی از لینک‌ها را در پنجره جدید باز کنید (/Survey/Take/TokenGuid). تایمر ۵ دقیقه‌ای ثانیه‌شمار فعال شده و نوار پیشرفت با انتخاب گزینه‌ها به‌روز می‌شود.", "هماهنگی دقیق با سرور"),
    ("بررسی لاگ‌های امنیتی", "به صفحه لاگ‌ها (/Admin/AuditLogs) بروید. ورود مخاطب، بازگشایی لینک، انقضا و ارسال نهایی با ساعت دقیق و آدرس IP کلاینت ثبت شده است.", "ثبت اتمیک در دیتابیس")
]

test_tbl = doc.add_table(rows=6, cols=3)
test_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
test_tbl._tbl.tblPr.append(parse_xml(r'<w:bidiVisual xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>'))

test_headers = ["مرحله آزمون", "شرح عملیات مورد انتظار", "شاخص پذیرش"]
for c_idx, h_text in enumerate(test_headers):
    c = test_tbl.cell(0, c_idx)
    set_cell_rtl(c)
    set_cell_background(c, "065F46") # Emerald 800
    p = c.paragraphs[0]
    set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
    append_rtl_run(p, h_text, bold=True, size_pt=10, color=RGBColor(255, 255, 255))

for r_idx, (col1, col2, col3) in enumerate(test_cases, start=1):
    row = test_tbl.rows[r_idx]
    bg = "ECFDF5" if r_idx % 2 == 1 else "FFFFFF"
    for c_idx, val in enumerate([col1, col2, col3]):
        c = row.cells[c_idx]
        set_cell_rtl(c)
        set_cell_background(c, bg)
        p = c.paragraphs[0]
        set_paragraph_rtl(p, WD_ALIGN_PARAGRAPH.RIGHT)
        append_rtl_run(p, val, bold=(c_idx==0), size_pt=9.5, color=RGBColor(15, 23, 42))

# ----------------- SECTION 6: BEST PRACTICES -----------------
add_rtl_heading(doc, "۶. الزامات امنیتی و پایداری در محیط عملیاتی (Production Guidelines)", level=1)

bps = [
    ("فعال‌سازی پروتکل امن HTTPS (SSL/TLS):", " جهت حفظ حریم خصوصی پاسخ‌ها و جلوگیری از استراق سمع توکن‌های محرمانه در شبکه، حتماً یک گواهی SSL بر روی سایت IIS بایند فرمایید."),
    ("پشتیبان‌گیری زمان‌بندی‌شده (SQL Server Maintenance Plan):", " از طریق SQL Server Agent یک جاب روزانه برای Full Backup و نگهداری فایل‌های پشتیبان روی سرور مجزا ایجاد کنید."),
    ("پیکربندی فایروال شبکه (Firewall Isolation):", " تنها پورت‌های وب (80 یا 443) به روی کلاینت‌ها باز باشد و پورت ۱۴۳۳ دیتابیس صرفاً از طریق Localhost یا آی‌پی وب‌سرور قابل دسترسی باشد."),
    ("تنظیمات Session و Concurrency:", " سامانه به صورت خودکار از قفل همزمانی RowVersion بر روی جدول RespondentTokens و تراکنش‌های Database.BeginTransaction استفاده می‌کند تا از سناریوی Race Condition (پاسخ‌دهی همزمان با یک لینک) ممانعت شود.")
]

for title, desc in bps:
    p = add_rtl_p(doc, space_after=3)
    append_rtl_run(p, "• " + title, bold=True, color=RGBColor(79, 70, 229), size_pt=10)
    append_rtl_run(p, desc, bold=False, color=RGBColor(30, 41, 59), size_pt=9.5)

# ----------------- SECTION 7: TROUBLESHOOTING -----------------
add_rtl_heading(doc, "۷. راهنمای عیب‌یابی و خطاهای متداول (Troubleshooting)", level=1)

troubles = [
    ("خطای HTTP 500.19 (Internal Server Error) در IIS", "دلیل: رول ASP.NET 4.8 یا ماژول URL Rewrite نصب نشده است.\nراهکار: در Server Manager مطمئن شوید ویژگی .NET 4.8 Features -> ASP.NET 4.8 تیک خورده باشد."),
    ("خطای Cannot open database 'EnterpriseSurveyDb' / Login failed", "دلیل: کاربر IIS (IIS_IUSRS یا ApplicationPoolIdentity) در دیتابیس لاگین ندارد یا Connection String اشتباه است.\nراهکار: فایل Web.config را بررسی کنید یا دسترسی کاربر IIS به دیتابیس را در SSMS بررسی فرمایید."),
    ("خطای لود نشدن آیکون‌ها یا فونت‌ها در مرورگرهای قدیمی", "دلیل: عدم رجیستر بودن فرمت .woff2 در وب‌سرور.\nراهکار: در فایل Web.config تگ staticContent برای پسوند .woff2 با مقدار application/font-woff2 تعبیه شده و به درستی عمل می‌کند."),
    ("خطای انقضای لینک حتی در اولین ورود", "دلیل: ناهماهنگی زمانی ساعت کلاینت با ساعت سرور.\nراهکار: ساعت و TimeZone سرور (معمولاً Iran Standard Time یا UTC+03:30) را هماهنگ و بررسی فرمایید.")
]

for title, desc in troubles:
    p = add_rtl_p(doc, space_after=3)
    append_rtl_run(p, "❌ " + title, bold=True, color=RGBColor(185, 28, 28), size_pt=10)
    p_desc = add_rtl_p(doc, space_after=6)
    append_rtl_run(p_desc, desc, bold=False, color=RGBColor(51, 65, 85), size_pt=9.5)

# Save Document
output_path = r'd:\Antigravity\EnterpriseSurveySystem\راهنمای_استقرار_سامانه_نظرسنجی.docx'
doc.save(output_path)
print(f"Document saved successfully at: {output_path}")

output_path_en = r'd:\Antigravity\EnterpriseSurveySystem\Deployment_Guide.docx'
doc.save(output_path_en)
print(f"English named copy saved at: {output_path_en}")
