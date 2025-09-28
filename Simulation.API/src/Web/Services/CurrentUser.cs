using System.Security.Claims;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Constants;

namespace GameWeb.Web.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    public List<string>? Roles => httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
        .Select(x => x.Value).ToList();
}
