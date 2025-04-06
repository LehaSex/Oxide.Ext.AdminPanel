using System;
using System.IO;
using Newtonsoft.Json;

namespace Oxide.Ext.AdminPanel
{
    public class AdminPanelConfig
    {
        public string JwtSecretKey { get; set; } = GenerateSecureKey();
        public bool RequireHttps { get; set; } = true;

        private static string GenerateSecureKey()
        {
            using var cryptoProvider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] secretKey = new byte[32]; 
            cryptoProvider.GetBytes(secretKey);
            return Convert.ToBase64String(secretKey);
        }

        public static AdminPanelConfig Load(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<AdminPanelConfig>(json);
            }

            var config = new AdminPanelConfig();
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            return config;
        }
    }
}