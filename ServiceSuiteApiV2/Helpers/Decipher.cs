using System.Security.Cryptography;
using System.Text;

namespace ServiceSuiteApiV2.Helpers
{
    public class Decipher
    {
#pragma warning disable SYSLIB0021
        public string Decripting(string CipherText)
        {
            const string SecurityKey = "koKRmH,HdaP5993fwfk33232!23#+*()__*&^^^&*STK";
            byte[] toEncryptArray = Convert.FromBase64String(CipherText);

            var objMD5 = new MD5CryptoServiceProvider();
            byte[] securityKeyArray = objMD5.ComputeHash(Encoding.UTF8.GetBytes(SecurityKey));
            objMD5.Clear();

            var des = new TripleDESCryptoServiceProvider
            {
                Key     = securityKeyArray,
                Mode    = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            var transform   = des.CreateDecryptor();
            byte[] result   = transform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            des.Clear();

            return Encoding.UTF8.GetString(result);
        }
#pragma warning restore SYSLIB0021
    }
}
