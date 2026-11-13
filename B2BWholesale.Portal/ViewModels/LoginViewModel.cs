using System.ComponentModel.DataAnnotations;

namespace B2BWholesale.Portal.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your username.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
