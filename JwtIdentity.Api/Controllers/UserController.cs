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
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> GetUserInformation()
        {
            var user = _userManager.Users.Include(x => x.Claims).Include(x => x.Roles).FirstOrDefault(x => x.Id == HttpContext.User.Identity.Name);
            return Ok(user);
        }
    }
}