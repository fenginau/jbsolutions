﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using jbsolutions.Db;
using jbsolutions.Models;

namespace jbsolutions.Controllers
{
    [Authorize]
    public class ProductsController : ApiController
    {
        [Route("products")]
        [HttpGet]
        public IHttpActionResult GetProducts(string description = null, string model = null, string brand = null)
        {
            try
            {
                var products = Database.Products;
                if (!string.IsNullOrEmpty(description))
                {
                    Debug.WriteLine(description);
                    products = products.Where(p => p.Description.Contains(description));
                }

                if (!string.IsNullOrEmpty(model))
                {
                    Debug.WriteLine(model);
                    products = products.Where(p => p.Model.Contains(model));
                }

                if (!string.IsNullOrEmpty(brand))
                {
                    Debug.WriteLine(brand);
                    products = products.Where(p => p.Brand.Contains(brand));
                }
                return Ok(products);
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
                Debug.WriteLine(fields.Count);
                Database.Modify(id, fields);
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("products/{id}")]
        [HttpDelete]
        public IHttpActionResult DeleteProduct(string id)
        {
            try
            {
                Database.Delete(id);
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}
