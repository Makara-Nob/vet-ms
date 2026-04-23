using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class PetDetailsForm : Form
{
    private readonly Pet _pet;

    public PetDetailsForm(Pet pet)
    {
        _pet = pet;
        BuildForm();
    }

    private void BuildForm()
    {
        Text = $"Patient Chart - {_pet.Name}";
        Size = new Size(950, 760);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var footer = BuildFooter();

        content.Controls.Add(BuildTabs());
        content.Controls.Add(BuildHeader());

        Controls.Add(content);
        Controls.Add(footer);
    }

    private Panel BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = UIHelper.LightBg
        };

        var btnClose = UIHelper.CreateButton("Close", Color.Gray, 100);
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.Location = new Point(820, 14);
        btnClose.DialogResult = DialogResult.OK;

        footer.Controls.Add(btnClose);
        return footer;
    }

    private Control BuildHeader()
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = 170,
            BackColor = UIHelper.LightBg,
            Padding = new Padding(20)
        };

        var picture = BuildPetPicture();
        var patientInfo = BuildPatientInfo();
        var ownerInfo = BuildOwnerInfo();

        card.Controls.Add(picture);
        card.Controls.Add(patientInfo);
        card.Controls.Add(ownerInfo);

        return card;
    }

    private PictureBox BuildPetPicture()
    {
        var pic = new PictureBox
        {
            Size = new Size(100, 100),
            Location = new Point(20, 25),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        if (_pet.ProfilePicture?.Length > 0)
        {
            using var ms = new MemoryStream(_pet.ProfilePicture);
            pic.Image = Image.FromStream(ms);
        }
        else
        {
            pic.Image = UIHelper.CreateAvatar(_pet.Name, 100);
        }

        UIHelper.AttachImageViewer(pic, () => pic.Image);

        return pic;
    }

    private Control BuildPatientInfo()
    {
        var age = GetAge();

        var lbl = new Label
        {
            AutoSize = true,
            Location = new Point(140, 22),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Black,
            Text =
                $"{_pet.Name}\n" +
                $"{_pet.SpeciesName} | {_pet.BreedName} | {_pet.Gender}\n" +
                $"Age: {age} | Weight: {_pet.Weight} kg | Color: {_pet.Color}\n" +
                $"Status: {(_pet.IsActive ? "Active" : "Inactive")}"
        };

        return lbl;
    }

    private Control BuildOwnerInfo()
    {
        var lbl = new Label
        {
            AutoSize = true,
            Location = new Point(540, 35),
            Font = new Font("Segoe UI", 10),
            ForeColor = UIHelper.Primary,
            Text =
                $"Owner: {_pet.CustomerName}\n" +
                $"Microchip: {(string.IsNullOrWhiteSpace(_pet.MicrochipNo) ? "None" : _pet.MicrochipNo)}"
        };

        return lbl;
    }

    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };

        tabs.TabPages.Add(CreateTab("Appointments", BuildAppointmentsGrid()));
        tabs.TabPages.Add(CreateTab("Medical Records", BuildMedicalRecordsGrid()));
        tabs.TabPages.Add(CreateTab("CBC Results", BuildCbcGrid()));

        return tabs;
    }

    private TabPage CreateTab(string title, Control content)
    {
        var page = new TabPage(title);
        page.Controls.Add(content);
        return page;
    }

    private DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White
        };

        UIHelper.StyleGrid(grid);
        return grid;
    }

    private Control BuildAppointmentsGrid()
    {
        var grid = CreateGrid();

        grid.DataSource = DataStore.GetAppointments()
            .Where(a => a.PetId == _pet.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new
            {
                Date = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm"),
                Vet = a.VetName,
                Service = a.ServiceTypeName,
                Status = a.Status
            })
            .ToList();

        return grid;
    }

    private Control BuildMedicalRecordsGrid()
    {
        var grid = CreateGrid();

        grid.DataSource = DataStore.GetMedicalRecords()
            .Where(r => r.PetId == _pet.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                Date = r.CreatedAt.ToString("yyyy-MM-dd"),
                Vet = r.VetName,
                Diagnosis = r.Diagnosis,
                FollowUp = r.FollowUpDate?.ToString("yyyy-MM-dd") ?? "-"
            })
            .ToList();

        return grid;
    }

    private Control BuildCbcGrid()
    {
        var grid = CreateGrid();

        grid.DataSource = DataStore.GetCbcRecords()
            .Where(c => c.PetId == _pet.Id)
            .OrderByDescending(c => c.TestDate)
            .Select(c => new
            {
                Date = c.TestDate.ToString("yyyy-MM-dd"),
                WBC = c.Wbc,
                RBC = c.Rbc,
                HGB = c.Hgb,
                HCT = $"{c.Hct:F1}%",
                PLT = c.Plt
            })
            .ToList();

        return grid;
    }

    private string GetAge()
    {
        if (_pet.DateOfBirth == null)
            return "Unknown";

        var years = DateTime.Today.Year - _pet.DateOfBirth.Value.Year;
        if (_pet.DateOfBirth.Value.Date > DateTime.Today.AddYears(-years))
            years--;

        return $"{years} yr";
    }
}