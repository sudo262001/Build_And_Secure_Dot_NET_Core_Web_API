using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using StudentAPI.DataSimulation;
using StudentAPI.DTOs;
using StudentAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }


        private static List<Student> _Students = StudentDataSimulation.StudentsList;

        [HttpPost("Login")]
        [EnableRateLimiting("AuthLimiter")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var student = _Students.FirstOrDefault(s=> s.Email == request.Email);
            if(student == null)
            {
                _logger.LogWarning(
                "Failed login attempt (email not found). Email={Email}, IP={IP}",
                request.Email,
                ip
                );

                return Unauthorized("Invalid credentials");
            }

            bool isValidPass = BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash);
            if (!isValidPass)
            {
                _logger.LogWarning(
                "Failed login attempt (bad password). Email={Email}, IP={IP}",
                request.Email,
                ip
                );

                return Unauthorized("Invalid credentials");
            }

            //Login is successful => issue claims (JWT payload fields)
            var Claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
                new Claim(ClaimTypes.Email, student.Email),
                new Claim(ClaimTypes.Role, student.Role)
            };
            //Payload is created successfully => next is to sign the header and payload
            //Symmetric key will be used to sign
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Shared_Key_To_Verify_Must_Be_>256"));
            //Specify the algorithm that'll sign the header and payload
            var Creds = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
            //Create the JWT
            var Token = new JwtSecurityToken(
                issuer: "StudentApi",
                audience: "StudentApiUsers",
                claims: Claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: Creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(Token);

            // Create refresh token (random)
            var refreshToken = GenerateRefreshToken();

            // Store refresh token securely (hash + expiry + not revoked)
            student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            student.RefreshTokenRevokedAt = null;


            _logger.LogInformation(
           "Successful login. UserId={UserId}, Email={Email}, IP={IP}",
            student.Id,
            student.Email,
            ip
            );


            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }


        [HttpPost("refresh")]
        [EnableRateLimiting("AuthLimiter")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var student = StudentDataSimulation.StudentsList
                .FirstOrDefault(s => s.Email == request.Email);

            if (student == null)
            {
                
                _logger.LogWarning(
                "Invalid refresh token attempt. Email={Email}, IP={IP}",
                request.Email,
                ip
                );
                return Unauthorized("Invalid refresh request");
            }
            if (student.RefreshTokenRevokedAt != null)
            {
                _logger.LogWarning(
                    "Refresh attempt using revoked token. UserId={UserId}, Email={Email}, IP={IP}",
                    student.Id,
                    student.Email,
                    ip
                );

                return Unauthorized("Refresh token is revoked");
            }

            if (student.RefreshTokenExpiresAt == null || student.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Refresh attempt using expired token. UserId={UserId}, Email={Email}, IP={IP}",
                    student.Id,
                    student.Email,
                    ip
                );

                return Unauthorized("Refresh token expired");
            }

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);
            if (!refreshValid)
            {
                _logger.LogWarning(
                    "Invalid refresh token attempt. UserId={UserId}, Email={Email}, IP={IP}",
                    student.Id,
                    student.Email,
                    ip
                );

                return Unauthorized("Invalid refresh token");
            }


            // Issue NEW access token (same claims & signing settings as login)
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
        new Claim(ClaimTypes.Email, student.Email),
        new Claim(ClaimTypes.Role, student.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("THIS_IS_A_VERY_SECRET_KEY_123456"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: "StudentApi",
                audience: "StudentApiUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Rotation: replace refresh token
            var newRefreshToken = GenerateRefreshToken();
            student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            student.RefreshTokenRevokedAt = null;

            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            var student = StudentDataSimulation.StudentsList
                .FirstOrDefault(s => s.Email == request.Email);

            if (student == null)
                return Ok(); // Do not reveal if user exists

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);
            if (!refreshValid)
                return Ok();

            student.RefreshTokenRevokedAt = DateTime.UtcNow;
            return Ok("Logged out successfully");
        }

    }
}
