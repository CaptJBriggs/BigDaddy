using System.Security.Cryptography;

namespace BigDaddy
{
    // Creates and verifies a salted PIN hash without storing the PIN itself.
    internal static class BigDaddyPinService
    {
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static bool HasPin(BigDaddySettings settings) =>
            !string.IsNullOrWhiteSpace(settings.EmergencyPinHash) &&
            !string.IsNullOrWhiteSpace(settings.EmergencyPinSalt);

        public static void SetPin(BigDaddySettings settings, string pin)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            settings.EmergencyPinSalt = Convert.ToBase64String(salt);
            settings.EmergencyPinHash = Convert.ToBase64String(hash);
        }

        public static void ClearPin(BigDaddySettings settings)
        {
            settings.EmergencyPinHash = string.Empty;
            settings.EmergencyPinSalt = string.Empty;
        }

        public static bool VerifyPin(BigDaddySettings settings, string pin)
        {
            if (!HasPin(settings))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(settings.EmergencyPinSalt);
                byte[] expectedHash = Convert.FromBase64String(settings.EmergencyPinHash);
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
