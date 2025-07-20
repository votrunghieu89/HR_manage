// File: Integration-System/Dtos/AuthenticationDTO/LoginResponseDto.cs
namespace Integration_System.Dtos.AuthenticationDTO
{
    public class LoginResponseDto
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string Username { get; set; }
        public string Id { get; set; }
        public string Roles { get; set; }
    }
}