using Microsoft.AspNetCore.Identity;

namespace LoginAuthentication.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Step 1: Basic Info fields
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string Country { get; set; } = "England"; // Default value from form
        public string City { get; set; }
        public string ZipCode { get; set; }

        // Step 2: Professional Details fields
        public string? ServicesOffered { get; set; }
        public string? AboutMe { get; set; }
        public bool? AcceptedTermsAndPrivacy { get; set; }

        // Photo upload
        public string? PhotoPath { get; set; } // Stores the path to the uploaded photo

        // Note: IdentityUser already includes:
        // - UserName
        // - Email
        // - PhoneNumber
        // - Password handling
    }
}
