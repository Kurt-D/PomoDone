using Microsoft.Maui.Graphics;

namespace PomoDone.Controls;

// Display-only countdown ring for the Timer page: a faint track circle plus an
// amber arc whose sweep reflects Progress (1 = full, 0 = empty). Pure
// presentation — it renders the Progress the ViewModel derives from the
// wall-clock timer (§3.1); it contains NO timing logic and stores nothing.
public class RingDrawable : IDrawable
{
    // Remaining fraction, 0..1. The hosting page sets this and calls Invalidate.
    public double Progress { get; set; } = 1;

    // Colors match the Vanta dark tokens (the ring can't consume XAML resources
    // directly; kept in sync with Colors.xaml VantaRingTrack / VantaAccent).
    public Color TrackColor { get; set; } = Color.FromArgb("#222222");
    public Color ProgressColor { get; set; } = Color.FromArgb("#F59E0B");
    public float Thickness { get; set; } = 18f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Inset by half the stroke so the ring isn't clipped at the edges.
        var inset = Thickness / 2f + 2f;
        var rect = new RectF(
            dirtyRect.Left + inset,
            dirtyRect.Top + inset,
            dirtyRect.Width - inset * 2f,
            dirtyRect.Height - inset * 2f);

        canvas.StrokeSize = Thickness;
        canvas.StrokeLineCap = LineCap.Round;

        // Full track.
        canvas.StrokeColor = TrackColor;
        canvas.DrawEllipse(rect);

        // Remaining-time arc: start at the top (12 o'clock = 90°) and sweep
        // clockwise by the remaining fraction.
        var fraction = (float)Math.Clamp(Progress, 0, 1);
        if (fraction > 0f)
        {
            var sweep = 360f * fraction;
            canvas.StrokeColor = ProgressColor;
            canvas.DrawArc(rect, 90f, 90f - sweep, true, false);
        }
    }
}
