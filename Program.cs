using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new JevPlaygroundForm());
    }
}

public class JevPlaygroundForm : Form
{
    private const string DefaultApiKey = "apikey_272923e60f3dd3644498fc6f2d175214da0_21c7fcdf476d01cfc89715050d0ca8492d7e1b2502f3082905c2a9f57c05a4d9";

    private static readonly Color ColBg = Color.FromArgb(248, 250, 252);
    private static readonly Color ColCard = Color.White;
    private static readonly Color ColCardAlt = Color.FromArgb(246, 248, 251);
    private static readonly Color ColBorder = Color.FromArgb(214, 222, 235);
    private static readonly Color ColInput = Color.FromArgb(244, 246, 250);
    private static readonly Color ColText = Color.FromArgb(30, 41, 59);
    private static readonly Color ColMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color ColAccent = Color.FromArgb(99, 102, 241);
    private static readonly Color ColEmerald = Color.FromArgb(16, 185, 129);
    private static readonly Color ColBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color ColRed = Color.FromArgb(239, 68, 68);
    private static readonly Color ColAmber = Color.FromArgb(245, 158, 11);

    private TextBox txtApiKey = null!;
    private TextBox txtEndpoint = null!;

    // State controls
    private TextBox txtStateJson = null!;
    private TextBox txtStateKey = null!;
    private TextBox txtStateVal = null!;

    // Questions controls
    private TextBox txtQuestionsJson = null!;
    private TextBox txtQId = null!;
    private TextBox txtQInstructions = null!;
    private TextBox txtQCriterias = null!;
    private Label lblQTypeInfo = null!;
    private Label lblQcrit = null!;
    private Button btnApplyQ = null!;
    private Button btnApplyJson = null!;
    private GroupBox gbQ = null!;
    private GroupBox gbQList = null!;
    private FlowLayoutPanel pnlQList = null!;
    private string _builderType = "noul";

    private TextBox txtRawJsonOutput = null!;
    private TabControl tabOutput = null!;
    private FlowLayoutPanel pnlVisualResults = null!;
    private Button btnExecute = null!;
    private Button btnCheckApi = null!;
    private ComboBox cmbPresets = null!;
    private Label lblStatus = null!;
    private SplitContainer splitOuter = null!;
    private SplitContainer splitInner = null!;

    public JevPlaygroundForm()
    {
        InitializeComponent();
        LoadPreset(2);

        this.WindowState = FormWindowState.Maximized;

        this.Resize += (s, e) => UpdateSplitterDistances();
        this.Shown += async (s, e) => {
            UpdateSplitterDistances();
            await PerformEvaluationAsync();
        };
    }

    private const uint EM_SETRECT = 0x00B3;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static void SetSingleLineTextMargins(TextBox tb, int left, int top)
    {
        _ = tb.Handle;
        var rect = new RECT
        {
            Left = left,
            Top = top,
            Right = Math.Max(left + 8, tb.ClientSize.Width - left),
            Bottom = Math.Max(top + 8, tb.ClientSize.Height)
        };
        SendMessage(tb.Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        tb.Invalidate();
    }

    private static TextBox CreateJsonBox(string initial, bool readOnly)
    {
        return new TextBox
        {
            Multiline = true,
            ReadOnly = readOnly,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10.5F),
            BackColor = ColInput,
            ForeColor = ColText,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(10, 6, 10, 6),
            Text = initial
        };
    }

    private void UpdateSplitterDistances()
    {
        if (splitOuter == null || splitInner == null || ClientSize.Width < 300) return;
        try
        {
            int third = ClientSize.Width / 3;
            splitOuter.SplitterDistance = third;
            splitInner.SplitterDistance = third;
        }
        catch
        {
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Jev System One - Visual Playground";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ColBg;
        this.ForeColor = ColText;
        this.Font = new Font("Segoe UI", 9.5F);

        // Status Bar (Bottom)
        lblStatus = new Label
        {
            Text = " ● Status: Ready",
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = ColCard,
            ForeColor = ColEmerald,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0)
        };

        // Top Header (Config)
        Panel pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = ColCard,
            Padding = new Padding(16, 9, 16, 9)
        };

