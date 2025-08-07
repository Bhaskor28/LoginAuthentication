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

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _cache = cache;
            _configuration = configuration;
        }



        [HttpPost("Step1BasicInfo")]
        public IActionResult Step1BasicInfo([FromBody] BasicInfo dto)
        {
            var tempId = Guid.NewGuid().ToString();
            _cache.Set(tempId, dto, TimeSpan.FromMinutes(15));

            return Ok(new { message = "Step 1 completed", tempId });
        }


        [HttpPost("Step2ProfessionalDetails")]
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
                ServicesOffered = dto.ServicesOffered,
                AboutMe = dto.AboutMe,
                AcceptedTermsAndPrivacy = dto.AcceptedTermsAndPrivacy
            };

            // Handle photo upload
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

            var result = await _userManager.CreateAsync(user, step1Dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Remove from temp cache
            _cache.Remove(tempId);

            var response = new CreateAccount
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                ServicesOffered = user.ServicesOffered,
                PhotoUrl = user.PhotoPath
            };

            return Ok(response);
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
    }
}
