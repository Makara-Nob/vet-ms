using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CbcDialog : Form
{
    private readonly ComboBox cboPet;
    private readonly DateTimePicker dtpTestDate;
    private readonly NumericUpDown nudRbc, nudHgb, nudHct, nudMcv, nudMch, nudMchc, nudPlt, nudWbc;
    private readonly NumericUpDown nudNeu, nudLym, nudMon, nudEos, nudBas;
    private readonly TextBox txtRemarks;
    private readonly List<Pet> _pets;

    public CbcRecord Result { get; private set; } = new();

    public CbcDialog(CbcRecord? existing = null)
    {
        Text = existing is null ? "New CBC Record" : "Edit CBC Record";
        Size = new Size(680, 720); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; BackColor = Color.White;

        _pets = DataStore.GetPets().Where(p => p.IsActive).ToList();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20,15,20,15), AutoScroll = true };

        // Pet Selection
        flow.Controls.Add(UIHelper.CreateFormLabel("Pet Selection *"));
        cboPet = new ComboBox { Width = 600, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0,0,0,12) };
        cboPet.DataSource = _pets; cboPet.DisplayMember = ""; cboPet.ValueMember = "Id";
        cboPet.Format += (_, fe) => { if (fe.ListItem is Pet p) fe.Value = $"{p.Name} ({p.CustomerName}) — {p.SpeciesName}"; };
        flow.Controls.Add(cboPet);

        flow.Controls.Add(UIHelper.CreateFormLabel("Test Date"));
        dtpTestDate = new DateTimePicker { Width = 200, Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Margin = new Padding(0,0,0,15) };
        flow.Controls.Add(dtpTestDate);

        // Erythrogram Section
        flow.Controls.Add(CreateSectionTitle("Erythrogram (Red Blood Cells)"));
        var gridEry = new TableLayoutPanel { Width = 600, AutoSize = true, ColumnCount = 3, Margin = new Padding(0,0,0,10) };
        gridEry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); gridEry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); gridEry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        
        nudRbc = CreateLabInput(gridEry, "RBC (10^12/L)", 0, 0, 15);
        nudHgb = CreateLabInput(gridEry, "HGB (g/dL)", 0, 1, 25);
        nudHct = CreateLabInput(gridEry, "HCT (%)", 0, 2, 70);
        nudMcv = CreateLabInput(gridEry, "MCV (fL)", 1, 0, 100);
        nudMch = CreateLabInput(gridEry, "MCH (pg)", 1, 1, 40);
        nudMchc = CreateLabInput(gridEry, "MCHC (g/dL)", 1, 2, 40);
        flow.Controls.Add(gridEry);

        // Platelets & WBC Section
        flow.Controls.Add(CreateSectionTitle("Platelets & WBC Count"));
        var gridWbc = new TableLayoutPanel { Width = 600, AutoSize = true, ColumnCount = 2, Margin = new Padding(0,0,0,10) };
        gridWbc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); gridWbc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        
        nudPlt = CreateLabInput(gridWbc, "PLT (10^9/L)", 0, 0, 1000);
        nudWbc = CreateLabInput(gridWbc, "WBC (10^9/L)", 0, 1, 100);
        flow.Controls.Add(gridWbc);

        // Differential (WBC %)
        flow.Controls.Add(CreateSectionTitle("WBC Differential (%)"));
        var gridDiff = new TableLayoutPanel { Width = 600, AutoSize = true, ColumnCount = 3, Margin = new Padding(0,0,0,10) };
        gridDiff.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); gridDiff.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); gridDiff.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        
        nudNeu = CreateLabInput(gridDiff, "Neutrophils (%)", 0, 0, 100);
        nudLym = CreateLabInput(gridDiff, "Lymphocytes (%)", 0, 1, 100);
        nudMon = CreateLabInput(gridDiff, "Monocytes (%)", 0, 2, 100);
        nudEos = CreateLabInput(gridDiff, "Eosinophils (%)", 1, 0, 100);
        nudBas = CreateLabInput(gridDiff, "Basophils (%)", 1, 1, 100);
        flow.Controls.Add(gridDiff);

        flow.Controls.Add(UIHelper.CreateFormLabel("Clinical Remarks / Interpretation"));
        txtRemarks = new TextBox { Width = 600, Height = 80, Multiline = true, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical };
        flow.Controls.Add(txtRemarks);

        // Buttons
        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = UIHelper.LightBg };
        var btnSave = UIHelper.CreateButton("Save Results", UIHelper.Success, 120);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108,117,125), 90);
        btnSave.Top = btnCancel.Top = 15; btnSave.Left = pnlBtn.Width - 230; btnCancel.Left = pnlBtn.Width - 100;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel; btnSave.Click += Save;
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(flow); Controls.Add(pnlBtn); AcceptButton = btnSave; CancelButton = btnCancel;

        if (existing is not null)
        {
            var pet = _pets.FirstOrDefault(p => p.Id == existing.PetId); if (pet != null) cboPet.SelectedItem = pet;
            dtpTestDate.Value = existing.TestDate;
            nudRbc.Value = existing.Rbc; nudHgb.Value = existing.Hgb; nudHct.Value = existing.Hct;
            nudMcv.Value = existing.Mcv; nudMch.Value = existing.Mch; nudMchc.Value = existing.Mchc;
            nudPlt.Value = existing.Plt; nudWbc.Value = existing.Wbc;
            nudNeu.Value = existing.Neu; nudLym.Value = existing.Lym; nudMon.Value = existing.Mon;
            nudEos.Value = existing.Eos; nudBas.Value = existing.Bas;
            txtRemarks.Text = existing.Remarks;
            Result.Id = existing.Id;
        }
    }

    private Label CreateSectionTitle(string text)
    {
        return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = UIHelper.Primary, Margin = new Padding(0,10,0,5) };
    }

    private NumericUpDown CreateLabInput(TableLayoutPanel parent, string labelText, int row, int col, decimal max)
    {
        var p = new Panel { Dock = DockStyle.Fill, Height = 55, Padding = new Padding(0,0,10,0) };
        var lbl = new Label { Text = labelText, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray };
        var nud = new NumericUpDown { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10f), DecimalPlaces = 2, Maximum = max, Minimum = 0 };
        p.Controls.Add(nud); p.Controls.Add(lbl);
        parent.Controls.Add(p, col, row);
        return nud;
    }

    private void Save(object? s, EventArgs e)
    {
        if (cboPet.SelectedItem is not Pet p) { VetMS.Forms.CustomMessageBox.Show("Please select a pet.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        
        Result = new CbcRecord
        {
            Id = Result.Id, PetId = p.Id, PetName = p.Name,
            CustomerId = p.CustomerId, CustomerName = p.CustomerName,
            TestDate = dtpTestDate.Value,
            Rbc = nudRbc.Value, Hgb = nudHgb.Value, Hct = nudHct.Value,
            Mcv = nudMcv.Value, Mch = nudMch.Value, Mchc = nudMchc.Value,
            Plt = nudPlt.Value, Wbc = nudWbc.Value,
            Neu = nudNeu.Value, Lym = nudLym.Value, Mon = nudMon.Value,
            Eos = nudEos.Value, Bas = nudBas.Value,
            Remarks = txtRemarks.Text.Trim()
        };

        DialogResult = DialogResult.OK;
    }
}
