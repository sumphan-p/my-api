using AuthAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly IJwtService _jwt;

    public WellKnownController(IJwtService jwt)
    {
        _jwt = jwt;
    }

    [HttpGet(".well-known/jwks.json")]
    [ResponseCache(Duration = 3600)] // Cache 1 hour
    public IActionResult GetJwks()
    {
        return Ok(_jwt.GetJwks());
    }
}
