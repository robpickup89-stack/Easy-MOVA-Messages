using MoVALiveViewer.Config;
using MoVALiveViewer.Models;
using MoVALiveViewer.Pipeline;
using MoVALiveViewer.Sources;
using MoVALiveViewer.State;
using MoVALiveViewer.UI.Controls;
using System.Diagnostics;

namespace MoVALiveViewer.UI;

public sealed class MovaViewerForm : Form
{
    // Pipeline
    private readonly AppState _state;
    private readonly PipelineOrchestrator _pipeline;
    private AppSettings _settings;

    // UI refresh timer
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly System.Windows.Forms.Timer _alertTimer = new();

    // Toolbar controls
    private readonly ToolStrip _toolbar = new();
    private readonly ToolStripComboBox _sourceMode = new();
    private readonly ToolStripComboBox _processSelector = new();
    private readonly ToolStripComboBox _textboxSelector = new();
    private readonly ToolStripButton _refreshBtn = new();
    private readonly ToolStripButton _connectBtn = new();
    private readonly ToolStripButton _disconnectBtn = new();
    private readonly ToolStripButton _pauseBtn = new();
    private readonly ToolStripButton _recordBtn = new();
    private readonly ToolStripButton _pinBtn = new();
    private readonly ToolStripButton _themeBtn = new();
    private readonly ToolStripTextBox _searchBox = new();
    private readonly ToolStripButton _filterStageBtn = new();
    private readonly ToolStripButton _filterOptBtn = new();
    private readonly ToolStripButton _filterDemBtn = new();
    private readonly ToolStripButton _filterNxBtn = new();

    // Replay toolbar
    private readonly ToolStripComboBox _replaySpeed = new();

    // Status bar
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripStatusLabel _lineCountLabel = new();
    private readonly ToolStripStatusLabel _snapshotCountLabel = new();
    private readonly ToolStripStatusLabel _alertCountLabel = new();
    private readonly StatusBadge _connectionBadge = new();

    // Main layout
    private readonly Panel _mainPanel = new();
    private readonly SplitContainer _mainSplit = new();
    private readonly DataGridView _eventGrid = new();
    private readonly TabControl _tabControl = new();

    // Tab panels
    private readonly OverviewPanel _overviewPanel = new();
    private readonly StagesPanel _stagesPanel = new();
    private readonly LinksPanel _linksPanel = new();
    private readonly RawViewPanel _rawPanel = new();
    private readonly AlertsPanel _alertsPanel = new();

    // Filtering
    private readonly List<EventRow> _allEvents = new();
    private string _searchFilter = "";
    private bool _filterStageOnly;
    private bool _filterOptOnly;
    private bool _filterDemOnly;
    private bool _filterNxOnly;
    private bool _uiPaused;

    // Process list cache for textbox selector
    private List<(int pid, string name, string title)> _processList = new();

    public MovaViewerForm()
    {
        _settings = AppSettings.Load();
        _state = new AppState(_settings.RingBufferSize);
        _pipeline = new PipelineOrchestrator(_state);

        // Apply saved theme preference
        Theme.SetDark(_settings.ThemeMode != "Light");

        InitializeForm();
        BuildToolbar();
        BuildStatusBar();
        BuildMainLayout();

        // Fix docking order: Fill panel at lowest z-index (processed last)
        // so toolbar (Top) and status strip (Bottom) get their space first.
        Controls.Add(_mainPanel);    // Fill - index 0 (processed last)
        Controls.Add(_statusStrip);  // Bottom - index 1
        Controls.Add(_toolbar);      // Top - index 2 (processed first)

        WireEvents();

        _refreshTimer.Interval = _settings.UiRefreshMs;
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Start();

        _alertTimer.Interval = 5000;
        _alertTimer.Tick += (_, _) => _state.Alerts.PeriodicCheck();
        _alertTimer.Start();
    }

