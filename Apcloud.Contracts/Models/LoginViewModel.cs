using System.ComponentModel.DataAnnotations;

namespace Apcloud.Contracts.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username or email address.")]
    [Display(Name = "Username or email")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
