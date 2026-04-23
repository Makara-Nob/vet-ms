namespace VetMS.Models;

public class Pet : BaseEntity
{
    public int    CustomerId    { get; set; }
    public string CustomerName  { get; set; } = "";
    public int    SpeciesId     { get; set; }
    public string SpeciesName   { get; set; } = "";
    public int?   BreedId       { get; set; }
    public string BreedName     { get; set; } = "";
    public string Name          { get; set; } = "";
    public string Gender        { get; set; } = "Unknown"; // Male / Female / Unknown
    public DateTime? DateOfBirth { get; set; }
    public decimal Weight       { get; set; }
    public string Color         { get; set; } = "";
    public string? MicrochipNo   { get; set; } = "";
    public string Notes         { get; set; } = "";
    public bool   IsActive      { get; set; } = true;
    public byte[]? ProfilePicture { get; set; }
}
