using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace my_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly DbSettings _dbSettings;

    public TestController(DbSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestConnection()
    {

        try
        {
            using var connection = new SqlConnection(_dbSettings.ConnectionString);
            await connection.OpenAsync();

            return Ok(new { status = "success", message = "Database connection successful" });
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}
