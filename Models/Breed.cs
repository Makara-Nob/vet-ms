namespace VetMS.Models;

public class Breed : BaseEntity
{
    public int SpeciesId { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
