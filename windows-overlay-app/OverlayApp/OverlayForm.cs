using System.Drawing.Drawing2D;

namespace GameOverlay;

public class OverlayForm : Form
{
    private readonly HardwareMonitor _hw = new();
    private readonly RtssReader _rtss = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };

    private readonly Label _fpsVal = MakeValueLabel(Color.FromArgb(34, 211, 238));
    private readonly Label _cpuVal = MakeValueLabel(Color.FromArgb(34, 197, 94));
    private readonly Label _cpuTempVal = MakeValueLabel(Color.FromArgb(245, 158, 11));
    private readonly Label _gpuTempVal = MakeValueLabel(Color.FromArgb(239, 68, 68));
    private readonly Label _ramVal = MakeValueLabel(Color.FromArgb(59, 130, 246));
    private readonly Label _vramVal = MakeValueLabel(Color.FromArgb(168, 85, 247));

    // drag state
    private bool _dragging;
    private Point _dragStart;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(11, 16, 22);
        ForeColor = Color.White;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(16, 70);
        Size = new Size(560, 90);
        DoubleBuffered = true;
        Padding = new Padding(14, 10, 14, 10);

        BuildLayout();

        _timer.Tick += (_, _) => RefreshReadings();
        _timer.Start();

        MouseDown += StartDrag;
        MouseMove += DoDrag;
        MouseUp += (_, _) => _dragging = false;

        var menu = new ContextMenuStrip();
        menu.Items.Add("ปิดโปรแกรม", null, (_, _) => Close());
        ContextMenuStrip = menu;
    }

    private static Label MakeValueLabel(Color color) => new()
    {
        AutoSize = true,
        ForeColor = color,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        Text = "--",
    };

    private void BuildLayout()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
        };

        flow.Controls.Add(Column("FPS", _fpsVal));
        flow.Controls.Add(Column("CPU", _cpuVal, "%"));
        flow.Controls.Add(Column("CPU TEMP", _cpuTempVal, "°C"));
        flow.Controls.Add(Column("GPU TEMP", _gpuTempVal, "°C"));
        flow.Controls.Add(Column("RAM", _ramVal, "GB"));
        flow.Controls.Add(Column("VRAM", _vramVal, "GB"));

        Controls.Add(flow);
        foreach (Control c in flow.Controls) HookDrag(c);
        HookDrag(flow);
    }

    private Control Column(string label, Label valueLabel, string? suffix = null)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 22, 0),
            BackColor = Color.Transparent,
        };
        var lbl = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(139, 149, 165),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Text = label,
        };
        if (suffix != null) valueLabel.Text += suffix; // placeholder until first refresh
        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(valueLabel, 0, 1);
        HookDrag(panel); HookDrag(lbl); HookDrag(valueLabel);
        return panel;
    }

    private void HookDrag(Control c)
    {
        c.MouseDown += StartDrag;
        c.MouseMove += DoDrag;
        c.MouseUp += (_, _) => _dragging = false;
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragStart = Cursor.Position;
    }

    private void DoDrag(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var now = Cursor.Position;
        Location = new Point(
            Location.X + (now.X - _dragStart.X),
            Location.Y + (now.Y - _dragStart.Y));
        _dragStart = now;
    }

    private void RefreshReadings()
    {
        _hw.Refresh();

        _fpsVal.Text = FmtFps(_rtss.GetForegroundFps());
        _cpuVal.Text = Fmt(_hw.CpuLoad, "0") + "%";
        _cpuTempVal.Text = Fmt(_hw.CpuTemp, "0") + "°C";
        _gpuTempVal.Text = Fmt(_hw.GpuTemp, "0") + "°C";
        _ramVal.Text = Fmt(_hw.RamUsedGb, "0.0") + "GB";
        _vramVal.Text = Fmt(_hw.VramUsedGb, "0.0") + "GB";
    }

    private static string Fmt(double? v, string fmt) => v.HasValue ? v.Value.ToString(fmt) : "N/A";
    private static string FmtFps(double? v) => v.HasValue ? v.Value.ToString("0") : "N/A*";

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = RoundedRect(ClientRectangle, 14);
        using var pen = new Pen(Color.FromArgb(34, 211, 238), 1.5f);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var r = radius * 2;
        var path = new GraphicsPath();
        var rect = Rectangle.Inflate(bounds, -1, -1);
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _hw.Dispose(); _timer.Dispose(); }
        base.Dispose(disposing);
    }
}
