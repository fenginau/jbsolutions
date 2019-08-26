using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace jbsolutions.Utils
{
    public class RsaEncryption
    {
        internal static RSACryptoServiceProvider RSA;

        internal static void Setup()
        {
            RSA = new RSACryptoServiceProvider(2048);
        }

        internal static string Decrypt(string base64Str)
        {
            if (string.IsNullOrEmpty(base64Str))
            {
                return "";
            }
            try
            {
                var data = Convert.FromBase64String(base64Str);
                var decryptedData = RSA.Decrypt(data, false);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                return null;
            }
        }

        internal static string Encrypt(string rawStr)
        {
            if (string.IsNullOrEmpty(rawStr))
            {
                return "";
            }
            try
            {

                var data = Encoding.UTF8.GetBytes(rawStr);
                var encryptedData = RSA.Encrypt(data, false);
                return Convert.ToBase64String(encryptedData);
            }
            catch
            {
                return "";
            }
        }

        internal static string GetPrivateKeyString()
        {
            using (var stream = new MemoryStream())
            {
                var parameters = RSA.ExportParameters(true);
                var outputString = new StringBuilder();
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30);
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeInteger(innerWriter, new byte[] { 0x00 });
                    EncodeInteger(innerWriter, parameters.Modulus);
                    EncodeInteger(innerWriter, parameters.Exponent);
                    EncodeInteger(innerWriter, parameters.D);
                    EncodeInteger(innerWriter, parameters.P);
                    EncodeInteger(innerWriter, parameters.Q);
                    EncodeInteger(innerWriter, parameters.DP);
                    EncodeInteger(innerWriter, parameters.DQ);
                    EncodeInteger(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
                outputString.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    var subBase64 = base64.Substring(i, Math.Min(64, base64.Length - i));
                    outputString.AppendLine(subBase64);
                }
                outputString.AppendLine("-----END RSA PRIVATE KEY-----");
                return outputString.ToString();
            }
        }

        internal static string GetPublicKeyString()
        {
            using (var stream = new MemoryStream())
            {
                var parameters = RSA.ExportParameters(false);
                var outputString = new StringBuilder();
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30);
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    innerWriter.Write((byte)0x30);
                    EncodeLength(innerWriter, 13);
                    innerWriter.Write((byte)0x06);
                    var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                    EncodeLength(innerWriter, rsaEncryptionOid.Length);
                    innerWriter.Write(rsaEncryptionOid);
                    innerWriter.Write((byte)0x05);
                    EncodeLength(innerWriter, 0);
                    innerWriter.Write((byte)0x03);
                    using (var bitStringStream = new MemoryStream())
                    {
                        var bitStringWriter = new BinaryWriter(bitStringStream);
                        bitStringWriter.Write((byte)0x00);
                        bitStringWriter.Write((byte)0x30);
                        using (var paramsStream = new MemoryStream())
                        {
                            var paramsWriter = new BinaryWriter(paramsStream);
                            EncodeInteger(paramsWriter, parameters.Modulus);
                            EncodeInteger(paramsWriter, parameters.Exponent);
                            var paramsLength = (int)paramsStream.Length;
                            EncodeLength(bitStringWriter, paramsLength);
                            bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                        }
                        var bitStringLength = (int)bitStringStream.Length;
                        EncodeLength(innerWriter, bitStringLength);
                        innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                    }
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
                outputString.AppendLine("-----BEGIN PUBLIC KEY-----");
                for (var i = 0; i < base64.Length; i += 64)
                {
                    var subBase64 = base64.Substring(i, Math.Min(64, base64.Length - i));
                    outputString.AppendLine(subBase64);
                }
                outputString.AppendLine("-----END PUBLIC KEY-----");
                return outputString.ToString();
            }
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        private static void EncodeInteger(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            foreach (var v in value)
            {
                if (v != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }
    }
}