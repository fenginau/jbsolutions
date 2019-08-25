using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using jbsolutions.Db;
using jbsolutions.Models;
using jbsolutions.Utils;

namespace jbsolutions.Controllers
{
    [EnableCors(origins: "http://localhost", headers: "*", methods: "*")]
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
        public IHttpActionResult Signin([FromBody] AuthModel auth)
        {
            try
            {
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
        
    }
}
