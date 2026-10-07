using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenInk
{
    public enum InkTool { Pen, Highlighter, Eraser, Laser, Line, Arrow, Rectangle }

    public sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public event EventHandler Pressed;
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr window, int id);
        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
            if (!RegisterHotKey(Handle, 0x51A7, 0x4000, 0x77))
            {
                DestroyHandle();
                throw new InvalidOperationException("F8 is already in use. Exit the other ScreenInk instance or use the tray menu.");
            }
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x312 && message.WParam.ToInt32() == 0x51A7 && Pressed != null)
                Pressed(this, EventArgs.Empty);
            base.WndProc(ref message);
        }
        public void Dispose() { if (Handle != IntPtr.Zero) { UnregisterHotKey(Handle, 0x51A7); DestroyHandle(); } }
    }

    public static class Native
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        public static void EnableDpi() { try { SetProcessDPIAware(); } catch { } }
        public static Icon CreateAppIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (GraphicsPath shape = StudioTheme.Round(new RectangleF(1, 1, 30, 30), 8))
                using (Brush surface = new SolidBrush(StudioTheme.Background))
                {
                    InkCanvas.Quality(graphics); graphics.FillPath(surface, shape);
                    StudioTheme.Icon(graphics, "Pen", new RectangleF(5, 4, 22, 22), StudioTheme.Accent);
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); } finally { DestroyIcon(handle); }
            }
        }
        public static void Round(Control control, int radius)
        {
            if (control.Width < radius * 2 || control.Height < radius * 2) return;
            using (GraphicsPath path = new GraphicsPath())
            {
                int d = radius * 2, w = control.Width, h = control.Height;
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(w - d, 0, d, d, 270, 90);
                path.AddArc(w - d, h - d, d, d, 0, 90);
                path.AddArc(0, h - d, d, d, 90, 90);
                path.CloseFigure();
                Region old = control.Region;
                control.Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }
    }

    public struct InkSample
    {
        public PointF Position;
        public float Width;
        public InkSample(PointF position, float width) { Position = position; Width = width; }
    }

    // A stroke owns one filled outline: highlighter opacity stays uniform even
    // across its overlapping segments, and live/final rendering are identical.
    public sealed class Stroke : IDisposable
    {
        public readonly InkTool Tool;
        public readonly Color Color;
        public readonly float BaseWidth;
        public readonly int Opacity;
        public readonly List<InkSample> Samples = new List<InkSample>();
        public GraphicsPath Outline { get; private set; }
        public bool HasArrowhead { get; private set; }
        public RectangleF Bounds { get { return Outline == null ? RectangleF.Empty : Outline.GetBounds(); } }
        PointF lastRaw;
        double lastTime;
        float filteredSpeed;
        GraphicsPath hitSource;
        PointF[] hitPoints;
        byte[] hitTypes;
        public Stroke(InkTool tool, Color color, float width, PointF start, double milliseconds)
            : this(tool, color, width, start, milliseconds, tool == InkTool.Highlighter ? 82 : 255) { }
        public Stroke(InkTool tool, Color color, float width, PointF start, double milliseconds, int opacity)
        {
            Tool = tool; Color = color; BaseWidth = width; Opacity = Math.Max(1, Math.Min(255, opacity));
            lastRaw = start; lastTime = milliseconds;
            Samples.Add(new InkSample(start, tool == InkTool.Highlighter ? width * 3.2f : width));
            RebuildOutline();
        }
        public bool Append(PointF raw, double milliseconds, bool dynamicWidth)
        {
            float dx = raw.X - lastRaw.X, dy = raw.Y - lastRaw.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance < 0.65f) return false;
            double dt = Math.Max(1, Math.Min(80, milliseconds - lastTime));
            float velocity = distance / (float)dt;
            float speedBlend = 1 - (float)Math.Exp(-dt / 35);
            filteredSpeed += (velocity - filteredSpeed) * speedBlend;
            InkSample previous = Samples[Samples.Count - 1];
            // More stabilization at handwriting speeds; less lag during fast sweeps.
            float alpha = Math.Max(0.55f, Math.Min(0.92f, 0.55f + filteredSpeed * 0.22f));
            PointF smooth = Lerp(previous.Position, raw, alpha);
            float target = BaseWidth;
            if (Tool == InkTool.Highlighter) target *= 3.2f;
            else if (dynamicWidth) target *= 0.45f + 1.15f / (1 + filteredSpeed / 0.35f);
            float widthBlend = 1 - (float)Math.Exp(-dt / 34);
            float width = previous.Width + (target - previous.Width) * widthBlend;
            Samples.Add(new InkSample(smooth, width));
            lastRaw = raw; lastTime = milliseconds;
            RebuildOutline();
            return true;
        }
        public void SetShapeEndpoint(PointF end, bool constrain)
        {
            if (Tool != InkTool.Line && Tool != InkTool.Rectangle) throw new InvalidOperationException("This tool uses freehand ink.");
            PointF start = Samples[0].Position;
            float dx = end.X - start.X, dy = end.Y - start.Y;
            if (constrain)
            {
                if (Tool == InkTool.Rectangle)
                {
                    float side = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    end = new PointF(start.X + (dx < 0 ? -side : side), start.Y + (dy < 0 ? -side : side));
                }
                else
                {
                    double angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * Math.PI / 4;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    end = new PointF(start.X + (float)(Math.Cos(angle) * distance), start.Y + (float)(Math.Sin(angle) * distance));
                }
            }
            Samples.Clear(); Samples.Add(new InkSample(start, BaseWidth)); Samples.Add(new InkSample(end, BaseWidth));
            GraphicsPath center = new GraphicsPath();
            if (Tool == InkTool.Rectangle)
            {
                float x = Math.Min(start.X, end.X), y = Math.Min(start.Y, end.Y);
                center.AddRectangle(new RectangleF(x, y, Math.Max(0.1f, Math.Abs(end.X - start.X)), Math.Max(0.1f, Math.Abs(end.Y - start.Y))));
            }
            else
            {
                center.AddLine(start, end);
            }
            using (Pen pen = new Pen(System.Drawing.Color.Black, BaseWidth))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                center.Widen(pen);
            }
            if (Outline != null) Outline.Dispose(); Outline = center;
        }
        public void FinishArrow()
        {
            if (Tool != InkTool.Arrow || HasArrowhead || Samples.Count < 2) return;
            float total = 0;
            for (int i = 1; i < Samples.Count; i++) total += Distance(Samples[i - 1].Position, Samples[i].Position);
            if (total < 6) return; // A click stays a dot, rather than a large arrow.
            InkSample tail = Samples[Samples.Count - 1];
            float lookback = Math.Min(total * 0.35f, Math.Max(8, tail.Width * 2));
            float traversed = 0; PointF direction = Samples[0].Position;
            for (int i = Samples.Count - 1; i > 0; i--)
            {
                PointF a = Samples[i - 1].Position, b = Samples[i].Position;
                float distance = Distance(a, b);
                if (traversed + distance >= lookback && distance > 0)
                { direction = Lerp(b, a, (lookback - traversed) / distance); break; }
                traversed += distance;
            }
            double angle = Math.Atan2(tail.Position.Y - direction.Y, tail.Position.X - direction.X);
            float length = Math.Min(total * 0.45f, Math.Max(12, tail.Width * 3.2f));
            PointF left = new PointF(tail.Position.X - length * (float)Math.Cos(angle - 0.48), tail.Position.Y - length * (float)Math.Sin(angle - 0.48));
            PointF right = new PointF(tail.Position.X - length * (float)Math.Cos(angle + 0.48), tail.Position.Y - length * (float)Math.Sin(angle + 0.48));
            using (GraphicsPath head = new GraphicsPath())
            using (Pen pen = new Pen(System.Drawing.Color.Black, Math.Max(1, tail.Width * 0.9f)))
            {
                pen.StartCap = pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                head.AddLines(new PointF[] { left, tail.Position, right }); head.Widen(pen);
                Outline.AddPath(head, false);
            }
            HasArrowhead = true;
            hitSource = null;
        }
        static PointF Lerp(PointF a, PointF b, float t)
        { return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); }
        static float Distance(PointF a, PointF b)
        { float x = b.X - a.X, y = b.Y - a.Y; return (float)Math.Sqrt(x * x + y * y); }

        public void RebuildOutline()
        {
            List<InkSample> curve = new List<InkSample>();
            curve.Add(Samples[0]);
            PointF start = Samples[0].Position;
            float startWidth = Samples[0].Width;
            for (int i = 1; i < Samples.Count; i++)
            {
                InkSample control = Samples[i - 1], next = Samples[i];
                PointF end = Lerp(control.Position, next.Position, 0.5f);
                float endWidth = (control.Width + next.Width) * 0.5f;
                int count = Math.Max(1, (int)Math.Ceiling((Distance(start, control.Position) + Distance(control.Position, end)) / 1.5f));
                for (int j = 1; j <= count; j++)
                {
                    float t = (float)j / count, u = 1 - t;
                    PointF p = new PointF(u * u * start.X + 2 * u * t * control.Position.X + t * t * end.X,
                        u * u * start.Y + 2 * u * t * control.Position.Y + t * t * end.Y);
                    curve.Add(new InkSample(p, startWidth + (endWidth - startWidth) * t));
                }
                start = end; startWidth = endWidth;
            }
            InkSample tail = Samples[Samples.Count - 1];
            int tailCount = Math.Max(1, (int)Math.Ceiling(Distance(start, tail.Position) / 1.5f));
            for (int j = 1; j <= tailCount; j++)
            {
                float t = (float)j / tailCount;
                curve.Add(new InkSample(Lerp(start, tail.Position, t), startWidth + (tail.Width - startWidth) * t));
            }
            GraphicsPath path = new GraphicsPath(FillMode.Winding);
            // Clockwise circles + clockwise segment ribbons form one solid outline.
            for (int i = 0; i < curve.Count; i++)
            {
                InkSample current = curve[i];
                float r = current.Width / 2;
                path.AddEllipse(current.Position.X - r, current.Position.Y - r, current.Width, current.Width);
                if (i == 0) continue;
                InkSample prev = curve[i - 1];
                float dx = current.Position.X - prev.Position.X, dy = current.Position.Y - prev.Position.Y;
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                if (length < 0.001f) continue;
                float nx = -dy / length, ny = dx / length, pr = prev.Width / 2;
                path.AddPolygon(new PointF[] {
                    new PointF(prev.Position.X + nx * pr, prev.Position.Y + ny * pr),
                    new PointF(prev.Position.X - nx * pr, prev.Position.Y - ny * pr),
                    new PointF(current.Position.X - nx * r, current.Position.Y - ny * r),
                    new PointF(current.Position.X + nx * r, current.Position.Y + ny * r) });
            }
            if (Outline != null) Outline.Dispose();
            Outline = path;
        }
        public bool Hit(PointF point, float radius)
        {
            RectangleF bounds = Bounds; bounds.Inflate(radius, radius);
            if (!bounds.Contains(point)) return false;
            // GDI+ IsOutlineVisible widens every overlapping ribbon/circle in
            // the ink path on every mouse move. Long strokes can freeze the UI.
            // Flatten once per outline revision and test its actual geometry.
            if (!ReferenceEquals(hitSource, Outline))
            {
                using (GraphicsPath flat = (GraphicsPath)Outline.Clone())
                {
                    flat.Flatten(null, 0.25f);
                    hitPoints = flat.PathPoints; hitTypes = flat.PathTypes;
                }
                hitSource = Outline;
            }
            int winding = 0;
            PointF start = PointF.Empty, previous = PointF.Empty;
            for (int i = 0; i < hitPoints.Length; i++)
            {
                PointF current = hitPoints[i];
                if ((hitTypes[i] & 7) == 0) start = current;
                else if (HitEdge(previous, current, point, radius, ref winding)) return true;
                previous = current;
                if ((hitTypes[i] & 128) != 0 || i == hitPoints.Length - 1 || (hitTypes[i + 1] & 7) == 0)
                    if (HitEdge(current, start, point, radius, ref winding)) return true;
            }
            return Outline.FillMode == FillMode.Winding ? winding != 0 : (winding & 1) != 0;
        }
        static bool HitEdge(PointF a, PointF b, PointF p, float radius, ref int winding)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float lengthSquared = dx * dx + dy * dy;
            float t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared));
            float x = p.X - a.X - t * dx, y = p.Y - a.Y - t * dy;
            if (x * x + y * y <= radius * radius) return true;
            float cross = dx * (p.Y - a.Y) - dy * (p.X - a.X);
            if (a.Y <= p.Y && b.Y > p.Y && cross > 0) winding++;
            else if (a.Y > p.Y && b.Y <= p.Y && cross < 0) winding--;
            return false;
        }
        public void Draw(Graphics graphics, int overrideAlpha, Color? overrideColor)
        {
            int alpha = overrideAlpha >= 0 ? overrideAlpha : Opacity;
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(alpha, overrideColor ?? Color)))
                graphics.FillPath(brush, Outline);
        }
        public void Dispose() { if (Outline != null) { Outline.Dispose(); Outline = null; } hitSource = null; hitPoints = null; hitTypes = null; }
    }

    public sealed class IndexedStroke
    {
        public readonly int Index;
        public readonly Stroke Stroke;
        public IndexedStroke(int index, Stroke stroke) { Index = index; Stroke = stroke; }
    }

    public sealed class Edit
    {
        public readonly List<IndexedStroke> Removed = new List<IndexedStroke>();
        public readonly List<IndexedStroke> Added = new List<IndexedStroke>();
        public Edit(List<Stroke> before, List<Stroke> after)
        {
            HashSet<Stroke> previous = new HashSet<Stroke>(before), current = new HashSet<Stroke>(after);
            for (int i = 0; i < before.Count; i++) if (!current.Contains(before[i])) Removed.Add(new IndexedStroke(i, before[i]));
            for (int i = 0; i < after.Count; i++) if (!previous.Contains(after[i])) Added.Add(new IndexedStroke(i, after[i]));
        }
        public void Apply(List<Stroke> strokes, bool undo)
        {
            List<IndexedStroke> remove = undo ? Added : Removed, add = undo ? Removed : Added;
            foreach (IndexedStroke entry in remove) strokes.Remove(entry.Stroke);
            foreach (IndexedStroke entry in add) strokes.Insert(entry.Index, entry.Stroke);
        }
    }

    public sealed class DrawingPage : IDisposable
    {
        public readonly Bitmap Background;
        public readonly Rectangle ScreenBounds;
        public readonly DateTime Created = DateTime.Now;
        public readonly List<Stroke> Strokes = new List<Stroke>();
        public readonly Stack<Edit> Undo = new Stack<Edit>();
        public readonly Stack<Edit> Redo = new Stack<Edit>();
        readonly HashSet<Stroke> owned = new HashSet<Stroke>();
        public DrawingPage(Bitmap background, Rectangle bounds) { Background = background; ScreenBounds = bounds; }
        public void Commit(List<Stroke> before)
        {
            if (before.Count == Strokes.Count)
            {
                bool equal = true;
                for (int i = 0; i < before.Count; i++) if (before[i] != Strokes[i]) { equal = false; break; }
                if (equal) return;
            }
            foreach (Stroke s in before) owned.Add(s);
            foreach (Stroke s in Strokes) owned.Add(s);
            // History stores only changed strokes, rather than copying the
            // complete drawing at every edit. There is no artificial step cap.
            Undo.Push(new Edit(before, Strokes));
            Redo.Clear();
        }
        public bool UndoEdit()
        {
            if (Undo.Count == 0) return false;
            Edit edit = Undo.Pop(); edit.Apply(Strokes, true); Redo.Push(edit); return true;
        }
        public bool RedoEdit()
        {
            if (Redo.Count == 0) return false;
            Edit edit = Redo.Pop(); edit.Apply(Strokes, false); Undo.Push(edit); return true;
        }
        public Bitmap Export()
        {
            Bitmap image = (Bitmap)Background.Clone();
            using (Graphics graphics = Graphics.FromImage(image))
            {
                InkCanvas.Quality(graphics);
                foreach (Stroke stroke in Strokes) stroke.Draw(graphics, -1, null);
            }
            return image;
        }
        public void Dispose()
        {
            foreach (Stroke stroke in Strokes) owned.Add(stroke);
            foreach (Stroke stroke in owned) stroke.Dispose();
            owned.Clear(); Strokes.Clear(); Undo.Clear(); Redo.Clear(); Background.Dispose();
        }
    }

    public struct LaserPoint
    {
        public PointF Point;
        public double Time;
        public LaserPoint(PointF point, double time) { Point = point; Time = time; }
    }

    public sealed class InkCanvas : Control
    {
        public event EventHandler StateChanged;
        public DrawingPage Page { get; private set; }
        Color inkColor = Color.FromArgb(237, 73, 73);
        public Color InkColor { get { return inkColor; } set { inkColor = value; } }
        public float BaseWidth = 5;
        public bool DynamicWidth = true;
        public int HighlighterOpacity = 82;
        public double LaserLifetime = 650;
        public float EraserRadius = 12;
        public string TransientMessage = "";
        InkTool tool;
        public InkTool Tool
        {
            get { return tool; }
            set { if (tool == value) return; FinishGesture(); tool = value; hover.Clear(); Invalidate(); Notify(); }
        }
        Bitmap committed;
        Stroke active;
        List<Stroke> beforeGesture;
        readonly List<Stroke> hover = new List<Stroke>();
        readonly List<Stroke> fading = new List<Stroke>();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly Timer animation = new Timer();
        readonly List<LaserPoint> laser = new List<LaserPoint>();
        double fadeStart;
        double messageUntil;
        PointF pointer;
        bool pointerInside, dragging;
        bool temporaryEraser;
        public InkCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Cross; TabStop = true;
            animation.Interval = 16;
            animation.Tick += delegate {
                double now = clock.Elapsed.TotalMilliseconds;
                if (now - fadeStart >= 220) fading.Clear();
                laser.RemoveAll(delegate(LaserPoint p) { return now - p.Time > LaserLifetime; });
                if (now >= messageUntil) TransientMessage = "";
                if (fading.Count == 0 && laser.Count == 0 && TransientMessage.Length == 0) animation.Stop();
                Invalidate();
            };
        }
        public static void Quality(Graphics g)
        { g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.CompositingQuality = CompositingQuality.HighQuality; }
        void Notify() { if (StateChanged != null) StateChanged(this, EventArgs.Empty); }
        public void RefreshSettings() { Notify(); Invalidate(); }
        public void SetPage(DrawingPage page)
        {
            FinishGesture(); Page = page; hover.Clear(); fading.Clear(); laser.Clear(); TransientMessage = ""; animation.Stop(); pointerInside = false;
            RebuildCache(); Notify();
        }
        void RebuildCache()
        {
            if (committed != null) committed.Dispose();
            committed = Page == null ? null : Page.Export();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (committed == null) return;
            e.Graphics.DrawImageUnscaled(committed, 0, 0); Quality(e.Graphics);
            if (active != null) active.Draw(e.Graphics, -1, null);
            foreach (Stroke s in hover) s.Draw(e.Graphics, 140, Color.FromArgb(255, 80, 110));
            int fadeAlpha = Math.Max(0, (int)(130 * (1 - (clock.Elapsed.TotalMilliseconds - fadeStart) / 220)));
            foreach (Stroke s in fading) s.Draw(e.Graphics, fadeAlpha, Color.FromArgb(255, 80, 110));
            if (pointerInside && (tool == InkTool.Eraser || temporaryEraser))
            {
                RectangleF ring = new RectangleF(pointer.X - EraserRadius, pointer.Y - EraserRadius, EraserRadius * 2, EraserRadius * 2);
                using (Pen shadow = new Pen(Color.FromArgb(160, 0, 0, 0), 4)) e.Graphics.DrawEllipse(shadow, ring);
                using (Pen pen = new Pen(hover.Count > 0 ? Color.FromArgb(255, 100, 130) : Color.White, 2)) e.Graphics.DrawEllipse(pen, ring);
            }
            else if (pointerInside && !dragging && tool != InkTool.Laser)
            {
                float width = tool == InkTool.Highlighter ? BaseWidth * 3.2f : BaseWidth;
                float radius = Math.Max(3, width / 2);
                RectangleF ring = new RectangleF(pointer.X - radius, pointer.Y - radius, radius * 2, radius * 2);
                using (Pen outline = new Pen(Color.FromArgb(120, 0, 0, 0), 3)) e.Graphics.DrawEllipse(outline, ring);
                using (Pen ink = new Pen(InkColor, 1.5f)) e.Graphics.DrawEllipse(ink, ring);
            }
            double now = clock.Elapsed.TotalMilliseconds;
            for (int i = 1; i < laser.Count; i++)
            {
                int alpha = (int)(210 * Math.Max(0, 1 - (now - laser[i].Time) / LaserLifetime));
                using (Pen glow = new Pen(Color.FromArgb(alpha / 4, InkColor), 15))
                using (Pen core = new Pen(Color.FromArgb(alpha, InkColor), 3))
                {
                    glow.StartCap = glow.EndCap = core.StartCap = core.EndCap = LineCap.Round;
                    e.Graphics.DrawLine(glow, laser[i - 1].Point, laser[i].Point); e.Graphics.DrawLine(core, laser[i - 1].Point, laser[i].Point);
                }
            }
            if (pointerInside && tool == InkTool.Laser)
            {
                using (Brush glow = new SolidBrush(Color.FromArgb(70, InkColor))) e.Graphics.FillEllipse(glow, pointer.X - 12, pointer.Y - 12, 24, 24);
                using (Brush dot = new SolidBrush(InkColor)) e.Graphics.FillEllipse(dot, pointer.X - 4, pointer.Y - 4, 8, 8);
                using (Brush core = new SolidBrush(Color.White)) e.Graphics.FillEllipse(core, pointer.X - 1, pointer.Y - 1, 2, 2);
            }
            if (TransientMessage.Length > 0)
            {
                using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    SizeF size = e.Graphics.MeasureString(TransientMessage, font);
                    RectangleF badge = new RectangleF((Width - size.Width - 32) / 2, Height - 80, size.Width + 32, 42);
                    using (GraphicsPath path = StudioTheme.Round(badge, 12))
                    using (Brush bg = new SolidBrush(Color.FromArgb(235, 25, 31, 46))) e.Graphics.FillPath(bg, path);
                    using (Brush text = new SolidBrush(Color.White)) e.Graphics.DrawString(TransientMessage, font, text, badge.X + 16, badge.Y + 11);
                }
            }
        }
        protected override void OnMouseEnter(EventArgs e) { pointerInside = true; base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { pointerInside = false; hover.Clear(); Invalidate(); base.OnMouseLeave(e); }
        void UpdateHover()
        {
            hover.Clear();
            if (Page != null && (tool == InkTool.Eraser || temporaryEraser))
                foreach (Stroke stroke in Page.Strokes) if (stroke.Hit(pointer, EraserRadius)) hover.Add(stroke);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if ((e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) || Page == null) return;
            FinishGesture();
            Focus(); Capture = true; dragging = true; pointer = e.Location;
            temporaryEraser = e.Button == MouseButtons.Right;
            beforeGesture = new List<Stroke>(Page.Strokes);
            if (tool == InkTool.Eraser || temporaryEraser) EraseBetween(pointer, pointer);
            else if (tool == InkTool.Laser) AddLaser(pointer);
            else { active = new Stroke(tool, InkColor, BaseWidth, pointer, clock.Elapsed.TotalMilliseconds, tool == InkTool.Highlighter ? HighlighterOpacity : 255); Invalidate(); }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF previous = pointer; pointer = e.Location; pointerInside = ClientRectangle.Contains(e.Location);
            if (dragging && (tool == InkTool.Eraser || temporaryEraser)) EraseBetween(previous, pointer);
            else if (dragging && active != null)
            {
                RectangleF old = active.Bounds;
                bool shape = tool == InkTool.Line || tool == InkTool.Rectangle;
                if (shape) active.SetShapeEndpoint(pointer, (ModifierKeys & Keys.Shift) == Keys.Shift);
                if (shape || active.Append(pointer, clock.Elapsed.TotalMilliseconds, DynamicWidth))
                {
                    RectangleF dirty = RectangleF.Union(old, active.Bounds); dirty.Inflate(3, 3); Invalidate(Rectangle.Ceiling(dirty));
                }
            }
            if (tool == InkTool.Laser && !temporaryEraser) AddLaser(pointer);
            if (tool == InkTool.Eraser || temporaryEraser) { UpdateHover(); Invalidate(); }
            else if (!dragging) { Invalidate(new Rectangle((int)previous.X - 48, (int)previous.Y - 48, 96, 96)); Invalidate(new Rectangle((int)pointer.X - 48, (int)pointer.Y - 48, 96, 96)); }
        }
        void AddLaser(PointF point) { laser.Add(new LaserPoint(point, clock.Elapsed.TotalMilliseconds)); animation.Start(); Invalidate(); }
        public void ShowMessage(string message) { TransientMessage = message; messageUntil = clock.Elapsed.TotalMilliseconds + 1600; animation.Start(); Invalidate(); }
        void EraseBetween(PointF from, PointF to)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y;
            int count = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / 6));
            bool changed = false;
            for (int i = Page.Strokes.Count - 1; i >= 0; i--)
            {
                Stroke stroke = Page.Strokes[i]; bool hit = false;
                for (int j = 0; j <= count && !hit; j++)
                    hit = stroke.Hit(new PointF(from.X + dx * j / count, from.Y + dy * j / count), EraserRadius);
                if (hit) { Page.Strokes.RemoveAt(i); fading.Add(stroke); changed = true; }
            }
            if (changed) { fadeStart = clock.Elapsed.TotalMilliseconds; animation.Start(); RebuildCache(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) FinishGesture(); }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) FinishGesture(); }
        public void FinishGesture()
        {
            if (!dragging) return;
            dragging = false;
            if (active != null) { active.FinishArrow(); Page.Strokes.Add(active); active = null; }
            Page.Commit(beforeGesture); beforeGesture = null; temporaryEraser = false; Capture = false;
            RebuildCache(); UpdateHover(); Notify();
        }
        public void UndoInk() { FinishGesture(); if (Page != null && Page.UndoEdit()) { ResetPreview(); RebuildCache(); Notify(); } }
        public void RedoInk() { FinishGesture(); if (Page != null && Page.RedoEdit()) { ResetPreview(); RebuildCache(); Notify(); } }
        void ResetPreview() { hover.Clear(); fading.Clear(); laser.Clear(); TransientMessage = ""; animation.Stop(); }
        public void ClearInk()
        {
            FinishGesture(); if (Page == null || Page.Strokes.Count == 0) return;
            List<Stroke> before = new List<Stroke>(Page.Strokes);
            fading.Clear(); fading.AddRange(before); fadeStart = clock.Elapsed.TotalMilliseconds; animation.Start();
            Page.Strokes.Clear(); Page.Commit(before); hover.Clear(); RebuildCache(); Notify();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { FinishGesture(); animation.Dispose(); if (committed != null) { committed.Dispose(); committed = null; } }
            base.Dispose(disposing);
        }
    }

    // Shortcuts work even after a toolbar button/slider has keyboard focus.
    public sealed class InkForm : Form
    {
        public InkCanvas Canvas;
        public event EventHandler Done;
        public event EventHandler PreviousPage;
        public event EventHandler NextPage;
        public event EventHandler SaveRequested;
        public event EventHandler CopyRequested;
        public event EventHandler CompactRequested;
        protected override bool ProcessCmdKey(ref Message message, Keys key)
        {
            if (key == Keys.Escape) { if (Done != null) Done(this, EventArgs.Empty); return true; }
            if (key == (Keys.Control | Keys.Z)) { if (Canvas != null) Canvas.UndoInk(); return true; }
            if (key == (Keys.Control | Keys.Y) || key == (Keys.Control | Keys.Shift | Keys.Z)) { if (Canvas != null) Canvas.RedoInk(); return true; }
            if (key == (Keys.Alt | Keys.Left)) { if (PreviousPage != null) PreviousPage(this, EventArgs.Empty); return true; }
            if (key == (Keys.Alt | Keys.Right)) { if (NextPage != null) NextPage(this, EventArgs.Empty); return true; }
            if (key == (Keys.Control | Keys.S)) { if (SaveRequested != null) SaveRequested(this, EventArgs.Empty); return true; }
            if (key == (Keys.Control | Keys.C)) { if (CopyRequested != null) CopyRequested(this, EventArgs.Empty); return true; }
            if (key == (Keys.Control | Keys.Tab)) { if (CompactRequested != null) CompactRequested(this, EventArgs.Empty); return true; }
            if (Canvas != null)
            {
                if (key == Keys.P) { Canvas.Tool = InkTool.Pen; return true; }
                if (key == Keys.H) { Canvas.Tool = InkTool.Highlighter; return true; }
                if (key == Keys.E) { Canvas.Tool = InkTool.Eraser; return true; }
                if (key == Keys.L) { Canvas.Tool = InkTool.Laser; return true; }
                if (key == Keys.I) { Canvas.Tool = InkTool.Line; return true; }
                if (key == Keys.A) { Canvas.Tool = InkTool.Arrow; return true; }
                if (key == Keys.R) { Canvas.Tool = InkTool.Rectangle; return true; }
                if (key == Keys.OemOpenBrackets || key == Keys.OemCloseBrackets)
                {
                    Canvas.BaseWidth = Math.Max(1, Math.Min(24, Canvas.BaseWidth + (key == Keys.OemCloseBrackets ? 1 : -1)));
                    Canvas.RefreshSettings(); return true;
                }
            }
            return base.ProcessCmdKey(ref message, key);
        }
    }

    public static class CoreTests
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Mouse(InkCanvas canvas, string method, MouseEventArgs args)
        {
            typeof(InkCanvas).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(canvas, new object[] { args });
        }
        public static void RenderPreview(string file)
        {
            using (DrawingPage page = new DrawingPage(new Bitmap(1000, 540), new Rectangle(0, 0, 1000, 540)))
            {
                using (Graphics g = Graphics.FromImage(page.Background))
                using (Font title = new Font("Segoe UI", 22, FontStyle.Bold))
                using (Font label = new Font("Segoe UI", 12))
                {
                    g.Clear(Color.FromArgb(246, 248, 253));
                    g.DrawString("ScreenInk / live ink preview", title, Brushes.MidnightBlue, 35, 25);
                    g.DrawString("Slow pen: fuller ink", label, Brushes.DimGray, 35, 105);
                    g.DrawString("Fast pen: finer ink", label, Brushes.DimGray, 35, 240);
                    g.DrawString("Highlighter: consistent translucency", label, Brushes.DimGray, 35, 370);
                }
                for (int row = 0; row < 3; row++)
                {
                    float y = 175 + row * 132;
                    Stroke stroke = new Stroke(row == 2 ? InkTool.Highlighter : InkTool.Pen,
                        row == 2 ? Color.FromArgb(255, 206, 35) : Color.FromArgb(69, 106, 238), 10, new PointF(40, y), 0);
                    for (int i = 1; i < 285; i++) stroke.Append(new PointF(40 + i * 3.2f, y + (float)Math.Sin(i * 0.065) * 28), i * (row == 0 ? 35 : 3), true);
                    List<Stroke> before = new List<Stroke>(page.Strokes); page.Strokes.Add(stroke); page.Commit(before);
                }
                using (Bitmap image = page.Export()) image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        public static string Run()
        {
            Stroke slow = new Stroke(InkTool.Pen, Color.Red, 8, new PointF(10, 20), 0);
            Stroke fast = new Stroke(InkTool.Pen, Color.Red, 8, new PointF(10, 20), 0);
            for (int i = 1; i <= 60; i++)
            {
                slow.Append(new PointF(10 + i * 3, 20), i * 30, true);
                fast.Append(new PointF(10 + i * 3, 20), i * 2, true);
            }
            Check(slow.Samples[60].Width > fast.Samples[60].Width * 1.65f, "Expressive ink should produce a stronger slow/fast width contrast.");
            Check(slow.Hit(slow.Samples[30].Position, 4), "Eraser should hit the rendered ink.");
            Check(!slow.Hit(new PointF(200, 150), 4), "Eraser should ignore distant ink.");
            using (DrawingPage page = new DrawingPage(new Bitmap(320, 180), new Rectangle(0, 0, 320, 180)))
            {
                for (int i = 0; i < 125; i++)
                {
                    List<Stroke> initial = new List<Stroke>(page.Strokes);
                    page.Strokes.Add(new Stroke(InkTool.Pen, Color.Black, 3, new PointF(i, 50), 0)); page.Commit(initial);
                }
                for (int i = 0; i < 125; i++) Check(page.UndoEdit(), "All 125 edits should undo.");
                Check(page.Strokes.Count == 0 && !page.UndoEdit(), "Undo must return to a clean screen.");
                for (int i = 0; i < 125; i++) Check(page.RedoEdit(), "All edits should redo.");
                List<Stroke> before = new List<Stroke>(page.Strokes);
                page.Strokes.Clear(); page.Commit(before); Check(page.UndoEdit() && page.Strokes.Count == 125, "Clear should restore every stroke.");
                before = new List<Stroke>(page.Strokes); page.Strokes.RemoveAt(5); page.Commit(before);
                Check(page.UndoEdit() && page.Strokes.Count == 125, "Whole-stroke erase should undo.");
                using (Bitmap image = page.Export()) Check(image.Width == 320, "Export failed.");
                using (InkCanvas canvas = new InkCanvas())
                using (DrawingPage second = new DrawingPage(new Bitmap(320, 180), new Rectangle(0, 0, 320, 180)))
                {
                    canvas.SetPage(page); canvas.SetPage(second); canvas.SetPage(page);
                    Check(canvas.Page.Strokes.Count == 125, "Navigating away must preserve edits.");
                    canvas.UndoInk(); Check(page.Strokes.Count == 124, "Undo history must survive page navigation.");
                }
            }
            using (DrawingPage page = new DrawingPage(new Bitmap(320, 180), new Rectangle(0, 0, 320, 180)))
            using (InkCanvas canvas = new InkCanvas())
            using (InkForm toolbar = new InkForm())
            {
                using (Graphics g = Graphics.FromImage(page.Background)) g.Clear(Color.White);
                canvas.Size = new Size(320, 180); canvas.SetPage(page); toolbar.Canvas = canvas;
                Mouse(canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 25, 80, 0));
                for (int i = 1; i <= 50; i++) Mouse(canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 25 + i * 4, 80 + (int)(Math.Sin(i * 0.16) * 30), 0));
                using (Bitmap live = new Bitmap(320, 180))
                using (Bitmap finished = new Bitmap(320, 180))
                {
                    Mouse(canvas, "OnMouseLeave", new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
                    canvas.DrawToBitmap(live, new Rectangle(0, 0, 320, 180));
                    Mouse(canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 225, 110, 0));
                    canvas.DrawToBitmap(finished, new Rectangle(0, 0, 320, 180));
                    for (int x = 0; x < 320; x++) for (int y = 0; y < 180; y++)
                        Check(live.GetPixel(x, y) == finished.GetPixel(x, y), "Live ink must not change on mouse release.");
                }
                Check(page.Strokes.Count == 1 && page.Undo.Count == 1, "A pointer gesture must commit exactly one undo step.");
                object[] keyArgs = new object[] { new Message(), Keys.Control | Keys.Z };
                typeof(InkForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(toolbar, keyArgs);
                Check(page.Strokes.Count == 0, "Ctrl+Z must work from the toolbar.");
                canvas.RedoInk(); canvas.Tool = InkTool.Eraser;
                Mouse(canvas, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 80, 0));
                Mouse(canvas, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, 240, 80, 0));
                Mouse(canvas, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 240, 80, 0));
                Check(page.Strokes.Count == 0 && page.Undo.Count == 2, "Swept erasing must remove a whole stroke in one undo step.");
                canvas.UndoInk(); Check(page.Strokes.Count == 1, "Undo must restore an erased connected stroke.");
            }
            slow.Dispose(); fast.Dispose();
            Stopwatch benchmark = Stopwatch.StartNew();
            using (Stroke longStroke = new Stroke(InkTool.Pen, Color.Blue, 5, new PointF(0, 100), 0))
                for (int i = 1; i <= 1000; i++) longStroke.Append(new PointF(i * 2, 100 + (float)Math.Sin(i * 0.02) * 30), i * 8, true);
            benchmark.Stop();
            return "PASS: speed-sensitive ink, 125-step undo/redo, editable history, live/final pixel match, toolbar Ctrl+Z, swept stroke erasing. Long-stroke append average: " + (benchmark.Elapsed.TotalMilliseconds / 1000).ToString("F2") + " ms.";
        }
    }
}
