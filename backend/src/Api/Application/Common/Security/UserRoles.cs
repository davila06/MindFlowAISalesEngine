namespace Api.Application.Common.Security;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Sales = "Sales";
    public const string Viewer = "Viewer";

    public static bool IsValid(string role)
    {
        return role is Admin or Sales or Viewer;
    }
}
