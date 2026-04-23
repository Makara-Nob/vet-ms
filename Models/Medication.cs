namespace VetMS.Models;

public class Medication : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;   // Tablet, Injection, Syrup, etc.
    public string Unit { get; set; } = string.Empty;          // mg, ml, etc.
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
