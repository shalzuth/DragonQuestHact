using System;
using System.Security.Cryptography;
using Grpc.Core;

namespace DragonQuestHact
{
    public class Headers
    {
        public String AccessToken = "";
        public String SharedSecurityKey;
        public Func<Metadata, Metadata> Add;
        public Headers()
        {
            Add = (Metadata source) =>
            {
                foreach (var header in Metadata.Headers)
                    source.Add(header.Key, header.Value);
                return source;
            };
        }
        public CallOptions Metadata
        {
            get
            {
                var md = new Metadata();
                md.Add("request_id", Guid.NewGuid().ToString().ToLower());
                md.Add("retrying", "false");
                md.Add("accept-language", "en");
                if (AccessToken == "") md.Add("user-agent", "grpc-csharp/2.24.0-dev grpc-c/8.0.0 (android; chttp2; ganges)");
                else
                {
                    var signature = Sign(new DQTRPC.Empty(), SharedSecurityKey, out String salt);
                    md.Add("auth_token", AccessToken);
                    md.Add("signature", signature);
                    md.Add("salt", salt);
                }
                var co = new CallOptions(md);
                return co;
            }
        }
        String Sign(Google.Protobuf.IMessage message, String key, out String salt)
        {
            using (var md5 = MD5.Create())
            {
                var data = Google.Protobuf.MessageExtensions.ToByteArray(message);
                var hashBytes = md5.ComputeHash(data);
                using (var aes = new AesManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    var rfc2898 = new Rfc2898DeriveBytes(key, 8, 1000);
                    //if (salt != "") rfc2898 = new Rfc2898DeriveBytes(key, Convert.FromBase64String(salt), 1000);
                    salt = Convert.ToBase64String(rfc2898.Salt);
                    aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
                    aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
                    ICryptoTransform encryptTransform = aes.CreateEncryptor();
                    return Convert.ToBase64String(encryptTransform.TransformFinalBlock(hashBytes, 0, hashBytes.Length));
                }
            }
        }
    }
}
