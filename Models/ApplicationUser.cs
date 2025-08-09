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
        public string OfferedServiceId { get; set; }
        public ICollection<OfferedServeces> OfferedServiceList { get; set; }
        public string? AboutMe { get; set; }
        public bool? AcceptedTermsAndPrivacy { get; set; }

        // Photo upload
        public string? PhotoPath { get; set; } // Stores the path to the uploaded photo

        // Note: IdentityUser already includes:
        // - UserName
        // - Email
        // - PhoneNumber
        // - Password handling

        //Verify your identity
        public string? NationalIdOrPassportNumber { get; set; }
        public string? IdDocumentPath { get; set; } // Path to uploaded ID (PDF/JPG/PNG)
        public string? AdditionalNotes { get; set; }
        public string? PhoneNumberId { get; set; }
        public PhoneDetails? PhoneDetails { get; set; }
        public string? WhatsAppId { get; set; }
        public WhatsAppDetails? WhatsAppDetails { get; set; }
        public string? IdentityId { get; set; }
        public IdentityDetails? IdentityDetails { get; set; }
    }
}
