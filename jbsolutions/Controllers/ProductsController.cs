using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using jbsolutions.Db;
using jbsolutions.Models;

namespace jbsolutions.Controllers
{
    public class ProductsController : ApiController
    {
        [Route("products")]
        [HttpGet]
        public IHttpActionResult GetProducts()
        {
            try
            {
                return Ok(Database.Products);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("products/{id}")]
        [HttpGet]
        public IHttpActionResult GetProduct(string id)
        {
            try
            {
                var product = Database.Products.FirstOrDefault(p => p.Id == id);
                if (product == null)
                {
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("products")]
        [HttpPost]
        public IHttpActionResult AddProduct([FromBody] Product product)
        {
            try
            {
                Database.Add(product);
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("products/{id}")]
        [HttpPut]
        public IHttpActionResult EditProduct(string id, [FromBody] List<ModifyModel> fields)
        {
            try
            {
                Database.Modify(id, fields);
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}