    private void InitializeForm()
    {
        Text = "MOVA Live Viewer";
        Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Theme.ApplyToForm(this);

        // Enable double-buffering to reduce flicker
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    private void BuildToolbar()
    {
        _toolbar.BackColor = Theme.SurfaceLight;
        _toolbar.ForeColor = Theme.TextPrimary;
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Padding = new Padding(8, 2, 8, 2);
        _toolbar.RenderMode = ToolStripRenderMode.Professional;
        _toolbar.Renderer = new ThemedToolStripRenderer();
        _toolbar.ImageScalingSize = new Size(16, 16);

        // Source mode
        _sourceMode.Items.AddRange(new object[] { "File Replay", "UIA Textbox" });
        _sourceMode.SelectedIndex = _settings.SourceMode == "UIA" ? 1 : 0;
        _sourceMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _sourceMode.BackColor = Theme.SurfaceLight;
        _sourceMode.ForeColor = Theme.TextPrimary;
        _sourceMode.Size = new Size(110, 25);
        _sourceMode.SelectedIndexChanged += SourceMode_Changed;

        // Process selector
        _processSelector.Size = new Size(180, 25);
        _processSelector.BackColor = Theme.SurfaceLight;
        _processSelector.ForeColor = Theme.TextPrimary;
        _processSelector.Visible = _sourceMode.SelectedIndex == 1;
        _processSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _processSelector.SelectedIndexChanged += ProcessSelector_Changed;

        // Textbox selector
        _textboxSelector.Size = new Size(220, 25);
        _textboxSelector.BackColor = Theme.SurfaceLight;
        _textboxSelector.ForeColor = Theme.TextPrimary;
        _textboxSelector.Visible = false;
        _textboxSelector.DropDownStyle = ComboBoxStyle.DropDownList;

        // Refresh button
        _refreshBtn.Text = "\u21BB";
        _refreshBtn.ToolTipText = "Refresh process list";
        _refreshBtn.ForeColor = Theme.TextSecondary;
        _refreshBtn.Visible = false;
        _refreshBtn.Click += (_, _) => RefreshProcessList();

        // Replay speed
        _replaySpeed.Items.AddRange(new object[] { "Real-time", "50 lines/s", "200 lines/s", "500 lines/s", "Instant", "Step" });
        _replaySpeed.SelectedIndex = 2;
        _replaySpeed.DropDownStyle = ComboBoxStyle.DropDownList;
        _replaySpeed.BackColor = Theme.SurfaceLight;
        _replaySpeed.ForeColor = Theme.TextPrimary;
        _replaySpeed.Size = new Size(100, 25);

        // Buttons
        _connectBtn.Text = "Connect";
        _connectBtn.ForeColor = Theme.Green;
        _connectBtn.Click += ConnectBtn_Click;

        _disconnectBtn.Text = "Disconnect";
        _disconnectBtn.ForeColor = Theme.Red;
        _disconnectBtn.Enabled = false;
        _disconnectBtn.Click += DisconnectBtn_Click;

        _pauseBtn.Text = "Pause UI";
        _pauseBtn.ForeColor = Theme.TextSecondary;
        _pauseBtn.CheckOnClick = true;
        _pauseBtn.Click += (_, _) => { _uiPaused = _pauseBtn.Checked; _pauseBtn.ForeColor = _uiPaused ? Theme.Yellow : Theme.TextSecondary; };

        _recordBtn.Text = "Record";
        _recordBtn.ForeColor = Theme.TextSecondary;
        _recordBtn.CheckOnClick = true;
        _recordBtn.Click += RecordBtn_Click;

        _pinBtn.Text = "Pin";
        _pinBtn.ForeColor = Theme.TextSecondary;
        _pinBtn.CheckOnClick = true;
        _pinBtn.Click += PinBtn_Click;

        // Theme toggle
        _themeBtn.Text = Theme.IsDark ? "Light" : "Dark";
        _themeBtn.ToolTipText = "Toggle light/dark theme";
        _themeBtn.ForeColor = Theme.TextSecondary;
        _themeBtn.Click += ThemeBtn_Click;

        // Search
        _searchBox.Size = new Size(140, 25);
        _searchBox.BackColor = Theme.SurfaceLight;
        _searchBox.ForeColor = Theme.TextPrimary;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.TextChanged += (_, _) => { _searchFilter = _searchBox.Text; ApplyFilters(); };

        // Filters
        _filterStageBtn.Text = "S";
        _filterStageBtn.ToolTipText = "Stage headers only";
        _filterStageBtn.CheckOnClick = true;
        _filterStageBtn.ForeColor = Theme.TextMuted;
        _filterStageBtn.Click += (_, _) => { _filterStageOnly = _filterStageBtn.Checked; _filterStageBtn.ForeColor = _filterStageOnly ? Theme.Accent : Theme.TextMuted; ApplyFilters(); };

        _filterOptBtn.Text = "OPT";
        _filterOptBtn.ToolTipText = "OPT/BDR only";
        _filterOptBtn.CheckOnClick = true;
        _filterOptBtn.ForeColor = Theme.TextMuted;
        _filterOptBtn.Click += (_, _) => { _filterOptOnly = _filterOptBtn.Checked; _filterOptBtn.ForeColor = _filterOptOnly ? Theme.Purple : Theme.TextMuted; ApplyFilters(); };

        _filterDemBtn.Text = "DEM";
        _filterDemBtn.ToolTipText = "DEM only";
        _filterDemBtn.CheckOnClick = true;
        _filterDemBtn.ForeColor = Theme.TextMuted;
        _filterDemBtn.Click += (_, _) => { _filterDemOnly = _filterDemBtn.Checked; _filterDemBtn.ForeColor = _filterDemOnly ? Theme.Orange : Theme.TextMuted; ApplyFilters(); };

        _filterNxBtn.Text = "NX";
        _filterNxBtn.ToolTipText = "NX only";
        _filterNxBtn.CheckOnClick = true;
        _filterNxBtn.ForeColor = Theme.TextMuted;
        _filterNxBtn.Click += (_, _) => { _filterNxOnly = _filterNxBtn.Checked; _filterNxBtn.ForeColor = _filterNxOnly ? Theme.Cyan : Theme.TextMuted; ApplyFilters(); };

        _toolbar.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripLabel("Source:") { ForeColor = Theme.TextMuted },
            _sourceMode,
            _processSelector,
            _textboxSelector,
            _refreshBtn,
            _replaySpeed,
            new ToolStripSeparator(),
            _connectBtn,
            _disconnectBtn,
            new ToolStripSeparator(),
            _pauseBtn,
            _recordBtn,
            _pinBtn,
            new ToolStripSeparator(),
            new ToolStripLabel("Search:") { ForeColor = Theme.TextMuted },
            _searchBox,
            new ToolStripSeparator(),
            new ToolStripLabel("Filter:") { ForeColor = Theme.TextMuted },
            _filterStageBtn,
            _filterOptBtn,
            _filterDemBtn,
            _filterNxBtn,
            new ToolStripSeparator(),
            _themeBtn
        });

