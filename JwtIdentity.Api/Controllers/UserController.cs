using System.Linq;
using System.Threading.Tasks;
using JwtIdentity.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Api.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        [Route("Current")]
        [Authorize("View Profiles")]
        [Produces("application/json")]
        public async Task<IActionResult> GetUserInformation()
        {
            var claims = User.Claims.Select(claim => new { claim.Type, claim.Value }).ToArray();
            return Json(claims);
        }
    }
}