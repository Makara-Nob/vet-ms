namespace VetMS.Models;

public class Customer : BaseEntity
{
    public string FullName    { get; set; } = "";
    public string Phone       { get; set; } = "";
    public string Email       { get; set; } = "";
    public string Address     { get; set; } = "";
    public string Notes       { get; set; } = "";
    public bool   IsActive    { get; set; } = true;
}
