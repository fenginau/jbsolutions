using jbsolutions.Db;
using System.Web.Http;

namespace jbsolutions
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            Database.Initialize();
        }
    }
}
