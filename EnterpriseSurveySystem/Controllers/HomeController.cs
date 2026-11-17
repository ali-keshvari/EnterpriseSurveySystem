using System.Web.Mvc;

namespace EnterpriseSurveySystem.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Admin");
        }
    }
}
