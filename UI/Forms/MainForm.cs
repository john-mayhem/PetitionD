// File: UI/Forms/MainForm.cs
using PetitionD.Configuration;
using System.Drawing;
using System.Windows.Forms;

namespace PetitionD.UI.Forms;

public partial class MainForm : Form
{
    private readonly AppSettings _settings = new();
    private readonly RichTextBox _consoleOutput;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _petitionCountLabel;
    private readonly ToolStripStatusLabel _gmCountLabel;
    private readonly ToolStripStatusLabel _worldCountLabel;
    private bool _isRunning;

    public MainForm(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        // Set window properties
        this.Text = "PetitionD Server";
        this.Size = new System.Drawing.Size(1024, 768);
        this.BackColor = System.Drawing.Color.Black;

        // Create rich text console
        _consoleOutput = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.FromArgb(50, 255, 50),  // Matrix green
            Font = new Font("Cascadia Code", 10F),     // Modern programming font
            ReadOnly = true,
            Multiline = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false,
            Margin = new System.Windows.Forms.Padding(10)
        };

        // Create status strip
        _statusStrip = new StatusStrip
        {
            BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
            ForeColor = System.Drawing.Color.White
        };

        _petitionCountLabel = new ToolStripStatusLabel
        {
            Text = "Petitions: 0",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            BorderStyle = Border3DStyle.Etched
        };

        _gmCountLabel = new ToolStripStatusLabel
        {
            Text = "GMs Online: 0",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            BorderStyle = Border3DStyle.Etched
        };

        _worldCountLabel = new ToolStripStatusLabel
        {
            Text = "Worlds: 0"
        };

        _statusStrip.Items.AddRange(
        [
            _petitionCountLabel,
            _gmCountLabel,
            _worldCountLabel
        ]);

        // Add controls
        Controls.Add(_consoleOutput);
        Controls.Add(_statusStrip);

        ShowWelcomeMessage();
        StartServer();
    }

    private void ShowWelcomeMessage()
    {
        var welcomeText = @"";
        AppendText(welcomeText, System.Drawing.Color.Cyan);
        AppendText($"\nPetition Server v{_settings.ServerBuildNumber}\n", System.Drawing.Color.White);
        AppendText($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n", System.Drawing.Color.Gray);
        AppendText("----------------------------------------\n", System.Drawing.Color.DarkGray);
    }

    private void AppendText(string text, System.Drawing.Color color)
    {
        if (_consoleOutput.InvokeRequired)
        {
            _consoleOutput.Invoke(() => AppendText(text, color));
            return;
        }

        _consoleOutput.SelectionStart = _consoleOutput.TextLength;
        _consoleOutput.SelectionLength = 0;
        _consoleOutput.SelectionColor = color;
        _consoleOutput.AppendText(text);
        _consoleOutput.ScrollToCaret();
    }

    private void UpdateStatus(int petitions, int gmsOnline, int worlds)
    {
        if (_statusStrip.InvokeRequired)
        {
            _statusStrip.Invoke(() => UpdateStatus(petitions, gmsOnline, worlds));
            return;
        }

        _petitionCountLabel.Text = $"Petitions: {petitions}";
        _gmCountLabel.Text = $"GMs Online: {gmsOnline}";
        _worldCountLabel.Text = $"Worlds: {worlds}";
    }

    private void LogMessage(string message, LogLevel level = LogLevel.Information)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var color = level switch
        {
            LogLevel.Error => System.Drawing.Color.Red,
            LogLevel.Warning => System.Drawing.Color.Yellow,
            LogLevel.Information => System.Drawing.Color.FromArgb(50, 255, 50),
            LogLevel.Debug => System.Drawing.Color.Gray,
            _ => System.Drawing.Color.White
        };

        AppendText($"[{timestamp}] {message}\n", color);
    }

    private void StartServer()
    {
        try
        {
            LogMessage("Initializing services...");
            LogMessage($"GM Service started on port {_settings.GmServicePort}", LogLevel.Information);
            _isRunning = true;

            // Update status periodically
            var statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };

            statusTimer.Tick += (s, e) =>
            {
                UpdateStatus(
                    GetActivePetitionCount(),
                    GetGmCount(),
                    GetWorldCount()
                );
            };
            statusTimer.Start();
        }
        catch (Exception ex)
        {
            LogMessage($"Error starting server: {ex.Message}", LogLevel.Error);
        }
    }

    // Status methods - these will be updated later with real implementation
    private static int GetActivePetitionCount() => 0;
    private static int GetGmCount() => 0;
    private static int GetWorldCount() => 0;
}