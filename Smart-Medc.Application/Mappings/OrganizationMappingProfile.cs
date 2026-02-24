using AutoMapper;
using Smart_Medc.Application.DTOs.Organization;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Application.Mappings
{
    public class OrganizationMappingProfile : Profile
    {
        public OrganizationMappingProfile()
        {
            // Search Result
            CreateMap<Organization, OrganizationSearchResultDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Rating,
                    opt => opt.MapFrom(src => CalculateOrganizationRating(src)))
                .ForMember(dest => dest.ReviewCount,
                    opt => opt.MapFrom(src => CalculateReviewCount(src)))
                .ForMember(dest => dest.NextAvailable,
                    opt => opt.Ignore()) // Calculated separately if needed
                .ForMember(dest => dest.Specializations,
                    opt => opt.MapFrom(src => src.Specializations != null
                        ? src.Specializations.Select(s => s.Specialization.Name).ToList()
                        : new List<string>()));

            // Detailed View
            CreateMap<Organization, OrganizationDetailDto>()
                .ForMember(dest => dest.Phone,
                    opt => opt.MapFrom(src => src.User != null ? src.User.PhoneNumber ?? string.Empty : string.Empty))
                .ForMember(dest => dest.Email,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Email ?? string.Empty : string.Empty))
                .ForMember(dest => dest.Doctors,
                    opt => opt.MapFrom(src => src.Doctors != null
                        ? src.Doctors.Where(d => d.IsActive).ToList()
                        : new List<Doctor>()))
                .ForMember(dest => dest.OperatingHours,
                    opt => opt.MapFrom(src => src.OperatingHours ?? new List<OrganizationOperatingHours>()))
                .ForMember(dest => dest.ConsultationFees,
                    opt => opt.MapFrom(src => src.ConsultationFees != null
                        ? src.ConsultationFees.Where(cf => cf.IsActive).ToList()
                        : new List<ConsultationFee>()));

            // Sub-objects
            CreateMap<Doctor, DoctorDto>()
                .ForMember(dest => dest.Specialization,
                    opt => opt.MapFrom(src => src.Specialization ?? string.Empty))
                .ForMember(dest => dest.Rating,
                    opt => opt.MapFrom(src => src.AverageRating));

            CreateMap<OrganizationOperatingHours, OperatingHoursDto>()
                .ForMember(dest => dest.Day,
                    opt => opt.MapFrom(src => src.DayOfWeek.ToString()))
                .ForMember(dest => dest.Hours,
                    opt => opt.MapFrom(src => src.IsOpen && src.OpenTime.HasValue && src.CloseTime.HasValue
                        ? $"{src.OpenTime.Value:hh:mm tt} - {src.CloseTime.Value:hh:mm tt}"
                        : "Closed"));

            CreateMap<ConsultationFee, ConsultationFeeDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.FeeType))
                .ForMember(dest => dest.Amount,
                    opt => opt.MapFrom(src => src.MinAmount));
        }

        // Helper methods for rating calculation
        private static decimal CalculateOrganizationRating(Organization org)
        {
            if (org.Doctors == null || !org.Doctors.Any())
                return 0m;

            var activeDoctors = org.Doctors.Where(d => d.IsActive).ToList();
            if (!activeDoctors.Any()) return 0m;

            return Math.Round(activeDoctors.Average(d => d.AverageRating), 2);
        }

        private static int CalculateReviewCount(Organization org)
        {
            if (org.Doctors == null)
                return 0;

            return org.Doctors.Sum(d => d.TotalReviews);
        }
    }
}
