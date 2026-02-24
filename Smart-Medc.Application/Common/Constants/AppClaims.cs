namespace Smart_Medc.Application.Common.Constants
{
    public static class AppClaims
    {
        // Standard claims
        public const string UserId = "uid";
        public const string Email = "email";
        public const string UserType = "user_type";
        public const string FullName = "full_name";

        // Patient specific
        public const string PatientId = "patient_id";

        // Organization specific
        public const string OrganizationId = "organization_id";
        public const string OrganizationName = "organization_name";
        public const string VerificationStatus = "verification_status";

        // Permissions
        public const string CanAccessMedicalRecords = "can_access_medical_records";
        public const string CanManageAppointments = "can_manage_appointments";
        public const string CanVerifyOrganizations = "can_verify_organizations";
    }
}