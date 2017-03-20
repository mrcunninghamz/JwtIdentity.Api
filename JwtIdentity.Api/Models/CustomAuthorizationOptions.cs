using JwtIdentity.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ForTheWinGamingLeague.Api.Models
{
    public static class CustomAuthorizationOptions
    {
        public static IServiceCollection AddApplicationPolicies(this IServiceCollection services)
        {
            return services.AddAuthorization(options =>
            {
                options.AddPolicy("View Profiles",
                    policy => policy.RequireClaim(CustomClaimsType.Permission, "profile.view"));
            });
        }
    }
}
