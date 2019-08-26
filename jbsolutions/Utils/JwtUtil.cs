using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using jbsolutions.Models;

namespace jbsolutions.Utils
{
    public class JwtUtil
    {
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