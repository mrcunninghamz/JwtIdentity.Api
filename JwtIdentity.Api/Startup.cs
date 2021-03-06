﻿using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using ForTheWinGamingLeague.Api.Models;
using JwtIdentity.Api.Data;
using JwtIdentity.Api.Data.Entities;
using JwtIdentity.Api.Models;
using JwtIdentity.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace JwtIdentity.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // Add our Config object so it can be injected
            services.Configure<Settings>(Configuration.GetSection("Settings"));

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));

                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict();
            });

            // Register the Identity services.
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            // Register the OpenIddict services.
            // Note: use the generic overload if you need
            // to replace the default OpenIddict entities.
            services.AddOpenIddict(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                options.AddMvcBinders();

                // Enable the token endpoint (required to use the password flow).
                options.EnableTokenEndpoint("/connect/token");

                // Allow client applications to use the grant_type=password flow.
                options.AllowPasswordFlow();

                // During development, you can disable the HTTPS requirement.
                options.DisableHttpsRequirement();

                // Note: to use JWT access tokens instead of the default
                // encrypted format, the following lines are required:
                options.UseJsonWebTokens();
                options.AddEphemeralSigningKey();
            });

            services.AddApplicationPolicies();
            
            // Add Swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Title = "JwtIdentity API",
                    Version = "v1",
                    Description = "This is the boilerplate API. \n\n" +
                                  "To get a token use Postman to send an x-www-form-urlencoded POST to '/connect/token' with grant_type = 'password', username, and password.",
                    TermsOfService = "NA"
                });

                options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();

                // include documentation from XML
                var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "JwtIdentity.Api.xml");
                options.IncludeXmlComments(filePath);
            });

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IOptions<Settings> settings)
        {
            Task.Run(() => InitializeUsers(roleManager, userManager)).Wait();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseOAuthValidation();

            // If you prefer using JWT, don't forget to disable the automatic
            // JWT -> WS-Federation claims mapping used by the JWT middleware:

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                Authority = settings.Value.Authority,
                Audience = "resource-server",
                RequireHttpsMetadata = false,
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = OpenIdConnectConstants.Claims.Subject,
                    RoleClaimType = OpenIdConnectConstants.Claims.Role
                }
            });

            app.UseOpenIddict();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "JwtIdentity API");
            });
        }

        // Initialize some test roles. In the real world, these would be setup explicitly by a role manager
        // Role claims: http://benfoster.io/blog/asp-net-identity-role-claims
        // TODO: move to some other class
        private async Task InitializeUsers(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            var roles = new Dictionary<string, IEnumerable<Claim>>
            {
                {
                    "User",
                    new List<Claim>
                    {
                        new Claim(CustomClaimsType.Permission, "profile.view"),
                        new Claim(CustomClaimsType.Permission, "profile.update"),
                        new Claim(CustomClaimsType.Permission, "event.view"),
                        new Claim(CustomClaimsType.Permission, "event.signup"),
                    }
                },  
                {
                    "Administrator",   
                    new List<Claim>
                    {
                        new Claim(CustomClaimsType.Permission, "profile.view"),
                        new Claim(CustomClaimsType.Permission, "profile.update"),
                        new Claim(CustomClaimsType.Permission, "event.view"),
                        new Claim(CustomClaimsType.Permission, "event.signup"),
                        new Claim(CustomClaimsType.Permission, "event.update"),
                        new Claim(CustomClaimsType.Permission, "event.delete")
                    }
                }
                
            };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Key))
                {
                    var newRole = new IdentityRole(role.Key);
                    await roleManager.CreateAsync(newRole);

                    foreach (var claim in role.Value)
                    {
                        await roleManager.AddClaimAsync(newRole, claim);
                    }
                }
                else
                {
                    var dbRole = roleManager.Roles.Include(x => x.Claims).First(x => x.Name.Equals(role.Key));
                    var roleClaims = role.Value;
                    
                    // delete claims that no longer exists
                    var claimsToDelete = dbRole.Claims.Where(x => x.ClaimType.Equals(CustomClaimsType.Permission) && !roleClaims.Select(rc => rc.Value).Contains(x.ClaimValue)).ToList();
                    foreach (var identityRoleClaim in claimsToDelete)
                    {
                        await roleManager.RemoveClaimAsync(dbRole, identityRoleClaim.ToClaim());
                    }

                    // add new claims
                    var claimsToAdd = roleClaims
                                        .Where(rc => 
                                            !dbRole.Claims
                                                    .Where(dbc => dbc.ClaimType.Equals(CustomClaimsType.Permission))
                                                    .Select(dbc => dbc.ClaimValue)
                                                    .Contains(rc.Value)
                                        );

                    foreach (var identityRoleClaim in claimsToAdd)
                    {
                        await roleManager.AddClaimAsync(dbRole, identityRoleClaim);
                    }
                }
            }

            // NOTE: Every seeded user is an administrator
            var users = new List<ApplicationUser>
            {
                new ApplicationUser {UserName = "kmerecido@gmail.com"}
            };

            foreach (var user in users)
            {
                if (!userManager.Users.Any(u => u.UserName == user.UserName))
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(user, "test123");
                    user.PasswordHash = hashed;

                    await userManager.CreateAsync(user);
                    await userManager.AddToRoleAsync(user, "Administrator");
                }
            }
        }
    }
}
