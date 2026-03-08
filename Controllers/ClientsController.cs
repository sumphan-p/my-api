using AuthAPI.Extensions;
using AuthAPI.DTOs;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[ApiController]
[Route("api/v1/clients")]
[Authorize(Roles = "admin")]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _clients;
    private readonly IPasswordService _password;
    private readonly IAuditService _audit;

    public ClientsController(IClientRepository clients, IPasswordService password, IAuditService audit)
    {
        _clients = clients;
        _password = password;
        _audit = audit;
    }

    // GET /api/v1/clients
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _clients.GetAllAsync();
        var response = clients.Select(c => new ClientResponse(c.Id, c.ClientId, c.Name, c.IsActive, c.CreatedAt));
        return Ok(ApiResponse<IEnumerable<ClientResponse>>.Success(response));
    }

    // POST /api/v1/clients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientRequest req)
    {
        if (await _clients.ExistsByClientIdAsync(req.ClientId))
            return Conflict(ApiErrorResponse.Create("CLIENT_EXISTS", "ClientId already exists"));

        var plainSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var hashedSecret = _password.HashPassword(plainSecret);
        var id = await _clients.CreateAsync(req.ClientId, hashedSecret, req.Name);

        await _audit.LogAsync(null, AuditActions.ClientCreated, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent(), $"ClientId: {req.ClientId}");

        return CreatedAtAction(nameof(GetAll), null,
            ApiResponse<ClientCreateResponse>.Success(
                new ClientCreateResponse(id, req.ClientId, req.Name, plainSecret),
                "Client created. Save the secret — it will not be shown again."));
    }

    // PUT /api/v1/clients/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClientRequest req)
    {
        if (await _clients.ExistsByClientIdExcludingAsync(req.ClientId, id))
            return Conflict(ApiErrorResponse.Create("CLIENT_EXISTS", "ClientId already exists"));

        if (!await _clients.UpdateAsync(id, req.ClientId, req.Name))
            return NotFound(ApiErrorResponse.Create("CLIENT_NOT_FOUND", "Client not found"));

        return Ok(ApiResponse<object>.Success(new { Id = id }, "Client updated"));
    }

    // DELETE /api/v1/clients/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clients.DisableAsync(id);
        if (client == null)
            return NotFound(ApiErrorResponse.Create("CLIENT_NOT_FOUND", "Client not found"));

        await _audit.LogAsync(null, AuditActions.ClientDisabled, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent(), $"ClientId: {client.ClientId}");

        return Ok(ApiResponse<object>.Success(new { Id = id }, "Client disabled"));
    }
}
