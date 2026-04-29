using System.Text.Json.Serialization;

namespace VetMS.Models;

public class ClinicSettings
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = "";
    public string NameKhmer      { get; set; } = "";
    public string AddressEnglish { get; set; } = "";
    public string AddressKhmer   { get; set; } = "";
    public string Phone          { get; set; } = "";
    public string Email          { get; set; } = "";
    public List<SocialLink> SocialLinks { get; set; } = [];
}

public class SocialLink
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name     { get; set; } = "";
}
