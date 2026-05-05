using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Helpers;

public static class CbcPdfExporter
{
    static CbcPdfExporter() => QuestPDF.Settings.License = LicenseType.Community;

    // ── One-call helper: save dialog → generate → open ────────────────────────
    public static void ShowExportDialog(CbcRecord record)
    {
        using var sfd = new System.Windows.Forms.SaveFileDialog
        {
            Title    = "Export CBC Report as PDF",
            Filter   = "PDF Files (*.pdf)|*.pdf",
            FileName = $"CBC_{record.PetName}_{record.TestDate:yyyy-MM-dd}.pdf"
        };
        if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        try
        {
            var pet      = DataStore.GetPets().FirstOrDefault(p => p.Id == record.PetId);
            var customer = DataStore.GetCustomers().FirstOrDefault(c => c.Id == record.CustomerId);
            string age   = "";
            if (pet?.DateOfBirth is DateTime dob)
                age = $"{DateTime.Today.Year - dob.Year} year{(DateTime.Today.Year - dob.Year == 1 ? "" : "s")}";
            var bytes = Generate(
                record,
                species:    pet?.SpeciesName ?? "Dog",
                sex:        pet?.Gender      ?? "",
                age:        age,
                ownerPhone: customer?.Phone  ?? "");
            File.WriteAllBytes(sfd.FileName, bytes);
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Export failed: {ex.Message}", "Error",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    // ── Main generation ───────────────────────────────────────────────────────
    public static byte[] Generate(CbcRecord rec, string species = "Dog",
        string sex = "", string age = "", string ownerPhone = "")
    {
        var clinic   = DataStore.GetClinicSettings();
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor(Colors.Black));

                // ── Header ────────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logo is not null)
                            row.ConstantItem(90).AlignMiddle().Image(logo).FitArea();

                        row.RelativeItem().PaddingLeft(logo is not null ? 14 : 0).Column(c =>
                        {
                            // Khmer clinic name first (big, blue)
                            if (!string.IsNullOrWhiteSpace(clinic.NameKhmer))
                                c.Item().AlignCenter()
                                    .Text(clinic.NameKhmer).FontFamily("Khmer UI").Bold().FontSize(16f).FontColor(Colors.Blue.Darken2);

                            // English clinic name
                            var displayName = string.IsNullOrWhiteSpace(clinic.Name) ? "Veterinary Clinic" : clinic.Name;
                            c.Item().AlignCenter().PaddingTop(1)
                                .Text(displayName).Bold().FontSize(14).FontColor(Colors.Blue.Darken2);

                            // Khmer address — collapse newlines, uses "Khmer UI"
                            if (!string.IsNullOrWhiteSpace(clinic.AddressKhmer))
                            {
                                var addrKh = string.Join(" ", clinic.AddressKhmer
                                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim()).Where(l => l.Length > 0));
                                c.Item().AlignCenter().PaddingTop(2)
                                    .Text(addrKh).FontFamily("Khmer UI").FontSize(10.5f).FontColor(Colors.Grey.Darken2);
                            }

                            // English address — collapse newlines
                            if (!string.IsNullOrWhiteSpace(clinic.AddressEnglish))
                            {
                                var addrEn = string.Join(", ", clinic.AddressEnglish
                                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim()).Where(l => l.Length > 0));
                                c.Item().AlignCenter().PaddingTop(1)
                                    .Text(addrEn).FontSize(10f).FontColor(Colors.Grey.Darken2);
                            }

                            // Social media links
                            if (clinic.SocialLinks.Count > 0)
                            {
                                var socials = string.Join("   |   ",
                                    clinic.SocialLinks.Select(s => $"{s.Platform}: {s.Name}"));
                                c.Item().AlignCenter().PaddingTop(1)
                                    .Text(socials).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                            }

                            // Phone / Email last
                            var contacts = string.Join("   /   ", new[] { clinic.Phone, clinic.Email }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (!string.IsNullOrWhiteSpace(contacts))
                                c.Item().AlignCenter().PaddingTop(1)
                                    .Text($"Tel: {contacts}").FontSize(10f).FontColor(Colors.Grey.Darken2);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1f).LineColor(Colors.Grey.Darken1);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    // Patient info block
                    col.Item().PaddingBottom(10).Column(info =>
                    {
                        info.Item().Text("Blood result:").Bold().FontSize(11);
                        info.Item().PaddingTop(3).Text($"   -   Date: {rec.TestDate:dd/MM/yy}");
                        info.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text($"   -   Owner's Name: {rec.CustomerName}");
                            r.RelativeItem().Text($"Animal name: {rec.PetName}");
                            r.RelativeItem().Text($"Species: {species}");
                        });
                        if (!string.IsNullOrEmpty(ownerPhone) || !string.IsNullOrEmpty(sex) || !string.IsNullOrEmpty(age))
                            info.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text($"   -   Number: {ownerPhone}");
                                r.RelativeItem().Text(!string.IsNullOrEmpty(sex) ? $"Sex: {sex}" : "");
                                r.RelativeItem().Text(!string.IsNullOrEmpty(age) ? $"Age: {age}" : "");
                            });
                    });

                    // HEMATOLOGY
                    col.Item().PaddingBottom(12).Border(0.75f).Table(t => BuildHematologyTable(t, rec));

                    // BIOCHEMISTRY (only when data entered)
                    bool hasBiochem = rec.Creatinine.HasValue || rec.Urea.HasValue || rec.Bun.HasValue
                                   || rec.Alt.HasValue        || rec.Ast.HasValue;
                    if (hasBiochem)
                        col.Item().Border(0.75f).Table(t => BuildBiochemTable(t, rec));
                });
            });
        }).GeneratePdf();
    }

    // ── Hematology table ──────────────────────────────────────────────────────
    private static void BuildHematologyTable(TableDescriptor table, CbcRecord rec)
    {
        DefineColumns(table);

        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten2)
            .Padding(5).AlignCenter().Text("HEMATOLOGY").Bold().FontSize(10);

        AddHeaderRow(table);

        AddRow(table, "WBC",            Fmt(rec.Wbc,  1), "x 10⁹/L",  "5.0 – 14.1",  rec.Wbc,  5.0m,  14.1m);
        AddRow(table, "Red Blood Cell", Fmt(rec.Rbc,  2), "mm³",       "4.95 – 7.87", rec.Rbc,  4.95m, 7.87m);
        AddRow(table, "Hemoglobin",     Fmt(rec.Hgb,  1), "x10g/L",    "11.9 – 18.9", rec.Hgb,  11.9m, 18.9m);
        AddRow(table, "Hematocrit",     Fmt(rec.Hct,  0), "%",         "35 – 57",     rec.Hct,  35m,   57m);
        AddRow(table, "MCV",            Fmt(rec.Mcv,  0), "fL",        "66 – 77",     rec.Mcv,  66m,   77m);
        AddRow(table, "MCH",            Fmt(rec.Mch,  1), "pg",        "21.0 – 26.2", rec.Mch,  21.0m, 26.2m);
        AddRow(table, "MCHC",           Fmt(rec.Mchc, 1), "x10 g/L",   "32.0 – 36.3", rec.Mchc, 32.0m, 36.3m);
        AddRow(table, "Platelets",      Fmt(rec.Plt,  0), "10³/mm³",   "211 – 621",   rec.Plt,  211m,  621m);

        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3)
            .PaddingVertical(3).PaddingHorizontal(8)
            .Text("FORMULE LEUCOCYTE").Bold().FontSize(9);

        AddRow(table, "Neutrophiles",  Fmt(rec.Neu, 0), "%", "58 – 85", rec.Neu, 58m, 85m);
        AddRow(table, "Eosinophiles",  Fmt(rec.Eos, 0), "%", "0 – 9",   rec.Eos,  0m,  9m);
        AddRow(table, "Basophiles",    Fmt(rec.Bas, 0), "%", "0 – 1",   rec.Bas,  0m,  1m);
        AddRow(table, "Lymphocytes",   Fmt(rec.Lym, 0), "%", "8 – 21",  rec.Lym,  8m, 21m);
        AddRow(table, "Monocytes",     Fmt(rec.Mon, 0), "%", "0 – 10",  rec.Mon,  0m, 10m, isLast: true);
    }

    // ── Biochemistry table ────────────────────────────────────────────────────
    private static void BuildBiochemTable(TableDescriptor table, CbcRecord rec)
    {
        DefineColumns(table);

        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten2)
            .Padding(5).AlignCenter().Text("BIOCHEMISTRY").Bold().FontSize(10);

        AddHeaderRow(table, "Test");

        bool anyLiver = rec.Alt.HasValue || rec.Ast.HasValue;

        if (rec.Creatinine.HasValue)
            AddRow(table, "Creatinine", Fmt(rec.Creatinine.Value, 2), "mg/dL", "0.5 – 1.7 mg/dL",
                rec.Creatinine.Value, 0.5m, 1.7m, isLast: !rec.Urea.HasValue && !rec.Bun.HasValue && !anyLiver);
        if (rec.Urea.HasValue)
            AddRow(table, "Urea", Fmt(rec.Urea.Value, 0), "mg/dL", "0.8 – 28 mg/dL",
                rec.Urea.Value, 0.8m, 28m, isLast: !rec.Bun.HasValue && !anyLiver);
        if (rec.Bun.HasValue)
            AddRow(table, "BUN", Fmt(rec.Bun.Value, 2), "mg/dL", "7 – 26 mg/dL",
                rec.Bun.Value, 7m, 26m, isLast: !anyLiver);

        if (anyLiver)
        {
            table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3)
                .PaddingVertical(3).PaddingHorizontal(8)
                .Text("ENZYMOLOGIE").Bold().FontSize(9);

            if (rec.Alt.HasValue)
                AddRow(table, "ALT", Fmt(rec.Alt.Value, 0), "U/L", "10 – 106 U/L",
                    rec.Alt.Value, 10m, 106m, isLast: !rec.Ast.HasValue);
            if (rec.Ast.HasValue)
                AddRow(table, "AST", Fmt(rec.Ast.Value, 0), "U/L", "13 – 65 U/L",
                    rec.Ast.Value, 13m, 65m, isLast: true);
        }
    }

    // ── Shared layout helpers ─────────────────────────────────────────────────
    private static void DefineColumns(TableDescriptor table) =>
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(3.5f);
            c.RelativeColumn(1.2f);
            c.RelativeColumn(2f);
            c.RelativeColumn(2.5f);
        });

    private static void AddHeaderRow(TableDescriptor table, string firstCol = "Parameter")
    {
        void HCell(string t) => table.Cell()
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(4).PaddingHorizontal(8)
            .Text(t).Bold().FontSize(9);
        HCell(firstCol); HCell("Result"); HCell("Unit"); HCell("Reference Value\nCanine (Dog)");
    }

    private static void AddRow(TableDescriptor table, string param, string result,
        string unit, string refRange, decimal value, decimal low, decimal high, bool isLast = false)
    {
        bool isHigh   = value > high;
        bool isLow    = value < low;
        bool abnormal = isHigh || isLow;

        IContainer Cell() => isLast
            ? table.Cell().PaddingVertical(3).PaddingHorizontal(8)
            : table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(8);

        Cell().Text(param).FontSize(9);

        if (abnormal)
            Cell().Text(result).Bold().FontSize(9).FontColor(isHigh ? Colors.Red.Medium : Colors.Blue.Medium);
        else
            Cell().Text(result).FontSize(9);

        Cell().Text(unit).FontSize(9);
        Cell().Text(refRange).FontSize(9);
    }

    private static string Fmt(decimal v, int dp) => dp switch
    {
        0 => v.ToString("F0"),
        1 => v.ToString("F1").TrimEnd('0').TrimEnd('.'),
        2 => v.ToString("F2").TrimEnd('0').TrimEnd('.'),
        _ => v.ToString("G")
    };
}
