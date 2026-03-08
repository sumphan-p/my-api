using System.ComponentModel.DataAnnotations;
using AuthAPI.DTOs;

namespace AuthAPI.Tests.Models;

public class AuthDtosValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    // === LoginRequest ===

    [Fact]
    public void LoginRequest_Valid_NoErrors()
    {
        var req = new LoginRequest { Username = "testuser", Password = "password" };
        var errors = ValidateModel(req);
        Assert.Empty(errors);
    }

    [Fact]
    public void LoginRequest_EmptyUsername_HasError()
    {
        var req = new LoginRequest { Username = "", Password = "password" };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Username"));
    }

    [Fact]
    public void LoginRequest_PasswordTooLong_HasError()
    {
        var req = new LoginRequest { Username = "user", Password = new string('a', 73) };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Password"));
    }

    [Fact]
    public void LoginRequest_Password72Chars_NoError()
    {
        var req = new LoginRequest { Username = "user", Password = new string('a', 72) };
        var errors = ValidateModel(req);
        Assert.DoesNotContain(errors, e => e.MemberNames.Contains("Password"));
    }

    // === RegisterRequest ===

    [Fact]
    public void RegisterRequest_Valid_NoErrors()
    {
        var req = new RegisterRequest
        {
            Username = "newuser",
            Email = "test@example.com",
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1"
        };
        var errors = ValidateModel(req);
        Assert.Empty(errors);
    }

    [Fact]
    public void RegisterRequest_ShortUsername_HasError()
    {
        var req = new RegisterRequest
        {
            Username = "ab",
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1"
        };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Username"));
    }

    [Fact]
    public void RegisterRequest_ShortPassword_HasError()
    {
        var req = new RegisterRequest
        {
            Username = "newuser",
            Password = "short",
            ConfirmPassword = "short"
        };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Password"));
    }

    [Fact]
    public void RegisterRequest_PasswordMismatch_HasError()
    {
        var req = new RegisterRequest
        {
            Username = "newuser",
            Password = "StrongP@ss1",
            ConfirmPassword = "DifferentPass"
        };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("ConfirmPassword"));
    }

    [Fact]
    public void RegisterRequest_InvalidEmail_HasError()
    {
        var req = new RegisterRequest
        {
            Username = "newuser",
            Email = "not-an-email",
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1"
        };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Email"));
    }

    // === ForgotPasswordRequest ===

    [Fact]
    public void ForgotPasswordRequest_InvalidEmail_HasError()
    {
        var req = new ForgotPasswordRequest { Email = "bad-email" };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("Email"));
    }

    [Fact]
    public void ForgotPasswordRequest_ValidEmail_NoError()
    {
        var req = new ForgotPasswordRequest { Email = "user@example.com" };
        var errors = ValidateModel(req);
        Assert.Empty(errors);
    }

    // === ChangePasswordRequest ===

    [Fact]
    public void ChangePasswordRequest_PasswordTooLong_HasError()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "current",
            NewPassword = new string('a', 73),
            ConfirmNewPassword = new string('a', 73)
        };
        var errors = ValidateModel(req);
        Assert.Contains(errors, e => e.MemberNames.Contains("NewPassword"));
    }
}
