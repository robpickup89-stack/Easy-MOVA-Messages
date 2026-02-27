namespace MoVALiveViewer.UI.Controls;

public sealed class RawViewPanel : Panel
{
    private readonly RichTextBox _rawText = new();
    private readonly Panel _toolbar = new();
    private readonly CheckBox _autoScroll = new();
    private readonly Label _lineCount = new();
    private int _totalLines;
    private const int MaxLines = 5000;

    public event Action<string>? LineClicked;

    public RawViewPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;

        BuildLayout();
    }

    private void BuildLayout()
    {
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 30;
        _toolbar.BackColor = Theme.SurfaceLight;
        _toolbar.Padding = new Padding(8, 4, 8, 4);

        _autoScroll.Text = "Auto-scroll";
        _autoScroll.Checked = true;
        _autoScroll.ForeColor = Theme.TextSecondary;
        _autoScroll.Font = Theme.FontSmall;
        _autoScroll.Dock = DockStyle.Left;
        _autoScroll.Width = 100;
        _autoScroll.BackColor = Color.Transparent;

        _lineCount.Text = "0 lines";
        _lineCount.ForeColor = Theme.TextMuted;
        _lineCount.Font = Theme.FontSmall;
        _lineCount.Dock = DockStyle.Right;
        _lineCount.TextAlign = ContentAlignment.MiddleRight;
        _lineCount.Width = 120;

        var clearBtn = new Button { Text = "Clear", Width = 60, Dock = DockStyle.Right };
        Theme.StyleButton(clearBtn);
        clearBtn.Click += (_, _) => ClearText();

        _toolbar.Controls.Add(_lineCount);
        _toolbar.Controls.Add(clearBtn);
        _toolbar.Controls.Add(_autoScroll);

        _rawText.Dock = DockStyle.Fill;
        _rawText.BackColor = Theme.Surface;
        _rawText.ForeColor = Theme.TextPrimary;
        _rawText.Font = Theme.FontMono;
        _rawText.ReadOnly = true;
        _rawText.WordWrap = false;
        _rawText.ScrollBars = RichTextBoxScrollBars.Both;
        _rawText.BorderStyle = BorderStyle.None;
        _rawText.DetectUrls = false;
        _rawText.MouseClick += RawText_MouseClick;

        Controls.Add(_rawText);
        Controls.Add(_toolbar);
    }

    public void AppendLines(IEnumerable<string> lines)
    {
        if (!lines.Any()) return;

        _rawText.SuspendLayout();

        foreach (var line in lines)
        {
            var color = GetLineColor(line);
            var isBold = line.TrimStart().StartsWith("S ");

            _rawText.SelectionStart = _rawText.TextLength;
            _rawText.SelectionLength = 0;
            _rawText.SelectionColor = color;
            _rawText.SelectionFont = isBold
                ? new Font(_rawText.Font, FontStyle.Bold)
                : _rawText.Font;
            _rawText.AppendText(line + "\n");
            _totalLines++;
        }

        // Trim if too long
        if (_totalLines > MaxLines)
        {
            int removeUpTo = _rawText.GetFirstCharIndexFromLine(Math.Min(_totalLines - MaxLines, _rawText.Lines.Length - 1));
            if (removeUpTo > 0)
            {
                _rawText.SelectionStart = 0;
                _rawText.SelectionLength = removeUpTo;
                _rawText.SelectedText = "";
                _totalLines = _rawText.Lines.Length;
            }
        }

        _rawText.ResumeLayout();

        if (_autoScroll.Checked)
        {
            _rawText.SelectionStart = _rawText.TextLength;
            _rawText.ScrollToCaret();
        }

        _lineCount.Text = $"{_totalLines:N0} lines";
    }

    private void ClearText()
    {
        _rawText.Clear();
        _totalLines = 0;
        _lineCount.Text = "0 lines";
    }

    private void RawText_MouseClick(object? sender, MouseEventArgs e)
    {
        int charIdx = _rawText.GetCharIndexFromPosition(e.Location);
        int lineIdx = _rawText.GetLineFromCharIndex(charIdx);
        if (lineIdx >= 0 && lineIdx < _rawText.Lines.Length)
        {
            LineClicked?.Invoke(_rawText.Lines[lineIdx]);
        }
    }

    private static Color GetLineColor(string line)
    {
        var trimmed = line.TrimStart();

        if (trimmed.StartsWith("S ")) return Theme.Accent;
        if (trimmed.StartsWith("SMCYC")) return Theme.TextSecondary;
        if (trimmed.Contains("OPT") || trimmed.Contains("BDR")) return Theme.Purple;
        if (trimmed.Contains("DEM") || trimmed.Contains("SDEM") || trimmed.Contains("IG:")) return Theme.Orange;
        if (trimmed.Contains("NX")) return Theme.Cyan;
        if (trimmed.Contains("ERROR") || trimmed.Contains("FAIL")) return Theme.Red;

        return Theme.TextPrimary;
    }
}
