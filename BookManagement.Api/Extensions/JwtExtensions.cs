using System.Security.Claims;
using System.Text;
using BookManagement.Service.JwtService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BookManagement.Api.Extensions;

public static class JwtExtensions
{
    public const string AdminPolicy = "AdminPolicy";
    public const string CustomerPolicy = "CustomerPolicy";
    public const string StaffPolicy = "StaffPolicy";

    public static void AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));

        var jwtOptions = new JwtOptions();
        configuration.GetSection("JwtOptions").Bind(jwtOptions);
        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<BookManagement.Repository.Data.AppDbContext>();
                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (Guid.TryParse(userIdClaim, out var userId))
                        {
                            var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                            if (user == null || user.Status == BookManagement.Repository.Entities.Enums.UserStatus.LOCKED)
                            {
                                context.Fail("User account is locked or disabled.");
                                return;
                            }

                            var hasActiveSession = await dbContext.UserSessions.AsNoTracking()
                                .AnyAsync(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);

                            if (!hasActiveSession)
                            {
                                context.Fail("Session has been logged out or revoked.");
                                return;
                            }
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole("ADMIN", "Admin", "SUPER_ADMIN", "SuperAdmin"));
            options.AddPolicy(CustomerPolicy, policy => policy.RequireRole("CUSTOMER", "Customer", "SHOP", "Shop"));
            options.AddPolicy(StaffPolicy, policy => policy.RequireRole("STAFF", "Staff"));
        });
    }
}
