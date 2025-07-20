using Integration_System.Model;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Integration_System.Dtos;
using Microsoft.Data.SqlClient.DataClassification;
using System.Data;
namespace Integration_System.DAL
{
    public class DepartmentDAL
    {
        public readonly string _mySQlConnectionString;
        public readonly string _SQLServerConnectionString;
        private readonly ILogger<EmployeeDAL> _logger;

        public DepartmentDAL(IConfiguration configuration, ILogger<EmployeeDAL> logger)
        {
            _logger = logger;
            _mySQlConnectionString = configuration.GetConnectionString("MySqlConnection");
            _SQLServerConnectionString = configuration.GetConnectionString("SQLServerConnection");
        }
        public DepartmentModel MapReaderMySQlToDepartmentModel(MySqlDataReader reader)
        {
            return new DepartmentModel
            {
                DepartmentId = reader.GetInt32("DepartmentID"),
                DepartmentName = reader.GetString("DepartmentName"),
            };
        }
        public DepartmentModel MapReaderSQLServerToDepartmentModel(SqlDataReader reader)
        {
            return new DepartmentModel
            {
                DepartmentId = reader.GetInt32("DepartmentID"),
                DepartmentName = reader.GetString("DepartmentName"),
            };
        }

        public async Task<List<DepartmentModel>> getDepartments()
        {
            List<DepartmentModel> departments = new List<DepartmentModel>();
            using var connectionMySQL = new MySqlConnection(_mySQlConnectionString);
            MySqlDataReader readerMySQL = null;
            try
            {
                await connectionMySQL.OpenAsync();
                string query = "SELECT DepartmentID, DepartmentName FROM departments";
                MySqlCommand command = new MySqlCommand(query, connectionMySQL);
                readerMySQL = (MySqlDataReader)await command.ExecuteReaderAsync();
                while (await readerMySQL.ReadAsync())
                {
                    DepartmentModel department = MapReaderMySQlToDepartmentModel(readerMySQL);
                    departments.Add(department);
                }
                _logger.LogInformation("Successfully retrieved all departments.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
            }
            finally
            {
                if (readerMySQL != null)
                {
                    await readerMySQL.CloseAsync();
                }
                await connectionMySQL.CloseAsync();
            }
            return departments;
        }
        public async Task<string> GetDepartmentByID(int DepartmentID)
        {
            using var connectionSQlServer = new SqlConnection(_SQLServerConnectionString);
            SqlDataReader readerSQLServer = null;
            try
            {
                string department;
                await connectionSQlServer.OpenAsync();
                string query = @"SELECT DepartmentName FROM Departments  WHERE DepartmentID = @DepartmentID";
                SqlCommand command = new SqlCommand(query, connectionSQlServer);
                command.Parameters.AddWithValue("@DepartmentID", DepartmentID);
       
                readerSQLServer = (SqlDataReader)await command.ExecuteReaderAsync();
                if (await readerSQLServer.ReadAsync())
                {
                    department = readerSQLServer.GetString(readerSQLServer.GetOrdinal("DepartmentName"));
                }
                else
                {
                    return null;
                }
                    _logger.LogInformation("Successfully retrieved employee.");
                return department;
            }
            catch
            (Exception ex)
             
            {
                _logger.LogError(ex, "Error retrieving department by ID");
                throw;
            }
            finally
            {
                if (readerSQLServer != null)
                {
                    await readerSQLServer.CloseAsync();
                }
                await connectionSQlServer.CloseAsync();
            }
        }
    }
}
