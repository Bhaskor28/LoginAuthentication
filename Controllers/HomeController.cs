using Microsoft.AspNetCore.Mvc;

namespace LoginAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : Controller
    {
        
        

        [HttpPost("reliability")]
        public async Task<IActionResult> reliability() {

            return Ok(new { message = "Step 1 completed"});
        }
    }
}
