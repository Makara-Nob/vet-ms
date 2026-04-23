using System;

namespace VetMS.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }

    public string? Metadata { get; set; }
}
