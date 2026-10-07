using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using ScreenInk;

// Real canvas mouse handlers, generated lesson content, no screen capture.
public static class DemoReplay
{
    static InkCanvas canvas;
    static StudioToolbar toolbar;
    static int frame;
    static string output, caption;
    static void Mouse(string name, int x, int y, MouseButtons button)
    {
        typeof(InkCanvas).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(canvas, new object[] { new MouseEventArgs(button, 1, x, y, 0) });
    }
    static void Frame(int copies)
    {
        Application.DoEvents(); toolbar.SettleAnimations();
        using (Bitmap image = new Bitmap(1100, 760))
        using (Bitmap surface = new Bitmap(1100, 700))
        using (Bitmap bar = new Bitmap(toolbar.Width, toolbar.Height))
        using (Graphics g = Graphics.FromImage(image))
        using (Font font = new Font("Segoe UI", 16, FontStyle.Bold))
        {
            canvas.DrawToBitmap(surface, new Rectangle(0, 0, 1100, 700));
            toolbar.DrawToBitmap(bar, new Rectangle(0, 0, bar.Width, bar.Height));
            g.Clear(Color.FromArgb(22, 27, 39)); g.DrawImageUnscaled(surface, 0, 0);
            g.DrawImageUnscaled(bar, (1100 - bar.Width) / 2, 12);
            g.DrawString(caption, font, Brushes.White, 40, 714);
            for (int i = 0; i < copies; i++) image.Save(Path.Combine(output, "frame-" + (frame++).ToString("D5") + ".png"), ImageFormat.Png);
        }
    }
    static void Select(InkTool tool, string text, Color color, float width)
    {
        canvas.Tool = tool; canvas.InkColor = color; canvas.BaseWidth = width;
        canvas.RefreshSettings(); caption = text; Frame(8);
    }
    static void Stroke(int x1, int y1, int x2, int y2, bool curve)
    {
        Mouse("OnMouseDown", x1, y1, MouseButtons.Left);
        for (int i = 1; i <= 28; i++)
        {
            float t = i / 28f;
            Mouse("OnMouseMove", (int)(x1+(x2-x1)*t), (int)(y1+(y2-y1)*t-(curve ? Math.Sin(t*Math.PI)*25 : 0)), MouseButtons.Left);
            if (i % 2 == 0) Frame(1);
        }
        Mouse("OnMouseUp", x2, y2, MouseButtons.Left); Frame(8);
    }
    public static void Run(string path)
    {
        output = path; Directory.CreateDirectory(path); frame = 0;
        using (Bitmap background = new Bitmap(1100, 700))
        {
            using (Graphics g = Graphics.FromImage(background))
            using (Font title = new Font("Segoe UI", 27, FontStyle.Bold))
            using (Font body = new Font("Segoe UI", 17))
            using (Font small = new Font("Segoe UI", 12))
            using (Pen border = new Pen(Color.FromArgb(172,185,207), 2))
            {
                g.Clear(Color.FromArgb(246,248,252));
                g.DrawString("A fraction is a part of a whole", title, Brushes.MidnightBlue, 66, 300);
                g.DrawString("Shade 3 of 4 equal parts. What fraction is shaded?", body, Brushes.SlateGray, 68, 362);
                for(int i=0;i<4;i++) { Rectangle r = new Rectangle(90+i*95,440,95,110); if(i<3)g.FillRectangle(Brushes.LightSteelBlue,r);g.DrawRectangle(border,r); }
                g.DrawString("3 / 4 = 0.75 = 75%", title, Brushes.MidnightBlue, 574, 461);
                g.DrawString("Three shaded parts", body, Brushes.SlateGray, 90, 590);
                g.DrawString("Same value, three ways to write it", small, Brushes.SlateGray, 582, 590);
                g.DrawString("SCREENINK  /  Scripted tool replay on a generated lesson slide", small, Brushes.SlateGray, 68, 663);
            }
            using (DrawingPage page = new DrawingPage((Bitmap)background.Clone(), new Rectangle(0,0,1100,700)))
            using (InkCanvas c = new InkCanvas())
            using (StudioToolbar t = new StudioToolbar(c, new InkPreferences()))
            {
                canvas = c; toolbar = t; c.Size = new Size(1100,700); c.CreateControl(); c.SetPage(page);
                t.Size = new Size(860,t.PreferredHeight(860)); t.CreateControl(); t.SetHistory(0,1);
                caption = "Start with a lesson, slide, document, or blank background."; Frame(16);
                Select(InkTool.Pen,"Pen: underline the key idea.", Color.FromArgb(115,160,255),5);
                Stroke(72,350,728,353,false);
                Select(InkTool.Rectangle,"Rectangle: frame the three shaded parts.",Color.FromArgb(115,160,255),3);
                Stroke(84,434,379,556,false);
                Select(InkTool.Arrow,"Arrow pen: connect the diagram to its fraction.",Color.FromArgb(245,100,119),5);
                Stroke(403,493,553,488,true);
                Select(InkTool.Highlighter,"Highlighter: emphasize the equivalent percentage.",Color.FromArgb(255,211,67),12);
                Stroke(813,492,896,492,false);
                Select(InkTool.Pen,"A stray mark? You can remove the whole stroke.",Color.FromArgb(189,146,255),5);
                Stroke(820,560,988,566,true);
                Select(InkTool.Eraser,"Eraser: one click removes the stray stroke.",Color.FromArgb(245,100,119),5);
                Mouse("OnMouseMove",900,540,MouseButtons.None);Frame(6);
                Mouse("OnMouseDown",900,540,MouseButtons.Left);Mouse("OnMouseUp",900,540,MouseButtons.Left);Frame(12);
                if(page.Strokes.Count != 4) throw new Exception("Demo eraser must remove exactly the stray stroke.");
                caption="Undo: restore an erased stroke with Ctrl+Z.";c.UndoInk();Frame(14);
                if(page.Strokes.Count != 5) throw new Exception("Demo undo must restore the erased stroke.");
                caption="Redo: apply the correction again with Ctrl+Y.";c.RedoInk();Frame(14);
                t.ToggleCompact();t.Size=new Size(860,t.PreferredHeight(860));
                caption="Compact dock: keep the lesson in view. Toggle with Ctrl+Tab.";Frame(18);
                caption="Save PNG: keep the lesson and ink, without the toolbar.";Frame(18);
                using(Bitmap export=page.Export()) export.Save(Path.Combine(output,"annotated-example.png"),ImageFormat.Png);
                Console.WriteLine("PASS: demo drawing, eraser, undo, redo, compact toolbar, and PNG export ("+frame+" frames).");
            }
        }
    }
}
