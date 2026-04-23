namespace VetMS.Models;

public class MedicalRecord : BaseEntity
{
    public int      AppointmentId { get; set; }
    public int      PetId         { get; set; }
    public string   PetName       { get; set; } = "";
    public int      CustomerId    { get; set; }
    public string   CustomerName  { get; set; } = "";
    public int?     VetId         { get; set; }
    public string   VetName       { get; set; } = "";
    public string   Diagnosis     { get; set; } = "";
    public string   Treatment     { get; set; } = "";
    public string   Notes         { get; set; } = "";
    public DateTime? FollowUpDate  { get; set; }
}

public class RecordMedication
{
    public int    MedicationId   { get; set; }
    public string MedicationName { get; set; } = "";
    public string Dosage         { get; set; } = "";
    public string Notes          { get; set; } = "";
}
