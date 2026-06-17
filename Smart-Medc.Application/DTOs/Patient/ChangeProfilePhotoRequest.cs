using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Smart_Medc.Application.DTOs.Patient
{
    /// <summary>
    /// Request to upload/change patient profile photo
    /// </summary>
    public class ChangeProfilePhotoRequest
    {
        [Required(ErrorMessage = "Photo file is required")]
        public IFormFile? Photo { get; set; }
    }
}