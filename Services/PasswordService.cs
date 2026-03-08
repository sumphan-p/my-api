using AuthAPI.Options;
using Microsoft.Extensions.Options;

namespace AuthAPI.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordService : IPasswordService
{
    private readonly int _workFactor;

    public PasswordService(IOptions<SecurityOptions> options)
    {
        _workFactor = options.Value.BcryptWorkFactor;
    }

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: _workFactor);

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
