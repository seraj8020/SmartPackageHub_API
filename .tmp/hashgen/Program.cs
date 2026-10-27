using System.Security.Cryptography;
using System.Text;

var password = args.Length > 0 ? args[0] : "Staff@123";
var salt = RandomNumberGenerator.GetBytes(16);
var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
Console.WriteLine($"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}");
