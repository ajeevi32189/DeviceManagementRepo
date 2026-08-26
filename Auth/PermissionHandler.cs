using Microsoft.AspNetCore.Authorization;

namespace DeviceManagementOnly.Auth
{
    // Company API ke paas apna Identity / User-Role-Permission DB nahi hai.
    // Token DemoAuth se issue hota hai, isliye yahan sirf JWT ke andar
    // aaye hue "perm" claims ko check karenge (DB fallback ki zaroorat nahi).
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            // Super Admin bypass (agar token me Admin role claim aa raha ho)
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // JWT me "perm" naam ke claims check karo
            var claimPermissions = context.User.FindAll("perm").Select(c => c.Value).ToList();
            if (claimPermissions.Contains(requirement.PermissionName, StringComparer.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