        // Don't add to Controls here - docking order is managed in constructor
    }

    private void BuildStatusBar()
    {
        _statusStrip.BackColor = Theme.SurfaceLight;
        _statusStrip.ForeColor = Theme.TextSecondary;
        _statusStrip.SizingGrip = true;
        _statusStrip.Padding = new Padding(4, 0, 16, 0);

        _statusLabel.Text = "Ready";
        _statusLabel.ForeColor = Theme.TextSecondary;
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _lineCountLabel.Text = "Lines: 0";
        _lineCountLabel.ForeColor = Theme.TextMuted;
        _lineCountLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _lineCountLabel.BorderStyle = Border3DStyle.Etched;

        _snapshotCountLabel.Text = "Snapshots: 0";
        _snapshotCountLabel.ForeColor = Theme.TextMuted;
        _snapshotCountLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _snapshotCountLabel.BorderStyle = Border3DStyle.Etched;

        _alertCountLabel.Text = "Alerts: 0";
        _alertCountLabel.ForeColor = Theme.TextMuted;
        _alertCountLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _alertCountLabel.BorderStyle = Border3DStyle.Etched;

        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _statusLabel,
            _lineCountLabel,
            _snapshotCountLabel,
            _alertCountLabel
        });

        // Don't add to Controls here - docking order is managed in constructor
    }

    private void BuildMainLayout()
    {
        _mainPanel.Dock = DockStyle.Fill;
        _mainPanel.BackColor = Theme.Background;
        _mainPanel.Padding = new Padding(4);

        // Connection badge
        _connectionBadge.Location = new Point(10, 2);
        _connectionBadge.Text = "Disconnected";
        _connectionBadge.BadgeColor = Theme.TextMuted;

        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Orientation = Orientation.Vertical;
        _mainSplit.SplitterDistance = 320;
        _mainSplit.SplitterWidth = 6;
        _mainSplit.BackColor = Theme.Border;

        // Left: Event grid
        var eventPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(0) };

        var eventHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.SurfaceLight };
        var eventLabel = new Label
        {
            Text = "  EVENTS",
            ForeColor = Theme.Accent,
            Font = Theme.FontLabel,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        eventHeader.Controls.Add(eventLabel);
        eventHeader.Controls.Add(_connectionBadge);
        _connectionBadge.Dock = DockStyle.Right;

        _eventGrid.Dock = DockStyle.Fill;
        Theme.StyleDataGridView(_eventGrid);

        _eventGrid.Columns.Add("Time", "Time");
        _eventGrid.Columns.Add("Type", "Type");
        _eventGrid.Columns.Add("Stage", "Stg");
        _eventGrid.Columns.Add("Link", "Lnk");
        _eventGrid.Columns.Add("Summary", "Summary");

        _eventGrid.Columns["Time"].FillWeight = 15;
        _eventGrid.Columns["Type"].FillWeight = 15;
        _eventGrid.Columns["Stage"].FillWeight = 8;
        _eventGrid.Columns["Link"].FillWeight = 8;
        _eventGrid.Columns["Summary"].FillWeight = 54;

        _eventGrid.CellClick += EventGrid_CellClick;

        eventPanel.Controls.Add(_eventGrid);
        eventPanel.Controls.Add(eventHeader);
        _mainSplit.Panel1.Controls.Add(eventPanel);

        // Right: Tab control
        _tabControl.Dock = DockStyle.Fill;
        Theme.StyleTabControl(_tabControl);
        _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabControl.SizeMode = TabSizeMode.Fixed;
        _tabControl.ItemSize = new Size(100, 32);
        _tabControl.DrawItem += TabControl_DrawItem;

        var overviewTab = new TabPage("Overview") { BackColor = Theme.Background };
        overviewTab.Controls.Add(_overviewPanel);

        var stagesTab = new TabPage("Stages") { BackColor = Theme.Background };
        stagesTab.Controls.Add(_stagesPanel);

        var linksTab = new TabPage("Links") { BackColor = Theme.Background };
        linksTab.Controls.Add(_linksPanel);

        var rawTab = new TabPage("Raw") { BackColor = Theme.Background };
        rawTab.Controls.Add(_rawPanel);

        var alertsTab = new TabPage("Alerts") { BackColor = Theme.Background };
        alertsTab.Controls.Add(_alertsPanel);

        _tabControl.TabPages.AddRange(new[] { overviewTab, stagesTab, linksTab, rawTab, alertsTab });
        _mainSplit.Panel2.Controls.Add(_tabControl);

        _mainPanel.Controls.Add(_mainSplit);

        // Don't add to Controls here - docking order is managed in constructor
    }

    private void WireEvents()
    {
        _pipeline.StatusChanged += status =>
        {
            if (InvokeRequired)
                BeginInvoke(() => UpdateConnectionStatus(status));
            else
                UpdateConnectionStatus(status);
        };

        _rawPanel.LineClicked += line =>
        {
            if (line.TrimStart().StartsWith("S "))
            {
                _state.Pin();
                _pinBtn.Checked = true;
                _pinBtn.ForeColor = Theme.Accent;
            }
        };

        _alertsPanel.AlertAcknowledged += id => _state.Alerts.Acknowledge(id);
        _alertsPanel.AlertsClearAll += () => _state.Alerts.ClearAll();
    }

    private void UpdateConnectionStatus(string status)
    {
        _statusLabel.Text = status;
        bool running = status.Contains("Running") || status.Contains("Replaying") || status.Contains("Connected");
        _connectionBadge.Text = running ? "Connected" : status;
        _connectionBadge.BadgeColor = running ? Theme.Green : Theme.TextMuted;
        _connectionBadge.Pulse = running;
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_uiPaused) return;

        try
        {
            // Drain pending events
            int eventsProcessed = 0;
            while (eventsProcessed < 200 && _state.PendingEvents.TryDequeue(out var ev))
            {
                _allEvents.Add(ev);
                if (_allEvents.Count > 10000)
                    _allEvents.RemoveRange(0, 1000);

                if (PassesFilter(ev))
                {
                    AddEventToGrid(ev);
                }
                eventsProcessed++;
            }

            // Trim event grid
            while (_eventGrid.Rows.Count > 2000)
                _eventGrid.Rows.RemoveAt(_eventGrid.Rows.Count - 1);

            // Drain raw lines
            var rawLines = new List<string>();
            int rawCount = 0;
            while (rawCount < 500 && _state.PendingRawLines.TryDequeue(out var rawLine))
            {
                rawLines.Add(rawLine);
                rawCount++;
            }
            if (rawLines.Count > 0)
                _rawPanel.AppendLines(rawLines);

            // Update snapshot-based views
            var snapshot = _state.GetDisplaySnapshot();
            _overviewPanel.UpdateSnapshot(snapshot);
            _linksPanel.UpdateSnapshot(snapshot);

            if (_tabControl.SelectedIndex == 1) // Stages tab
                _stagesPanel.UpdateSnapshots(_state.Snapshots, _state.CurrentSnapshot);

            if (_tabControl.SelectedIndex == 4) // Alerts tab
                _alertsPanel.UpdateAlerts(_state.Alerts);

            // Status bar
            _lineCountLabel.Text = $"Lines: {_state.TotalLinesProcessed:N0}";
            _snapshotCountLabel.Text = $"Snapshots: {_state.TotalSnapshots:N0}";
            _alertCountLabel.Text = $"Alerts: {_state.Alerts.GetActive().Length}";

            var activeAlerts = _state.Alerts.GetActive();
            if (activeAlerts.Length > 0)
            {
                _alertCountLabel.ForeColor = activeAlerts.Any(a => a.Severity == AlertSeverity.Critical)
                    ? Theme.Red : Theme.Yellow;
            }
            else
            {
                _alertCountLabel.ForeColor = Theme.TextMuted;
            }
        }
        catch (Exception)
        {
            // Don't crash the UI refresh
        }
    }

    private void AddEventToGrid(EventRow ev)
    {
        var typeColor = ev.TypeLabel switch
        {
            "StageHeader" => Theme.Accent,
            "StageDetail" or "StageMinLine" => Theme.TextSecondary,
            "NXHeader" => Theme.Cyan,
            "NXBDR" => Theme.Purple,
            "NXOPT" => Theme.Purple,
            "NXContinuation" => Theme.Orange,
            _ => Theme.TextPrimary
        };

        int idx = _eventGrid.Rows.Add(
            ev.Time.ToString("HH:mm:ss.f"),
            ev.TypeLabel,
            ev.Stage?.ToString() ?? "",
            ev.Link?.ToString() ?? "",
            ev.Summary
        );

        // Insert at top (newest first)
        if (_eventGrid.Rows.Count > 1 && idx > 0)
        {
            // Move to first position
            var row = _eventGrid.Rows[idx];
            _eventGrid.Rows.RemoveAt(idx);
            _eventGrid.Rows.Insert(0, row);
            idx = 0;
        }

        _eventGrid.Rows[idx].Cells[1].Style.ForeColor = typeColor;
        _eventGrid.Rows[idx].Tag = ev.SnapshotSeqId;
    }

    private bool PassesFilter(EventRow ev)
    {
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            bool matchSearch = ev.RawLine.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                              ev.Summary.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
            if (!matchSearch) return false;
        }

        bool anyTypeFilter = _filterStageOnly || _filterOptOnly || _filterDemOnly || _filterNxOnly;
        if (!anyTypeFilter) return true;

        if (_filterStageOnly && ev.TypeLabel == "StageHeader") return true;
        if (_filterOptOnly && (ev.TypeLabel == "NXOPT" || ev.TypeLabel == "NXBDR")) return true;
        if (_filterDemOnly && (ev.RawLine.Contains("DEM") || ev.RawLine.Contains("SDEM"))) return true;
        if (_filterNxOnly && ev.TypeLabel.StartsWith("NX")) return true;

        return !anyTypeFilter;
    }

    private void ApplyFilters()
    {
        _eventGrid.Rows.Clear();
        foreach (var ev in _allEvents.AsEnumerable().Reverse().Take(2000))
        {
            if (PassesFilter(ev))
                AddEventToGrid(ev);
        }
    }

    private void EventGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var row = _eventGrid.Rows[e.RowIndex];
        if (row.Tag is int seqId)
        {
            _state.Pin(seqId);
            _pinBtn.Checked = true;
            _pinBtn.ForeColor = Theme.Accent;
        }
    }

    private async void ConnectBtn_Click(object? sender, EventArgs e)
    {
        try
        {
            _state.Reset();
            _eventGrid.Rows.Clear();
            _allEvents.Clear();

            ITextSource source;

            if (_sourceMode.SelectedIndex == 0) // File Replay
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    Title = "Select MOVA log file"
                };

                if (ofd.ShowDialog() != DialogResult.OK) return;

                var replaySource = new FileReplayTextSource(ofd.FileName);
                replaySource.Speed = _replaySpeed.SelectedIndex switch
                {
                    0 => ReplaySpeed.RealTime,
                    1 => ReplaySpeed.Fast50,
                    2 => ReplaySpeed.Fast200,
                    3 => ReplaySpeed.Fast500,
                    4 => ReplaySpeed.Instant,
                    5 => ReplaySpeed.StepByStep,
                    _ => ReplaySpeed.Fast200
                };

                _settings.LastReplayFile = ofd.FileName;
                source = replaySource;
            }
            else // UIA
            {
                if (_processSelector.SelectedIndex < 0 || _processSelector.SelectedIndex >= _processList.Count)
                {
                    MessageBox.Show("Please select a process from the dropdown.", "No Process Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selected = _processList[_processSelector.SelectedIndex];
                var uiaSource = new UiAutomationTextSource();
                uiaSource.TargetProcessName = selected.name;
                uiaSource.TargetWindowTitle = selected.title;
                uiaSource.PollIntervalMs = _settings.PollIntervalMs;
                uiaSource.TargetControlIndex = _textboxSelector.SelectedIndex >= 0 ? _textboxSelector.SelectedIndex : 0;
                source = uiaSource;
            }

            _connectBtn.Enabled = false;
            _disconnectBtn.Enabled = true;
            _sourceMode.Enabled = false;

            await _pipeline.StartAsync(source);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _connectBtn.Enabled = true;
            _disconnectBtn.Enabled = false;
            _sourceMode.Enabled = true;
        }
    }

    private void DisconnectBtn_Click(object? sender, EventArgs e)
    {
        _pipeline.Stop();
        _connectBtn.Enabled = true;
        _disconnectBtn.Enabled = false;
        _sourceMode.Enabled = true;
    }

    private void RecordBtn_Click(object? sender, EventArgs e)
    {
        if (_recordBtn.Checked)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Save recording",
                FileName = $"mova_recording_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _pipeline.StartRecording(sfd.FileName);
                _recordBtn.ForeColor = Theme.Red;
            }
            else
            {
                _recordBtn.Checked = false;
            }
        }
        else
        {
            _pipeline.StopRecording();
            _recordBtn.ForeColor = Theme.TextSecondary;
        }
    }

    private void PinBtn_Click(object? sender, EventArgs e)
    {
        if (_pinBtn.Checked)
        {
            _state.Pin();
            _pinBtn.ForeColor = Theme.Accent;
        }
        else
        {
            _state.Unpin();
            _pinBtn.ForeColor = Theme.TextSecondary;
        }
    }

    private void SourceMode_Changed(object? sender, EventArgs e)
    {
        bool isUia = _sourceMode.SelectedIndex == 1;
        _processSelector.Visible = isUia;
        _textboxSelector.Visible = false;
        _refreshBtn.Visible = isUia;
        _replaySpeed.Visible = !isUia;

        if (isUia)
        {
            RefreshProcessList();
        }
    }

    private void RefreshProcessList()
    {
        _processSelector.Items.Clear();
        _textboxSelector.Items.Clear();
        _textboxSelector.Visible = false;

        _processList = UiAutomationTextSource.GetProcessesWithWindows();
        foreach (var p in _processList)
            _processSelector.Items.Add($"{p.name} - {p.title}");

        if (_processSelector.Items.Count > 0)
            _processSelector.SelectedIndex = 0;
    }

    private void ProcessSelector_Changed(object? sender, EventArgs e)
    {
        PopulateTextboxSelector();
    }

    private void PopulateTextboxSelector()
    {
        _textboxSelector.Items.Clear();

        if (_processSelector.SelectedIndex < 0 || _processSelector.SelectedIndex >= _processList.Count)
        {
            _textboxSelector.Visible = false;
            return;
        }

        var selected = _processList[_processSelector.SelectedIndex];

        try
        {
            var processes = Process.GetProcessesByName(selected.name);
            foreach (var proc in processes)
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero &&
                        (proc.MainWindowTitle?.Contains(selected.title, StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        var editControls = UiAutomationTextSource.GetEditControlsForWindow(proc.MainWindowHandle);
                        foreach (var ctrl in editControls)
                            _textboxSelector.Items.Add($"[{ctrl.index}] {ctrl.className}: {ctrl.textPreview}");
                        break;
                    }
                }
                finally { proc.Dispose(); }
            }
        }
        catch { }

        _textboxSelector.Visible = _textboxSelector.Items.Count > 0;
        if (_textboxSelector.Items.Count > 0)
            _textboxSelector.SelectedIndex = 0;
    }

    private void ThemeBtn_Click(object? sender, EventArgs e)
    {
        Theme.Toggle();
        _themeBtn.Text = Theme.IsDark ? "Light" : "Dark";
        ReapplyTheme();
    }

    private void ReapplyTheme()
    {
        SuspendLayout();

        // Form
        Theme.ApplyToForm(this);

        // Toolbar
        _toolbar.BackColor = Theme.SurfaceLight;
        _toolbar.ForeColor = Theme.TextPrimary;
        _toolbar.Renderer = new ThemedToolStripRenderer();
        foreach (ToolStripItem item in _toolbar.Items)
        {
            switch (item)
            {
                case ToolStripComboBox cb:
                    cb.BackColor = Theme.SurfaceLight;
                    cb.ForeColor = Theme.TextPrimary;
                    break;
                case ToolStripLabel lbl:
                    lbl.ForeColor = Theme.TextMuted;
                    break;
                case ToolStripTextBox tb:
                    tb.BackColor = Theme.SurfaceLight;
                    tb.ForeColor = Theme.TextPrimary;
                    break;
            }
        }

        // Re-color specific buttons
        _connectBtn.ForeColor = Theme.Green;
        _disconnectBtn.ForeColor = Theme.Red;
        _pauseBtn.ForeColor = _uiPaused ? Theme.Yellow : Theme.TextSecondary;
        _recordBtn.ForeColor = _recordBtn.Checked ? Theme.Red : Theme.TextSecondary;
        _pinBtn.ForeColor = _pinBtn.Checked ? Theme.Accent : Theme.TextSecondary;
        _themeBtn.ForeColor = Theme.TextSecondary;
        _refreshBtn.ForeColor = Theme.TextSecondary;
        _filterStageBtn.ForeColor = _filterStageOnly ? Theme.Accent : Theme.TextMuted;
        _filterOptBtn.ForeColor = _filterOptOnly ? Theme.Purple : Theme.TextMuted;
        _filterDemBtn.ForeColor = _filterDemOnly ? Theme.Orange : Theme.TextMuted;
        _filterNxBtn.ForeColor = _filterNxOnly ? Theme.Cyan : Theme.TextMuted;

        // Status strip
        _statusStrip.BackColor = Theme.SurfaceLight;
        _statusStrip.ForeColor = Theme.TextSecondary;
        _statusLabel.ForeColor = Theme.TextSecondary;
        _lineCountLabel.ForeColor = Theme.TextMuted;
        _snapshotCountLabel.ForeColor = Theme.TextMuted;
        _alertCountLabel.ForeColor = Theme.TextMuted;

        // Main panel & split
        _mainPanel.BackColor = Theme.Background;
        _mainSplit.BackColor = Theme.Border;

        // Recursively re-style all child controls
        Theme.ReapplyRecursive(_mainPanel);

        // Tab control custom draw will use current Theme colors automatically
        _tabControl.Invalidate();

        ResumeLayout(true);
        Invalidate(true);
    }

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tab = _tabControl.TabPages[e.Index];
        var bounds = _tabControl.GetTabRect(e.Index);
        bool selected = e.Index == _tabControl.SelectedIndex;

        using var bgBrush = new SolidBrush(selected ? Theme.SurfaceLight : Theme.Surface);
        e.Graphics.FillRectangle(bgBrush, bounds);

        if (selected)
        {
            using var accentPen = new Pen(Theme.Accent, 2);
            e.Graphics.DrawLine(accentPen, bounds.Left + 4, bounds.Bottom - 1, bounds.Right - 4, bounds.Bottom - 1);
        }

        using var textBrush = new SolidBrush(selected ? Theme.Accent : Theme.TextSecondary);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(tab.Text, Theme.FontLabel, textBrush, bounds, sf);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _refreshTimer.Stop();
        _alertTimer.Stop();
        _pipeline.Dispose();

        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.SourceMode = _sourceMode.SelectedIndex == 1 ? "UIA" : "FileReplay";
        _settings.FilterStageHeaders = _filterStageOnly;
        _settings.FilterOPTBDR = _filterOptOnly;
        _settings.FilterDEM = _filterDemOnly;
        _settings.FilterNX = _filterNxOnly;
        _settings.ThemeMode = Theme.IsDark ? "Dark" : "Light";
        _settings.Save();

        base.OnFormClosing(e);
    }
}

