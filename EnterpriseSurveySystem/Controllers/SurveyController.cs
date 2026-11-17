using System.Web.Mvc;
using EnterpriseSurveySystem.Models.ViewModels;
using EnterpriseSurveySystem.Services;

namespace EnterpriseSurveySystem.Controllers
{
    public class SurveyController : Controller
    {
        private readonly ISurveyService _surveyService;

        public SurveyController() : this(new SurveyService())
        {
        }

        public SurveyController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        // GET: /Survey/Take/{id}
        [HttpGet]
        public ActionResult Take(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Invalid");
            }

            var clientIp = Request.UserHostAddress;
            var vm = _surveyService.GetSurveyForRespondent(id, clientIp);

            if (vm == null)
            {
                return RedirectToAction("Invalid");
            }

            if (vm.IsCompleted)
            {
                return RedirectToAction("Completed", new { id = vm.TokenGuid });
            }

            if (vm.IsExpired)
            {
                return RedirectToAction("Expired", new { id = vm.TokenGuid });
            }

            return View(vm);
        }

        // POST: /Survey/Submit
        [HttpPost]
        public ActionResult Submit(SurveySubmitModel model)
        {
            ValidateRequestToken();

            var clientIp = Request.UserHostAddress;
            var result = _surveyService.SubmitSurvey(model, clientIp);

            if (Request.IsAjaxRequest())
            {
                return Json(result);
            }

            if (result.Success)
            {
                return Redirect(result.RedirectUrl ?? Url.Action("Completed", new { id = model.TokenGuid }));
            }

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Take", new { id = model.TokenGuid });
        }

        private void ValidateRequestToken()
        {
            try
            {
                var cookie = Request.Cookies[System.Web.Helpers.AntiForgeryConfig.CookieName];
                var cookieToken = cookie != null ? cookie.Value : null;
                var formToken = Request.Headers["RequestVerificationToken"] ?? Request.Form["__RequestVerificationToken"];

                if (!string.IsNullOrEmpty(cookieToken) && !string.IsNullOrEmpty(formToken))
                {
                    System.Web.Helpers.AntiForgery.Validate(cookieToken, formToken);
                }
            }
            catch
            {
                // In production, reject or log CSRF attempts
            }
        }

        // GET: /Survey/Completed/{id}
        [HttpGet]
        public ActionResult Completed(string id)
        {
            ViewBag.TokenGuid = id;
            return View();
        }

        // GET: /Survey/Expired/{id}
        [HttpGet]
        public ActionResult Expired(string id)
        {
            ViewBag.TokenGuid = id;
            return View();
        }

        // GET: /Survey/Invalid
        [HttpGet]
        public ActionResult Invalid()
        {
            return View();
        }
    }
}
