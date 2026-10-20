using RaceDay.Domain;

namespace RaceDay.API.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute
{
    public UserRole Role { get; }

    public RequireRoleAttribute(UserRole role) => Role = role;
}
