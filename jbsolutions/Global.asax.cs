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
            GlobalConfiguration.Configure(WebApiConfig.Register);
            Database.Initialize();
            RsaEncryption.Setup();
            var abc = RsaEncryption.Encrypt("this is a thing");
            var bcd = RsaEncryption.Decrypt(abc);
            Debug.WriteLine(abc);
            Debug.WriteLine(bcd);
        }
    }
}
