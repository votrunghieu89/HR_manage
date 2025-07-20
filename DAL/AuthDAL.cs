using Integration_System.Model;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Utilities;

namespace Integration_System.DAL
{
    public class AuthDAL
    {
        public readonly string _SQLServerAuth;
        public readonly string _SQLServerConnectionString;
        private readonly ILogger<AuthDAL> logger;

        public AuthDAL(IConfiguration configuration, ILogger<AuthDAL> logger)
        {
            _SQLServerAuth = configuration.GetConnectionString("SqlServerConnection");
            this.logger = logger;
        }

        public async Task<bool> InsertNewUser(AuthModel authModel)
        {
            if (authModel == null)
            {
                logger.LogError("InsertNewUser called with null AuthModel.");
                return false;
            }
            else
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_SQLServerAuth))
                    {
                        await connection.OpenAsync();
                        string query = "INSERT INTO Users (UserName, Email, Password, Role) VALUES (@UserName, @Email, @Password, @Role)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@UserName", authModel.UserName);
                            command.Parameters.AddWithValue("@Email", authModel.Email);
                            command.Parameters.AddWithValue("@Password", authModel.Password);
                            command.Parameters.AddWithValue("@Role", authModel.Role);
                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected > 0)
                            {
                                logger.LogInformation("New user inserted successfully.");
                                return true;
                            }
                            else
                            {
                                logger.LogWarning("No rows affected when inserting new user.");
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error inserting new user.");
                    return false;
                }
            }
        }
        public async Task<bool> CheckEmailExists(string email)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_SQLServerAuth))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        int count = (int)await command.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if email exists.");
                return false;
            }
        }

        public async Task<bool> CheckLogin(string email, string password)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_SQLServerAuth))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email AND Password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);

                        int count = (int)await command.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            logger.LogInformation("Login successful for email: {Email}", email);
                            return true;
                        }
                        else
                        {
                            logger.LogWarning("Login failed for email: {Email}", email);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login check.");
                return false;
            }
        }

        public async Task<AuthModel?> GetUserByEmailPassword(string email, string password)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_SQLServerAuth))
                {
                    await connection.OpenAsync();
                    string query = "SELECT UserName, Email, Role FROM Users WHERE Email = @Email AND Password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new AuthModel
                                {
                                   UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
      
                                   
                                    Role = reader.GetString(reader.GetOrdinal("Role"))

                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user by email and password.");
                return null;
            }
        }
        }
}
