namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class Specialization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconName { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<OrganizationSpecialization> OrganizationSpecializations { get; set; } = new List<OrganizationSpecialization>();
    }
}
