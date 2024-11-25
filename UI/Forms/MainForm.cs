// File: UI/Forms/MainForm.cs
using PetitionD.Configuration;
using PetitionD.Core.Services;
using System.Drawing;
using System.Windows.Forms;

namespace PetitionD.UI.Forms;

public partial class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly RichTextBox _consoleOutput;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _petitionCountLabel;
    private readonly ToolStripStatusLabel _gmCountLabel;
    private readonly ToolStripStatusLabel _worldCountLabel;
    private readonly ServerService _serverService;
    private readonly TabControl _tabControl;
    private readonly ListView _connectionsListView;
    private readonly ListView _packetsListView;
    private bool _isRunning;

    public MainForm(AppSettings settings, ServerService serverService)
    {
        _settings = settings;
        _serverService = serverService;
        InitializeComponent();

        // Set window properties
        this.Text = $"PetitionD Server v{settings.ServerBuildNumber}";
        this.Size = new Size(1200, 800);

        // Create tab control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F)
        };

        // Create Console Tab
        var consoleTab = new TabPage("Console");
        _consoleOutput = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            ForeColor = Color.LightGreen,
            Font = new Font("Cascadia Code", 10F),
            ReadOnly = true,
            Multiline = true,
            WordWrap = false
        };
        consoleTab.Controls.Add(_consoleOutput);

        // Create Connections Tab
        var connectionsTab = new TabPage("Connections");
        _connectionsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _connectionsListView.Columns.AddRange(new[]
        {
            new ColumnHeader { Text = "ID", Width = 100 },
            new ColumnHeader { Text = "Type", Width = 100 },
            new ColumnHeader { Text = "Remote Endpoint", Width = 150 },
            new ColumnHeader { Text = "Connected Time", Width = 150 },
            new ColumnHeader { Text = "Status", Width = 100 }
        });
        connectionsTab.Controls.Add(_connectionsListView);

        // Create Packets Tab
        var packetsTab = new TabPage("Packets");
        _packetsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _packetsListView.Columns.AddRange(new[]
        {
            new ColumnHeader { Text = "Time", Width = 100 },
            new ColumnHeader { Text = "Direction", Width = 80 },
            new ColumnHeader { Text = "Session", Width = 100 },
            new ColumnHeader { Text = "Type", Width = 150 },
            new ColumnHeader { Text = "Size", Width = 80 },
            new ColumnHeader { Text = "Data", Width = 300 }
        });
        packetsTab.Controls.Add(_packetsListView);

        // Add tabs to control
        _tabControl.TabPages.AddRange(new[] { consoleTab, connectionsTab, packetsTab });

        // Create status strip
        _statusStrip = new StatusStrip();
        _petitionCountLabel = new ToolStripStatusLabel("Petitions: 0");
        _gmCountLabel = new ToolStripStatusLabel("GMs: 0");
        _worldCountLabel = new ToolStripStatusLabel("Worlds: 0");
        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _petitionCountLabel,
            new ToolStripSeparator(),
            _gmCountLabel,
            new ToolStripSeparator(),
            _worldCountLabel
        });

        // Add controls to form
        Controls.Add(_tabControl);
        Controls.Add(_statusStrip);

        // Setup context menu for packet list
        var packetContextMenu = new ContextMenuStrip();
        packetContextMenu.Items.Add("Copy", null, (s, e) => CopySelectedPacket());
        packetContextMenu.Items.Add("Clear", null, (s, e) => _packetsListView.Items.Clear());
        _packetsListView.ContextMenuStrip = packetContextMenu;

        StartServer();
    }

    public void LogPacket(DateTime time, string direction, string session, string type, int size, byte[] data)
    {
        if (_packetsListView.InvokeRequired)
        {
            _packetsListView.Invoke(() => LogPacket(time, direction, session, type, size, data));
            return;
        }

        var item = new ListViewItem(new[]
        {
            time.ToString("HH:mm:ss.fff"),
            direction,
            session,
            type,
            size.ToString(),
            BitConverter.ToString(data).Replace("-", " ")
        });

        if (direction == "OUT")
            item.BackColor = Color.FromArgb(240, 248, 255); // Light blue for outgoing
        else
            item.BackColor = Color.FromArgb(255, 240, 245); // Light pink for incoming

        _packetsListView.Items.Insert(0, item);

        // Keep only last 1000 packets
        if (_packetsListView.Items.Count > 1000)
            _packetsListView.Items.RemoveAt(_packetsListView.Items.Count - 1);
    }

    public void UpdateConnection(string id, string type, string endpoint, DateTime connectedTime, string status)
    {
        if (_connectionsListView.InvokeRequired)
        {
            _connectionsListView.Invoke(() => UpdateConnection(id, type, endpoint, connectedTime, status));
            return;
        }

        var existing = _connectionsListView.Items.Cast<ListViewItem>()
            .FirstOrDefault(x => x.Text == id);

        if (existing != null)
        {
            existing.SubItems[4].Text = status;
            if (status == "Disconnected")
                existing.BackColor = Color.LightGray;
        }
        else
        {
            var item = new ListViewItem(new[]
            {
                id,
                type,
                endpoint,
                connectedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                status
            });
            _connectionsListView.Items.Add(item);
        }
    }

    private void CopySelectedPacket()
    {
        if (_packetsListView.SelectedItems.Count > 0)
        {
            var item = _packetsListView.SelectedItems[0];
            var text = string.Join("\t", Enumerable.Range(0, item.SubItems.Count)
                .Select(i => item.SubItems[i].Text));

            // Use a new thread with STA apartment state to set the clipboard text
            Thread thread = new Thread(() =>
            {
                Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA); // Set the thread to STA
            thread.Start();
            thread.Join(); // Wait for the thread to end
        }
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
            ShowWelcomeMessage();
            _serverService.Start();

            // Add status updates
            var timer = new System.Windows.Forms.Timer
            {
                Interval = _settings.ServerStatusRefreshInterval * 1000
            };

            timer.Tick += (s, e) => UpdateStatus(
                GetActivePetitionCount(),
                GetGmCount(),
                GetWorldCount()
            );
            timer.Start();

            LogMessage($"GM Service started on port {_settings.GmServicePort}", LogLevel.Information);
            LogMessage($"World Service started on port {_settings.WorldServicePort}", LogLevel.Information);
            LogMessage($"Notice Service started on port {_settings.NoticeServicePort}", LogLevel.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error starting server: {ex.Message}", LogLevel.Error);
            MessageBox.Show(
                $"Failed to start server: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // Status methods - these will be updated later with real implementation
    // File: UI/Forms/MainForm.cs
    // Replace the stub methods with:

    private int GetActivePetitionCount()
    {
        if (_serverService == null) return 0;
        // TODO: Implement by accessing PetitionList service
        return 0;
    }

    private int GetGmCount()
    {
        if (_serverService == null) return 0;
        // TODO: Get from GmSessionList
        return 0;
    }

    private int GetWorldCount()
    {
        if (_serverService == null) return 0;
        // TODO: Get from WorldSessionList
        return 0;
    }
}