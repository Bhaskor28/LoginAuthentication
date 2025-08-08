using Microsoft.Extensions.Caching.Memory;

namespace LoginAuthentication.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string email);
        bool VerifyOtp(string email, string otp);
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp(string email)
        {
            var otp = new Random().Next(1000, 9999).ToString();
            _cache.Set($"otp_{email}", otp, TimeSpan.FromMinutes(5));
            return otp;
        }

        public bool VerifyOtp(string email, string otp)
        {
            if (_cache.TryGetValue($"otp_{email}", out string storedOtp))
                return storedOtp == otp;

            return false;
        }
    }
}
