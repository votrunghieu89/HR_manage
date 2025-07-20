namespace Integration_System.Model
{
    public class AuthModel
    {
        public string UserName { get; set; }
        public  string Email { get; set; }
        public string Password { get; set; }

        public string Role { get; set; }
        public AuthModel(string userName, string email, string password, string role )
        {
            UserName = userName;
            Email = email;
            Password = password;
            Role = role;
        }
        public AuthModel() { }
    }
}
