// File: Integration-System/DAL/EmployeeDAL.cs
using Integration_System.Model;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.DataClassification;
using System.Data;
using Integration_System.Dtos.EmployeeDTO;
using MySqlX.XDevAPI.Common;
using System.Collections.Generic;
using Integration_System.Dtos.ReportDto;
using Microsoft.AspNetCore.Identity;
using static Integration_System.ENUM;
using Microsoft.AspNetCore.Mvc;
using Integration_System.Constants;
using Integration_System.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Integration_System.DAL
{
    public class EmployeeDAL
    {
        public readonly string _mySQlConnectionString;
        public readonly string _SQLServerConnectionString;
   
        private readonly ILogger<EmployeeDAL> _logger;
        private readonly IAuthService _authService;

        public EmployeeDAL(ILogger<EmployeeDAL> logger, IConfiguration configuration, IAuthService authService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _mySQlConnectionString = configuration?.GetConnectionString("MySqlConnection")
                                     ?? throw new ArgumentNullException(nameof(configuration), "MySqlConnection string is null.");
            _SQLServerConnectionString = configuration?.GetConnectionString("SQLServerConnection")
                                         ?? throw new ArgumentNullException(nameof(configuration), "SQLServerConnection string is null.");
        }
        public EmployeeModel MapReaderMySQlToEmployeeModel(MySqlDataReader reader)
        {
            return new EmployeeModel
            {
                EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                DepartmentId = reader.IsDBNull(reader.GetOrdinal("DepartmentID"))
                ? 0 : reader.GetInt32(reader.GetOrdinal("DepartmentID")),
                PositionId = reader.IsDBNull(reader.GetOrdinal("PositionID"))
                ? 0 : reader.GetInt32(reader.GetOrdinal("PositionID")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
            };
        }

        public EmployeeModel MapReaderSQLServerToEmployeeModel(SqlDataReader reader)
        {
            return new EmployeeModel
            {
                EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                DateofBirth = reader.GetDateTime(reader.GetOrdinal("DateofBirth")),
                Gender = reader.IsDBNull(reader.GetOrdinal("Gender"))
                ? "null" : reader.GetString(reader.GetOrdinal("Gender")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                HireDate = reader.GetDateTime(reader.GetOrdinal("HireDate")),
                DepartmentId = reader.IsDBNull(reader.GetOrdinal("DepartmentID"))
                ? 0 : reader.GetInt32(reader.GetOrdinal("DepartmentID")),
                PositionId = reader.IsDBNull(reader.GetOrdinal("PositionID"))
                ? 0 : reader.GetInt32(reader.GetOrdinal("PositionID")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.GetDateTime("UpdatedAt")
            };
        }

        public EmployeeGetDTO MapReaderSQLServerToGetEmployeeModel(SqlDataReader reader)
        {
            return new EmployeeGetDTO
            {

                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateofBirth")),
                Gender = reader.GetString(reader.GetOrdinal("Gender")),
                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                HireDate = reader.GetDateTime(reader.GetOrdinal("HireDate")),
                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentID")),
                PositionId = reader.GetInt32(reader.GetOrdinal("PositionID")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
            };
        }

        public async Task<List<EmployeeModel>> GetAllEmployeesAsync()
        {
            List<EmployeeModel> employees = new List<EmployeeModel>();
            using var connectionSQLServer = new SqlConnection(_SQLServerConnectionString);
            SqlDataReader? readerSQLServer = null;
            try
            {
                await connectionSQLServer.OpenAsync();
                _logger.LogInformation("✅ Kết nối SQL Server thành công!");
                string query = "SELECT * FROM Employees";
                using (SqlCommand command = new SqlCommand(query, connectionSQLServer))
                {
                    readerSQLServer = await command.ExecuteReaderAsync();
                    while (await readerSQLServer.ReadAsync())
                    {
                        EmployeeModel employee = MapReaderSQLServerToEmployeeModel(readerSQLServer);
                        employees.Add(employee);
                    }
                }
                _logger.LogInformation("Successfully retrieved {Count} employees.", employees.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all employees from SQL Server");
                return new List<EmployeeModel>();
            }
            finally
            {
                if (readerSQLServer != null && !readerSQLServer.IsClosed)
                {
                    await readerSQLServer.CloseAsync();
                }
                if (connectionSQLServer.State == ConnectionState.Open)
                {
                    await connectionSQLServer.CloseAsync();
                    _logger.LogDebug("SQL Server connection closed for GetAllEmployeesAsync.");
                }
            }
            return employees;
        }
       

        public async Task<bool> InsertEmployeeAsync(EmployeeInsertDTO employeeDTO)
        {
            if (employeeDTO == null)
            {
                _logger.LogError("InsertEmployeeAsync called with a null employeeDTO.");
                throw new ArgumentNullException(nameof(employeeDTO));
            }
            if (string.IsNullOrWhiteSpace(employeeDTO.FullName) || string.IsNullOrWhiteSpace(employeeDTO.Email))
            {
                _logger.LogError("InsertEmployeeAsync called with missing FullName or Email.");
                return false;
            }

            using var connectionSQLServer = new SqlConnection(_SQLServerConnectionString);
            using var connectionMySQL = new MySqlConnection(_mySQlConnectionString);
     
            int? newEmployeeID = null;

            try
            {
                await connectionSQLServer.OpenAsync();
             
                _logger.LogInformation("✅ Kết nối SQL Server thành công và bắt đầu transaction.");

                string querySQLServer = @"
            INSERT INTO Employees
            (FullName, DateofBirth, Gender, PhoneNumber, Email, HireDate, DepartmentID, PositionID, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.EmployeeID
            VALUES
            (@FullName, @DateofBirth, @Gender, @PhoneNumber, @Email, @HireDate, @DepartmentId, @PositionId, @Status, @CreatedAt, @UpdatedAt);";

                using (SqlCommand commandSQLServer = new SqlCommand(querySQLServer, connectionSQLServer))
                {
                    commandSQLServer.Parameters.AddWithValue("@FullName", employeeDTO.FullName);
                    commandSQLServer.Parameters.AddWithValue("@DateofBirth", employeeDTO.DateofBirth);
                    commandSQLServer.Parameters.AddWithValue("@Gender", (object)employeeDTO.Gender ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@PhoneNumber", (object)employeeDTO.PhoneNumber ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@Email", employeeDTO.Email);
                    commandSQLServer.Parameters.AddWithValue("@HireDate", employeeDTO.HireDate);
                    commandSQLServer.Parameters.AddWithValue("@DepartmentId", (object)employeeDTO.DepartmentId ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@PositionId", (object)employeeDTO.PositionId ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@Status", (object)employeeDTO.Status ?? "working");
                    commandSQLServer.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    commandSQLServer.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    object? insertedIdObj = await commandSQLServer.ExecuteScalarAsync();
                    if (insertedIdObj == null || !int.TryParse(insertedIdObj.ToString(), out int id))
                    {
                        _logger.LogError("❌ Insert vào SQL Server thất bại hoặc không trả về ID.");
                     
                        return false;
                    }
                    newEmployeeID = id;
                    _logger.LogInformation($"✅ Insert vào SQL Server thành công, ID mới: {newEmployeeID}");
                }

                _logger.LogInformation("Đang tạo người dùng Identity...");

                await connectionMySQL.OpenAsync();
                _logger.LogInformation("✅ Kết nối MySQL thành công!");

                string queryMySQL = @"INSERT INTO employees (EmployeeID, FullName, DepartmentID, PositionID, Status)
                              VALUES (@EmployeeID, @FullName, @DepartmentId, @PositionId, @Status)";
                using (MySqlCommand commandMySQL = new MySqlCommand(queryMySQL, connectionMySQL))
                {
                    commandMySQL.Parameters.AddWithValue("@EmployeeID", newEmployeeID.Value);
                    commandMySQL.Parameters.AddWithValue("@FullName", employeeDTO.FullName);
                    commandMySQL.Parameters.AddWithValue("@DepartmentId", employeeDTO.DepartmentId);
                    commandMySQL.Parameters.AddWithValue("@PositionId", employeeDTO.PositionId);
                    commandMySQL.Parameters.AddWithValue("@Status", employeeDTO.Status ?? "working");

                    int rowsAffectedMySQL = await commandMySQL.ExecuteNonQueryAsync();
                    if (rowsAffectedMySQL <= 0)
                    {
                        _logger.LogError("❌ Insert vào MySQL thất bại (không có dòng nào bị ảnh hưởng). Rollback và xóa user.");
                      
                        return false;
                    }
                    _logger.LogInformation("✅ Insert vào MySQL thành công.");
                }

             
                _logger.LogInformation("✅ Insert thành công vào cả hai hệ thống và tạo Identity user. SQL Transaction Committed.");
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi nghiêm trọng trong quá trình InsertEmployeeAsync cho Email: {Email}", employeeDTO?.Email);
                return false;
            }
            finally
            {
                if (connectionSQLServer.State == ConnectionState.Open) await connectionSQLServer.CloseAsync();
                if (connectionMySQL.State == ConnectionState.Open) await connectionMySQL.CloseAsync();
                _logger.LogDebug("Connections closed for InsertEmployeeAsync.");
            }
        }
        private void LogIdentityErrors(string operation, string username, IEnumerable<IdentityError> errors)
        {
            var errorDetails = string.Join("; ", errors.Select(e => $"{e.Code}: {e.Description}"));
            _logger.LogError("Identity operation '{Operation}' failed for user '{Username}'. Errors: {Errors}", operation, username, errorDetails);
        }

        public async Task<bool> DeleteEmployeeAsync(int EmployeeId)
        {
            using var connectionSQLServer = new SqlConnection(_SQLServerConnectionString);
            using var connectionMySQL = new MySqlConnection(_mySQlConnectionString);
            SqlTransaction? sqlTransaction = null;
            string? emailToDelete = null;

            try
            {
                await connectionSQLServer.OpenAsync();
                sqlTransaction = connectionSQLServer.BeginTransaction();
                _logger.LogDebug("SQL Server connection opened and transaction started for DeleteEmployeeAsync (ID: {EmployeeId}).", EmployeeId);

                string getEmailQuery = "SELECT Email FROM Employees WHERE EmployeeID = @EmployeeId";
                using (SqlCommand getEmailCommand = new SqlCommand(getEmailQuery, connectionSQLServer, sqlTransaction))
                {
                    getEmailCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    object? result = await getEmailCommand.ExecuteScalarAsync();
                    emailToDelete = result?.ToString();
                }

                if (string.IsNullOrEmpty(emailToDelete))
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found or email is missing in SQL Server. Cannot proceed with deletion.", EmployeeId);
                    await sqlTransaction.RollbackAsync();
                    return false;
                }
                _logger.LogInformation("Found email '{Email}' for EmployeeID {EmployeeId}. Proceeding with deletion.", emailToDelete, EmployeeId);

                string querySQLServer = "DELETE FROM Employees WHERE EmployeeID = @EmployeeId;";
                using (SqlCommand commandSQLServer = new SqlCommand(querySQLServer, connectionSQLServer, sqlTransaction))
                {
                    commandSQLServer.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    int rowsAffectedSQLServer = await commandSQLServer.ExecuteNonQueryAsync();
                    if (rowsAffectedSQLServer == 0)
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} was not found during SQL Server DELETE operation (potentially deleted concurrently). Rolling back.", EmployeeId);
                        await sqlTransaction.RollbackAsync();
                        return false;
                    }
                    _logger.LogInformation("Successfully deleted record from SQL Server Employees table for ID: {EmployeeId}", EmployeeId);
                }

                await connectionMySQL.OpenAsync();
                _logger.LogDebug("MySQL connection opened for DeleteEmployeeAsync (ID: {EmployeeId}).", EmployeeId);

                string deleteSalariesQuery = "DELETE FROM salaries WHERE EmployeeID = @EmployeeId";
                using (MySqlCommand deleteSalariesCommand = new MySqlCommand(deleteSalariesQuery, connectionMySQL))
                {
                    deleteSalariesCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    int salaryRowsAffected = await deleteSalariesCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Deleted {Count} salary records from MySQL for EmployeeID: {EmployeeId}", salaryRowsAffected, EmployeeId);
                }

                string deleteAttendanceQuery = "DELETE FROM attendance WHERE EmployeeID = @EmployeeId";
                using (MySqlCommand deleteAttendanceCommand = new MySqlCommand(deleteAttendanceQuery, connectionMySQL))
                {
                    deleteAttendanceCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    int attendanceRowsAffected = await deleteAttendanceCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Deleted {Count} attendance records from MySQL for EmployeeID: {EmployeeId}", attendanceRowsAffected, EmployeeId);
                }

                string queryMySQL = @"DELETE FROM employees WHERE EmployeeID = @EmployeeId";
                using (MySqlCommand commandMySQL = new MySqlCommand(queryMySQL, connectionMySQL))
                {
                    commandMySQL.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    int rowsAffectedMySQL = await commandMySQL.ExecuteNonQueryAsync();
                    if (rowsAffectedMySQL == 0)
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} was not found during MySQL DELETE operation (employees table).", EmployeeId);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully deleted record from MySQL employees table for ID: {EmployeeId}", EmployeeId);
                    }
                }

                //bool authUserDeleted = await _authService.DeleteUser(emailToDelete);

                //if (!authUserDeleted)
                //{
                //    _logger.LogError("CRITICAL: Failed to delete Identity user with email {Email} (associated with EmployeeID {EmployeeId}). Manual cleanup of Identity user might be required!", emailToDelete, EmployeeId);
                //}
                //else
                //{
                //    _logger.LogInformation("Successfully requested deletion of Identity user with email {Email}.", emailToDelete);
                //}

                //await sqlTransaction.CommitAsync();
                _logger.LogInformation("SQL Server transaction committed for DeleteEmployeeAsync (ID: {EmployeeId}). Employee deletion process completed (Identity user deletion status logged separately).", EmployeeId);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during DeleteEmployeeAsync for EmployeeID: {EmployeeId}", EmployeeId);
                if (sqlTransaction != null && connectionSQLServer.State == ConnectionState.Open)
                {
                    try
                    {
                        await sqlTransaction.RollbackAsync();
                        _logger.LogWarning("Rolled back SQL Server transaction due to exception during delete.");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback SQL Server transaction during delete.");
                    }
                }
                return false;
            }
            finally
            {
                if (connectionSQLServer.State == ConnectionState.Open)
                {
                    await connectionSQLServer.CloseAsync();
                    _logger.LogDebug("SQL Server connection closed for DeleteEmployeeAsync.");
                }
                if (connectionMySQL.State == ConnectionState.Open)
                {
                    await connectionMySQL.CloseAsync();
                    _logger.LogDebug("MySQL connection closed for DeleteEmployeeAsync.");
                }
            }
        }

        public async Task<bool> UpdateEmployee(int EmployeeId, EmployeeUpdateDTO employeeDTO)
        {
            if (employeeDTO == null)
            {
                _logger.LogError("UpdateEmployee called with a null employeeDTO for ID {EmployeeId}.", EmployeeId);
                throw new ArgumentNullException(nameof(employeeDTO));
            }
            if (string.IsNullOrWhiteSpace(employeeDTO.FullName) || string.IsNullOrWhiteSpace(employeeDTO.Email))
            {
                _logger.LogError("UpdateEmployee called with missing FullName or Email for ID {EmployeeId}.", EmployeeId);
                return false;
            }

            using var connectionSQLServer = new SqlConnection(_SQLServerConnectionString);
            using var connectionMySQL = new MySqlConnection(_mySQlConnectionString);
            SqlTransaction? sqlTransaction = null;
            string? oldEmail = null;

            try
            {
                await connectionSQLServer.OpenAsync();
                sqlTransaction = connectionSQLServer.BeginTransaction();
                _logger.LogDebug("SQL Server connection opened and transaction started for UpdateEmployee (ID: {EmployeeId}).", EmployeeId);

                string getEmailQuery = "SELECT Email FROM Employees WHERE EmployeeID = @EmployeeId";
                using (var cmdGetEmail = new SqlCommand(getEmailQuery, connectionSQLServer, sqlTransaction))
                {
                    cmdGetEmail.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    oldEmail = (await cmdGetEmail.ExecuteScalarAsync())?.ToString();
                }

                if (oldEmail == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found in SQL Server during update attempt.", EmployeeId);
                    await sqlTransaction.RollbackAsync();
                    return false;
                }

                string querySQLServer = @"
                    UPDATE Employees SET
                        FullName = @FullName,
                        DateofBirth = @DateofBirth,
                        Gender = @Gender,
                        PhoneNumber = @PhoneNumber,
                        Email = @Email,
                        HireDate = @HireDate,
                        DepartmentID = @DepartmentId,
                        PositionID = @PositionId,
                        Status = @Status,
                        UpdatedAt = @UpdatedAt
                    WHERE EmployeeID = @EmployeeId";

                using (SqlCommand commandSQLServer = new SqlCommand(querySQLServer, connectionSQLServer, sqlTransaction))
                {
                    commandSQLServer.Parameters.AddWithValue("@FullName", employeeDTO.FullName);
                    commandSQLServer.Parameters.AddWithValue("@DateofBirth", employeeDTO.DateofBirth);
                    commandSQLServer.Parameters.AddWithValue("@Gender", (object)employeeDTO.Gender ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@PhoneNumber", (object)employeeDTO.PhoneNumber ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@Email", employeeDTO.Email);
                    commandSQLServer.Parameters.AddWithValue("@HireDate", employeeDTO.HireDate);
                    commandSQLServer.Parameters.AddWithValue("@DepartmentId", (object)employeeDTO.DepartmentId ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@PositionId", (object)employeeDTO.PositionId ?? DBNull.Value);
                    commandSQLServer.Parameters.AddWithValue("@Status", (object)employeeDTO.Status ?? "working");
                    commandSQLServer.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    commandSQLServer.Parameters.AddWithValue("@EmployeeId", EmployeeId);

                    int affectedRowsSQLServer = await commandSQLServer.ExecuteNonQueryAsync();
                    if (affectedRowsSQLServer == 0)
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} not found during SQL Server UPDATE operation (or no changes made). Rolling back.", EmployeeId);
                        await sqlTransaction.RollbackAsync();
                        return false;
                    }
                    _logger.LogInformation("✅ Updated record in SQL Server Employees table for ID: {EmployeeId}", EmployeeId);
                }

                await connectionMySQL.OpenAsync();
                _logger.LogDebug("MySQL connection opened for UpdateEmployee (ID: {EmployeeId}).", EmployeeId);

                string queryMySQL = @"
                    UPDATE employees SET
                        FullName = @FullName,
                        DepartmentID = @DepartmentId,
                        PositionID = @PositionId,
                        Status = @Status
                    WHERE EmployeeID = @EmployeeId";

                using (MySqlCommand commandMySQL = new MySqlCommand(queryMySQL, connectionMySQL))
                {
                    commandMySQL.Parameters.AddWithValue("@FullName", employeeDTO.FullName);
                    commandMySQL.Parameters.AddWithValue("@DepartmentId", employeeDTO.DepartmentId);
                    commandMySQL.Parameters.AddWithValue("@PositionId", employeeDTO.PositionId);
                    commandMySQL.Parameters.AddWithValue("@Status", employeeDTO.Status ?? "working");
                    commandMySQL.Parameters.AddWithValue("@EmployeeId", EmployeeId);

                    int affectedRowsMySQL = await commandMySQL.ExecuteNonQueryAsync();
                    if (affectedRowsMySQL == 0)
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} not found during MySQL UPDATE operation (or no changes made).", EmployeeId);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Updated record in MySQL employees table for ID: {EmployeeId}", EmployeeId);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during UpdateEmployee for EmployeeID: {EmployeeId}", EmployeeId);
                if (sqlTransaction != null && connectionSQLServer.State == ConnectionState.Open)
                {
                    try { await sqlTransaction.RollbackAsync(); _logger.LogWarning("Rolled back SQL Server transaction due to exception during update."); }
                    catch (Exception rbEx) { _logger.LogError(rbEx, "Error rolling back SQL Server transaction during update."); }
                }
                return false;
            }
            finally
            {
                if (connectionSQLServer.State == ConnectionState.Open) await connectionSQLServer.CloseAsync();
                if (connectionMySQL.State == ConnectionState.Open) await connectionMySQL.CloseAsync();
                _logger.LogDebug("Connections closed for UpdateEmployee.");
            }
        }

        public async Task<EmployeeGetDTO?> GetEmployeeIdAsync(int EmployeeId)
        {
            EmployeeGetDTO? employee = null;
            using var connectionSQLServer = new SqlConnection(_SQLServerConnectionString);
            SqlDataReader? readerSQLServer = null;
            try
            {
                await connectionSQLServer.OpenAsync();
                _logger.LogDebug("SQL Server connection opened for GetEmployeeIdAsync (ID: {EmployeeId}).", EmployeeId);
                string query = "SELECT FullName, DateofBirth, Gender, PhoneNumber, Email, HireDate, DepartmentID, PositionID, Status FROM Employees WHERE EmployeeID = @EmployeeId";
                using (SqlCommand command = new SqlCommand(query, connectionSQLServer))
                {
                    command.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                    readerSQLServer = await command.ExecuteReaderAsync();

                    if (await readerSQLServer.ReadAsync())
                    {
                        employee = MapReaderSQLServerToGetEmployeeModel(readerSQLServer);
                        _logger.LogInformation("Successfully retrieved employee with ID: {EmployeeId}.", EmployeeId);
                    }
                    else
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} not found.", EmployeeId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting employee with ID: {EmployeeId}", EmployeeId);
                return null;
            }
            finally
            {
                if (readerSQLServer != null && !readerSQLServer.IsClosed)
                {
                    await readerSQLServer.CloseAsync();
                }
                if (connectionSQLServer.State == ConnectionState.Open)
                {
                    await connectionSQLServer.CloseAsync();
                    _logger.LogDebug("SQL Server connection closed for GetEmployeeIdAsync.");
                }
            }
            return employee;
        }

        public async Task<List<DistributionByDeptDto>> GetEmployeeDistributionByDeptAsync()
        {
            var distribution = new List<DistributionByDeptDto>();
            using var connection = new SqlConnection(_SQLServerConnectionString);
            string query = @"
                SELECT ISNULL(d.DepartmentName, 'N/A') as DepartmentName, COUNT(e.EmployeeID) as EmployeeCount
                FROM Employees e
                LEFT JOIN Departments d ON e.DepartmentID = d.DepartmentID
                GROUP BY ISNULL(d.DepartmentName, 'N/A')
                ORDER BY DepartmentName;";
            _logger.LogDebug("Executing query for employee distribution by department.");
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            distribution.Add(new DistributionByDeptDto
                            {
                                DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                                EmployeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                            });
                        }
                    }
                    _logger.LogInformation("Successfully retrieved {Count} department distribution records.", distribution.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting employee distribution by department.");
                    return new List<DistributionByDeptDto>();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                    _logger.LogDebug("SQL Server connection closed for GetEmployeeDistributionByDeptAsync.");
                }
            }
            return distribution;
        }

        public async Task<List<StatusDistributionDto>> GetEmployeeDistributionByStatusAsync()
        {
            var distribution = new List<StatusDistributionDto>();
            using var connection = new SqlConnection(_SQLServerConnectionString);
            string query = @"
                SELECT ISNULL(Status, 'N/A') as Status, COUNT(EmployeeID) as EmployeeCount
                FROM Employees
                GROUP BY ISNULL(Status, 'N/A')
                ORDER BY Status;";
            _logger.LogDebug("Executing query for employee distribution by status.");
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            distribution.Add(new StatusDistributionDto
                            {
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                EmployeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                            });
                        }
                    }
                    _logger.LogInformation("Successfully retrieved {Count} status distribution records.", distribution.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting employee distribution by status.");
                    return new List<StatusDistributionDto>();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                    _logger.LogDebug("SQL Server connection closed for GetEmployeeDistributionByStatusAsync.");
                }
            }
            return distribution;
        }

        private string GetRoleForDepartment(int departmentId)
        {
            return departmentId switch
            {
                1 => UserRoles.Hr,
                2 => UserRoles.PayrollManagement,
                _ => UserRoles.Employee
            };
        }

    }
}