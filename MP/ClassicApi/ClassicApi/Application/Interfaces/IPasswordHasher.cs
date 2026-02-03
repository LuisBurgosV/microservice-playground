namespace ClassicApi.Application.Interfaces
{
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a plain text password using bcrypt.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plain text password against a hashed password.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="hash">The hashed password to compare against.</param>
        /// <returns>True if the password matches the hash; otherwise false.</returns>
        bool VerifyPassword(string password, string hash);
    }
}
