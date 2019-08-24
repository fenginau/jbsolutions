using System;
using System.IO;
using System.Security.Cryptography;

namespace jbsolutions.Utils
{

    public class AesEncryption
    {
        private static readonly string Key = "7D8086AF1F0D47C99B168CC483FB2FC7";
        private static readonly string Iv = "kBQuQIVxVsdX0U+sRzwCwQ==";

        /// <summary>
        /// Encrypt the text use the AES encryption method
        /// </summary>
        /// <param name="plainText">the text</param>
        internal static string Encrypt(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    return "";
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(Key);
                    aes.IV = Convert.FromBase64String(Iv);
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;
                    var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            return Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when encrypting using AES.", e);
            }
        }


        /// <summary>
        /// Decrypt the text use the AES encryption method
        /// </summary>
        /// <param name="cipherText">the text</param>
        internal static string Decrypt(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                {
                    return "";
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(Key);
                    aes.IV = Convert.FromBase64String(Iv);
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when decrypting using AES.", e);
            }
        }
    }
}