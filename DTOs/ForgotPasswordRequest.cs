using System.ComponentModel.DataAnnotations;

namespace LoginAuthentication.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}
