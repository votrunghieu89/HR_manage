// File: Integration-System/Services/IAuthService.cs
using Integration_System.Constants;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Integration_System.Services
{
    public interface IAuthService
    {
        //Task<bool> DeleteUser(string email);
        Task<string> setRole(int departmentID);
        JwtSecurityToken CreateToken(List<Claim> authClaims);
    }
}