using Microsoft.AspNetCore.Authorization;

namespace DeviceManagementOnly.Auth
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionName { get; }
        public PermissionRequirement(string permissionName) => PermissionName = permissionName;
    }
}
