using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class Organization
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public OrganizationType Type { get; set; }
        public string? Description { get; set; }

        // Contact Information
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }

        // Business Information
        public string? SSN { get; set; } = string.Empty; // SSN/Tax ID

        // Verification
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }

        // Ratings
        //public decimal? AverageRating { get; set; } = 0;
        //public int? TotalReviews { get; set; } = 0;

        // Settings
        public bool AllowSameDayBooking { get; set; } = true;
        public int MinCancellationNoticeHours { get; set; } = 24;
        public int MaxReschedulesAllowed { get; set; } = 2;
        public int AutoRejectDays { get; set; } = 3; // Days before auto-rejecting pending requests for appointments booking

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<OrganizationSpecialization> Specializations { get; set; } = new List<OrganizationSpecialization>();
        public virtual ICollection<OrganizationDocument> Documents { get; set; } = new List<OrganizationDocument>();
        public virtual ICollection<OrganizationOperatingHours> OperatingHours { get; set; } = new List<OrganizationOperatingHours>();
        public virtual ICollection<OrganizationAvailabilitySlot> AvailabilitySlots { get; set; } = new List<OrganizationAvailabilitySlot>();
        public virtual ICollection<ConsultationFee> ConsultationFees { get; set; } = new List<ConsultationFee>();
        public virtual ICollection<OrganizationPhoto> Photos { get; set; } = new List<OrganizationPhoto>();
        public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<OrganizationNotification> Notifications { get; set; } = new List<OrganizationNotification>();
    }

    

    
}
