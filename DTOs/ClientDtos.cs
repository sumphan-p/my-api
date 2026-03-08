using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs;

public class ClientRequest
{
    [Required][StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    [Required][StringLength(200)]
    public string Name { get; set; } = string.Empty;
}

public record ClientResponse(int Id, string ClientId, string Name, bool IsActive, DateTime CreatedAt);
public record ClientCreateResponse(int Id, string ClientId, string Name, string ClientSecret);
