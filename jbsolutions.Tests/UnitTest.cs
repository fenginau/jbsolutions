using System;
using jbsolutions.Controllers;
using jbsolutions.Models;
using System.Linq;
using System.Web.Http.Results;
using System.Collections.Generic;
using jbsolutions.Db;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using System.Web.Http;

namespace jbsolutions.Tests
{
    public class UnitTest
    {
        private readonly ITestOutputHelper Output;
        private ProductsController Controller;

        public UnitTest(ITestOutputHelper output)
        {
            Output = output;

            // give the path to the log directory
            var path = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "jbsolutions", "App_Data");

            Database.Initialize(path);
            Controller = new ProductsController();
        }

        /// <summary>
        /// Add a single product
        /// </summary>
        [Fact]
        public void AddProduct()
        {
            // Get products for the first time and check return.
            var beforeAddProductsCount = GetProducts().Count();

            // Add a product.
            Controller.AddProduct(new Product
            {
                Brand = $"new brand {DateTime.UtcNow.Ticks}",
                Description = $"new description {DateTime.UtcNow.Ticks}",
                Model = $"new model {DateTime.UtcNow.Ticks}",
            });

            // Get products for the second time and check return.
            var afterAddProductsCount = GetProducts().Count();

            // Compare the count of products returned in previous get call.
            Assert.Equal(afterAddProductsCount, beforeAddProductsCount + 1);
        }

        /// <summary>
        /// Get all aproducts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public IEnumerable<Product> GetProducts()
        {
            var actionResult = Controller.GetProducts();
            Assert.NotNull(actionResult);
            var contentResult1 = Assert.IsType<OkNegotiatedContentResult<IEnumerable<Product>>>(actionResult);
            Assert.NotNull(contentResult1);

            // check returned content type is Product list
            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(contentResult1.Content);

            // check list object is Product
            var product = Assert.IsAssignableFrom<Product>(contentResult1.Content.FirstOrDefault());
            return products;
        }

        /// <summary>
        /// Get single product
        /// </summary>
        [Fact]
        public void GetSingleProducts()
        {
            // get the ID of exist product then get the product
            var actionResult = Controller.GetProduct(GetProducts().FirstOrDefault().Id);
            Assert.NotNull(actionResult);
            var contentResult = Assert.IsType<OkNegotiatedContentResult<Product>>(actionResult);
            Assert.NotNull(contentResult);

            // check returned content type is Product
            var product = Assert.IsAssignableFrom<Product>(contentResult.Content);
            Assert.NotNull(product);
        }

        /// <summary>
        /// Modify a product
        /// </summary>
        [Fact]
        public void ModifyProduct()
        {
            var timeNow = DateTime.UtcNow.Ticks.ToString();
            var product = GetProducts().FirstOrDefault();

            Controller.EditProduct(product.Id, new List<ModifyModel>
            {
                new ModifyModel {Prop = "Description", Value= $"modified desc {timeNow}" },
                new ModifyModel {Prop = "Model", Value= $"modified model {timeNow}" },
                new ModifyModel {Prop = "Brand", Value= $"modified brand {timeNow}" },
            });

            var actionResult = Controller.GetProduct(product.Id);
            Assert.NotNull(actionResult);
            var contentResult = Assert.IsType<OkNegotiatedContentResult<Product>>(actionResult);
            Assert.NotNull(contentResult);

            // check returned content type is Product
            var newProduct = Assert.IsAssignableFrom<Product>(contentResult.Content);
            Assert.NotNull(newProduct);

            // check the value has been changed
            Assert.Equal(newProduct.Brand, $"modified brand {timeNow}");
            Assert.Equal(newProduct.Model, $"modified model {timeNow}");
            Assert.Equal(newProduct.Description, $"modified desc {timeNow}");
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        [Fact]
        public void DeleteProduct()
        {
            // get a product
            var product = GetProducts().FirstOrDefault();

            // delete the product
            Controller.DeleteProduct(product.Id);

            // get same product again
            var actionResult = Controller.GetProduct(product.Id);
            Assert.NotNull(actionResult);

            // should return not found
            var contentResult = Assert.IsType<NotFoundResult>(actionResult);
            Assert.NotNull(contentResult);
        }

        /// <summary>
        /// Get filtered products
        /// </summary>
        [Fact]
        public void GetFilteredProduct()
        {
            // Add products.
            GetNewProducts().ForEach(p =>
            {
                Controller.AddProduct(p);
            });

            // get all products
            var products = GetProducts();

            // get the actual count of filterd product
            var descHitCount = products.Where(p => p.Description.Contains("filtered description hit")).Count();
            var modelHitCount = products.Where(p => p.Model.Contains("filtered model hit")).Count();
            var brandHitCount = products.Where(p => p.Brand.Contains("filtered brand hit")).Count();

            // get filtered products
            var descProducts =
                GetProductsFromActionResult(Controller.GetProducts(description: "filtered description hit"));
            var modelProducts =
                GetProductsFromActionResult(Controller.GetProducts(model: "filtered model hit"));
            var brandProducts =
                GetProductsFromActionResult(Controller.GetProducts(brand: "filtered brand hit"));

            // should contain the string in result
            foreach (var product in descProducts)
            {
                Assert.Contains("filtered description hit", product.Description);
            }
            foreach (var product in modelProducts)
            {
                Assert.Contains("filtered model hit", product.Model);
            }
            foreach (var product in brandProducts)
            {
                Assert.Contains("filtered brand hit", product.Brand);
            }

            // check the count match
            Assert.Equal(descHitCount, descProducts.Count());
            Assert.Equal(modelHitCount, modelProducts.Count());
            Assert.Equal(brandHitCount, brandProducts.Count());
        }

        private List<Product> GetNewProducts()
        {
            return new List<Product>
            {
                new Product
                {
                    Brand = $"filtered brand hit {DateTime.UtcNow.Ticks}",
                    Description = $"filtered description {DateTime.UtcNow.Ticks}",
                    Model = $"filtered model {DateTime.UtcNow.Ticks}",
                },
                new Product
                {
                    Brand = $"filtered brand {DateTime.UtcNow.Ticks}",
                    Description = $"filtered description hit {DateTime.UtcNow.Ticks}",
                    Model = $"filtered model {DateTime.UtcNow.Ticks}",
                },
                new Product
                {
                    Brand = $"filtered brand {DateTime.UtcNow.Ticks}",
                    Description = $"filtered description {DateTime.UtcNow.Ticks}",
                    Model = $"filtered model hit {DateTime.UtcNow.Ticks}",
                },
            };
        }

        private IEnumerable<Product> GetProductsFromActionResult(IHttpActionResult actionResult)
        {
            Assert.NotNull(actionResult);
            var contentResult1 = Assert.IsType<OkNegotiatedContentResult<IEnumerable<Product>>>(actionResult);
            Assert.NotNull(contentResult1);

            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(contentResult1.Content);
            return products;
        }
    }
}
