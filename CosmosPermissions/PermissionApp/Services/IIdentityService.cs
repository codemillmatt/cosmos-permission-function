using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace PermissionApp
{
    public interface IIdentityService
    {
        string DisplayName { get; set; }

        Task<AuthenticationResult> Login();
        Task<AuthenticationResult> GetCachedSignInToken();
        void Logout();
        UIParent UIParent { get; set; }
    }
}
