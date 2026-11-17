using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EnterpriseSurveySystem.Data;

namespace EnterpriseSurveySystem
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Database-First Mode: Disable Code-First EF migrations and automatic database initializers
            // The database is managed directly via SQL Server DDL scripts (CreateDatabase.sql)
            Database.SetInitializer<SurveyDbContext>(null);
        }
    }
}
