using System;

namespace VetMS.Models;

public class CbcRecord : BaseEntity
{
    public int      PetId           { get; set; }
    public string   PetName         { get; set; } = "";
    public int      CustomerId      { get; set; }
    public string   CustomerName    { get; set; } = "";
    public DateTime TestDate        { get; set; } = DateTime.Now;

    // Erythrogram
    public decimal  Rbc             { get; set; } // 10^12/L
    public decimal  Hgb             { get; set; } // g/dL
    public decimal  Hct             { get; set; } // %
    public decimal  Mcv             { get; set; } // fL
    public decimal  Mch             { get; set; } // pg
    public decimal  Mchc            { get; set; } // g/dL

    // Platelets
    public decimal  Plt             { get; set; } // 10^9/L

    // Leukogram
    public decimal  Wbc             { get; set; } // 10^9/L
    public decimal  Neu             { get; set; } // % or absolute? Let's stick to % for differential
    public decimal  Lym             { get; set; }
    public decimal  Mon             { get; set; }
    public decimal  Eos             { get; set; }
    public decimal  Bas             { get; set; }

    public string   Remarks         { get; set; } = "";
}
