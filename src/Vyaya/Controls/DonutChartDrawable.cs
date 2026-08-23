using Microsoft.Maui.Graphics;
using Vyaya.Models;

namespace Vyaya.Controls;

public class DonutChartDrawable : IDrawable
{
    public List<ChartSegment> Segments { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string CenterText { get; set; } = string.Empty;
    public string CenterSubtext { get; set; } = "Total Spending";

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        float size = Math.Min(width, height);
        float centerX = width / 2f;
        float centerY = height / 2f;

        float strokeWidth = size * 0.16f;
        float radius = (size - strokeWidth - 16) / 2f;

        if (radius <= 0)
        {
            canvas.RestoreState();
            return;
        }

        // Background Track Ring
        canvas.StrokeColor = Color.FromArgb("#2633333E");
        canvas.StrokeSize = strokeWidth;
        canvas.DrawCircle(centerX, centerY, radius);

        var activeSegments = Segments?.Where(s => s.Percentage > 0).ToList() ?? new();

        if (activeSegments.Count == 0 || TotalAmount <= 0)
        {
            // Empty State Ring
            canvas.StrokeColor = Color.FromArgb("#338E8E93");
            canvas.StrokeSize = strokeWidth;
            canvas.DrawCircle(centerX, centerY, radius);

            // Center Empty Text
            canvas.FontColor = Color.FromArgb("#8E8E93");
            canvas.FontSize = 14;
            canvas.DrawString("No Spending", centerX, centerY - 6, HorizontalAlignment.Center);
            canvas.FontSize = 11;
            canvas.DrawString("This Month", centerX, centerY + 14, HorizontalAlignment.Center);

            canvas.RestoreState();
            return;
        }

        if (activeSegments.Count == 1)
        {
            // Single segment (100% of spending in this category)
            var single = activeSegments[0];
            Color segColor;
            try
            {
                segColor = Color.FromArgb(single.ColorHex);
            }
            catch
            {
                segColor = Color.FromArgb("#0A84FF");
            }

            canvas.StrokeColor = segColor;
            canvas.StrokeSize = strokeWidth;
            canvas.DrawCircle(centerX, centerY, radius);
        }
        else
        {
            // Multi-segment: Draw each category with its distinct vibrant color
            float startAngle = -90f; // Start at 12 o'clock
            foreach (var segment in activeSegments)
            {
                float sweepAngle = (float)(segment.Percentage / 100.0 * 360.0);
                if (sweepAngle < 2f) sweepAngle = 2f;

                Color segColor;
                try
                {
                    segColor = Color.FromArgb(segment.ColorHex);
                }
                catch
                {
                    segColor = Color.FromArgb("#0A84FF");
                }

                canvas.StrokeColor = segColor;
                canvas.StrokeSize = strokeWidth;
                canvas.StrokeLineCap = LineCap.Round;

                // Draw arc for this category with a small gap for a refined, modern look
                float gap = 2.5f;
                float arcStart = startAngle + gap;
                float arcEnd = startAngle + sweepAngle - gap;

                if (arcEnd <= arcStart)
                    arcEnd = arcStart + 1f;

                canvas.DrawArc(
                    centerX - radius,
                    centerY - radius,
                    radius * 2,
                    radius * 2,
                    arcStart,
                    arcEnd,
                    true,
                    false
                );

                startAngle += sweepAngle;
            }
        }

        // Center Content (Total Amount + Subtext)
        canvas.FontColor = Color.FromArgb("#FFFFFF");
        canvas.FontSize = 18;
        var displayCenter = !string.IsNullOrEmpty(CenterText) ? CenterText : $"₹{TotalAmount:N0}";
        canvas.DrawString(displayCenter, centerX, centerY - 4, HorizontalAlignment.Center);

        canvas.FontColor = Color.FromArgb("#8E8E93");
        canvas.FontSize = 11;
        canvas.DrawString(CenterSubtext, centerX, centerY + 14, HorizontalAlignment.Center);

        canvas.RestoreState();
    }
}