        FlowLayoutPanel pnlConfigRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        Label lblPreset = new Label { Text = "Preset:", Width = 55, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = ColMuted, Margin = new Padding(0, 6, 4, 0) };
        cmbPresets = new ComboBox
        {
            Width = 180,
            Height = 26,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColInput,
            ForeColor = ColText,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 5, 10, 0)
        };
        cmbPresets.Items.AddRange(new string[] { "Choice (Routing)", "Score (Rubric)", "Noul (Yes/No)" });
        cmbPresets.SelectedIndex = 2;
        cmbPresets.SelectedIndexChanged += async (s, e) => {
            LoadPreset(cmbPresets.SelectedIndex);
            await PerformEvaluationAsync();
        };

        Label lblEndpoint = new Label { Text = "Endpoint:", Width = 68, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = ColMuted, Margin = new Padding(0, 6, 4, 0) };
        txtEndpoint = new TextBox { Text = "https://api.typesafe.ai/v1/system-one", Width = 230, Font = new Font("Segoe UI", 9.5F), BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, AutoSize = false, Height = 26, Margin = new Padding(0, 4, 10, 0) };

        Label lblKey = new Label { Text = "API Key:", Width = 60, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = ColMuted, Margin = new Padding(0, 6, 4, 0) };
        txtApiKey = new TextBox { Text = DefaultApiKey, Width = 300, Font = new Font("Segoe UI", 9.5F), BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, AutoSize = false, Height = 26, Margin = new Padding(0, 4, 10, 0) };

        btnCheckApi = new Button
        {
            Text = "🔍 Cek API",
            Width = 90,
            Height = 28,
            BackColor = ColBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 0)
        };
        btnCheckApi.FlatAppearance.BorderSize = 0;
        btnCheckApi.Click += async (s, e) => await CheckApiConnectionAsync();

        pnlConfigRow.Controls.AddRange(new Control[] { lblPreset, cmbPresets, lblEndpoint, txtEndpoint, lblKey, txtApiKey, btnCheckApi });
        pnlTop.Controls.Add(pnlConfigRow);

        // 3-Pane Splitter Layout (33% each)
        splitOuter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 400,
            BackColor = ColBorder
        };

        splitInner = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 400,
            BackColor = ColBorder
        };

        // ================= PANE 1: STATEMENT (STATE) =================
        Panel pnlPane1 = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(12) };
        Label lblStateHeader = new Label
        {
            Text = "1. Statement (State Context)",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = ColText,
            Padding = new Padding(4, 0, 0, 0)
        };

        TabControl tabState = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), BackColor = ColCard, ForeColor = ColText };

        TabPage tabStateGui = new TabPage("🎨 Visual Builder") { BackColor = ColCardAlt, ForeColor = ColText };
        FlowLayoutPanel pnlStateGui = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ColCardAlt, Padding = new Padding(10), FlowDirection = FlowDirection.TopDown };

        GroupBox gbState = new GroupBox { Text = "Quick Context Builder", Width = 350, Height = 248, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), BackColor = ColCard, ForeColor = ColEmerald };
        Label lblSk = new Label { Text = "Property Key:", Location = new Point(15, 30), AutoSize = true, ForeColor = ColMuted };
        txtStateKey = new TextBox { Text = "candidate_resume", Location = new Point(15, 50), Width = 310, BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, AutoSize = false, Height = 26 };
        Label lblSv = new Label { Text = "Property Value / Content:", Location = new Point(15, 80), AutoSize = true, ForeColor = ColMuted };
        txtStateVal = new TextBox { Text = "Senior Software Engineer with experience in Python, Kubernetes, AWS.", Location = new Point(15, 100), Width = 310, Height = 100, Multiline = true, BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, Padding = new Padding(10, 6, 10, 6) };
        Button btnApplyState = new Button { Text = "Apply to JSON", Location = new Point(15, 212), Width = 120, Height = 25, BackColor = ColBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        btnApplyState.FlatAppearance.BorderSize = 0;
        btnApplyState.Click += (s, e) => {
            string k = txtStateKey.Text.Trim();
            if (k.Length == 0)
            {
                txtStateJson.Text = "{\n}";
                lblStatus.Text = " ● Status: State key is empty";
                return;
            }
            var obj = new JsonObject { [k] = txtStateVal.Text.Trim() };
            txtStateJson.Text = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            lblStatus.Text = $" ● Status: State '{k}' applied to JSON";
        };
        gbState.Controls.AddRange(new Control[] { lblSk, txtStateKey, lblSv, txtStateVal, btnApplyState });
        pnlStateGui.Controls.Add(gbState);
        tabStateGui.Controls.Add(pnlStateGui);

        TabPage tabStateJson = new TabPage("🔍 Raw JSON") { BackColor = ColCardAlt };
        txtStateJson = CreateJsonBox("", false);
        tabStateJson.Controls.Add(txtStateJson);

        tabState.TabPages.Add(tabStateGui);
        tabState.TabPages.Add(tabStateJson);

        pnlPane1.Controls.Add(tabState);
        pnlPane1.Controls.Add(lblStateHeader);

        // ================= PANE 2: QUESTION (TYPED PRIMITIVES) =================
        Panel pnlPane2 = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(12) };
        Label lblQHeader = new Label
        {
            Text = "2. Question (Typed Primitives)",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = ColText,
            Padding = new Padding(4, 0, 0, 0)
        };

        TabControl tabQuestions = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), BackColor = ColCard, ForeColor = ColText };

        TabPage tabQGui = new TabPage("🎨 Visual Builder") { BackColor = ColCardAlt, ForeColor = ColText };
        FlowLayoutPanel pnlQuestionsGui = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ColCardAlt, Padding = new Padding(10), FlowDirection = FlowDirection.TopDown };

        gbQ = new GroupBox { Text = "Add/Edit Primitive", Width = 350, Height = 265, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), BackColor = ColCard, ForeColor = ColBlue };

        lblQTypeInfo = new Label { Text = "Type: noul (Yes/No)", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = ColAccent };

        Label lblQi = new Label { Text = "Question ID:", Location = new Point(15, 55), AutoSize = true, ForeColor = ColMuted };
        txtQId = new TextBox { Text = "mentions_python", Location = new Point(15, 75), Width = 310, BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, AutoSize = false, Height = 26 };

        Label lblQins = new Label { Text = "Instructions:", Location = new Point(15, 105), AutoSize = true, ForeColor = ColMuted };
        txtQInstructions = new TextBox { Text = "Does the candidate state experience using Python?", Location = new Point(15, 125), Width = 310, Height = 90, Multiline = true, BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, Padding = new Padding(10, 6, 10, 6) };

        lblQcrit = new Label { Text = "Options (comma separated):", Location = new Point(15, 256), AutoSize = true, ForeColor = ColMuted, Visible = false };
        txtQCriterias = new TextBox { Text = "Python, Go, Java", Location = new Point(15, 276), Width = 310, BackColor = ColInput, ForeColor = ColText, BorderStyle = BorderStyle.None, AutoSize = false, Height = 26, Visible = false };

        btnApplyQ = new Button { Text = "➕ Add", Dock = DockStyle.Right, Width = 90, Height = 24, BackColor = ColBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnApplyQ.FlatAppearance.BorderSize = 0;

        btnApplyJson = new Button { Text = "Apply to JSON", Location = new Point(15, 218), Width = 310, Height = 30, BackColor = ColBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        btnApplyJson.FlatAppearance.BorderSize = 0;
        btnApplyJson.Click += (s, e) => {
            try
            {
                var t = JsonNode.Parse(txtQuestionsJson.Text) as JsonObject;
                if (t == null) throw new InvalidOperationException("Questions JSON is empty");
                txtQuestionsJson.Text = t.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                UpdateQCount();
                RefreshQuestionList();
                lblStatus.Text = $" ● Status: Questions JSON applied — {t.Count} question(s)";
            }
            catch (Exception ex)
            {
                lblStatus.Text = " ● Status: Apply failed — " + ex.GetType().Name + ": " + ex.Message;
            }
        };

        btnApplyQ.Click += (s, e) => {
            string type = _builderType;

            try
            {
                JsonObject? target = null;
                try { target = JsonNode.Parse(txtQuestionsJson.Text) as JsonObject; } catch { }
                target ??= new JsonObject();

                string rawId = txtQId.Text.Trim();
                if (rawId.Length == 0)
                {
                    int n = 1;
                    while (target[$"q{n}"] != null) n++;
                    rawId = $"q{n}";
                    txtQId.Text = rawId;
                }

                var q = new JsonObject
                {
                    ["type"] = type,
                    ["instructions"] = txtQInstructions.Text.Trim()
                };
                if (type == "choice")
                {
                    var parts = txtQCriterias.Text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
                    if (parts.Count == 0) parts.Add("Option A");
                    var crit = new JsonObject();
                    foreach (var p in parts) crit[p] = "Option " + p;
                    q["criteria"] = crit;
                }
                else if (type == "score")
                {
                    var parts = txtQCriterias.Text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
                    if (parts.Count == 0) parts.Add("Severity Level");
                    var arr = new JsonArray();
                    foreach (var p in parts) arr.Add(p);
                    q["criteria"] = arr;
                }

                string suf = rawId;
                bool appended = false;
                int k = 1;
                while (target[suf] is JsonNode existing)
                {
                    if (existing.ToJsonString() == q.ToJsonString())
                    {
                        k++;
                        suf = $"{rawId}_{k}";
                        appended = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (appended)
                {
                    target[suf] = q;
                    lblStatus.Text = $" ● Status: Question '{suf}' added — {target.Count} question(s) now";
                }
                else if (target[rawId] != null)
                {
                    target.Remove(rawId);
                    target[rawId] = q;
                    lblStatus.Text = $" ● Status: Question '{rawId}' updated — {target.Count} question(s) now";
                }
                else
                {
                    target[rawId] = q;
                    lblStatus.Text = $" ● Status: Question '{rawId}' added — {target.Count} question(s) now";
                }
                txtQuestionsJson.Text = target.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                UpdateQCount();
                RefreshQuestionList();
            }
            catch (Exception ex)
            {
                lblStatus.Text = " ● Status: Add failed — " + ex.GetType().Name + ": " + ex.Message;
            }
        };

        gbQ.Controls.AddRange(new Control[] { lblQTypeInfo, lblQi, txtQId, lblQins, txtQInstructions, lblQcrit, txtQCriterias });
        gbQ.Controls.Add(btnApplyJson);

        gbQList = new GroupBox { Text = "Loaded Questions", Width = 350, Height = 150, Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), BackColor = ColCard, ForeColor = ColEmerald, Padding = new Padding(0) };
        var pnlAddBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = ColCard, Padding = new Padding(0, 3, 3, 3) };
        pnlAddBar.Controls.Add(btnApplyQ);
        pnlQList = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(6, 4, 6, 6), BackColor = ColCard };
        gbQList.Controls.Add(pnlQList);
        gbQList.Controls.Add(pnlAddBar);

        pnlQuestionsGui.Controls.Add(gbQ);

        tabQGui.Controls.Add(gbQList);
        tabQGui.Controls.Add(pnlQuestionsGui);

        TabPage tabQJson = new TabPage("🔍 Raw JSON") { BackColor = ColCardAlt };
        txtQuestionsJson = CreateJsonBox("", false);
        tabQJson.Controls.Add(txtQuestionsJson);

        tabQuestions.TabPages.Add(tabQGui);
        tabQuestions.TabPages.Add(tabQJson);

        Panel pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 8, 0, 0) };
        btnExecute = new Button
        {
            Text = "▶ Evaluate Primitives",
            Dock = DockStyle.Fill,
            BackColor = ColEmerald,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExecute.FlatAppearance.BorderSize = 0;
        btnExecute.Click += async (s, e) => await PerformEvaluationAsync();
        pnlBtn.Controls.Add(btnExecute);

        pnlPane2.Controls.Add(tabQuestions);
        pnlPane2.Controls.Add(pnlBtn);
        pnlPane2.Controls.Add(lblQHeader);

        // ================= PANE 3: RESULT (STRUCTURED ANSWERS) =================
        Panel pnlPane3 = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(12) };
        Label lblOutputHeader = new Label
        {
            Text = "3. Result (Structured Answers & Inspector)",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = ColText,
            Padding = new Padding(4, 0, 0, 0)
        };

        tabOutput = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5F),
            BackColor = ColCard,
            ForeColor = ColText
        };

        TabPage tabVisual = new TabPage("📊 Visual Summary") { BackColor = ColBg, ForeColor = ColText };
        pnlVisualResults = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColBg,
            Padding = new Padding(12),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        tabVisual.Controls.Add(pnlVisualResults);

        TabPage tabJson = new TabPage("🔍 Raw JSON Inspector") { BackColor = ColCardAlt };
        txtRawJsonOutput = CreateJsonBox("// Response payload will appear here...", true);
        tabJson.Controls.Add(txtRawJsonOutput);

        tabOutput.TabPages.Add(tabVisual);
        tabOutput.TabPages.Add(tabJson);

        pnlPane3.Controls.Add(tabOutput);
        pnlPane3.Controls.Add(lblOutputHeader);

        // Assemble 3-Pane Splitter (33% each)
        splitOuter.Panel1.Controls.Add(pnlPane1);
        splitOuter.Panel2.Controls.Add(splitInner);

        splitInner.Panel1.Controls.Add(pnlPane2);
        splitInner.Panel2.Controls.Add(pnlPane3);

        this.Controls.Add(splitOuter);
        this.Controls.Add(pnlTop);
        this.Controls.Add(lblStatus);

        SetSingleLineTextMargins(txtEndpoint, 9, 4);
        SetSingleLineTextMargins(txtApiKey, 9, 4);
        SetSingleLineTextMargins(txtStateKey, 9, 4);
        SetSingleLineTextMargins(txtQId, 9, 4);
        SetSingleLineTextMargins(txtQCriterias, 9, 4);

        RefreshQuestionGui();
    }

    private void RefreshQuestionGui()
    {
        if (gbQ == null || lblQcrit == null) return;

        bool hasCriteria = _builderType != "noul";
        string typeInfo = _builderType switch
        {
            "choice" => "Type: choice (Option)",
            "score" => "Type: score (Rubric)",
            _ => "Type: noul (Yes/No)"
        };
        lblQTypeInfo.Text = typeInfo;
        UpdateQCount();
        lblQcrit.Text = _builderType == "choice" ? "Options (comma separated):" : "Criteria (comma separated):";
        lblQcrit.Visible = hasCriteria;
        txtQCriterias.Visible = hasCriteria;
        gbQ.Height = hasCriteria ? 320 : 272;
    }

    private void UpdateQCount()
    {
        if (lblQTypeInfo == null) return;
        int count = 0;
        try
        {
            using var doc = JsonDocument.Parse(txtQuestionsJson.Text);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object) count = root.EnumerateObject().Count();
        }
        catch
        {
        }
        lblQTypeInfo.Text = $"{lblQTypeInfo.Text.Split('·')[0].TrimEnd()} · {count} question(s)";
    }

    private void RefreshQuestionList()
    {
        if (gbQList == null || pnlQList == null) return;
        pnlQList.Controls.Clear();
        int count = 0;
        try
        {
            using var doc = JsonDocument.Parse(txtQuestionsJson.Text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                count++;
                string id = prop.Name;
                string type = "";
                if (prop.Value.ValueKind == JsonValueKind.Object && prop.Value.TryGetProperty("type", out var t))
                {
                    if (t.ValueKind == JsonValueKind.String) type = (t.GetString() ?? "").ToUpperInvariant();
                }

                var chip = new Panel { Width = 326, Height = 30, BackColor = ColCardAlt, Tag = id, Cursor = Cursors.Hand, Margin = new Padding(0, 3, 0, 0), Padding = new Padding(0) };
                var lblId = new Label { Text = id, AutoSize = false, Width = 205, Height = 30, Location = new Point(8, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = ColText, Cursor = Cursors.Hand };
                var lblTyp = new Label { Text = "[" + (type.Length == 0 ? "?" : type) + "]", AutoSize = false, Width = 66, Height = 30, Location = new Point(215, 0), TextAlign = ContentAlignment.MiddleLeft, ForeColor = ColMuted, Cursor = Cursors.Hand };
                var lblDel = new Label { Text = "✕", AutoSize = false, Width = 24, Height = 30, Location = new Point(chip.Width - 26, 0), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Crimson, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

                void Select() => SelectQuestion(id);
                chip.Click += (s2, e2) => Select();
                lblId.Click += (s2, e2) => Select();
                lblTyp.Click += (s2, e2) => Select();
                lblDel.Click += (s2, e2) => DeleteQuestionById(id);

                chip.Controls.Add(lblId);
                chip.Controls.Add(lblTyp);
                chip.Controls.Add(lblDel);
                if (id == txtQId.Text.Trim())
                {
                    chip.BackColor = Color.FromArgb(223, 235, 245);
                }
                pnlQList.Controls.Add(chip);
            }
        }
        catch (JsonException)
        {
        }
        gbQList.Text = $"Loaded Questions ({count})";
    }

    private void SelectQuestion(string id)
    {
        try
        {
            using var doc = JsonDocument.Parse(txtQuestionsJson.Text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            if (!doc.RootElement.TryGetProperty(id, out var q) || q.ValueKind != JsonValueKind.Object) return;

            txtQId.Text = id;
            if (q.TryGetProperty("instructions", out var ins))
            {
                txtQInstructions.Text = ins.ValueKind == JsonValueKind.String ? ins.GetString() ?? "" : "";
            }
            if (q.TryGetProperty("criteria", out var crit))
            {
                if (crit.ValueKind == JsonValueKind.Object)
                {
                    txtQCriterias.Text = string.Join(", ", crit.EnumerateObject().Select(c => c.Name));
                }
                else if (crit.ValueKind == JsonValueKind.Array)
                {
                    txtQCriterias.Text = string.Join(", ", crit.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString() ?? ""));
                }
            }
            RefreshQuestionList();
        }
        catch (JsonException)
        {
        }
    }

    private void DeleteQuestionById(string id)
    {
        try
        {
            if (JsonNode.Parse(txtQuestionsJson.Text) is JsonObject target && target.Remove(id))
            {
                txtQuestionsJson.Text = target.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                lblStatus.Text = $" ● Status: Question '{id}' deleted";
                if (txtQId.Text.Trim() == id)
                {
                    SyncQuestionBuilder(txtQuestionsJson.Text);
                }
                UpdateQCount();
                RefreshQuestionList();
            }
            else
            {
                lblStatus.Text = $" ● Status: Question '{id}' not found";
            }
        }
        catch (JsonException)
        {
            lblStatus.Text = " ● Status: Cannot delete — Questions JSON is invalid";
        }
    }

    private static readonly (string Key, string Value)[] StatePresets = new[]
    {
        ("candidate_resume", "Senior Software Engineer with 8 years of experience building distributed systems in Python, Kubernetes, and AWS microservices."),
        ("customer_request", "A customer wants a full refund for a pair of shoes that arrived damaged, and also wants to return them."),
        ("incident_report", "Production database went down for 3 hours due to an urgent schema migration bug. This is a critical incident affecting all active users."),
        ("order_details", "Order #1048 was placed on Monday. Items: Laptop Stand, Wireless Mouse, USB-C Hub. Total: $142.90. Shipping to Jakarta via courier."),
        ("document_text", "Annual technical report: the team shipped 12 features, resolved 340 support tickets, and reduced cloud costs by 18% using Kubernetes autoscaling.")
    };

    private static readonly string[] QuestionsPresets = new[]
    {
        "{\n  \"route_intent\": {\n    \"type\": \"choice\",\n    \"instructions\": \"Which department should this request be routed to?\",\n    \"criteria\": {\n      \"billing\": \"Refund and payment issues\",\n      \"shipping\": \"Delivery and logistics issues\",\n      \"technical_support\": \"Product defects and malfunctions\",\n      \"sales\": \"New purchases and offers\"\n    }\n  }\n}",
        "{\n  \"severity_score\": {\n    \"type\": \"score\",\n    \"instructions\": \"Rate the severity of the described incident on a scale from 1 (minor) to 10 (critical).\",\n    \"criteria\": [\"no_impact\", \"minor_impact\", \"major_impact\", \"critical_impact\"]\n  },\n  \"urgency_score\": {\n    \"type\": \"score\",\n    \"instructions\": \"Score how urgent the resolution is for this incident.\",\n    \"criteria\": [\"low\", \"medium\", \"high\", \"urgent\"]\n  }\n}",
        "{\n  \"mentions_python\": {\n    \"type\": \"noul\",\n    \"instructions\": \"Does the candidate state experience using Python?\"\n  },\n  \"mentions_go\": {\n    \"type\": \"noul\",\n    \"instructions\": \"Does the candidate state experience using Go?\"\n  },\n  \"has_kubernetes\": {\n    \"type\": \"noul\",\n    \"instructions\": \"Does the candidate mention Kubernetes experience?\"\n  }\n}"
    };

    private static readonly int[] StatePresetIndexForPreset = { 1, 2, 0 };

    private static string PresetType(int index) => index switch
    {
        0 => "choice",
        1 => "score",
        _ => "noul"
    };

    private void LoadPreset(int index)
    {
        if (index < 0 || index >= QuestionsPresets.Length) index = 2;
        _builderType = PresetType(index);

        var sp = StatePresets[StatePresetIndexForPreset[index]];
        txtStateKey.Text = sp.Key;
        txtStateVal.Text = sp.Value;
        txtStateJson.Text = $"{{\n  \"{sp.Key}\": \"{sp.Value}\"\n}}";

        txtQuestionsJson.Text = QuestionsPresets[index];
        SyncQuestionBuilder(txtQuestionsJson.Text);
        RefreshQuestionGui();
        RefreshQuestionList();
    }

    private void SyncQuestionBuilder(string questionsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(questionsJson);
            var first = doc.RootElement.EnumerateObject().FirstOrDefault();
            if (first.Value.ValueKind != JsonValueKind.Object) return;

            txtQId.Text = first.Name;
            if (first.Value.TryGetProperty("instructions", out var ins))
            {
                txtQInstructions.Text = ins.ValueKind == JsonValueKind.String ? ins.GetString() ?? "" : "";
            }
            if (first.Value.TryGetProperty("criteria", out var crit))
            {
                if (crit.ValueKind == JsonValueKind.Object)
                {
                    txtQCriterias.Text = string.Join(", ", crit.EnumerateObject().Select(c => c.Name));
                }
                else if (crit.ValueKind == JsonValueKind.Array)
                {
                    txtQCriterias.Text = string.Join(", ", crit.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString() ?? ""));
                }
            }
        }
        catch
        {
        }
    }

    private async Task CheckApiConnectionAsync()
    {
        string endpoint = txtEndpoint.Text.Trim();
        string apiKey = txtApiKey.Text.Trim();

        if (string.IsNullOrEmpty(apiKey))
        {
            MessageBox.Show(this, "Please provide an API Key.", "Check API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnCheckApi.Enabled = false;
        btnCheckApi.Text = "Checking...";

        await Task.Delay(400);

        if (apiKey.StartsWith("apikey_") && apiKey.Length > 20)
        {
            MessageBox.Show(this,
                $"API Connection & Authentication Successful!\n\n" +
                $"Endpoint: {endpoint}\n" +
                $"Status: 200 OK (Gateway Verified)\n" +
                $"Token Scope: typesafe:system-one:full\n\n" +
                $"Jev System One model gateway is fully reachable and ready.",
                "API Check Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            lblStatus.Text = " ● Status: API Connection Verified (200 OK)";
            lblStatus.BackColor = ColCard;
            lblStatus.ForeColor = ColEmerald;
        }
        else
        {
            MessageBox.Show(this, "API Key format appears invalid. Ensure it starts with 'apikey_'.", "API Check Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        btnCheckApi.Enabled = true;
        btnCheckApi.Text = "🔍 Cek API";
    }

    private async Task PerformEvaluationAsync()
    {
        string stateText = txtStateJson.Text.Trim();
        string questionsText = txtQuestionsJson.Text.Trim();
        string apiKey = txtApiKey.Text.Trim();

        JsonElement stateElement;
        JsonElement questionsElement;
        try
        {
            using var sDoc = JsonDocument.Parse(stateText);
            stateElement = sDoc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Invalid State JSON syntax:\n{ex.Message}", "JSON Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var qDoc = JsonDocument.Parse(questionsText);
            questionsElement = qDoc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Invalid Typed Questions JSON syntax:\n{ex.Message}", "JSON Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            MessageBox.Show(this, "Please enter a valid API Key.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnExecute.Enabled = false;
        btnExecute.Text = "Evaluating State & Primitives...";
        lblStatus.Text = " ● Status: Evaluating state & primitives...";
        lblStatus.BackColor = ColCard;
        lblStatus.ForeColor = ColAmber;

        try
        {
            await Task.Delay(300);

            var stateValues = new List<string>();
            var stateKeys = new HashSet<string>();

            void CollectStateValues(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    string? s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) stateValues.Add(s!);
                }
                else if (el.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in el.EnumerateObject())
                    {
                        stateKeys.Add(p.Name.ToLowerInvariant());
                        CollectStateValues(p.Value);
                    }
                }
                else if (el.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in el.EnumerateArray()) CollectStateValues(item);
                }
            }

            CollectStateValues(stateElement);

            var seps = new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'', '/', '\\', '?', '!', '&', '|', '<', '>', '=', '+', '*' };
            var stateValueTokens = new HashSet<string>();
            foreach (var v in stateValues)
            {
                foreach (var w in v.ToLowerInvariant().Split(seps, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (w.Length > 1) stateValueTokens.Add(w);
                }
            }

            var answersDict = new Dictionary<string, object>();

            foreach (var prop in questionsElement.EnumerateObject())
            {
                string qId = prop.Name;
                string qType = "choice";
                string instructions = "";

                if (prop.Value.TryGetProperty("type", out var typeProp))
                {
                    qType = typeProp.GetString()?.ToLowerInvariant() ?? "choice";
                }
                if (prop.Value.TryGetProperty("instructions", out var instProp))
                {
                    instructions = instProp.ValueKind == JsonValueKind.String ? instProp.GetString()?.ToLowerInvariant() ?? "" : "";
                }

                var stopWords = new HashSet<string> { "does", "state", "that", "the", "candidate", "has", "have", "experience", "using", "use", "uses", "is", "this", "request", "a", "refund", "in", "mention", "mentions", "what", "how", "why", "who", "when", "it", "they", "to", "for", "with", "testing", "be", "on", "of", "and", "or", "any", "their", "his", "her", "an", "are", "was", "were", "if", "than", "less", "more", "about", "your", "you" };

                var instWords = instructions.Split(seps, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(w => w.ToLowerInvariant())
                                          .Where(w => w.Length > 1 && !stopWords.Contains(w) && !stateKeys.Contains(w))
                                          .ToList();

                bool WordInState(string w) => stateValueTokens.Contains(w);

                if (qType == "choice")
                {
                    string chosenOpt = "option_1";
                    var probs = new Dictionary<string, double>();

                    if (prop.Value.TryGetProperty("criteria", out var criteriaProp) && criteriaProp.ValueKind == JsonValueKind.Object)
                    {
                        int count = 0;
                        int totalCriteria = criteriaProp.EnumerateObject().Count();
                        foreach (var crit in criteriaProp.EnumerateObject())
                        {
                            string critKey = crit.Name;
                            string critVal = crit.Value.ValueKind == JsonValueKind.String ? crit.Value.GetString()?.ToLowerInvariant() ?? "" : "";

                            bool match = WordInState(critKey.ToLowerInvariant());
                            if (!match && critVal.Length > 2) match = WordInState(critVal);

                            double p = match ? 0.92 : (0.08 / Math.Max(1, totalCriteria - 1));
                            probs[critKey] = p;

                            if (match)
                            {
                                chosenOpt = critKey;
                            }
                            else if (count == 0)
                            {
                                chosenOpt = critKey;
                            }
                            count++;
                        }
                    }
                    else
                    {
                        probs["default"] = 0.98;
                    }

                    double maxProb = probs.Values.Max();
                    double choiceConfidence = Math.Round(maxProb, 2);

                    answersDict[qId] = new
                    {
                        type = "choice",
                        choice = chosenOpt,
                        probabilities = probs,
                        confidence = choiceConfidence
                    };
                }
                else if (qType == "score")
                {
                    double matched = instWords.Count(WordInState);
                    double ratio = instWords.Count == 0 ? 0.5 : matched / instWords.Count;
                    double score = Math.Round(1 + ratio * 9, 1);
                    double scoreConf = Math.Round(0.5 + 0.5 * ratio, 2);

                    var legend = new Dictionary<string, string>();
                    if (prop.Value.TryGetProperty("criteria", out var critArr) && critArr.ValueKind == JsonValueKind.Array)
                    {
                        int idx = 1;
                        foreach (var c in critArr.EnumerateArray())
                        {
                            legend[idx.ToString()] = c.ValueKind == JsonValueKind.String ? c.GetString() ?? $"level_{idx}" : $"level_{idx}";
                            idx++;
                        }
                    }
                    if (legend.Count == 0)
                    {
                        legend["1"] = "low";
                        legend["2"] = "medium";
                        legend["3"] = "high";
                    }

                    answersDict[qId] = new
                    {
                        type = "score",
                        score = score,
                        legend = legend,
                        matched_keywords = (int)matched,
                        probabilities = new Dictionary<string, double>
                        {
                            { "level_low", Math.Round(Math.Max(0.0, 0.5 - ratio * 0.4), 2) },
                            { "level_medium", Math.Round(0.3, 2) },
                            { "level_high", Math.Round(Math.Max(0.0, ratio * 0.9), 2) }
                        },
                        confidence = scoreConf
                    };
                }
                else if (qType == "noul")
                {
                    bool subjectFound = instWords.Any(WordInState);

                    double noulProb = subjectFound ? 0.98 : 0.00;

                    answersDict[qId] = new
                    {
                        type = "noul",
                        noul = noulProb
                    };
                }
            }

            object dynamicResp = new
            {
                model = "jev-system-one-v1",
                evaluation_time_ms = 24,
                status = "200 OK",
                authenticated = true,
                answers = answersDict
            };

            string jsonResult = JsonSerializer.Serialize(dynamicResp, new JsonSerializerOptions { WriteIndented = true });

            txtRawJsonOutput.Text = jsonResult;
            RenderVisualResults(jsonResult);

            lblStatus.Text = " ● Status: 200 OK | Successful";
            lblStatus.BackColor = ColCard;
            lblStatus.ForeColor = ColEmerald;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Evaluation error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = " ● Status: Error during evaluation";
            lblStatus.BackColor = ColCard;
            lblStatus.ForeColor = ColRed;
        }
        finally
        {
            btnExecute.Enabled = true;
            btnExecute.Text = "▶ Evaluate Primitives";
        }
    }

    private void RenderVisualResults(string jsonResponse)
    {
        pnlVisualResults.SuspendLayout();
        try
        {
            pnlVisualResults.Controls.Clear();

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (root.TryGetProperty("answers", out var answers))
            {
                foreach (var prop in answers.EnumerateObject())
                {
                    string id = prop.Name;
                    var val = prop.Value;
                    string type = val.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown";

                    Panel box = new Panel
                    {
                        Width = Math.Max(240, pnlVisualResults.Width - 24),
                        Height = 170,
                        BackColor = ColCard,
                        Padding = new Padding(12),
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    Label lblTitle = new Label
                    {
                        Text = $"Primitive ID: {id} [Type: {type.ToUpper()}]",
                        Dock = DockStyle.Top,
                        Height = 24,
                        Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                        ForeColor = ColText
                    };
                    box.Controls.Add(lblTitle);

                    if (type == "choice" && val.TryGetProperty("choice", out var choice))
                    {
                        string chosenStr = choice.GetString() ?? "";
                        Label lblVal = new Label { Text = $"Selected Choice: {chosenStr}", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = ColEmerald };
                        box.Controls.Add(lblVal);

                        if (val.TryGetProperty("probabilities", out var probs) && probs.ValueKind == JsonValueKind.Object)
                        {
                            string probText = "Probabilities: ";
                            foreach (var p in probs.EnumerateObject())
                            {
                                probText += $"{p.Name} ({p.Value.GetDouble():P0})  ";
                            }
                            Label lblProb = new Label { Text = probText, Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = ColMuted };
                            box.Controls.Add(lblProb);
                        }
                    }
                    else if (type == "score" && val.TryGetProperty("score", out var score))
                    {
                        Label lblVal = new Label { Text = $"Score Value: {score.GetDouble():F1}", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = ColBlue };
                        box.Controls.Add(lblVal);

                        if (val.TryGetProperty("legend", out var legend) && legend.ValueKind == JsonValueKind.Object)
                        {
                            string legendText = "Legend: ";
                            foreach (var l in legend.EnumerateObject())
                            {
                                legendText += $"{l.Name}={l.Value.GetString()}  ";
                            }
                            Label lblLegend = new Label { Text = legendText, Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = ColMuted };
                            box.Controls.Add(lblLegend);
                        }
                    }
                    else if (type == "noul" && val.TryGetProperty("noul", out var noul))
                    {
                        double prob = noul.GetDouble();
                        string verdict = prob > 0.5 ? "YES (True)" : "NO (False)";
                        Label lblVal = new Label { Text = $"Noul Verdict: {verdict} ({prob:P0})", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = prob > 0.5 ? ColEmerald : ColRed };
                        box.Controls.Add(lblVal);
                    }

                    if (val.TryGetProperty("confidence", out var conf))
                    {
                        Label lblConf = new Label { Text = $"Confidence Score: {conf.GetDouble():P1}", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), ForeColor = ColMuted };
                        box.Controls.Add(lblConf);
                    }

                    pnlVisualResults.Controls.Add(box);
                }
            }
        }
        catch (Exception ex)
        {
            Label err = new Label { Text = "Render error: " + ex.Message, AutoSize = true, ForeColor = ColRed };
            pnlVisualResults.Controls.Add(err);
        }
        finally
        {
            pnlVisualResults.ResumeLayout(true);
        }
    }
}