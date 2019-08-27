using jbsolutions.Constants;
using jbsolutions.Models;
using jbsolutions.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // Data files are located in App_Date folder 
        private static readonly string DbPath = HostingEnvironment.MapPath("~/App_Data");
        private static readonly char Divider = ',';
        private static readonly char SubDivider = ':';
        private static readonly string DividerReplacement = "%{1}";
        private static readonly string SubDividerReplacement = "%{2}";
        private static string _logFile;
        private static List<Product> _products = new List<Product>();

        internal static IEnumerable<Product> Products
        {
            get => _products;
        }

        /// <summary>
        /// Initialize the database. 
        /// If the path is given then will use the path to store the files.
        /// Otherwise it will use App_Data folder instead.
        /// It will use the latest data file in the directory to retore the data into memory.
        /// </summary>
        /// <param name="path">The path to data location.</param>
        public static void Initialize(string path = null)
        {
            try
            {
                // Get all files in the directory. The file with the biggest number is the latest snapshot-log file
                var log = Directory.GetFiles(path ?? DbPath).Select(f =>
                {
                    FileInfo fi = new FileInfo(f);
                    return new
                    {
                        Path = f,
                        Date = fi.CreationTimeUtc,
                    };
                }).ToArray().OrderByDescending(f => f.Date).FirstOrDefault();

                if (log != null)
                {
                    _logFile = log.Path;
                    LoadData();
                }

                TakeSnapshot(path ?? DbPath);
            }
            catch (Exception e)
            {
                throw new Exception("Error when initialize the in-memory database.", e);
            }
        }

        /// <summary>
        /// Load data into file.
        /// </summary>
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
                            var arr = AesEncryption.Decrypt(line).Split(Divider);
                            // line should start with indicator and other object
                            if (arr.Length < 2)
                            {
                                Log($"Line incorrent. Line {count}");
                                continue;
                            }
                            switch (arr[0])
                            {
                                case LogType.Snapshot:
                                case LogType.Add:
                                    var newProduct = new Product
                                    {
                                        Id = arr[1],
                                        Brand = ToMemoryFormat(arr[2]),
                                        Description = ToMemoryFormat(arr[3]),
                                        Model = ToMemoryFormat(arr[4]),
                                    };
                                    InsertOrUpdate(newProduct);
                                    break;
                                case LogType.Modify:
                                    var product = _products.Find(p => p.Id == arr[1]);
                                    if (product == null)
                                    {
                                        Log($"Cannot find product. Line {count}");
                                        break;
                                    }

                                    for (var i = 2; i < arr.Length; i++)
                                    {
                                        var fields = arr[i].Split(SubDivider);
                                        if (fields.Length != 2)
                                        {
                                            Log($"Incorrect data. Line {count}");
                                            break;
                                        }
                                        product.GetType()
                                            .GetProperty(fields[0])
                                            .SetValue(product, ToMemoryFormatSub(fields[1]));
                                    }
                                    break;
                                case LogType.Delete:
                                    Remove(arr[1]);
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

        /// <summary>
        /// Snap shot will be taken every time service is restarted.
        /// </summary>
        /// <param name="path">The directory that store the log files.</param>
        private static void TakeSnapshot(string path)
        {
            try
            {
                var file = Path.Combine(path, DateTime.UtcNow.Ticks.ToString());

                // Delete if exists.
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                using (StreamWriter fs = File.CreateText(file))
                {
                    // dump every product to file
                    foreach (var product in _products)
                    {
                        var line = string.Join(Divider.ToString(), new string[] {
                            LogType.Snapshot,
                            product.Id,
                            ToLogFormat(product.Brand),
                            ToLogFormat(product.Description),
                            ToLogFormat(product.Model),
                        });
                        fs.WriteLine(AesEncryption.Encrypt(line));
                    }
                    _logFile = file;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when taking the snapshot.", e);
            }
        }

        /// <summary>
        /// Modify the product according to the provided fields and value.
        /// </summary>
        /// <param name="id">The id of the product</param>
        /// <param name="fields">Fields that will be modified</param>
        internal static void Modify(string id, List<ModifyModel> fields)
        {
            try
            {
                var product = _products.Find(p => p.Id == id);
                if (product == null)
                {
                    throw new KeyNotFoundException("This product does not exist.");
                }

                if (File.Exists(_logFile))
                {
                    // Update the log file first.
                    using (StreamWriter fs = File.AppendText(_logFile))
                    {
                        var arr = new string[fields.Count + 2];
                        arr[0] = LogType.Modify; // first is the indicator
                        arr[1] = id; // second is the id
                        var i = 2;

                        fields.ForEach(m =>
                        {
                            // map property and set value
                            var prop = product.GetType()
                                .GetProperties()
                                .SingleOrDefault(p => p.Name.Equals(m.Prop, StringComparison.CurrentCultureIgnoreCase));
                            m.Prop = prop.Name;
                            arr[i] = $"{m.Prop}{SubDivider}{ToLogFormatSub(m.Value)}";
                            i++;
                        });

                        var line = string.Join(Divider.ToString(), arr);
                        fs.WriteLine(AesEncryption.Encrypt(line));
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

        /// <summary>
        /// Add a product
        /// </summary>
        /// <param name="product">The product to be added</param>
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
                        var line = string.Join(Divider.ToString(), new string[] {
                            LogType.Add,
                            product.Id,
                            ToLogFormat(product.Brand),
                            ToLogFormat(product.Description),
                            ToLogFormat(product.Model),
                        });
                        fs.WriteLine(AesEncryption.Encrypt(line));
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

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="id">The product Id that will be deleted</param>
        internal static void Delete(string id)
        {
            try
            {
                if (File.Exists(_logFile))
                {
                    // Update the log file first.
                    using (StreamWriter fs = File.AppendText(_logFile))
                    {
                        var line = string.Join(Divider.ToString(), new string[] {
                            LogType.Delete,
                            id,
                        });
                        fs.WriteLine(AesEncryption.Encrypt(line));
                    }

                    // Then update the object.
                    Remove(id);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when adding the record.", e);
            }
        }

        /// <summary>
        /// Remove from list according to id
        /// </summary>
        /// <param name="id">The id to be removed</param>
        private static void Remove(string id)
        {
            _products.RemoveAll(p => p.Id == id);
        }


        /// <summary>
        /// Insert or update a product.
        /// If not exist then insert otherwise then update.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="isInsert"></param>
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

        private static string ToLogFormat(string str)
        {
            return str.Replace(Divider.ToString(), DividerReplacement);
        }

        private static string ToLogFormatSub(string str)
        {
            return str.Replace(SubDivider.ToString(), SubDividerReplacement);
        }

        private static string ToMemoryFormat(string str)
        {
            return str.Replace(DividerReplacement, Divider.ToString());
        }

        private static string ToMemoryFormatSub(string str)
        {
            return str.Replace(SubDividerReplacement, SubDivider.ToString());
        }
    }
}