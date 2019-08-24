using jbsolutions.Models;
using jbsolutions.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;

namespace jbsolutions.Db
{
    /// <summary>
    /// This class performs as the in-memory persistence database with simple functionalities.
    /// Snapshots and change logs are saved as text format encrypted using AES.
    /// Each snapshot-log file starts with the snapshot output of all the objects, and continues with the change log.
    /// When recovered from off-state, it will load the latest snapshoted objects, then update the objects according to the log. 
    /// </summary>
    public class Database
    {
        /// <summary>
        /// The version code is the snapshot version code. It is the same as the snapshot-log file name.
        /// </summary>
        private static int Version = 0;

        // Data files are located in App_Date folder 
        private static readonly string DbPath = HostingEnvironment.MapPath("~/App_Data");
        private static string _logFile;
        private static List<Product> _products = new List<Product>();

        internal static IEnumerable<Product> Products
        {
            get => _products;
        }

        internal static void Initialize()
        {
            try
            {
                // Get all files in the directory. The file with the biggest number is the latest snapshot-log file
                var files = Directory.GetFiles(DbPath).OrderByDescending(f => f);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);

                    // A valid log filename must be integer.
                    if (int.TryParse(fileName, out Version))
                    {
                        _logFile = file;
                        break;
                    }
                }

                LoadData();
                //TakeSnapshot();
            }
            catch (Exception e)
            {
                throw new Exception("Error when initialize the in-memory database.", e);
            }
        }

        private static void LoadData()
        {
            var count = 1;
            try
            {
                string line;
                if (File.Exists(_logFile))
                {
                    using (var file = new StreamReader(_logFile))
                    {
                        while ((line = file.ReadLine()) != null)
                        {
                            var arr = line.Split(',');
                            if (arr.Length < 2)
                            {
                                Log($"Line incorrent. Line {count}");
                                continue;
                            }
                            switch (arr[0])
                            {
                                case "OBJECT":
                                case "ADD":
                                    var newProduct = new Product
                                    {
                                        Id = AesEncryption.Decrypt(arr[1]),
                                        Brand = AesEncryption.Decrypt(arr[2]),
                                        Description = AesEncryption.Decrypt(arr[3]),
                                        Model = AesEncryption.Decrypt(arr[4]),
                                    };
                                    InsertOrUpdate(newProduct);
                                    break;
                                case "MODIFY":
                                    var product = _products.Find(p => p.Id == AesEncryption.Decrypt(arr[1]));
                                    if (product == null)
                                    {
                                        Log($"Cannot find product. Line {count}");
                                        break;
                                    }

                                    for (var i = 2; i < arr.Length; i++)
                                    {
                                        var fields = arr[i].Split(':');
                                        if (fields.Length != 2)
                                        {
                                            Log($"Incorrect data. Line {count}");
                                            break;
                                        }
                                        product.GetType()
                                            .GetProperty(AesEncryption.Decrypt(fields[0]))
                                            .SetValue(product, AesEncryption.Decrypt(fields[1]));
                                    }
                                    break;
                            }
                            count++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error when loading the data into memory. Processing row {count}", e);
            }
        }

        internal static void TakeSnapshot()
        {
            try
            {
                var file = Path.Combine(DbPath, (++Version).ToString());

                // Delete if exists.
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                using (StreamWriter fs = File.CreateText(file))
                {
                    foreach (var product in _products)
                    {
                        var line = String.Join(",", new String[] {
                            "OBJECT",
                            AesEncryption.Encrypt(product.Id),
                            AesEncryption.Encrypt(product.Brand),
                            AesEncryption.Encrypt(product.Description),
                            AesEncryption.Encrypt(product.Model),
                        });
                        fs.WriteLine(line);
                    }
                    _logFile = file;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when taking the snapshot.", e);
            }
        }

        internal static void Modify(string Id, List<ModifyModel> fields)
        {
            try
            {
                var product = _products.Find(p => p.Id == Id);
                if (product == null)
                {
                    throw new KeyNotFoundException("This product does not exist.");
                }

                if (File.Exists(_logFile))
                {
                    // Update the log file first.
                    using (StreamWriter fs = File.AppendText(_logFile))
                    {
                        var arr = new String[fields.Count + 2];
                        arr[0] = "MODIFY";
                        arr[1] = AesEncryption.Encrypt(Id);
                        var i = 2;

                        fields.ForEach(m =>
                        {
                            var prop = product.GetType()
                                .GetProperties()
                                .SingleOrDefault(p => p.Name.Equals(m.Prop, StringComparison.CurrentCultureIgnoreCase));
                            m.Prop = prop.Name;
                            arr[i] = $"{AesEncryption.Encrypt(m.Prop)}:{AesEncryption.Encrypt(m.Value)}";
                            i++;
                        });

                        var line = String.Join(",", arr);
                        fs.WriteLine(line);
                    }

                    // Then update the object.
                    fields.ForEach(m =>
                    {
                        product.GetType().GetProperty(m.Prop).SetValue(product, m.Value);
                    });


                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when modifying the record.", e);
            }
        }

        internal static void Add(Product product)
        {
            try
            {
                if (File.Exists(_logFile))
                {
                    product.Id = Guid.NewGuid().ToString();
                    // Update the log file first.
                    using (StreamWriter fs = File.AppendText(_logFile))
                    {
                        var line = String.Join(",", new String[] {
                            "ADD",
                            AesEncryption.Encrypt(product.Id),
                            AesEncryption.Encrypt(product.Brand),
                            AesEncryption.Encrypt(product.Description),
                            AesEncryption.Encrypt(product.Model),
                        });
                        fs.WriteLine(line);
                    }

                    // Then update the object.
                    InsertOrUpdate(product, true);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when adding the record.", e);
            }
        }

        private static void InsertOrUpdate(Product product, bool isInsert = false)
        {
            try
            {
                if (isInsert)
                {
                    _products.Add(product);
                    return;
                }

                if (_products.Any(p => p.Id == product.Id))
                {
                    var existProduct = _products.Find(p => p.Id == product.Id);
                    existProduct.Brand = product.Brand;
                    existProduct.Description = product.Description;
                    existProduct.Model = product.Model;
                }
                else
                {
                    _products.Add(product);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when inserting the record.", e);
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}