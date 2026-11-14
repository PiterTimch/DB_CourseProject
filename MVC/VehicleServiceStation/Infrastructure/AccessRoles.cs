namespace VehicleServiceStation.Infrastructure;

public static class AccessRoles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string Director = "director";
    public const string Guest = "guest";
    public const string Staff = Admin + "," + Manager;
    public const string CatalogManagers = Admin;
}
