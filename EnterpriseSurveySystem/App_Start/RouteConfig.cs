using System.Web.Mvc;
using System.Web.Routing;

namespace EnterpriseSurveySystem
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Survey specific route for clean token URL: /Survey/Take/{id}
            routes.MapRoute(
                name: "SurveyTake",
                url: "Survey/Take/{id}",
                defaults: new { controller = "Survey", action = "Take", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "SurveyCompleted",
                url: "Survey/Completed/{id}",
                defaults: new { controller = "Survey", action = "Completed", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "SurveyExpired",
                url: "Survey/Expired/{id}",
                defaults: new { controller = "Survey", action = "Expired", id = UrlParameter.Optional }
            );

            // Default route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Admin", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
