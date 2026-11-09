using System.Security.Claims;
using System.Text;
using CourtBook.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CourtBook.Infrastructure.Authentication;

internal sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpiresInMinutes);

        // ① Τα claims: τι «γράφει» πάνω του το token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        // ② Το κλειδί και ο αλγόριθμος της υπογραφής
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ③ Η «περιγραφή» του token
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials
        };

        // ④ Δημιουργία και υπογραφή
        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return (token, expiresAt);
    }
}