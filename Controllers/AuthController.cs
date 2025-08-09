using LoginAuthentication.DTOs;
using LoginAuthentication.Models;
using LoginAuthentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoginAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IOtpService _otpService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env,
            IMemoryCache cache,
            IConfiguration configuration,
            EmailService emailService,
            IOtpService otpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _cache = cache;
            _configuration = configuration;
            _emailService = emailService;
            _otpService = otpService;
        }




        [HttpPost("step1-basic-info")]
        public async Task<IActionResult> Step1BasicInfo([FromBody] BasicInfo dto)
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is already in use" });
            }

            var tempId = Guid.NewGuid().ToString();
            _cache.Set(tempId, dto, TimeSpan.FromMinutes(15));

            return Ok(new { message = "Step 1 completed", tempId });
        }


        [HttpPost("step2-professional-details")]
        public async Task<IActionResult> Step2ProfessionalDetails(
            [FromForm] ProfessionalDetails dto,
            [FromForm] string tempId)
        {
            if (!_cache.TryGetValue<BasicInfo>(tempId, out var step1Dto))
                return NotFound("Step 1 data not found or expired");

            var user = new ApplicationUser
            {
                UserName = step1Dto.UserName,
                Email = step1Dto.Email,
                FullName = step1Dto.FullName,
                PhoneNumber = step1Dto.PhoneNumber,
                DateOfBirth = step1Dto.DateOfBirth,
                Address = step1Dto.Address,
                Country = step1Dto.Country,
                City = step1Dto.City,
                ZipCode = step1Dto.ZipCode,
                OfferedServiceList = dto.OfferedServeceList,
                AboutMe = dto.AboutMe,
                AcceptedTermsAndPrivacy = dto.AcceptedTermsAndPrivacy
            };

            // Handle photo
            if (dto.Photo != null && dto.Photo.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{dto.Photo.FileName}";
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");

                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                var fullPath = Path.Combine(filePath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.Photo.CopyToAsync(stream);
                }

                user.PhotoPath = $"/uploads/{fileName}";
            }

            // Save user info and password to cache
            _cache.Set($"pending_user_{user.Email}", new { user, step1Dto.Password }, TimeSpan.FromMinutes(10));

            // Send OTP
            var otp = _otpService.GenerateOtp(user.Email);
            _emailService.SendEmail(user.Email, "Your OTP Code", $"Your OTP is: {otp}");   // Initially send OTP to email

            return Ok(new { message = "OTP sent to email", email = user.Email });
        }


        [HttpPost("step3-send-otp")]     //used for only resend OTP when user not get OTP first time.
        public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
        {
            var otp = _otpService.GenerateOtp(request.Email);
            _emailService.SendEmail(request.Email, "Your OTP Code", $"Your OTP is: {otp}");
            return Ok(new { message = "OTP sent" });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerify request)
        {
            var isValid = _otpService.VerifyOtp(request.Email, request.OtpCode);
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired OTP" });

            if (!_cache.TryGetValue($"pending_user_{request.Email}", out dynamic cached))
                return NotFound("User info not found or expired");

            ApplicationUser user = cached.user;
            string password = cached.Password;

            var result = await _userManager.CreateAsync(user, password);  //upload user informatin to AspNetUser table.
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Clean up cache
            _cache.Remove($"pending_user_{request.Email}");

            return Ok(new
            {
                message = "OTP verified and account created",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FullName,
                    user.PhotoPath
                }
            });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email) ??
                       await _userManager.FindByNameAsync(model.Email);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid credentials" });

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return BadRequest(new { message = "Email not found" });

            var otp = _otpService.GenerateOtp(request.Email);
            _emailService.SendEmail(request.Email, "Password Reset OTP", $"Your OTP is: {otp}");

            return Ok(new { message = "OTP sent to email" });
        }

        [HttpPost("verify-reset-otp")]
        public IActionResult VerifyResetOtp([FromBody] OtpVerify request)
        {
            var isValid = _otpService.VerifyOtp(request.Email, request.OtpCode);
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired OTP" });

            // Cache the OTP verification flag (email is verified)
            _cache.Set($"reset_verified_{request.Email}", true, TimeSpan.FromMinutes(10));

            return Ok(new { message = "OTP verified. Proceed to reset password." });
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            // Check if OTP was already verified
            if (!_cache.TryGetValue($"reset_verified_{dto.Email}", out bool verified) || !verified)
                return BadRequest(new { message = "OTP session expired" });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { message = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Clear cache after successful reset
            _cache.Remove($"reset_verified_{dto.Email}");

            return Ok(new { message = "Password reset successfully" });
        }

    }
}
