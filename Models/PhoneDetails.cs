using LoginAuthentication.Models.Common;
using LoginAuthentication.Models.Enums;

namespace LoginAuthentication.Models
{
    public class PhoneDetails : BaseEntity
    {
        public Decimal Coins { get; set; }
        public Status Status { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
