using MoVALiveViewer.Models;

namespace MoVALiveViewer.UI.Controls;

public sealed class LinksPanel : Panel
{
    private readonly SplitContainer _split = new();
    private readonly TreeView _tree = new();
    private readonly Panel _detailPanel = new();
    private readonly DataGridView _linkSummaryGrid = new();
    private readonly DataGridView _laGrid = new();
    private readonly DataGridView _lkGrid = new();
    private readonly Label _detailTitle = new();
    private Snapshot? _currentSnapshot;

    public event Action<int>? LinkSelected;

    public LinksPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(4);

        BuildLayout();
    }

    private void BuildLayout()
    {
        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Vertical;
        _split.SplitterDistance = 220;
        _split.SplitterWidth = 4;
        _split.BackColor = Theme.Border;

        // Left tree
        _tree.Dock = DockStyle.Fill;
        _tree.BackColor = Theme.Surface;
        _tree.ForeColor = Theme.TextPrimary;
        _tree.Font = Theme.FontMono;
        _tree.BorderStyle = BorderStyle.None;
        _tree.LineColor = Theme.Border;
        _tree.ShowRootLines = true;
        _tree.ShowPlusMinus = true;
        _tree.FullRowSelect = true;
        _tree.HideSelection = false;
        _tree.AfterSelect += Tree_AfterSelect;
        _split.Panel1.Controls.Add(_tree);

        // Right detail
        _detailPanel.Dock = DockStyle.Fill;
        _detailPanel.BackColor = Theme.Background;

        _detailTitle.Dock = DockStyle.Top;
        _detailTitle.Height = 30;
        _detailTitle.ForeColor = Theme.Accent;
        _detailTitle.Font = Theme.FontSubtitle;
        _detailTitle.TextAlign = ContentAlignment.MiddleLeft;
        _detailTitle.Text = "  Link Summary";
        _detailTitle.BackColor = Theme.SurfaceLight;

        _linkSummaryGrid.Dock = DockStyle.Top;
        _linkSummaryGrid.Height = 180;
        Theme.StyleDataGridView(_linkSummaryGrid);
        _linkSummaryGrid.Columns.Add("Link", "Link");
        _linkSummaryGrid.Columns.Add("ESLI", "ESLI");
        _linkSummaryGrid.Columns.Add("DEM", "DEM");
        _linkSummaryGrid.Columns.Add("SDEM", "SDEM");
        _linkSummaryGrid.Columns.Add("CF", "CF");
        _linkSummaryGrid.Columns.Add("IG", "IG");
        _linkSummaryGrid.Columns.Add("RCX", "RCX");
        _linkSummaryGrid.Columns.Add("RCIN", "RCIN");

        var laLabel = Theme.CreateSectionLabel("  LA ENTRIES");
        laLabel.Dock = DockStyle.Top;
        laLabel.Height = 22;

        _laGrid.Dock = DockStyle.Top;
        _laGrid.Height = 120;
        Theme.StyleDataGridView(_laGrid);
        _laGrid.Columns.Add("Lane", "Lane");
        _laGrid.Columns.Add("V1", "V1");
        _laGrid.Columns.Add("V2", "V2");
        _laGrid.Columns.Add("V3", "V3");

        var lkLabel = Theme.CreateSectionLabel("  LK ENTRIES (from BDR blocks)");
        lkLabel.Dock = DockStyle.Top;
        lkLabel.Height = 22;

        _lkGrid.Dock = DockStyle.Fill;
        Theme.StyleDataGridView(_lkGrid);
        _lkGrid.Columns.Add("BDR", "BDR");
        _lkGrid.Columns.Add("Lane", "Lane");
        _lkGrid.Columns.Add("A", "A");
        _lkGrid.Columns.Add("B", "B");
        _lkGrid.Columns.Add("C", "C");
        _lkGrid.Columns.Add("D", "D");

        _detailPanel.Controls.Add(_lkGrid);
        _detailPanel.Controls.Add(lkLabel);
        _detailPanel.Controls.Add(_laGrid);
        _detailPanel.Controls.Add(laLabel);
        _detailPanel.Controls.Add(_linkSummaryGrid);
        _detailPanel.Controls.Add(_detailTitle);

        _split.Panel2.Controls.Add(_detailPanel);
        Controls.Add(_split);
    }

    public void UpdateSnapshot(Snapshot? snapshot)
    {
        _currentSnapshot = snapshot;

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        if (snapshot == null)
        {
            _tree.EndUpdate();
            ClearDetails();
            return;
        }

        var rootNode = _tree.Nodes.Add($"Snapshot #{snapshot.SequenceId} - Stage {snapshot.Stage}");
        rootNode.ForeColor = Theme.Accent;

        foreach (var kv in snapshot.Links.OrderBy(k => k.Key))
        {
            var linkNode = rootNode.Nodes.Add($"Link {kv.Key}");
            linkNode.Tag = kv.Key;
            linkNode.ForeColor = Theme.TextPrimary;

            if (!string.IsNullOrEmpty(kv.Value.ESLI))
                linkNode.Nodes.Add($"ESLI: {kv.Value.ESLI}").ForeColor = Theme.TextSecondary;

            foreach (var la in kv.Value.LAs)
                linkNode.Nodes.Add($"{la}").ForeColor = Theme.Green;

            foreach (var bdr in kv.Value.BDRs)
            {
                var bdrNode = linkNode.Nodes.Add($"{bdr}");
                bdrNode.ForeColor = Theme.Purple;
                foreach (var lk in bdr.LKEntries)
                    bdrNode.Nodes.Add($"{lk}").ForeColor = Theme.Cyan;
            }

            if (kv.Value.CF.HasValue)
                linkNode.Nodes.Add($"CF: {kv.Value.CF}").ForeColor = Theme.Yellow;

            if (!string.IsNullOrEmpty(kv.Value.DEM))
                linkNode.Nodes.Add($"DEM: {kv.Value.DEM}").ForeColor = Theme.Orange;

            if (!string.IsNullOrEmpty(kv.Value.SDEM))
                linkNode.Nodes.Add($"SDEM: {kv.Value.SDEM}").ForeColor = Theme.Orange;

            if (kv.Value.IG.HasValue)
                linkNode.Nodes.Add($"IG: {kv.Value.IG}").ForeColor = Theme.Cyan;

            if (kv.Value.RCIN != null)
                linkNode.Nodes.Add($"RCIN: {string.Join(" ", kv.Value.RCIN)}").ForeColor = Theme.TextSecondary;
        }

        rootNode.Expand();
        foreach (TreeNode n in rootNode.Nodes) n.Expand();
        _tree.EndUpdate();

        UpdateLinkSummaryGrid(snapshot);
    }

    private void UpdateLinkSummaryGrid(Snapshot snapshot)
    {
        _linkSummaryGrid.Rows.Clear();
        foreach (var kv in snapshot.Links.OrderBy(k => k.Key))
        {
            var ls = kv.Value;
            _linkSummaryGrid.Rows.Add(
                ls.LinkNo,
                ls.ESLI ?? "--",
                ls.DEM ?? "--",
                ls.SDEM ?? "--",
                ls.CF?.ToString() ?? "--",
                ls.IG?.ToString() ?? "--",
                ls.RCX != null ? string.Join(" ", ls.RCX) : "--",
                ls.RCIN != null ? string.Join(" ", ls.RCIN) : "--"
            );
        }
    }

    private void Tree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is int linkNo && _currentSnapshot != null)
        {
            ShowLinkDetail(linkNo);
            LinkSelected?.Invoke(linkNo);
        }
    }

    private void ShowLinkDetail(int linkNo)
    {
        if (_currentSnapshot == null || !_currentSnapshot.Links.TryGetValue(linkNo, out var ls))
            return;

        _detailTitle.Text = $"  Link {linkNo} Detail";

        _laGrid.Rows.Clear();
        foreach (var la in ls.LAs)
            _laGrid.Rows.Add(la.LaneIndex, la.V1, la.V2, la.V3);

        _lkGrid.Rows.Clear();
        int bdrIdx = 0;
        foreach (var bdr in ls.BDRs)
        {
            bdrIdx++;
            foreach (var lk in bdr.LKEntries)
                _lkGrid.Rows.Add($"{bdr.A}/{bdr.B}/{bdr.C}", lk.LaneIndex, lk.A, lk.B, lk.C, lk.D);
        }
    }

    private void ClearDetails()
    {
        _linkSummaryGrid.Rows.Clear();
        _laGrid.Rows.Clear();
        _lkGrid.Rows.Clear();
        _detailTitle.Text = "  Link Summary";
    }
}
