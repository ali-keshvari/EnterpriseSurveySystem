using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using EnterpriseSurveySystem.Models.ViewModels;
using EnterpriseSurveySystem.Services;

namespace EnterpriseSurveySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ISurveyService _surveyService;
        private readonly IAuditService _auditService;

        public AdminController() : this(new SurveyService(), new AuditService())
        {
        }

        public AdminController(ISurveyService surveyService, IAuditService auditService)
        {
            _surveyService = surveyService;
            _auditService = auditService;
        }

        // GET: /Admin
        [HttpGet]
        public ActionResult Index()
        {
            var data = _surveyService.GetDashboardData();
            return View(data);
        }

        // GET: /Admin/Create
        [HttpGet]
        public ActionResult Create()
        {
            var model = new CreateQuestionnaireViewModel
            {
                TargetRespondentsCount = 20,
                TimeLimitMinutes = 5,
                AutoGenerateTokens = true,
                Questions = new System.Collections.Generic.List<CreateQuestionDto>
                {
                    new CreateQuestionDto
                    {
                        QuestionText = "",
                        QuestionType = "Testy",
                        OptionsRaw = "",
                        IsRequired = true
                    }
                }
            };
            return View(model);
        }

        // POST: /Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateQuestionnaireViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var clientIp = Request.UserHostAddress;
            int qId = _surveyService.CreateQuestionnaire(model, clientIp);

            TempData["SuccessMessage"] = "پرسش‌نامه سازمانی جدید با موفقیت ایجاد و ذخیره شد.";
            return RedirectToAction("Details", new { id = qId });
        }

        // GET: /Admin/Details/{id}
        [HttpGet]
        public ActionResult Details(int id)
        {
            var baseUrl = GetBaseUrl();
            var details = _surveyService.GetQuestionnaireDetails(id, baseUrl);
            if (details == null)
            {
                TempData["ErrorMessage"] = "پرسش‌نامه مورد نظر یافت نشد.";
                return RedirectToAction("Index");
            }
            return View(details);
        }

        // POST: /Admin/GenerateTokens
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateTokens(int id, int count)
        {
            var clientIp = Request.UserHostAddress;
            int generated = _surveyService.GenerateTokens(id, count, clientIp);
            TempData["SuccessMessage"] = $"{generated} لینک اختصاصی یکبارمصرف جدید با موفقیت تولید شد.";
            return RedirectToAction("Details", new { id });
        }

        // GET: /Admin/Analytics/{id}
        [HttpGet]
        public ActionResult Analytics(int id)
        {
            var analytics = _surveyService.GetAnalytics(id);
            if (analytics == null)
            {
                TempData["ErrorMessage"] = "پرسش‌نامه مورد نظر برای تحلیل آماری یافت نشد.";
                return RedirectToAction("Index");
            }
            return View(analytics);
        }

        // GET: /Admin/AuditLogs
        [HttpGet]
        public ActionResult AuditLogs(int? questionnaireId, string actionType)
        {
            var logs = _auditService.GetLogs(questionnaireId, actionType, 200);
            var dashboard = _surveyService.GetDashboardData();

            var vm = new AuditLogFilterViewModel
            {
                QuestionnaireId = questionnaireId,
                ActionType = actionType,
                Logs = logs,
                Questionnaires = dashboard.Questionnaires
            };

            return View(vm);
        }

        // GET: /Admin/ExportTokens/{id}
        [HttpGet]
        public ActionResult ExportTokens(int id)
        {
            var baseUrl = GetBaseUrl();
            var tokens = _surveyService.GetTokensForExport(id, baseUrl);

            var sb = new StringBuilder();
            sb.AppendLine("ID,TokenGuid,Identifier,Status,OpenedAt,CompletedAt,ExpiresAt,SurveyUrl");

            foreach (var t in tokens)
            {
                sb.AppendLine($"{t.Id},\"{t.TokenGuid}\",\"{t.RespondentIdentifier}\",\"{t.StatusPersianTitle}\",\"{t.OpenedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.CompletedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.ExpiresAt:yyyy-MM-dd HH:mm:ss}\",\"{t.SurveyUrl}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Survey_{id}_Tokens_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private string GetBaseUrl()
        {
            var req = Request;
            if (req == null || req.Url == null) return "http://localhost:5000";
            return $"{req.Url.Scheme}://{req.Url.Authority}";
        }
    }
}
