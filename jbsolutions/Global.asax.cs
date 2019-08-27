using jbsolutions.Db;
using jbsolutions.Utils;
using System.Diagnostics;
using System.Web.Http;

namespace jbsolutions
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // initialize the RSA encryption
            RsaEncryption.Setup();
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // initialize the database
            Database.Initialize();
        }
    }
}
