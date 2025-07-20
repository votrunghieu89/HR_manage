// File: Integration-System/Controllers/AuthController.cs
using Integration_System.Dtos.AuthenticationDTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Integration_System.Dtos.AuthenticationDTO;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Integration_System.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Integration_System.Services;
using Integration_System.DAL;
using Integration_System.Model;
namespace Integration_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;
        private readonly AuthDAL _authDAL;
        public AuthController( IConfiguration configuration, ILogger<AuthController> logger, IAuthService authService, AuthDAL authDAL)
        {
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
            _authDAL = authDAL;

        }

        [HttpPost("register/admin")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            string role = UserRoles.Admin; // Default role for registration
            AuthModel newUser = new AuthModel
            {
                UserName = model.Username,
                Email = model.Email,
                Password = model.Password,
                Role = role
            };
            bool emailExists = await _authDAL.CheckEmailExists(model.Email);
            if (emailExists)
            {
                _logger.LogWarning("Email already exists: {Email}", model.Email);
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Email already exists." });
            }
            bool isRegistered = await _authDAL.InsertNewUser(newUser);
            if (isRegistered) {
                _logger.LogInformation("New user registered successfully: {Email}", model.Email);
                return CreatedAtAction(nameof(Login), new { email = model.Email }, new { message = "User registered successfully." });
            }
            else
            {
                _logger.LogError("Failed to register new user: {Email}", model.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Failed to register user." });
            }
        }
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (model == null)
                {
                    _logger.LogError("Login called with null model.");
                    return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Invalid login request." });
                }
            bool isCheck = await _authDAL.CheckEmailExists(model.Email);
                if (!isCheck)
                {
                    _logger.LogWarning("Login failed for user: {Email}. Email does not exist.", model.Email);
                    return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid email or password." });
                }
                bool isVerify = await _authDAL.CheckLogin(model.Email,model.Password);
                if (!isVerify)
                {
                    _logger.LogWarning("Login failed for user: {Email}. Invalid password.", model.Email);
                    return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid email or password." });
                }

                AuthModel user = await _authDAL.GetUserByEmailPassword(model.Email, model.Password);
                Console.WriteLine("ádasdasdasdsad"+user);
            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                };

           

                var token = _authService.CreateToken(authClaims);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("Login successful for user: {email}", model.Email);
                return Ok(new LoginResponseDto
                {
                    Token = tokenString,
                    Expiration = token.ValidTo,
                    Username = model.Email, // Return Email as Username
                    Roles = user.Role,
                });
        }
    }
}