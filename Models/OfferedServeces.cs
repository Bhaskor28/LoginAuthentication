using LoginAuthentication.Models.Common;

namespace LoginAuthentication.Models
{
    public class OfferedServeces : BaseEntity
    {
        public string Name { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
