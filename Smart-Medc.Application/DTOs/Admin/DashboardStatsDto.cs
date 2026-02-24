namespace Smart_Medc.Application.DTOs.Admin
{
    public class DashboardStatsDto
    {
        // Patient Statistics
        public int TotalActivePatients { get; set; }
        public int TotalDeletedPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }

        // Organization Statistics
        public int TotalPendingOrganizations { get; set; }
        public int TotalApprovedOrganizations { get; set; }
        public int TotalRejectedOrganizations { get; set; }
        public int NewOrganizationsThisMonth { get; set; }

        // Recent Activity
        public DateTime? LastPatientRegistration { get; set; }
        public DateTime? LastOrganizationRegistration { get; set; }
        public DateTime? LastOrganizationApproval { get; set; }
    }
}