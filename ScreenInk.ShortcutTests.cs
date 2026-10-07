using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace ScreenInk
{
    public static class ShortcutTests
    {
        static void Mouse(InkCanvas canvas, string name, MouseButtons button, int x, int y)
        { typeof(InkCanvas).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(canvas, new object[] { new MouseEventArgs(button, 1, x, y, 0) }); }
        static void Key(InkForm form, Keys key)
        { typeof(InkForm).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { new Message(), key }); }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        public static string Run()
        {
            using (Stroke stroke = new Stroke(InkTool.Pen, Color.Blue, 8, new PointF(50, 200), 0))
            {
                for (int i = 1; i <= 1000; i++) stroke.Append(new PointF(50 + i, 200 + (float)Math.Sin(i * 0.1) * 50), i * 12, true);
                System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                stroke.Hit(new PointF(550, 235), 12);
                for (int i = 0; i < 100; i++) stroke.Hit(new PointF(550, 235), 12);
                timer.Stop();
                Console.WriteLine("Eraser hit-test duration: " + timer.ElapsedMilliseconds + " ms");
                Check(timer.ElapsedMilliseconds < 250, "Eraser hit-testing must not block the UI after drawing a long stroke.");
            }
            using (Stroke rectangle = new Stroke(InkTool.Rectangle, Color.Blue, 8, new PointF(10, 10), 0))
            {
                rectangle.SetShapeEndpoint(new PointF(200, 150), false);
                Check(!rectangle.Hit(new PointF(100, 80), 12), "Eraser must ignore a rectangle's empty interior.");
                Check(rectangle.Hit(new PointF(10, 80), 12), "Eraser must hit the rectangle's edge.");
                rectangle.SetShapeEndpoint(new PointF(400, 300), false);
                Check(rectangle.Hit(new PointF(400, 80), 12), "Geometry cache must update after editing a shape.");
            }
            foreach (bool compact in new bool[] { false, true })
            foreach (bool releaseFirst in new bool[] { true, false })
            using (DrawingPage page = new DrawingPage(new Bitmap(1400, 800), new Rectangle(0, 0, 1400, 800)))
            using (InkCanvas canvas = new InkCanvas())
            using (InkForm form = new InkForm())
            using (Bitmap render = new Bitmap(1400, 800))
            {
                canvas.Size = new Size(1400, 800); canvas.SetPage(page); form.Canvas = canvas;
                using (StudioToolbar studio = new StudioToolbar(canvas, new InkPreferences()))
                {
                    studio.Size = new Size(820, studio.PreferredHeight(820)); studio.CreateControl();
                    if (compact) studio.ToggleCompact();
                    for (int round = 0; round < 4; round++)
                    {
                        Key(form, Keys.P);
                        Mouse(canvas, "OnMouseDown", MouseButtons.Left, 50, 200);
                        for (int i = 1; i <= 300; i++) Mouse(canvas, "OnMouseMove", MouseButtons.Left, 50 + i * 4, 200 + (int)(Math.Sin(i * 0.1) * 50));
                        if (releaseFirst) Mouse(canvas, "OnMouseUp", MouseButtons.Left, 1250, 150);
                        Key(form, Keys.E);
                        Check(canvas.Tool == InkTool.Eraser && page.Strokes.Count == 1, "E must switch safely and preserve the drawn stroke.");
                        Check(studio.ToolButtons[InkTool.Eraser].Selected && studio.CompactTools[InkTool.Eraser].Selected, "Eraser selection must update both toolbars.");
                        for (int i = 0; i < 12; i++)
                        {
                            Mouse(canvas, "OnMouseMove", MouseButtons.None, 120 + i * 70, 230);
                            canvas.DrawToBitmap(render, new Rectangle(0, 0, 1400, 800));
                        }
                        Mouse(canvas, "OnMouseDown", MouseButtons.Left, 50, 200);
                        Mouse(canvas, "OnMouseUp", MouseButtons.Left, 50, 200);
                        Check(page.Strokes.Count == 0, "The shortcut-selected eraser must erase the stroke.");
                        canvas.UndoInk(); Check(page.Strokes.Count == 1, "Erasing must remain undoable.");
                        canvas.ClearInk();
                    }
                }
            }
            return "PASS: draw -> E -> hover/erase, repeated in full/compact modes, before/after mouse release; drawing and undo preserved.";
        }
    }
}
