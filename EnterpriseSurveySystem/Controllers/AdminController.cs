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
        private readonly IHrService _hrService;

        public AdminController() : this(new SurveyService(), new AuditService(), new HrService())
        {
        }

        public AdminController(ISurveyService surveyService, IAuditService auditService, IHrService hrService)
        {
            _surveyService = surveyService;
            _auditService = auditService;
            _hrService = hrService;
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
                TimeLimitMinutes = 5,
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

            int generatedCount = 0;
            if (model.SelectedPersonIds != null && model.SelectedPersonIds.Count > 0)
            {
                var persons = _hrService.GetPersonsByIds(model.SelectedPersonIds);
                if (persons.Count > 0)
                {
                    generatedCount = _surveyService.GenerateTokensForPersons(qId, persons, clientIp);
                }
            }

            TempData["SuccessMessage"] = generatedCount > 0
                ? $"پرسش‌نامه با موفقیت ایجاد شد و {generatedCount} لینک اختصاصی برای پرسنل انتخابی صادر گردید."
                : "پرسش‌نامه سازمانی جدید با موفقیت ایجاد و ذخیره شد.";

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

        // GET: /Admin/GetDepartments
        [HttpGet]
        public JsonResult GetDepartments()
        {
            try
            {
                var deps = _hrService.GetActiveDepartments();
                return Json(new { success = true, data = deps }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: /Admin/GetPositions
        [HttpPost]
        public JsonResult GetPositions(System.Collections.Generic.List<int> departmentIds)
        {
            try
            {
                var positions = _hrService.GetActivePositions(departmentIds);
                return Json(new { success = true, data = positions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/GetPersons
        [HttpPost]
        public JsonResult GetPersons(System.Collections.Generic.List<int> departmentIds, System.Collections.Generic.List<int> positionIds)
        {
            try
            {
                var persons = _hrService.GetActivePersons(departmentIds, positionIds);
                return Json(new { success = true, data = persons });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/GenerateTokensForPersons
        [HttpPost]
        public JsonResult GenerateTokensForPersons(GeneratePersonTokensRequest request)
        {
            ValidateRequestToken();

            try
            {
                if (request == null || request.QuestionnaireId <= 0 || request.PersonIds == null || request.PersonIds.Count == 0)
                {
                    return Json(new { success = false, message = "هیچ پرسنلی برای صدور لینک انتخاب نشده است." });
                }

                var persons = _hrService.GetPersonsByIds(request.PersonIds);
                if (persons.Count == 0)
                {
                    return Json(new { success = false, message = "پرسنل انتخابی در پایگاه داده پرسنلی یافت نشدند." });
                }

                var clientIp = Request.UserHostAddress;
                int count = _surveyService.GenerateTokensForPersons(request.QuestionnaireId, persons, clientIp);

                return Json(new { success = true, message = $"{count} لینک اختصاصی برای پرسنل انتخابی با موفقیت صادر شد.", count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Admin/ExportTokens/{id}
        [HttpGet]
        public ActionResult ExportTokens(int id)
        {
            var baseUrl = GetBaseUrl();
            var tokens = _surveyService.GetTokensForExport(id, baseUrl);

            var sb = new StringBuilder();
            sb.AppendLine("شناسه,توکن,نام و نام خانوادگی,کد پرسنلی,دپارتمان,سمت,وضعیت,زمان بازگشایی,زمان ثبت نهایی,زمان انقضا,لینک اختصاصی");

            foreach (var t in tokens)
            {
                sb.AppendLine($"{t.Id},\"{t.TokenGuid}\",\"{t.PersonFullName ?? t.RespondentIdentifier}\",\"{t.PersonelCode}\",\"{t.DepartmentName}\",\"{t.PositionName}\",\"{t.StatusPersianTitle}\",\"{t.OpenedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.CompletedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.ExpiresAt:yyyy-MM-dd HH:mm:ss}\",\"{t.SurveyUrl}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Survey_{id}_Personalized_Tokens_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // GET: /Admin/GetRespondentAnswers
        [HttpGet]
        public JsonResult GetRespondentAnswers(int tokenId)
        {
            try
            {
                var response = _surveyService.GetRespondentResponse(tokenId);
                if (response == null)
                {
                    return Json(new { success = false, message = "اطلاعات پاسخ‌دهنده یافت نشد." }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = true, data = response }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: /Admin/ExportResponsesExcel/{id}
        [HttpGet]
        public ActionResult ExportResponsesExcel(int id)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                string fileName;
                var excelBytes = _surveyService.ExportResponsesToExcel(id, baseUrl, out fileName);

                if (excelBytes == null || excelBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "پرسش‌نامه مورد نظر یافت نشد یا پاسخی برای آن ثبت نشده است.";
                    return RedirectToAction("Details", new { id = id });
                }

                return File(excelBytes, "application/vnd.ms-excel", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در استخراج اکسل: " + ex.Message;
                return RedirectToAction("Details", new { id = id });
            }
        }

        private string GetBaseUrl()
        {
            var req = Request;
            if (req == null || req.Url == null) return "http://localhost:5000";
            return $"{req.Url.Scheme}://{req.Url.Authority}";
        }

        private void ValidateRequestToken()
        {
            try
            {
                var cookie = Request.Cookies[System.Web.Helpers.AntiForgeryConfig.CookieName];
                var headerToken = Request.Headers["RequestVerificationToken"] ?? Request.Form["__RequestVerificationToken"];
                if (cookie != null && !string.IsNullOrEmpty(headerToken))
                {
                    System.Web.Helpers.AntiForgery.Validate(cookie.Value, headerToken);
                }
            }
            catch { }
        }
    }
}
