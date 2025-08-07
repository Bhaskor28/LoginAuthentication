namespace LoginAuthentication.DTOs
{
    public class CreateAccount
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ServicesOffered { get; set; }
    }
}
