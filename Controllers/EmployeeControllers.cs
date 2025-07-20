
using Integration_System.DAL;
using Integration_System.Model;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Integration_System.Dtos; 
using Integration_System.Dtos.EmployeeDTO; 
using Integration_System.Middleware ;
using static Integration_System.ENUM; 
using Microsoft.AspNetCore.Authorization; 
using Integration_System.Constants;       
using System.Security.Claims;
using Integration_System.Services;

namespace Integration_System.Controllers
{
    [Route("api/employees")]
    [ApiController]
    [Authorize]
    public class EmployeeControllers : Controller
    {
        private readonly EmployeeDAL _employeeDAL;
        private readonly ILogger<EmployeeControllers> _logger;
        private readonly NotificationSalaryMDW _notificationSalaryMDW;
        private readonly IAuthService _authService;
        private readonly AuthDAL _authDAL;
        public EmployeeControllers(
            EmployeeDAL employeeDAL,
            ILogger<EmployeeControllers> logger,
            NotificationSalaryMDW notificationSalaryMDW,
            IAuthService authService,
            AuthDAL authDAL)
        {
            _employeeDAL = employeeDAL;
            _logger = logger;
            _notificationSalaryMDW = notificationSalaryMDW;
            _authService = authService;
            _authDAL = authDAL;
        }

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}, {UserRoles.PayrollManagement}")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                List<EmployeeModel> employees = await _employeeDAL.GetAllEmployeesAsync();
                _logger.LogInformation("User {User} retrieved all employees successfully.", User.Identity?.Name);
                return Ok(employees ?? Enumerable.Empty<EmployeeModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Internal server error retrieving employees." });
            }
        }

        [HttpGet("{EmployeeId}")]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr},{UserRoles.Employee},{UserRoles.PayrollManagement}")]
        [ProducesResponseType(typeof(EmployeeGetDTO), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int EmployeeId)
        {
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdminOrHr = User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.Hr);

            if (!isAdminOrHr)
            {
                _logger.LogWarning("Potential Forbidden Access: User {UserId} (Role: Employee) requested employee data for employee {TargetEmployeeId}. Access allowed for now, requires ID check implementation.", currentUserIdClaim, EmployeeId);
            }

            try
            {
                EmployeeGetDTO employee = await _employeeDAL.GetEmployeeIdAsync(EmployeeId);
                if (employee == null)
                {
                    _logger.LogWarning($"Employee with ID {EmployeeId} not found when requested by user {User.Identity?.Name}.");
                    return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Employee with ID {EmployeeId} not found." });
                }
                _logger.LogInformation($"User {User.Identity?.Name} retrieved employee with ID {EmployeeId} successfully.");
                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {EmployeeId}", EmployeeId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Internal server error retrieving employee." });
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}")]
        [ProducesResponseType(typeof(object), statusCode: 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeInsertDTO employeeDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (employeeDTO == null)
            {
                _logger.LogWarning("CreateEmployee attempted with a null DTO by user {User}.", User.Identity?.Name);
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Employee data cannot be null." });
            }
            try
            {
               
                        bool createdEmployee = await _employeeDAL.InsertEmployeeAsync(employeeDTO);
                        if (!createdEmployee)
                        {
                            _logger.LogError("Failed to insert employee into database after successful check (Attempt by User {User}).", User.Identity?.Name);
                            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Failed to save employee data after validation." });
                        }
                        else
                        {
                            string role = await _authService.setRole(employeeDTO.DepartmentId);
                            AuthModel newAuth = new AuthModel
                            {
                                UserName = employeeDTO.Email,
                                Email = employeeDTO.Email,
                                Password = employeeDTO.PhoneNumber,
                                Role = role
                            };
                            bool resultAuth = await _authDAL.InsertNewUser(newAuth);
                            if (!resultAuth)
                            {
                                _logger.LogError("Failed to create user for employee {FullName} (Attempt by User {User}).", employeeDTO.FullName, User.Identity?.Name);
                                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Failed to create user for the employee." });
                            }
                            _logger.LogInformation("User {User} created employee successfully: {FullName}", User.Identity?.Name, employeeDTO.FullName);
                            return Ok(new { Message = "Employee Inserted successfully." });
                        }
                   
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee {FullName} by user {User}", employeeDTO.FullName, User.Identity?.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "An internal error occurred while creating the employee." });
            }
        }

        [HttpPut("{EmployeeId}")]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}")]
        [ProducesResponseType(typeof(object), statusCode: 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int EmployeeId, [FromBody] EmployeeUpdateDTO employeeDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (employeeDTO == null)
            {
                _logger.LogWarning("UpdateEmployee attempted with a null DTO for ID {EmployeeId} by user {User}.", EmployeeId, User.Identity?.Name);
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Employee data cannot be null." });
            }
            try
            {
                bool updatedEmployee = await _employeeDAL.UpdateEmployee(EmployeeId, employeeDTO);
                if (!updatedEmployee)
                {
                    _logger.LogWarning($"UpdateEmployee failed for ID {EmployeeId} (Attempt by User {User}). Employee might not exist or DB error occurred.", User.Identity?.Name);
                    return NotFound(new ProblemDetails { Title = "Operation Failed", Detail = $"Failed to update employee with ID {EmployeeId}. Ensure the ID is correct." });
                }
                _logger.LogInformation($"User {User.Identity?.Name} updated employee with ID {EmployeeId} successfully.");
                return Ok(new { Message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId} by user {User}", EmployeeId, User.Identity?.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "An internal error occurred while updating the employee." });
            }
        }

        [HttpDelete("{EmployeeId}")]
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}")]
        [ProducesResponseType(typeof(object), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int EmployeeId)
        {
            try
            {
                bool deletedEmployee = await _employeeDAL.DeleteEmployeeAsync(EmployeeId);

                if (!deletedEmployee)
                {
                    _logger.LogWarning($"DeleteEmployee failed for ID {EmployeeId} (Attempt by User {User}). Employee might not exist or DB error occurred.", User.Identity?.Name);
                    return NotFound(new ProblemDetails { Title = "Operation Failed", Detail = $"Failed to delete employee with ID {EmployeeId}. Ensure the ID is correct." });
                }

                await _notificationSalaryMDW.CheckAndNotificationLeave(true, EmployeeId);
                
                _logger.LogInformation($"User {User.Identity?.Name} deleted employee with ID {EmployeeId} successfully.");
                return Ok(new { Message = "Employee Deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId} by user {User}", EmployeeId, User.Identity?.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "An internal error occurred while deleting the employee." });
            }
        }
    }
}