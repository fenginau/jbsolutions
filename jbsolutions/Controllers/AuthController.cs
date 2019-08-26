using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using jbsolutions.Db;
using jbsolutions.Models;
using jbsolutions.Utils;
using Newtonsoft.Json;

namespace jbsolutions.Controllers
{
    public class AuthController : ApiController
    {
        [Route("auth")]
        [HttpGet]
        public IHttpActionResult GetPublicKey()
        {
            try
            {
                return Ok(RsaEncryption.GetPublicKeyString());
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("auth")]
        [HttpPost]
        public IHttpActionResult Signin([FromBody] SigninRequest request)
        {
            try
            {
                var decrypted = RsaEncryption.Decrypt(request.Request);
                var auth = JsonConvert.DeserializeObject<AuthModel>(decrypted);
                
                // use hard-coded user name and hashed password
                if (auth.Username.ToLower() == "demo" && auth.Password == "QBG6AuURBMZ4wxp2pERIWzjzhl5QTYnDoKgLQ5uxojc=")
                {
                    return Ok(JwtUtil.GenerateToken(auth));
                }

                return Unauthorized();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
        
    }
}
