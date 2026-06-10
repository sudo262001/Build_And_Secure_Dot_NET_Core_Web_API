using Microsoft.AspNetCore.Mvc;
using StudentAPI.Models;
using StudentAPI.DataSimulation;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static List<Student> _Students = StudentDataSimulation.StudentsList;

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var student = _Students.FirstOrDefault(s=> s.Email == request.Email);
            if(student == null)
            {
                return Unauthorized("Invalid Credentials");
            }
            bool isValidPass = BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash);
            if (!isValidPass) { return Unauthorized("Invalid Credentials"); }
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
            //Return the token
            return Ok(
                new 
            { 
                token = new JwtSecurityTokenHandler().WriteToken(Token)
            });
        }
    }
}
