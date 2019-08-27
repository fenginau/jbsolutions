using System;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using jbsolutions.Models;

namespace jbsolutions.Utils
{
    public class JwtUtil
    {
        /// <summary>
        /// Generate the JWT token
        /// </summary>
        /// <param name="auth">Authentication Information</param>
        /// <returns>JWT Token</returns>
        internal static string GenerateToken(AuthModel auth)
        {
            try
            {
                var secretKey = new RsaSecurityKey(RsaEncryption.RSA);
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, auth.Username),
                    new Claim(ClaimTypes.Role, "user"),
                    new Claim(ClaimTypes.UserData, auth.AesKey),
                };
                var token = new JwtSecurityToken(
                    issuer: "TestIssuer",
                    audience: "TestAudience",
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddDays(30),
                    signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.RsaSha256)
                );
                return new JwtSecurityTokenHandler().WriteToken(token);

            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}