namespace VetMS.Models;

public class Appointment : BaseEntity
{
    public int      PetId           { get; set; }
    public string   PetName         { get; set; } = "";
    public int      CustomerId      { get; set; }
    public string   CustomerName    { get; set; } = "";
    public int?     AssignedVetId   { get; set; }
    public string   VetName         { get; set; } = "";
    public int?     ServiceTypeId   { get; set; }
    public string   ServiceTypeName { get; set; } = "";
    public DateTime AppointmentDate { get; set; } = DateTime.Now;
    public int      Duration        { get; set; } = 30; // minutes
    public string   Status          { get; set; } = "Scheduled"; // Scheduled / In Progress / Completed / Cancelled
    public string   Notes           { get; set; } = "";
}
