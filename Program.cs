using OrderApp.Data;
using OrderApp.Forms;
using System.Security.Cryptography;
using System.Text;

namespace OrderApp
{
    internal static class Program
    {
        private static readonly string LicenseFile =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OrderApp",
                "license.dat"
            );


        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();


            // ONLY RUN THIS ONCE TO GENERATE LICENSE
            GenerateLicense();


            if (!CheckLicense())
            {
                MessageBox.Show(
                    "License expired. Please contact Admin.",
                    "License Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }


            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(
                        new MainForm(loginForm.Username)
                    );
                }
            }
        }



        // ===============================
        // Generate encrypted license
        // ===============================

        static void GenerateLicense()
        {
            var license = new
            {
                Customer = "ABC Company",
                ExpireDate = "2026-08-07"
            };


            string json =
                System.Text.Json.JsonSerializer.Serialize(license);


            string encrypted = Encrypt(json);


            Directory.CreateDirectory(
                Path.GetDirectoryName(LicenseFile)!
            );


            File.WriteAllText(
                LicenseFile,
                encrypted
            );


            MessageBox.Show(
                "License Generated"
            );
        }
        // ===============================
        // Check License
        // ===============================
        static bool CheckLicense()
        {
            if (!File.Exists(LicenseFile))
                return false;


            string encrypted =
                File.ReadAllText(LicenseFile);


            string json =
                Decrypt(encrypted);


            var license =
                System.Text.Json.JsonSerializer
                .Deserialize<LicenseModel>(json);



            if (license == null)
                return false;



            DateTime expireDate =
                DateTime.Parse(
                    license.ExpireDate
                );


            return DateTime.Now <= expireDate;
        }
        // ===============================
        // Encryption
        // ===============================
        static string Encrypt(string text)
        {
            using Aes aes = Aes.Create();


            aes.Key = Encoding.UTF8.GetBytes(
                "12345678901234567890123456789012"
            );

            aes.IV = Encoding.UTF8.GetBytes(
                "1234567890123456"
            );


            using var encryptor =
                aes.CreateEncryptor();


            byte[] bytes =
                Encoding.UTF8.GetBytes(text);


            byte[] encrypted =
                encryptor.TransformFinalBlock(
                    bytes,
                    0,
                    bytes.Length
                );


            return Convert.ToBase64String(
                encrypted
            );
        }
        static string Decrypt(string encrypted)
        {
            using Aes aes = Aes.Create();


            aes.Key = Encoding.UTF8.GetBytes(
                "12345678901234567890123456789012"
            );

            aes.IV = Encoding.UTF8.GetBytes(
                "1234567890123456"
            );


            using var decryptor =
                aes.CreateDecryptor();


            byte[] bytes =
                Convert.FromBase64String(encrypted);


            byte[] decrypted =
                decryptor.TransformFinalBlock(
                    bytes,
                    0,
                    bytes.Length
                );


            return Encoding.UTF8.GetString(
                decrypted
            );
        }
        public class LicenseModel
        {
            public string Customer { get; set; }
            public string ExpireDate { get; set; }
        }
    }
}