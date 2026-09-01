using System.ComponentModel.DataAnnotations;

namespace Apcloud.Contracts.Models;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Enter your username or email address.")]
    [StringLength(320, MinimumLength = 3)]
    [Display(Name = "Username or email")]
    public string UserNameOrEmail { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12,128}$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and special characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
