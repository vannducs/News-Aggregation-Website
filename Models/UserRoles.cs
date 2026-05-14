namespace NewsAggregator.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Moderator = "Moderator";
    public const string Member = "Member";
    public const string AdminOrEditor = Admin + "," + Editor;
    public const string AdminOrModerator = Admin + "," + Moderator;
    public const string StaffRoles = Admin + "," + Editor + "," + Moderator;
}
