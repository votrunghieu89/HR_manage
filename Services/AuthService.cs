// File: Integration-System/Services/AuthService.cs
using Integration_System.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Integration_System.DAL;

namespace Integration_System.Services
{

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly DepartmentDAL _departmentDAL;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, DepartmentDAL departmentDAL)
        {
            _configuration = configuration;
            _logger = logger;
            _departmentDAL = departmentDAL;
        }
        public async Task<string> setRole(int departmentID)
        {
            string departmentName = await _departmentDAL.GetDepartmentByID(departmentID);

            if (string.IsNullOrEmpty(departmentName))
            {
                _logger.LogWarning("Department with ID {DepartmentID} not found.", departmentID);
                return string.Empty;
            }

            return departmentName switch
            {
                "Office" => UserRoles.PayrollManagement,
                "IT" => UserRoles.Employee,
                "Helpdesk" => UserRoles.Hr,
                _ => UserRoles.Admin
            };
        }
public JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT Key, Issuer or Audience is not configured in appsettings.json");
                throw new InvalidOperationException("JWT settings are missing or invalid.");
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            _ = int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);
            if (tokenValidityInMinutes <= 0)
            {
                tokenValidityInMinutes = 60;
                _logger.LogWarning("JWT:TokenValidityInMinutes not configured or invalid. Using default value: {DefaultMinutes} minutes.", tokenValidityInMinutes);
            }

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}