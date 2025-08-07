using LoginAuthentication.DTOs;
using LoginAuthentication.Models;
using LoginAuthentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LoginAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AuthController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        [HttpPost("Step1BasicInfo")]
        public async Task<IActionResult> Step1BasicInfo([FromBody] BasicInfo dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                Address = dto.Address,
                Country = dto.Country,
                City = dto.City,
                ZipCode = dto.ZipCode
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Step 1 done", userId = user.Id });
        }

        [HttpPost("Step2ProfessionalDetails")]
        public async Task<IActionResult> Step2ProfessionalDetails(
            [FromForm] ProfessionalDetails dto,
            [FromForm] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

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

            user.ServicesOffered = dto.ServicesOffered;
            user.AboutMe = dto.AboutMe;
            user.AcceptedTermsAndPrivacy = dto.AcceptedTermsAndPrivacy;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors);

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
    }
}
