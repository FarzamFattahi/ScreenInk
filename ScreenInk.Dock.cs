using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;

namespace ScreenInk
{
    public enum DockSide { Top, Bottom, Left, Right }

    public sealed partial class StudioToolbar
    {
        Panel compactSurface;
        readonly List<Control> compactItems = new List<Control>();
        public readonly Dictionary<InkTool, StudioButton> CompactTools = new Dictionary<InkTool, StudioButton>();
        StudioButton compactUndo, compactRedo;
        ContextMenuStrip placementMenu;
        ToolStripDropDown toolPopup;
        InkTool? suppressAnchorClick;
        double suppressAnchorAt;
        readonly Stopwatch popupClock = Stopwatch.StartNew();
        public ToolOptionsPanel CurrentToolOptions { get; private set; }
        public bool ToolOptionsOpen { get; private set; }
        public DockSide Placement { get { return preferences.DockSide; } }
        bool VerticalDock { get { return Placement == DockSide.Left || Placement == DockSide.Right; } }
        const int Cell = 46;

        void InitializeDock()
        {
            compactSurface = new Panel(); compactSurface.BackColor = BackColor; Controls.Add(compactSurface);
            DragHeader grip = new DragHeader(); grip.CompactDrawing = true; grip.Size = new Size(42, 42);
            grip.MouseUp += delegate { SnapAfterDrag(); }; AddCompact(grip); tips.SetToolTip(grip, "Drag to a screen edge to dock");
            foreach (InkTool tool in Enum.GetValues(typeof(InkTool)))
            {
                InkTool captured = tool;
                StudioButton button = Make("", tool.ToString(), 42, 42, ToolHint(tool) + " / Click again for options");
                button.Accent = tool == InkTool.Eraser || tool == InkTool.Laser ? Color.FromArgb(255, 138, 168) : StudioTheme.Accent;
                button.Click += delegate { ClickTool(captured); }; CompactTools[tool] = button; AddCompact(button);
            }
            compactUndo = Make("", "Undo", 42, 42, "Undo / Ctrl+Z"); compactUndo.Click += delegate { canvas.UndoInk(); }; AddCompact(compactUndo);
            compactRedo = Make("", "Redo", 42, 42, "Redo / Ctrl+Y"); compactRedo.Click += delegate { canvas.RedoInk(); }; AddCompact(compactRedo);
            StudioButton dock = Make("", "Dock", 42, 42, "Dock at top, bottom, left, or right");
            dock.Click += delegate { ShowPlacementMenu(dock); }; AddCompact(dock);
            StudioButton expand = Make("", "Expand", 42, 42, "Open the full toolbar / Ctrl+Tab"); expand.Click += delegate { ToggleCompact(); }; AddCompact(expand);
            StudioButton done = Make("", "Done", 42, 42, "Return to the live screen / F8"); done.Click += delegate { Raise(DoneRequested); }; AddCompact(done);
            StudioButton fullDock = Make("", "Dock", 34, 34, "Choose screen edge"); fullDock.Click += delegate { ShowPlacementMenu(fullDock); }; header.Controls.Add(fullDock);
            placementMenu = new ContextMenuStrip();
            foreach (DockSide side in Enum.GetValues(typeof(DockSide)))
            {
                DockSide captured = side;
                ToolStripMenuItem item = new ToolStripMenuItem("Dock " + side.ToString().ToLowerInvariant()); item.Tag = side;
                item.Click += delegate { SetDockSide(captured); }; placementMenu.Items.Add(item);
            }
            collapse.AccessibleName = "Shrink to quick tool dock / Ctrl+Tab";
            tips.SetToolTip(collapse, "Shrink to quick tool dock / Ctrl+Tab");
        }
        void AddCompact(Control control) { compactItems.Add(control); compactSurface.Controls.Add(control); }
        void ShowPlacementMenu(Control anchor)
        {
            CloseToolOptions();
            foreach (ToolStripMenuItem item in placementMenu.Items) item.Checked = (DockSide)item.Tag == Placement;
            placementMenu.Show(anchor, new Point(0, anchor.Height));
        }
        public void SetDockSide(DockSide side)
        {
            CloseToolOptions(); preferences.DockSide = side; PerformLayout(); Raise(SizeRequested);
        }
        void SnapAfterDrag()
        {
            Form form = FindForm(); if (form == null || !form.Visible) return;
            Rectangle area = Screen.FromRectangle(form.Bounds).WorkingArea;
            int[] distances = new int[] { Math.Abs(form.Top - area.Top), Math.Abs(form.Bottom - area.Bottom), Math.Abs(form.Left - area.Left), Math.Abs(form.Right - area.Right) };
            int nearest = 0; for (int i = 1; i < distances.Length; i++) if (distances[i] < distances[nearest]) nearest = i;
            if (distances[nearest] <= 60) SetDockSide((DockSide)nearest);
        }
        void ApplyDockMode()
        {
            header.Visible = tools.Visible = options.Visible = actions.Visible = StatusLabel.Visible = !Collapsed;
            if (compactSurface != null) compactSurface.Visible = Collapsed;
        }
        void RefreshDockState()
        {
            foreach (KeyValuePair<InkTool, StudioButton> item in CompactTools) item.Value.Selected = item.Key == canvas.Tool;
            if (compactUndo != null) compactUndo.Enabled = canvas.Page != null && canvas.Page.Undo.Count > 0;
            if (compactRedo != null) compactRedo.Enabled = canvas.Page != null && canvas.Page.Redo.Count > 0;
            if (CurrentToolOptions != null && ToolOptionsOpen) CurrentToolOptions.RefreshState();
        }
        Size CompactSize(Size available)
        {
            int count = Math.Max(1, compactItems.Count);
            if (VerticalDock)
            {
                int rows = Math.Min(count, Math.Max(1, (available.Height - 16) / Cell));
                int columns = (count + rows - 1) / rows;
                return new Size(columns * Cell + 16, rows * Cell + 16);
            }
            int cols = Math.Min(count, Math.Max(1, (available.Width - 16) / Cell));
            return new Size(cols * Cell + 16, ((count + cols - 1) / cols) * Cell + 16);
        }
        void LayoutCompactDock()
        {
            if (compactSurface == null) return;
            compactSurface.SetBounds(4, 4, Math.Max(1, Width - 8), Math.Max(1, Height - 8));
            int capacity = Math.Max(1, ((VerticalDock ? Height : Width) - 16) / Cell);
            for (int i = 0; i < compactItems.Count; i++)
            {
                int column = VerticalDock ? i / capacity : i % capacity;
                int row = VerticalDock ? i % capacity : i / capacity;
                compactItems[i].SetBounds(4 + column * Cell, 4 + row * Cell, 42, 42);
            }
        }
        public Rectangle DockBounds(Rectangle area)
        {
            Size size = Collapsed ? CompactSize(new Size(Math.Max(80, area.Width - 24), Math.Max(80, area.Height - 24)))
                : new Size(Math.Min(820, area.Width - 24), PreferredHeight(Math.Min(820, area.Width - 24)));
            int x = area.Left + (area.Width - size.Width) / 2, y = area.Top + 12;
            if (Placement == DockSide.Bottom) y = area.Bottom - size.Height - 12;
            if (Placement == DockSide.Left || Placement == DockSide.Right)
            { x = Placement == DockSide.Left ? area.Left + 12 : area.Right - size.Width - 12; y = area.Top + (area.Height - size.Height) / 2; }
            x = Math.Max(area.Left, Math.Min(area.Right - size.Width, x)); y = Math.Max(area.Top, Math.Min(area.Bottom - size.Height, y));
            return new Rectangle(new Point(x, y), size);
        }
        void ClickTool(InkTool tool)
        {
            // Outside-click dismissal happens on mouse-down, before a dock
            // button's Click. Avoid immediately reopening the same popup.
            if (suppressAnchorClick == tool && popupClock.Elapsed.TotalMilliseconds - suppressAnchorAt < 500)
            { suppressAnchorClick = null; CloseToolOptions(); return; }
            suppressAnchorClick = null;
            if (Collapsed && canvas.Tool == tool)
            {
                if (ToolOptionsOpen) CloseToolOptions(); else OpenToolOptions(tool);
                return;
            }
            CloseToolOptions(); canvas.Tool = tool;
        }
        public void CloseToolOptions()
        {
            ToolOptionsOpen = false;
            ToolStripDropDown previous = toolPopup; toolPopup = null;
            ToolOptionsPanel panel = CurrentToolOptions; CurrentToolOptions = null;
            if (previous != null) { previous.Close(); previous.Dispose(); }
            else if (panel != null) panel.Dispose();
        }
        void OpenToolOptions(InkTool tool)
        {
            CloseToolOptions();
            CurrentToolOptions = new ToolOptionsPanel(canvas);
            CurrentToolOptions.CloseRequested += delegate { CloseToolOptions(); };
            ToolOptionsOpen = true;
            Form owner = FindForm();
            // Standalone controls are used by the headless rendering tests.
            if (owner == null || !owner.Visible) return;
            toolPopup = new ToolStripDropDown(); toolPopup.Padding = new Padding(0); toolPopup.Margin = new Padding(0); toolPopup.AutoClose = true;
            toolPopup.BackColor = StudioTheme.Background;
            ToolStripControlHost host = new ToolStripControlHost(CurrentToolOptions); host.AutoSize = false;
            host.Size = CurrentToolOptions.Size; host.Padding = new Padding(0); host.Margin = new Padding(0);
            toolPopup.Items.Add(host);
            StudioButton anchor = CompactTools[tool]; Rectangle button = anchor.RectangleToScreen(anchor.ClientRectangle);
            toolPopup.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs args) {
                ToolOptionsOpen = false;
                if (args.CloseReason == ToolStripDropDownCloseReason.AppClicked && anchor.RectangleToScreen(anchor.ClientRectangle).Contains(MousePosition))
                { suppressAnchorClick = tool; suppressAnchorAt = popupClock.Elapsed.TotalMilliseconds; }
            };
            Rectangle area = Screen.FromRectangle(button).WorkingArea;
            Point point = new Point(button.Left, button.Bottom + 8);
            if (Placement == DockSide.Bottom) point.Y = button.Top - CurrentToolOptions.Height - 8;
            if (Placement == DockSide.Left) { point.X = owner.Right + 8; point.Y = button.Top; }
            if (Placement == DockSide.Right) { point.X = owner.Left - CurrentToolOptions.Width - 8; point.Y = button.Top; }
            point.X = Math.Max(area.Left + 4, Math.Min(area.Right - CurrentToolOptions.Width - 4, point.X));
            point.Y = Math.Max(area.Top + 4, Math.Min(area.Bottom - CurrentToolOptions.Height - 4, point.Y));
            toolPopup.Show(owner, owner.PointToClient(point));
        }
        void DisposeDock()
        {
            CloseToolOptions(); if (placementMenu != null) { placementMenu.Dispose(); placementMenu = null; }
        }
    }

    public sealed class ToolOptionsPanel : UserControl
    {
        public event EventHandler CloseRequested;
        public readonly StudioSlider WidthSlider = new StudioSlider();
        public readonly StudioSlider ExtraSlider = new StudioSlider();
        public readonly StudioButton DynamicButton = new StudioButton();
        public readonly List<StudioButton> Colors = new List<StudioButton>();
        public readonly InkTool Tool;
        readonly InkCanvas canvas;
        readonly Label title = new Label(), hint = new Label();
        bool updating;
        public ToolOptionsPanel(InkCanvas canvas)
        {
            this.canvas = canvas; Tool = canvas.Tool; BackColor = StudioTheme.Background;
            Size = new Size(344, 244);
            title.Text = (Tool == InkTool.Arrow ? "Arrow pen" : Tool.ToString()) + " options";
            title.ForeColor = StudioTheme.Text; title.Font = new Font("Segoe UI", 11, FontStyle.Bold); title.SetBounds(18, 14, 272, 26); Controls.Add(title);
            StudioButton close = new StudioButton(); close.Icon = "Close"; close.SetBounds(300, 10, 30, 30); close.AccessibleName = "Close tool options";
            close.Click += delegate { if (CloseRequested != null) CloseRequested(this, EventArgs.Empty); }; Controls.Add(close);
            FlowLayoutPanel palette = new FlowLayoutPanel(); palette.SetBounds(14, 48, 320, 44); palette.WrapContents = false; Controls.Add(palette);
            foreach (string hex in new string[] { "#F56477", "#FFD343", "#59D6B0", "#73A0FF", "#BD92FF", "#FFFFFF", "#192133" })
            {
                StudioButton color = new StudioButton(); color.IsSwatch = true; color.SwatchColor = ColorTranslator.FromHtml(hex);
                color.Size = new Size(34, 38); color.Margin = new Padding(3, 0, 3, 0); color.AccessibleName = "Color " + hex;
                color.Click += delegate { canvas.InkColor = color.SwatchColor; canvas.RefreshSettings(); RefreshState(); };
                Colors.Add(color); palette.Controls.Add(color);
            }
            StudioButton custom = new StudioButton(); custom.Icon = "Color"; custom.Size = new Size(34, 38); custom.Margin = new Padding(3, 0, 3, 0); custom.AccessibleName = "Custom color";
            custom.Click += delegate { using (ColorDialog dialog = new ColorDialog()) { dialog.Color = canvas.InkColor; dialog.FullOpen = true; if (dialog.ShowDialog() == DialogResult.OK) { canvas.InkColor = dialog.Color; canvas.RefreshSettings(); RefreshState(); } } };
            palette.Controls.Add(custom);
            WidthSlider.SetBounds(18, 100, 308, 46); Controls.Add(WidthSlider);
            WidthSlider.ValueChanged += delegate {
                if (updating) return;
                if (Tool == InkTool.Eraser) canvas.EraserRadius = WidthSlider.Value / 2f; else canvas.BaseWidth = WidthSlider.Value;
                canvas.RefreshSettings();
            };
            DynamicButton.Text = "Dynamic ink"; DynamicButton.Icon = "Dynamic"; DynamicButton.SetBounds(18, 154, 145, 36); Controls.Add(DynamicButton);
            DynamicButton.Click += delegate { canvas.DynamicWidth = !canvas.DynamicWidth; canvas.RefreshSettings(); RefreshState(); };
            ExtraSlider.SetBounds(18, 153, 308, 46); Controls.Add(ExtraSlider);
            ExtraSlider.ValueChanged += delegate {
                if (updating) return;
                if (Tool == InkTool.Laser) canvas.LaserLifetime = ExtraSlider.Value;
                if (Tool == InkTool.Highlighter) canvas.HighlighterOpacity = (int)Math.Round(ExtraSlider.Value * 255 / 100.0);
                canvas.RefreshSettings();
            };
            hint.ForeColor = StudioTheme.Muted; hint.Font = new Font("Segoe UI", 8.5f); hint.SetBounds(18, 205, 308, 34); Controls.Add(hint);
            hint.Text = "Click the selected tool again to close these options.";
            DynamicButton.Visible = Tool == InkTool.Pen || Tool == InkTool.Arrow;
            ExtraSlider.Visible = Tool == InkTool.Highlighter || Tool == InkTool.Laser;
            if (Tool == InkTool.Highlighter) { ExtraSlider.Title = "OPACITY"; ExtraSlider.Suffix = "%"; ExtraSlider.Minimum = 10; ExtraSlider.Maximum = 65; }
            if (Tool == InkTool.Laser) { ExtraSlider.Title = "TRAIL FADE"; ExtraSlider.Suffix = " ms"; ExtraSlider.Minimum = 250; ExtraSlider.Maximum = 1500; WidthSlider.Visible = false; ExtraSlider.Top = 100; hint.Top = 158; Height = 204; }
            if (Tool == InkTool.Eraser)
            {
                palette.Visible = false; WidthSlider.Title = "ERASER TARGET SIZE"; WidthSlider.Minimum = 12; WidthSlider.Maximum = 80; WidthSlider.Top = 54;
                hint.Text = "Touch ink to preview and erase its whole connected stroke."; hint.Top = 110; Height = 158;
            }
            if (Tool == InkTool.Line || Tool == InkTool.Rectangle) { hint.Text = Tool == InkTool.Line ? "Hold Shift to snap your line to 45-degree angles." : "Hold Shift to draw a perfect square."; hint.Top = 159; Height = 205; }
            RefreshState();
        }
        public void RefreshState()
        {
            updating = true;
            WidthSlider.Value = Tool == InkTool.Eraser ? (int)(canvas.EraserRadius * 2) : (int)canvas.BaseWidth;
            if (Tool == InkTool.Laser) ExtraSlider.Value = (int)canvas.LaserLifetime;
            if (Tool == InkTool.Highlighter) ExtraSlider.Value = (int)Math.Round(canvas.HighlighterOpacity * 100 / 255.0);
            DynamicButton.Selected = canvas.DynamicWidth;
            foreach (StudioButton color in Colors) color.Selected = color.SwatchColor.ToArgb() == canvas.InkColor.ToArgb();
            updating = false;
        }
        public void SettleAnimations() { DynamicButton.SettleAnimation(); foreach (StudioButton color in Colors) color.SettleAnimation(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); InkCanvas.Quality(e.Graphics);
            using (GraphicsPath edge = StudioTheme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 12))
            using (Pen pen = new Pen(Color.FromArgb(65, 81, 114))) e.Graphics.DrawPath(pen, edge);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if (key == (Keys.Control | Keys.Z)) { canvas.UndoInk(); return true; }
            if (key == (Keys.Control | Keys.Y)) { canvas.RedoInk(); return true; }
            return base.ProcessCmdKey(ref msg, key);
        }
        protected override void Dispose(bool disposing) { if (disposing) { title.Font.Dispose(); hint.Font.Dispose(); } base.Dispose(disposing); }
    }

    public static class DockTests
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Click(Control control)
        { typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(control, new object[] { EventArgs.Empty }); }
        static void Bounds(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (!child.Visible) continue;
                Check(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width && child.Bottom <= parent.ClientSize.Height,
                    "Dock control is clipped: " + child.GetType().Name + " / " + child.Bounds + " in " + parent.ClientSize);
                Bounds(child);
            }
        }
        static void Save(Control control, string path)
        {
            using (Bitmap image = new Bitmap(control.Width, control.Height))
            { control.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height)); image.Save(path, System.Drawing.Imaging.ImageFormat.Png); }
        }
        public static string Run(string artifacts)
        {
            using (DrawingPage page = new DrawingPage(new Bitmap(800, 500), new Rectangle(0, 0, 800, 500)))
            using (InkCanvas canvas = new InkCanvas())
            {
                canvas.SetPage(page); InkPreferences preferences = new InkPreferences();
                using (StudioToolbar studio = new StudioToolbar(canvas, preferences))
                {
                    studio.CreateControl(); studio.ToggleCompact();
                    foreach (DockSide side in Enum.GetValues(typeof(DockSide)))
                    {
                        studio.SetDockSide(side);
                        foreach (Rectangle area in new Rectangle[] { new Rectangle(100, 50, 1200, 800), new Rectangle(-600, -200, 480, 300) })
                        {
                            Rectangle dock = studio.DockBounds(area);
                            Check(area.Contains(dock), "Dock must fit entirely inside its monitor.");
                            if (area.Width > 1000) Check(side == DockSide.Top || side == DockSide.Bottom ? dock.Width > dock.Height : dock.Height > dock.Width, "Dock orientation must follow the screen edge.");
                            studio.Size = dock.Size; studio.PerformLayout(); Bounds(studio);
                        }
                        Rectangle display = studio.DockBounds(new Rectangle(0, 0, 1200, 800)); studio.Size = display.Size; studio.PerformLayout(); studio.SettleAnimations();
                        if (!String.IsNullOrEmpty(artifacts)) Save(studio, System.IO.Path.Combine(artifacts, "dock-" + side.ToString().ToLowerInvariant() + ".png"));
                    }
                    foreach (InkTool tool in Enum.GetValues(typeof(InkTool)))
                    {
                        studio.CloseToolOptions(); canvas.Tool = tool == InkTool.Pen ? InkTool.Highlighter : InkTool.Pen;
                        Click(studio.CompactTools[tool]);
                        Check(canvas.Tool == tool && studio.CompactTools[tool].Selected && !studio.ToolOptionsOpen, "First click must select, without opening options.");
                        Click(studio.CompactTools[tool]);
                        Check(studio.ToolOptionsOpen && studio.CurrentToolOptions.Tool == tool, "Second click must open options for the selected tool.");
                        ToolOptionsPanel panel = studio.CurrentToolOptions; panel.CreateControl(); panel.PerformLayout(); Bounds(panel);
                        if (tool == InkTool.Pen || tool == InkTool.Arrow) { panel.WidthSlider.Value = 13; Check(canvas.BaseWidth == 13, "Pen width option must update ink."); }
                        if (tool == InkTool.Highlighter) { panel.ExtraSlider.Value = 50; Check(canvas.HighlighterOpacity == 128, "Opacity option must update highlighter ink."); }
                        if (tool == InkTool.Eraser) { panel.WidthSlider.Value = 40; Check(canvas.EraserRadius == 20, "Eraser size option must update hit detection."); }
                        if (tool == InkTool.Laser) { panel.ExtraSlider.Value = 1000; Check(canvas.LaserLifetime == 1000, "Laser fade option must update its lifetime."); }
                        panel.SettleAnimations();
                        if (!String.IsNullOrEmpty(artifacts)) Save(panel, System.IO.Path.Combine(artifacts, "options-" + tool.ToString().ToLowerInvariant() + ".png"));
                        Click(studio.CompactTools[tool]); Check(!studio.ToolOptionsOpen, "Third click must close options.");
                    }
                    Click(studio.CompactTools[canvas.Tool]); Check(studio.ToolOptionsOpen, "Active tool should reopen its options.");
                    canvas.Tool = InkTool.Pen; Check(!studio.ToolOptionsOpen, "Switching tools must close old options.");
                    studio.ToggleCompact(); studio.Size = new Size(820, studio.PreferredHeight(820)); studio.PerformLayout(); Bounds(studio);
                    Check(!studio.Collapsed && studio.ToolButtons[InkTool.Pen].Visible, "Expanding must restore the full toolbar.");
                }
                Check(preferences.DockSide == DockSide.Right && preferences.EraserRadius == 20 && preferences.LaserLifetime == 1000 && preferences.HighlighterOpacity == 128,
                    "Placement and tool options must persist during the session.");
            }
            return "PASS: all four dock edges, small/negative-coordinate monitors, compact selection, second-click tool options, live option changes, expansion, retained settings.";
        }
    }
}
