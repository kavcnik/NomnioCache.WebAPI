namespace Nomnio.WebAPI.Services
{
    public static class EmailService
    {
        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        public static string ExtractDomain(string normalizedEmail)
        {
            var atIndex = normalizedEmail.IndexOf('@');
            if (atIndex < 0 || atIndex == normalizedEmail.Length - 1)
                throw new ArgumentException("Invalid email format — missing '@' or domain.", nameof(normalizedEmail));

            return normalizedEmail[(atIndex + 1)..];
        }
    }
}
