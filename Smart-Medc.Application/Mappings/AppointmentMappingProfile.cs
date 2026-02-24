using AutoMapper;
using Smart_Medc.Application.DTOs.Appointment;
using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Application.Mappings
{
    public class AppointmentMappingProfile : Profile
    {
        public AppointmentMappingProfile()
        {
            // Base Appointment DTO
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.OrganizationName,
                    opt => opt.MapFrom(src => src.Organization != null ? src.Organization.Name : string.Empty))
                .ForMember(dest => dest.DoctorName,
                    opt => opt.MapFrom(src => src.Doctor != null ? src.Doctor.Name : "No Doctor Assigned"))
                .ForMember(dest => dest.PatientName,
                    opt => opt.MapFrom(src => src.Patient != null ? $"{src.Patient.User.FirstName} {src.Patient.User.LastName}" : string.Empty))
                .ForMember(dest => dest.Date,
                    opt => opt.MapFrom(src => src.AppointmentDate))
                .ForMember(dest => dest.Time,
                    opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.VisitType,
                    opt => opt.MapFrom(src => src.Type.ToString()));

            // Detailed Appointment View
            CreateMap<Appointment, AppointmentDetailDto>()
                .IncludeBase<Appointment, AppointmentDto>()
                .ForMember(dest => dest.Address,
                    opt => opt.MapFrom(src => src.Organization != null ? src.Organization.Address : string.Empty))
                .ForMember(dest => dest.Phone,
                    opt => opt.MapFrom(src => src.Organization != null && src.Organization.User != null ?
                        src.Organization.User.PhoneNumber ?? string.Empty : string.Empty))
                .ForMember(dest => dest.Reason,
                    opt => opt.MapFrom(src => src.ReasonForVisit ?? string.Empty))
                .ForMember(dest => dest.AccessCode,
                    opt => opt.MapFrom(src => src.DataShareCode != null ? src.DataShareCode.Code : null))
                .ForMember(dest => dest.PreparationInstructions,
                    opt => opt.MapFrom(src => src.PreparationInstructions))
                .ForMember(dest => dest.CancellationPolicy,
                    opt => opt.MapFrom(src => src.Organization != null ?
                        $"Cancellation must be made at least {src.Organization.MinCancellationNoticeHours} hours in advance. Maximum {src.Organization.MaxReschedulesAllowed} reschedules allowed." :
                        string.Empty))
                .ForMember(dest => dest.History,
                    opt => opt.MapFrom(src => src.StatusHistory
                        .OrderByDescending(h => h.CreatedAt)
                        .Select(h => new AppointmentHistoryItemDto
                        {
                            FromStatus = h.FromStatus.ToString(),
                            ToStatus = h.ToStatus.ToString(),
                            ChangedAt = h.CreatedAt,
                            Reason = h.Reason
                        })
                        .ToList()));

            // Request View
            CreateMap<Appointment, AppointmentRequestDto>()
                .IncludeBase<Appointment, AppointmentDto>()
                .ForMember(dest => dest.RequestedAt,
                    opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.HasSharedRecords,
                    opt => opt.MapFrom(src => src.IsRecordsShared));
        }
    }
}
