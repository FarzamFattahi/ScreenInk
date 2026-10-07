using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenInk
{
    public static class StudioTheme
    {
        public static readonly Color Background = Color.FromArgb(22, 27, 39);
        public static readonly Color Surface = Color.FromArgb(32, 39, 55);
        public static readonly Color Accent = Color.FromArgb(132, 165, 255);
        public static readonly Color Text = Color.FromArgb(232, 237, 249);
        public static readonly Color Muted = Color.FromArgb(151, 164, 190);
        public static GraphicsPath Round(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (d < 1) { path.AddRectangle(rect); return path; }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
        public static Color Mix(Color a, Color b, float t)
        { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        public static void Icon(Graphics g, string name, RectangleF rect, Color color)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(rect.X, rect.Y); g.ScaleTransform(rect.Width / 24, rect.Height / 24);
            using (Pen p = new Pen(color, 1.65f))
            {
                p.StartCap = p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                switch (name)
                {
                    case "Pen":
                        g.DrawPolygon(p, new PointF[] { new PointF(5, 18), new PointF(8, 19), new PointF(20, 7), new PointF(17, 4) });
                        g.DrawLines(p, new PointF[] { new PointF(5, 18), new PointF(4, 21), new PointF(8, 19) }); g.DrawLine(p, 14, 7, 17, 10); break;
                    case "Highlighter":
                        g.DrawPolygon(p, new PointF[] { new PointF(6, 14), new PointF(13, 4), new PointF(20, 9), new PointF(13, 19) });
                        g.DrawLines(p, new PointF[] { new PointF(6, 14), new PointF(3, 19), new PointF(8, 22), new PointF(13, 19) }); g.DrawLine(p, 4, 22, 20, 22); break;
                    case "Eraser":
                        g.DrawPolygon(p, new PointF[] { new PointF(3, 14), new PointF(13, 4), new PointF(21, 12), new PointF(12, 21), new PointF(10, 21) });
                        g.DrawLine(p, 7, 10, 16, 18); g.DrawLine(p, 10, 21, 21, 21); break;
                    case "Laser":
                        g.DrawEllipse(p, 8, 8, 8, 8); g.DrawLine(p, 12, 2, 12, 5); g.DrawLine(p, 12, 19, 12, 22);
                        g.DrawLine(p, 2, 12, 5, 12); g.DrawLine(p, 19, 12, 22, 12); g.DrawLine(p, 4, 4, 6, 6); g.DrawLine(p, 18, 18, 20, 20); break;
                    case "Line": g.DrawLine(p, 4, 20, 20, 4); g.DrawEllipse(p, 2, 18, 4, 4); g.DrawEllipse(p, 18, 2, 4, 4); break;
                    case "Arrow": g.DrawLine(p, 4, 20, 20, 4); g.DrawLines(p, new PointF[] { new PointF(10, 4), new PointF(20, 4), new PointF(20, 14) }); break;
                    case "Rectangle": g.DrawRectangle(p, 3, 5, 18, 14); break;
                    case "Undo": g.DrawLines(p, new PointF[] { new PointF(9, 4), new PointF(4, 9), new PointF(9, 14) }); g.DrawArc(p, 4, 9, 16, 12, 180, -235); break;
                    case "Redo": g.DrawLines(p, new PointF[] { new PointF(15, 4), new PointF(20, 9), new PointF(15, 14) }); g.DrawArc(p, 4, 9, 16, 12, 0, 235); break;
                    case "Clear": g.DrawLine(p, 5, 6, 19, 6); g.DrawLine(p, 9, 3, 15, 3); g.DrawLines(p, new PointF[] { new PointF(7, 6), new PointF(8, 21), new PointF(16, 21), new PointF(17, 6) }); g.DrawLine(p, 11, 10, 11, 17); g.DrawLine(p, 14, 10, 14, 17); break;
                    case "Save": g.DrawRectangle(p, 4, 3, 16, 18); g.DrawRectangle(p, 8, 3, 8, 6); g.DrawRectangle(p, 8, 14, 8, 7); break;
                    case "Copy": g.DrawRectangle(p, 8, 7, 13, 14); g.DrawLines(p, new PointF[] { new PointF(16, 4), new PointF(16, 2), new PointF(3, 2), new PointF(3, 16), new PointF(5, 16) }); break;
                    case "New": g.DrawRectangle(p, 3, 4, 18, 16); g.DrawLine(p, 12, 8, 12, 16); g.DrawLine(p, 8, 12, 16, 12); break;
                    case "Previous": g.DrawLines(p, new PointF[] { new PointF(15, 5), new PointF(8, 12), new PointF(15, 19) }); break;
                    case "Next": g.DrawLines(p, new PointF[] { new PointF(9, 5), new PointF(16, 12), new PointF(9, 19) }); break;
                    case "Done": g.DrawLines(p, new PointF[] { new PointF(4, 12), new PointF(9, 17), new PointF(20, 6) }); break;
                    case "Color": g.DrawEllipse(p, 3, 3, 18, 18); g.DrawEllipse(p, 7, 6, 2, 2); g.DrawEllipse(p, 13, 5, 2, 2); g.DrawEllipse(p, 16, 11, 2, 2); g.DrawEllipse(p, 7, 13, 2, 2); break;
                    case "Dynamic": g.DrawBezier(p, 2, 16, 8, 0, 14, 24, 22, 7); break;
                    case "Collapse": g.DrawLines(p, new PointF[] { new PointF(5, 15), new PointF(12, 8), new PointF(19, 15) }); break;
                    case "Expand": g.DrawLines(p, new PointF[] { new PointF(5, 9), new PointF(12, 16), new PointF(19, 9) }); break;
                    case "Dock": g.DrawRectangle(p, 3, 3, 18, 18); g.DrawLine(p, 7, 17, 17, 17); g.DrawLine(p, 7, 7, 17, 7); break;
                    case "Close": g.DrawLine(p, 6, 6, 18, 18); g.DrawLine(p, 18, 6, 6, 18); break;
                }
            }
            g.Restore(state);
        }
    }

    public sealed class StudioButton : Control
    {
        public string Icon = "";
        public bool ToolTile;
        public Color Accent = StudioTheme.Accent;
        public bool IsSwatch;
        public Color SwatchColor { get; set; }
        public bool Quiet;
        bool selected, hovered, pressed;
        float hoverAmount, selectedAmount;
        readonly Timer animation = new Timer();
        readonly Stopwatch clock = Stopwatch.StartNew();
        double rippleStart = -1000;
        Point rippleOrigin;
        public bool Selected
        {
            get { return selected; }
            set { if (selected == value) return; selected = value; animation.Start(); Invalidate(); }
        }
        public void SettleAnimation() { hoverAmount = hovered ? 1 : 0; selectedAmount = selected ? 1 : 0; rippleStart = -1000; animation.Stop(); Invalidate(); }
        public StudioButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick | ControlStyles.Selectable, true);
            BackColor = StudioTheme.Background; ForeColor = StudioTheme.Text; Font = new Font("Segoe UI", 9, FontStyle.Regular);
            Cursor = Cursors.Hand; AccessibleRole = AccessibleRole.PushButton; TabStop = true;
            animation.Interval = 16;
            animation.Tick += delegate {
                float h = hovered ? 1 : 0, s = selected ? 1 : 0;
                hoverAmount += (h - hoverAmount) * 0.26f; selectedAmount += (s - selectedAmount) * 0.24f;
                if (Math.Abs(h - hoverAmount) < 0.01f && Math.Abs(s - selectedAmount) < 0.01f && clock.Elapsed.TotalMilliseconds - rippleStart > 300)
                { hoverAmount = h; selectedAmount = s; animation.Stop(); }
                Invalidate();
            };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor); InkCanvas.Quality(e.Graphics);
            RectangleF bounds = new RectangleF(1, 1, Width - 3, Height - 3);
            if (IsSwatch)
            {
                float diameter = Math.Min(Width, Height) - 13;
                RectangleF circle = new RectangleF((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                using (Brush color = new SolidBrush(Enabled ? SwatchColor : StudioTheme.Mix(BackColor, SwatchColor, 0.24f))) e.Graphics.FillEllipse(color, circle);
                using (Pen edge = new Pen(Color.FromArgb(80, 180, 193, 218), 1)) e.Graphics.DrawEllipse(edge, circle);
                if (selectedAmount > 0.01)
                {
                    circle.Inflate(3, 3);
                    using (Pen ring = new Pen(Color.FromArgb((int)((Enabled ? 255 : 80) * selectedAmount), StudioTheme.Text), 2)) e.Graphics.DrawEllipse(ring, circle);
                }
                if (hoverAmount > 0.01) using (Pen ring = new Pen(Color.FromArgb((int)(100 * hoverAmount), StudioTheme.Accent), 2)) e.Graphics.DrawEllipse(ring, bounds);
                return;
            }
            Color background = Quiet ? BackColor : StudioTheme.Surface;
            background = StudioTheme.Mix(background, Color.FromArgb(49, 60, 84), hoverAmount * 0.8f);
            background = StudioTheme.Mix(background, Color.FromArgb(50, 67, 112), selectedAmount);
            if (pressed) background = StudioTheme.Mix(background, Accent, 0.18f);
            using (GraphicsPath path = StudioTheme.Round(bounds, ToolTile ? 12 : 9))
            {
                using (Brush bg = new SolidBrush(background)) e.Graphics.FillPath(bg, path);
                using (Pen border = new Pen(StudioTheme.Mix(Color.FromArgb(48, 58, 80), Accent, selectedAmount * 0.8f), 1)) e.Graphics.DrawPath(border, path);
                float ripple = (float)((clock.Elapsed.TotalMilliseconds - rippleStart) / 280);
                if (ripple >= 0 && ripple < 1)
                {
                    GraphicsState saved = e.Graphics.Save(); e.Graphics.SetClip(path);
                    float radius = Math.Max(Width, Height) * ripple;
                    using (Brush glow = new SolidBrush(Color.FromArgb((int)(45 * (1 - ripple)), Accent)))
                        e.Graphics.FillEllipse(glow, rippleOrigin.X - radius, rippleOrigin.Y - radius, radius * 2, radius * 2);
                    e.Graphics.Restore(saved);
                }
                if (Focused) using (Pen focus = new Pen(StudioTheme.Accent, 1)) { focus.DashStyle = DashStyle.Dot; e.Graphics.DrawPath(focus, path); }
            }
            Color text = Enabled ? StudioTheme.Mix(StudioTheme.Text, Accent, selectedAmount * 0.7f) : Color.FromArgb(93, 108, 134);
            if (ToolTile)
            {
                StudioTheme.Icon(e.Graphics, Icon, new RectangleF((Width - 23) / 2, 9, 23, 23), text);
                TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, 37, Width, 20), text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (selectedAmount > 0.01) using (Brush marker = new SolidBrush(Color.FromArgb((int)(255 * selectedAmount), Accent))) e.Graphics.FillEllipse(marker, Width - 11, 7, 4, 4);
            }
            else
            {
                int textOffset = Icon.Length > 0 ? 30 : 0;
                if (Icon.Length > 0) StudioTheme.Icon(e.Graphics, Icon, new RectangleF(Text.Length == 0 ? (Width - 18) / 2 : 10, (Height - 18) / 2, 18, 18), text);
                if (Text.Length > 0) TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(textOffset, 0, Width - textOffset - 4, Height), text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; animation.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; animation.Start(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left && Enabled) { pressed = true; rippleOrigin = e.Location; rippleStart = clock.Elapsed.TotalMilliseconds; Focus(); animation.Start(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e) { if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)) { OnClick(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
        protected override void Dispose(bool disposing) { if (disposing) { animation.Dispose(); Font.Dispose(); } base.Dispose(disposing); }
    }

    public sealed class StudioSlider : Control
    {
        public event EventHandler ValueChanged;
        int value = 5;
        public int Minimum = 1, Maximum = 24;
        public string Title = "THICKNESS", Suffix = " px";
        public int Value
        {
            get { return value; }
            set { int next = Math.Max(Minimum, Math.Min(Maximum, value)); if (this.value == next) return; this.value = next; Invalidate(); if (ValueChanged != null) ValueChanged(this, EventArgs.Empty); }
        }
        bool dragging, hovered;
        public StudioSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Size = new Size(174, 46); BackColor = StudioTheme.Background; Font = new Font("Segoe UI", 8.5f); Cursor = Cursors.Hand; TabStop = true;
            AccessibleName = "Stroke thickness"; AccessibleRole = AccessibleRole.Slider;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor); InkCanvas.Quality(e.Graphics);
            Color label = Enabled ? StudioTheme.Muted : Color.FromArgb(85, 102, 132);
            Color accent = Enabled ? StudioTheme.Accent : Color.FromArgb(72, 85, 111);
            TextRenderer.DrawText(e.Graphics, Title, Font, new Rectangle(7, 0, Width - 95, 18), label, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, Enabled ? Value + Suffix : "--", Font, new Rectangle(Width - 90, 0, 85, 18), Enabled ? StudioTheme.Text : label, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            float x = 10 + (Width - 20) * (Value - Minimum) / (float)Math.Max(1, Maximum - Minimum);
            using (Pen rail = new Pen(Color.FromArgb(51, 63, 86), 4)) { rail.StartCap = rail.EndCap = LineCap.Round; e.Graphics.DrawLine(rail, 10, 32, Width - 10, 32); }
            using (Pen fill = new Pen(accent, 4)) { fill.StartCap = fill.EndCap = LineCap.Round; e.Graphics.DrawLine(fill, 10, 32, x, 32); }
            if (hovered || dragging || Focused) using (Brush glow = new SolidBrush(Color.FromArgb(45, StudioTheme.Accent))) e.Graphics.FillEllipse(glow, x - 11, 21, 22, 22);
            using (Brush dot = new SolidBrush(Enabled ? StudioTheme.Text : label)) e.Graphics.FillEllipse(dot, x - 6, 26, 12, 12);
            using (Brush center = new SolidBrush(accent)) e.Graphics.FillEllipse(center, x - 3, 29, 6, 6);
        }
        void SetFromX(int x) { Value = Minimum + (int)Math.Round((x - 10) * (Maximum - Minimum) / (double)Math.Max(1, Width - 20)); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; Capture = true; Focus(); SetFromX(e.X); } base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (dragging) SetFromX(e.X); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) dragging = false; base.OnMouseCaptureChanged(e); }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Left) { Value--; e.Handled = true; } if (e.KeyCode == Keys.Right) { Value++; e.Handled = true; } base.OnKeyDown(e); }
        protected override void Dispose(bool disposing) { if (disposing) Font.Dispose(); base.Dispose(disposing); }
    }

    public sealed class BrushSettings
    {
        public Color Color;
        public float Width;
        public BrushSettings(Color color, float width) { Color = color; Width = width; }
    }
    public sealed class InkPreferences
    {
        public readonly Dictionary<InkTool, BrushSettings> Brushes = new Dictionary<InkTool, BrushSettings>();
        public InkTool LastTool = InkTool.Pen;
        public bool Dynamic = true;
        public bool ToolbarCollapsed;
        public DockSide DockSide = DockSide.Top;
        public float EraserRadius = 12;
        public double LaserLifetime = 650;
        public int HighlighterOpacity = 82;
        public InkPreferences()
        {
            foreach (InkTool tool in Enum.GetValues(typeof(InkTool)))
                Brushes[tool] = new BrushSettings(tool == InkTool.Highlighter ? Color.FromArgb(255, 211, 67) : Color.FromArgb(245, 100, 119), tool == InkTool.Highlighter ? 9 : 5);
            foreach (InkTool tool in new InkTool[] { InkTool.Line, InkTool.Arrow, InkTool.Rectangle }) Brushes[tool] = new BrushSettings(Color.FromArgb(115, 160, 255), 4);
        }
    }

    public sealed class DragHeader : Control
    {
        public bool CompactDrawing;
        Point origin, formOrigin;
        bool dragging;
        public DragHeader() { Height = 34; Width = 190; BackColor = StudioTheme.Background; Cursor = Cursors.SizeAll; SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor); InkCanvas.Quality(e.Graphics);
            if (CompactDrawing)
            {
                using (Brush grip = new SolidBrush(StudioTheme.Muted)) for (int y = 13; y < 30; y += 6) { e.Graphics.FillEllipse(grip, Width / 2 - 5, y, 2, 2); e.Graphics.FillEllipse(grip, Width / 2 + 3, y, 2, 2); }
                return;
            }
            using (Brush grip = new SolidBrush(Color.FromArgb(77, 94, 128))) for (int y = 10; y <= 22; y += 6) { e.Graphics.FillEllipse(grip, 4, y, 2, 2); e.Graphics.FillEllipse(grip, 10, y, 2, 2); }
            StudioTheme.Icon(e.Graphics, "Pen", new RectangleF(24, 6, 22, 22), StudioTheme.Accent);
            using (Font font = new Font("Segoe UI", 12, FontStyle.Bold)) TextRenderer.DrawText(e.Graphics, "ScreenInk", font, new Rectangle(53, 0, 135, Height), StudioTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left && FindForm() != null) { dragging = true; Capture = true; origin = MousePosition; formOrigin = FindForm().Location; } base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging && FindForm() != null)
            {
                Point next = new Point(formOrigin.X + MousePosition.X - origin.X, formOrigin.Y + MousePosition.Y - origin.Y);
                Rectangle area = Screen.FromPoint(MousePosition).WorkingArea;
                next.X = Math.Max(area.Left, Math.Min(area.Right - FindForm().Width, next.X));
                next.Y = Math.Max(area.Top, Math.Min(area.Bottom - FindForm().Height, next.Y));
                FindForm().Location = next;
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) dragging = false; base.OnMouseCaptureChanged(e); }
    }

    public sealed partial class StudioToolbar : UserControl
    {
        public event EventHandler PreviousRequested, NextRequested, NewRequested, SaveRequested, CopyRequested, DoneRequested;
        public event EventHandler SizeRequested;
        public readonly Dictionary<InkTool, StudioButton> ToolButtons = new Dictionary<InkTool, StudioButton>();
        public readonly StudioSlider Thickness = new StudioSlider();
        public readonly StudioButton Dynamic = new StudioButton();
        public readonly StudioButton Previous = new StudioButton(), Next = new StudioButton(), Undo = new StudioButton(), Redo = new StudioButton();
        public readonly List<StudioButton> Swatches = new List<StudioButton>();
        public readonly Label PageLabel = new Label(), StatusLabel = new Label();
        readonly FlowLayoutPanel header = new FlowLayoutPanel(), tools = new FlowLayoutPanel(), options = new FlowLayoutPanel(), actions = new FlowLayoutPanel();
        readonly ToolTip tips = new ToolTip();
        readonly InkCanvas canvas;
        readonly InkPreferences preferences;
        StudioButton customColor;
        StudioButton collapse;
        StudioButton clearButton;
        public bool Collapsed { get { return preferences.ToolbarCollapsed; } }
        InkTool lastTool;
        bool updating;
        public StudioToolbar(InkCanvas canvas, InkPreferences preferences)
        {
            this.canvas = canvas; this.preferences = preferences; lastTool = preferences.LastTool;
            BackColor = StudioTheme.Background; Padding = new Padding(14, 10, 14, 10);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            tips.BackColor = StudioTheme.Surface; tips.ForeColor = StudioTheme.Text; tips.InitialDelay = 350;
            foreach (FlowLayoutPanel row in new FlowLayoutPanel[] { header, tools, options, actions })
            { row.BackColor = BackColor; row.WrapContents = true; row.Margin = Padding.Empty; Controls.Add(row); }
            header.WrapContents = true;
            DragHeader fullGrip = new DragHeader(); fullGrip.MouseUp += delegate { SnapAfterDrag(); }; header.Controls.Add(fullGrip);
            Configure(Previous, "", "Previous", 32, 34, "Previous drawing / Alt+Left");
            Previous.Click += delegate { Raise(PreviousRequested); }; header.Controls.Add(Previous);
            PageLabel.ForeColor = StudioTheme.Text; PageLabel.Font = new Font("Segoe UI", 9); PageLabel.TextAlign = ContentAlignment.MiddleCenter;
            PageLabel.Size = new Size(116, 34); PageLabel.Margin = new Padding(0, 0, 0, 0); header.Controls.Add(PageLabel);
            Configure(Next, "", "Next", 32, 34, "Next drawing / Alt+Right"); Next.Click += delegate { Raise(NextRequested); }; header.Controls.Add(Next);
            StudioButton fresh = Make("New screen", "New", 120, 34, "Capture the current live desktop"); fresh.Click += delegate { Raise(NewRequested); }; header.Controls.Add(fresh);
            StudioButton done = Make("Done  F8", "Done", 110, 34, "Return to the live desktop / F8 or Esc"); done.Accent = Color.FromArgb(107, 218, 181); done.Selected = true; header.Controls.Add(done); done.Click += delegate { Raise(DoneRequested); };
            collapse = Make("", "Collapse", 34, 34, "Compact toolbar / Ctrl+Tab"); collapse.Click += delegate { ToggleCompact(); }; header.Controls.Add(collapse);
            foreach (InkTool tool in Enum.GetValues(typeof(InkTool)))
            {
                InkTool captured = tool;
                StudioButton button = Make(tool == InkTool.Arrow ? "Arrow pen" : tool.ToString(), tool.ToString(), tool == InkTool.Highlighter ? 100 : 84, 64, ToolHint(tool));
                button.ToolTile = true; button.Accent = tool == InkTool.Eraser || tool == InkTool.Laser ? Color.FromArgb(255, 138, 168) : StudioTheme.Accent;
                button.Click += delegate { ClickTool(captured); }; tools.Controls.Add(button); ToolButtons[tool] = button;
            }
            foreach (string hex in new string[] { "#F56477", "#FFD343", "#59D6B0", "#73A0FF", "#BD92FF", "#FFFFFF", "#192133" })
            {
                StudioButton swatch = Make("", "", 34, 38, "Ink color " + hex); swatch.IsSwatch = true; swatch.SwatchColor = ColorTranslator.FromHtml(hex);
                swatch.Click += delegate { canvas.InkColor = swatch.SwatchColor; canvas.RefreshSettings(); }; options.Controls.Add(swatch); Swatches.Add(swatch);
            }
            StudioButton custom = Make("", "Color", 34, 38, "Choose a custom ink color");
            customColor = custom;
            custom.Click += delegate {
                using (ColorDialog dialog = new ColorDialog()) { dialog.Color = canvas.InkColor; dialog.FullOpen = true; if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { canvas.InkColor = dialog.Color; canvas.RefreshSettings(); } }
            }; options.Controls.Add(custom);
            Thickness.Margin = new Padding(12, 0, 12, 0); options.Controls.Add(Thickness);
            Thickness.ValueChanged += delegate { if (!updating) { canvas.BaseWidth = Thickness.Value; SaveBrush(canvas.Tool); canvas.RefreshSettings(); } };
            Configure(Dynamic, "Dynamic ink", "Dynamic", 136, 38, "More expressive slow/thick and fast/fine pen ink");
            Dynamic.Click += delegate { canvas.DynamicWidth = !canvas.DynamicWidth; preferences.Dynamic = canvas.DynamicWidth; canvas.RefreshSettings(); }; options.Controls.Add(Dynamic);
            Configure(Undo, "Undo", "Undo", 88, 36, "Undo / Ctrl+Z"); Undo.Click += delegate { canvas.UndoInk(); }; actions.Controls.Add(Undo);
            Configure(Redo, "Redo", "Redo", 88, 36, "Redo / Ctrl+Y"); Redo.Click += delegate { canvas.RedoInk(); }; actions.Controls.Add(Redo);
            StudioButton clear = Make("Clear", "Clear", 88, 36, "Clear ink / undo restores it"); clearButton = clear; clear.Click += delegate { canvas.ClearInk(); }; actions.Controls.Add(clear);
            StudioButton copy = Make("Copy", "Copy", 88, 36, "Copy annotated image / Ctrl+C"); copy.Click += delegate { Raise(CopyRequested); }; actions.Controls.Add(copy);
            StudioButton save = Make("Save PNG", "Save", 112, 36, "Save annotated image / Ctrl+S"); save.Click += delegate { Raise(SaveRequested); }; actions.Controls.Add(save);
            StatusLabel.ForeColor = StudioTheme.Muted; StatusLabel.Font = new Font("Segoe UI", 8.5f); StatusLabel.AutoSize = false; StatusLabel.AutoEllipsis = true; Controls.Add(StatusLabel);
            canvas.InkColor = preferences.Brushes[lastTool].Color; canvas.BaseWidth = preferences.Brushes[lastTool].Width;
            canvas.DynamicWidth = preferences.Dynamic; canvas.Tool = lastTool;
            canvas.EraserRadius = preferences.EraserRadius; canvas.LaserLifetime = preferences.LaserLifetime; canvas.HighlighterOpacity = preferences.HighlighterOpacity;
            canvas.StateChanged += CanvasChanged;
            InitializeDock();
            ApplyCompact();
            RefreshState();
        }
        static string ToolHint(InkTool tool)
        {
            switch (tool) {
                case InkTool.Pen: return "Smooth expressive pen / P";
                case InkTool.Highlighter: return "Translucent marker / H";
                case InkTool.Eraser: return "Preview and erase a whole stroke / E";
                case InkTool.Laser: return "Temporary glowing pointer / L";
                case InkTool.Line: return "Straight line / I / Shift snaps to 45 degrees";
                case InkTool.Arrow: return "Freehand arrow pen / A / Release to add an arrowhead";
                default: return "Rectangle / R / Shift makes a square";
            }
        }
        void Raise(EventHandler handler) { if (handler != null) handler(this, EventArgs.Empty); }
        StudioButton Make(string text, string icon, int width, int height, string hint)
        { StudioButton button = new StudioButton(); Configure(button, text, icon, width, height, hint); return button; }
        void Configure(StudioButton button, string text, string icon, int width, int height, string hint)
        { button.Text = text; button.Icon = icon; button.Size = new Size(width, height); button.Margin = new Padding(3, 2, 3, 2); button.AccessibleName = hint; tips.SetToolTip(button, hint); }
        void SaveBrush(InkTool tool) { preferences.Brushes[tool].Color = canvas.InkColor; preferences.Brushes[tool].Width = canvas.BaseWidth; }
        void CanvasChanged(object sender, EventArgs args)
        {
            if (canvas.Tool != lastTool)
            {
                CloseToolOptions();
                SaveBrush(lastTool); lastTool = canvas.Tool;
                canvas.InkColor = preferences.Brushes[lastTool].Color; canvas.BaseWidth = preferences.Brushes[lastTool].Width;
            }
            preferences.LastTool = lastTool; RefreshState();
        }
        public void RefreshState()
        {
            updating = true;
            foreach (KeyValuePair<InkTool, StudioButton> entry in ToolButtons) entry.Value.Selected = entry.Key == canvas.Tool;
            RefreshDockState();
            bool drawingTool = canvas.Tool != InkTool.Eraser;
            bool presetColor = false;
            foreach (StudioButton swatch in Swatches)
            {
                swatch.Selected = swatch.SwatchColor.ToArgb() == canvas.InkColor.ToArgb();
                swatch.Enabled = drawingTool; if (swatch.Selected) presetColor = true;
            }
            customColor.Selected = !presetColor && drawingTool; customColor.Enabled = drawingTool; customColor.Accent = canvas.InkColor;
            Thickness.Value = (int)canvas.BaseWidth; Dynamic.Selected = canvas.DynamicWidth;
            Dynamic.Enabled = canvas.Tool == InkTool.Pen || canvas.Tool == InkTool.Arrow;
            Thickness.Enabled = drawingTool && canvas.Tool != InkTool.Laser;
            Undo.Enabled = canvas.Page != null && canvas.Page.Undo.Count > 0; Redo.Enabled = canvas.Page != null && canvas.Page.Redo.Count > 0;
            clearButton.Enabled = canvas.Page != null && canvas.Page.Strokes.Count > 0;
            string description = ToolHint(canvas.Tool);
            if (canvas.Tool == InkTool.Pen && canvas.DynamicWidth) description = "PEN / expressive ink: slower = fuller, faster = finer";
            if (canvas.Tool == InkTool.Eraser) description = "ERASER / pink preview marks the complete stroke that will disappear";
            if (canvas.Tool == InkTool.Laser) description = "LASER / glowing trail fades away; your drawing stays clean";
            if (canvas.Tool == InkTool.Arrow) description = "ARROW PEN / draw a smooth curve; release to finish its arrow tip";
            StatusLabel.Text = description + "    |    Right-drag: quick erase    |    [ ]: thickness";
            tips.SetToolTip(StatusLabel, StatusLabel.Text);
            updating = false;
        }
        public void SetHistory(int index, int count)
        { PageLabel.Text = "Screen " + (index + 1) + " / " + count; Previous.Enabled = index > 0; Next.Enabled = index < count - 1; RefreshState(); }
        public void SettleAnimations()
        {
            foreach (StudioButton button in ToolButtons.Values) button.SettleAnimation();
            foreach (StudioButton button in CompactTools.Values) button.SettleAnimation();
            foreach (StudioButton button in Swatches) button.SettleAnimation();
            Dynamic.SettleAnimation();
            foreach (Control c in header.Controls) { StudioButton b = c as StudioButton; if (b != null) b.SettleAnimation(); }
        }
        void ApplyCompact()
        {
            ApplyDockMode();
            collapse.Icon = Collapsed ? "Expand" : "Collapse"; collapse.Invalidate();
        }
        public void ToggleCompact()
        {
            CloseToolOptions(); preferences.ToolbarCollapsed = !preferences.ToolbarCollapsed; ApplyCompact();
            PerformLayout(); Raise(SizeRequested);
        }
        public int PreferredHeight(int width)
        {
            int inner = Math.Max(1, width - 28);
            if (Collapsed) return CompactSize(new Size(width, 1000)).Height;
            return 80 + RowHeight(header, inner, 38) + RowHeight(tools, inner, 68) + RowHeight(options, inner, 48) + RowHeight(actions, inner, 42);
        }
        static int RowHeight(FlowLayoutPanel panel, int available, int unit)
        {
            int total = 0, rows = 1;
            foreach (Control c in panel.Controls)
            {
                int w = c.Width + c.Margin.Horizontal;
                if (total > 0 && total + w > available) { rows++; total = 0; }
                total += w;
            }
            return rows * unit;
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e); if (tools == null || options == null || actions == null) return;
            if (Collapsed) { LayoutCompactDock(); return; }
            int w = Math.Max(1, Width - 28), y = 12;
            int headerHeight = RowHeight(header, w, 38);
            header.SetBounds(14, y, w, headerHeight); y += headerHeight + 10;
            if (Collapsed) return;
            int h = RowHeight(tools, w, 68); tools.SetBounds(14, y, w, h); y += h + 9;
            h = RowHeight(options, w, 48); options.SetBounds(14, y, w, h); y += h + 7;
            h = RowHeight(actions, w, 42); actions.SetBounds(14, y, w, h); y += h + 7;
            StatusLabel.SetBounds(20, y, Math.Max(1, Width - 40), 25);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); InkCanvas.Quality(e.Graphics);
            using (GraphicsPath outline = StudioTheme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 16))
            using (Pen edge = new Pen(Color.FromArgb(59, 74, 101), 1)) e.Graphics.DrawPath(edge, outline);
            if (!Collapsed) using (Pen divider = new Pen(Color.FromArgb(44, 55, 76), 1)) { e.Graphics.DrawLine(divider, 18, header.Bottom + 5, Width - 18, header.Bottom + 5); e.Graphics.DrawLine(divider, 18, options.Bottom + 2, Width - 18, options.Bottom + 2); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { DisposeDock(); SaveBrush(canvas.Tool); preferences.Dynamic = canvas.DynamicWidth; preferences.LastTool = canvas.Tool; preferences.EraserRadius = canvas.EraserRadius; preferences.LaserLifetime = canvas.LaserLifetime; preferences.HighlighterOpacity = canvas.HighlighterOpacity; canvas.StateChanged -= CanvasChanged; tips.Dispose(); PageLabel.Font.Dispose(); StatusLabel.Font.Dispose(); }
            base.Dispose(disposing);
        }
    }

    public static class StudioTests
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Click(Control control)
        { typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(control, new object[] { EventArgs.Empty }); }
        static void Mouse(InkCanvas canvas, string name, MouseEventArgs args)
        { typeof(InkCanvas).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(canvas, new object[] { args }); }
        static void Key(InkForm form, Keys key)
        { typeof(InkForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(form, new object[] { new Message(), key }); }
        static void CheckBounds(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (!child.Visible) continue;
                Check(child.Left >= 0 && child.Top >= 0 && child.Right <= control.ClientSize.Width && child.Bottom <= control.ClientSize.Height,
                    "Control is clipped: " + child.GetType().Name + " / " + child.Text + " / " + child.Bounds + " in " + control.ClientSize);
                CheckBounds(child);
            }
        }
        public static string Run(string artifacts)
        {
            using (Icon icon = Native.CreateAppIcon()) Check(icon.Width == 32, "Branded tray icon must render correctly.");
            if (!String.IsNullOrEmpty(artifacts)) System.IO.Directory.CreateDirectory(artifacts);
            using (DrawingPage page = new DrawingPage(new Bitmap(800, 500), new Rectangle(0, 0, 800, 500)))
            using (InkCanvas canvas = new InkCanvas())
            using (InkForm form = new InkForm())
            {
                using (Graphics graphics = Graphics.FromImage(page.Background)) graphics.Clear(Color.White);
                canvas.Size = new Size(800, 500); canvas.SetPage(page); form.Canvas = canvas;
                InkPreferences preferences = new InkPreferences();
                using (StudioToolbar studio = new StudioToolbar(canvas, preferences))
                {
                    studio.SetHistory(0, 3);
                    canvas.InkColor = Color.Blue; studio.Thickness.Value = 7;
                    Click(studio.ToolButtons[InkTool.Highlighter]);
                    Check(canvas.Tool == InkTool.Highlighter && canvas.BaseWidth == 9, "Highlighter must use its own preset.");
                    Click(studio.ToolButtons[InkTool.Pen]);
                    Check(canvas.InkColor == Color.Blue && canvas.BaseWidth == 7, "Pen must remember color and width.");
                    Key(form, Keys.E); Check(studio.ToolButtons[InkTool.Eraser].Selected, "Keyboard tool switch must update selection.");
                    Key(form, Keys.P); Key(form, Keys.OemCloseBrackets); Check(canvas.BaseWidth == 8 && studio.Thickness.Value == 8, "Thickness shortcut must update the slider.");
                    foreach (int width in new int[] { 820, 640, 480 })
                    {
                        studio.Size = new Size(width, studio.PreferredHeight(width)); studio.CreateControl(); studio.PerformLayout();
                        foreach (Control c in studio.Controls) c.PerformLayout();
                        CheckBounds(studio);
                    }
                    studio.Size = new Size(820, studio.PreferredHeight(820)); studio.PerformLayout();
                    studio.ToggleCompact(); studio.Size = new Size(820, studio.PreferredHeight(820)); studio.PerformLayout();
                    Check(studio.Height < 90 && studio.Collapsed, "Compact toolbar should leave more screen space."); CheckBounds(studio);
                    if (!String.IsNullOrEmpty(artifacts)) using (Bitmap image = new Bitmap(studio.Width, studio.Height))
                    { studio.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height)); image.Save(System.IO.Path.Combine(artifacts, "studio-compact.png"), System.Drawing.Imaging.ImageFormat.Png); }
                    studio.ToggleCompact(); studio.Size = new Size(820, studio.PreferredHeight(820)); studio.PerformLayout();
                    foreach (InkTool tool in Enum.GetValues(typeof(InkTool)))
                    {
                        Click(studio.ToolButtons[tool]);
                        Check(canvas.Tool == tool, "Tool button did not select " + tool);
                        foreach (KeyValuePair<InkTool, StudioButton> entry in studio.ToolButtons)
                            Check(entry.Value.Selected == (entry.Key == tool), "Exactly one tool must be selected.");
                        studio.SettleAnimations();
                        if (!String.IsNullOrEmpty(artifacts)) using (Bitmap image = new Bitmap(studio.Width, studio.Height))
                        {
                            studio.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height));
                            image.Save(System.IO.Path.Combine(artifacts, "studio-" + tool.ToString().ToLowerInvariant() + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    canvas.Tool = InkTool.Laser;
                    Mouse(canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 40, 60, 0));
                    Mouse(canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 40, 60, 0));
                    Mouse(canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 160, 90, 0));
                    Mouse(canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 160, 90, 0));
                    Check(page.Strokes.Count == 0 && page.Undo.Count == 0, "Laser must not alter the drawing or undo history.");
                    using (Bitmap export = page.Export()) Check(export.GetPixel(160, 90).ToArgb() == Color.White.ToArgb(), "Laser glow must stay out of the exported image.");
                    canvas.Tool = InkTool.Arrow;
                    Mouse(canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 30, 100, 0));
                    Mouse(canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 160, 100, 0));
                    Check(page.Strokes.Count == 0, "Arrow pen should remain a live stroke until release.");
                    Mouse(canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 160, 100, 0));
                    Check(page.Strokes.Count == 1 && page.Strokes[0].HasArrowhead && page.Strokes[0].Hit(new PointF(100, 100), 3), "Release must finish the arrow as one erasable stroke.");
                    Mouse(canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 100, 100, 0));
                    Mouse(canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Right, 1, 100, 100, 0));
                    Check(page.Strokes.Count == 0 && canvas.Tool == InkTool.Arrow, "Right-click erase must preserve the selected tool.");
                    canvas.UndoInk(); Check(page.Strokes.Count == 1, "Quick erase must undo.");
                    using (Stroke shape = new Stroke(InkTool.Rectangle, Color.Blue, 4, new PointF(20, 20), 0))
                    {
                        shape.SetShapeEndpoint(new PointF(90, 60), true);
                        Check(Math.Abs(shape.Samples[1].Position.X - 20) == Math.Abs(shape.Samples[1].Position.Y - 20), "Shift rectangle must become a square.");
                        Check(shape.Hit(new PointF(20, 40), 2) && !shape.Hit(new PointF(50, 50), 2), "Rectangle eraser must target its border.");
                    }
                    using (Stroke shape = new Stroke(InkTool.Line, Color.Blue, 4, new PointF(0, 0), 0))
                    {
                        shape.SetShapeEndpoint(new PointF(80, 45), true);
                        Check(Math.Abs(shape.Samples[1].Position.X - shape.Samples[1].Position.Y) < 0.01f, "Shift line must snap to 45 degrees.");
                    }
                }
                Check(preferences.LastTool == InkTool.Arrow, "Selected tool must persist when the toolbar closes.");
            }
            using (StudioButton button = new StudioButton())
            {
                button.Size = new Size(90, 64); button.ToolTile = true; button.Icon = "Pen"; button.Text = "Pen"; button.CreateControl();
                using (Bitmap before = new Bitmap(90, 64))
                using (Bitmap after = new Bitmap(90, 64))
                {
                    button.DrawToBitmap(before, new Rectangle(0, 0, 90, 64)); button.Selected = true;
                    Stopwatch elapsed = Stopwatch.StartNew();
                    while (elapsed.ElapsedMilliseconds < 220) { Application.DoEvents(); System.Threading.Thread.Sleep(5); }
                    button.DrawToBitmap(after, new Rectangle(0, 0, 90, 64));
                    Check(before.GetPixel(40, 5) != after.GetPixel(40, 5), "Selection animation must visibly update the button.");
                }
            }
            if (!String.IsNullOrEmpty(artifacts))
            {
                CoreTests.RenderPreview(System.IO.Path.Combine(artifacts, "ink-preview.png"));
                RenderTeachingPreview(System.IO.Path.Combine(artifacts, "teaching-preview.png"));
            }
            return "PASS: animated selection, all tool buttons, per-tool brushes, 480/640/820px layouts, shortcuts, laser isolation, shapes/snapping, quick-erase undo.";
        }
        public static void RenderTeachingPreview(string file)
        {
            using (DrawingPage page = new DrawingPage(new Bitmap(1000, 560), new Rectangle(0, 0, 1000, 560)))
            {
                using (Graphics g = Graphics.FromImage(page.Background))
                using (Font heading = new Font("Segoe UI", 23, FontStyle.Bold))
                using (Font body = new Font("Segoe UI", 13))
                {
                    g.Clear(Color.FromArgb(245, 247, 252));
                    g.DrawString("Make your explanation clear", heading, Brushes.MidnightBlue, 42, 28);
                    g.DrawString("Expressive pen, clean diagrams, precise emphasis.", body, Brushes.SlateGray, 44, 83);
                    g.DrawString("Capture", body, Brushes.MidnightBlue, 94, 345); g.DrawString("Explain", body, Brushes.MidnightBlue, 444, 345); g.DrawString("Share", body, Brushes.MidnightBlue, 800, 345);
                }
                Stroke pen = new Stroke(InkTool.Pen, Color.FromArgb(115, 160, 255), 10, new PointF(50, 190), 0);
                double time = 0;
                for (int i = 1; i <= 275; i++) { time += i < 100 || i > 200 ? 30 : 3; pen.Append(new PointF(50 + i * 3.2f, 185 + (float)Math.Sin(i * 0.06) * 30), time, true); }
                page.Strokes.Add(pen);
                foreach (float x in new float[] { 65, 415, 765 })
                { Stroke box = new Stroke(InkTool.Rectangle, Color.FromArgb(115, 160, 255), 3, new PointF(x, 315), 0); box.SetShapeEndpoint(new PointF(x + 160, 400), false); page.Strokes.Add(box); }
                foreach (float x in new float[] { 250, 600 })
                {
                    Stroke arrow = new Stroke(InkTool.Arrow, Color.FromArgb(245, 100, 119), 5, new PointF(x, 356), 0);
                    for (int i = 1; i <= 35; i++) arrow.Append(new PointF(x + i * 4, 356 - (float)Math.Sin(i * Math.PI / 35) * 25), i * 12, true);
                    arrow.FinishArrow(); page.Strokes.Add(arrow);
                }
                using (Bitmap image = page.Export()) image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