// Custom themed renderer for ToolStrip
file sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
{
    public ThemedToolStripRenderer() : base(new ThemedColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Theme.TextPrimary;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is ToolStripButton btn && btn.Checked)
        {
            using var brush = new SolidBrush(Theme.AccentDim);
            e.Graphics.FillRectangle(brush, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
        }
        else if (e.Item.Selected)
        {
            using var brush = new SolidBrush(Theme.SurfaceHover);
            e.Graphics.FillRectangle(brush, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.SurfaceLight);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, 0, e.AffectedBounds.Bottom - 1, e.AffectedBounds.Width, e.AffectedBounds.Bottom - 1);
    }
}

file sealed class ThemedColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.Surface;
    public override Color MenuItemBorder => Theme.Border;
    public override Color MenuItemSelected => Theme.SurfaceHover;
    public override Color MenuBorder => Theme.Border;
    public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => Theme.AccentDim;
    public override Color MenuItemPressedGradientEnd => Theme.AccentDim;
    public override Color ToolStripGradientBegin => Theme.SurfaceLight;
    public override Color ToolStripGradientEnd => Theme.SurfaceLight;
    public override Color ToolStripGradientMiddle => Theme.SurfaceLight;
    public override Color ImageMarginGradientBegin => Theme.Surface;
    public override Color ImageMarginGradientEnd => Theme.Surface;
    public override Color ImageMarginGradientMiddle => Theme.Surface;
    public override Color SeparatorDark => Theme.Border;
    public override Color SeparatorLight => Theme.BorderLight;
}
