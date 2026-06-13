using Microsoft.AspNetCore.Authorization;

namespace StudentAPI.Authorization
{
    public class StudentOwnerOrAdminRequirement : IAuthorizationRequirement
    {
    }
}
