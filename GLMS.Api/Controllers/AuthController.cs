using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GLMS.Api.Data;
using GLMS.Api.Models;

namespace GLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly GlmsDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(GlmsDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            // Check username is not already taken
            if (await _context.Users.AnyAsync(
                u => u.Username == request.Username))
            {
                return BadRequest(new
                {
                    message = "Username already exists."
                });
            }

            // Hash the password — never store plain text passwords
            var user = new AppUser
            {
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            // Find the user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });

            // Check the password hash
            if (!VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials." });

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiry = DateTime.UtcNow.AddHours(1);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiry
            });
        }

        // ── Private helpers ───────────────────────────────────────────────
        private string HashPassword(string password)
        {
            // SHA256 hash — in production use BCrypt or ASP.NET Identity
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private string GenerateJwtToken(AppUser user)
        {
            var secret = _config["JwtSettings:Secret"]
                          ?? "DefaultSecretKeyForDevelopment123!";
            var key = new SymmetricSecurityKey(
                              Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(
                              key, SecurityAlgorithms.HmacSha256);

            // Claims are pieces of information embedded in the token
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim(ClaimTypes.Role,           user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: "GLMS.Api",
                audience: "GLMS.Web",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}