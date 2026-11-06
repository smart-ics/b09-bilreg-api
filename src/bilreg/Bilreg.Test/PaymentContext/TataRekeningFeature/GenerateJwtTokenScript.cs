using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class GenerateJwtTokenScript
{
    public static string CreateToken()
    {
        var key = "ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0=";
        var issuer = "BilregApiServer";
        var audience = "BilregApiClient";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", "petugas1"),
            new Claim(ClaimTypes.NameIdentifier, "petugas1"),
            new Claim(ClaimTypes.Name, "Petugas Billing"),
            new Claim(ClaimTypes.Role, "PetugasBilling")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
