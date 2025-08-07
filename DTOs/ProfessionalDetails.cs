using System.ComponentModel.DataAnnotations;

namespace LoginAuthentication.DTOs
{
    public class ProfessionalDetails
    {
        [Display(Name = "Your Photo")]
        public IFormFile Photo { get; set; }

        [Required]
        [Display(Name = "Your Services")]
        public string ServicesOffered { get; set; }

        [Required]
        [Display(Name = "About Me")]
        public string AboutMe { get; set; }

        [Required]
        [Display(Name = "I accept the Terms & Privacy Policy")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and privacy policy")]
        public bool AcceptedTermsAndPrivacy { get; set; }
    }
}
