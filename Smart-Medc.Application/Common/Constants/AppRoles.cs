namespace Smart_Medc.Application.Common.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Patient = "Patient";
        public const string Organization = "Organization";

        public static readonly string[] AllRoles = { Admin, Patient, Organization };
    }
}