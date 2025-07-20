using Integration_System.Constants;
using Integration_System.DAL;
using Integration_System.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
namespace Integration_System.Controllers
{
    [Route("api/departments")] // Route to access the API
    [ApiController]
    [Authorize]
    public class DepartmenControllers : Controller
    {
        public readonly DepartmentDAL _departmentDAL;
        public readonly ILogger<DepartmenControllers> _logger;
        public DepartmenControllers(DepartmentDAL departmentDAL, ILogger<DepartmenControllers> logger)
        {
            _departmentDAL = departmentDAL;
            _logger = logger;
        }
        [HttpGet] // api/departments
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}, {UserRoles.PayrollManagement}")]
        [ProducesResponseType(typeof(IEnumerable<DepartmentModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                List<DepartmentModel> departments = await _departmentDAL.getDepartments();
                _logger.LogInformation("Retrieved all departments successfully by user {User}", User.Identity?.Name);
                return Ok(departments ?? Enumerable.Empty<DepartmentModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Internal server error retrieving departments." });
            }
        }
        [HttpGet("{DepartmentID}")] // api/departments/{id}
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Hr}, {UserRoles.PayrollManagement}")]
        [ProducesResponseType(typeof(DepartmentModel), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDepartmentById(int DepartmentID)
        {
            try
            {
                string department = await _departmentDAL.GetDepartmentByID(DepartmentID);
                if (department == null)
                {
                    _logger.LogWarning($"Department with ID {DepartmentID} not found");
                    return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Department with ID {DepartmentID} not found." });
                }
                _logger.LogInformation($"Retrieved department with ID {DepartmentID} successfully by user {User.Identity?.Name}");
                return Ok(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department by ID {DepartmentID}", DepartmentID);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Server Error", Detail = "Internal server error retrieving department." });
            }
        }
    }
    
}

