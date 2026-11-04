using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Giatep.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Giatep.Api.ValidacionesDTO;

namespace Giatep.Api.Endpoints;
public record LoginDTO(string Cuenta, string Password);
public record PasswordDTO(string Actual, string Nueva);
public static class AuthEndpoints
{
    public static object Publico(Usuario u) => new { u.Id, u.Cuenta, u.Nombre, u.Rol, u.EstablecimientoId, u.Activo };
    public static void MapAuth(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/login", (LoginDTO dto, GiatepDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Cuenta) || dto.Cuenta.Length > 50 || string.IsNullOrEmpty(dto.Password) || dto.Password.Length > 200)
                throw new ValidationException("Ingresa una cuenta y una contraseña válidas.");
            var user = db.Usuarios.SingleOrDefault(u => u.Cuenta == dto.Cuenta.Trim().ToLower());
            var hasher = new PasswordHasher<Usuario>();
            if (user is null || !user.Activo || user.BloqueadoHasta > DateTimeOffset.UtcNow) return Results.Problem(statusCode: 401, title: "Credenciales inválidas o cuenta temporalmente bloqueada.");
            if (hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            {
                user.Fallos++;
                if (user.Fallos >= 5) { user.BloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(10); user.Fallos = 0; }
                db.SaveChanges();
                return Results.Problem(statusCode: 401, title: "Credenciales inválidas o cuenta temporalmente bloqueada.");
            }
            user.Fallos = 0; user.BloqueadoHasta = null; db.SaveChanges();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Nombre), new(ClaimTypes.Role, user.Rol), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
            claims.Add(new("version", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)))));
            if (user.EstablecimientoId.HasValue) claims.Add(new("establecimiento", user.EstablecimientoId.ToString()!));
            var expiry = DateTime.UtcNow.AddMinutes(30);
            var token = new JwtSecurityToken("giatep", "giatep-web", claims, expires: expiry, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), usuario = Publico(user), expira = expiry });
        }).Produces<SesionDTO>().AllowAnonymous().RequireRateLimiting("login").WithTags("Autenticación");
        app.MapGet("/api/v1/auth/me", (ClaimsPrincipal actor, GiatepDbContext db) => Results.Ok(Publico(db.Usuarios.Single(u => u.Id == Guid.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!))))).RequireAuthorization().WithTags("Autenticación");
        app.MapPut("/api/v1/auth/password", (PasswordDTO dto, ClaimsPrincipal actor, GiatepDbContext db) =>
        {
            if (string.IsNullOrEmpty(dto.Nueva) || dto.Nueva.Length < 12 || dto.Nueva.Length > 200) throw new ValidationException("La nueva contraseña debe tener entre 12 y 200 caracteres.");
            var user = db.Usuarios.Single(u => u.Id == Guid.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!));
            var hasher = new PasswordHasher<Usuario>();
            if (string.IsNullOrEmpty(dto.Actual) || hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Actual) == PasswordVerificationResult.Failed) return Results.Problem(statusCode: 400, title: "La contraseña actual no es correcta.");
            user.PasswordHash = hasher.HashPassword(user, dto.Nueva); db.SaveChanges(); return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("login").WithTags("Autenticación");
    }
}
