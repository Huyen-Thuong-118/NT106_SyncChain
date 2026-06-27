using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Charts;

public sealed class OrderTrendChartDrawable : IDrawable
{
	public IReadOnlyList<OrderTrendPoint> Points { get; init; } = Array.Empty<OrderTrendPoint>();

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var chart = new RectF(28, 18, dirtyRect.Width - 48, dirtyRect.Height - 58);
		if (chart.Width <= 0 || chart.Height <= 0)
			return;

		DrawGrid(canvas, chart);

		if (Points.Count == 0)
			return;

		var max = Math.Max(1, Points.Max(x => Math.Max(x.Completed, x.Processing)));
		DrawSeries(canvas, chart, max, Points.Select(x => x.Completed).ToList(), Color.FromArgb("#213145"));
		DrawSeries(canvas, chart, max, Points.Select(x => x.Processing).ToList(), Color.FromArgb("#D7ECFB"));
		DrawLabels(canvas, chart);
	}

	private static void DrawGrid(ICanvas canvas, RectF chart)
	{
		canvas.StrokeColor = Color.FromArgb("#C7CCD2");
		canvas.StrokeSize = 1;

		for (var i = 0; i < 4; i++)
		{
			var y = chart.Top + chart.Height * i / 3f;
			canvas.DrawLine(chart.Left, y, chart.Right, y);
		}
	}

	private void DrawLabels(ICanvas canvas, RectF chart)
	{
		canvas.FontColor = Color.FromArgb("#4A4F57");
		canvas.FontSize = 12;
		canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

		for (var i = 0; i < Points.Count; i++)
		{
			var x = XAt(chart, i, Points.Count);
			canvas.DrawString(
				Points[i].Label,
				x - 34,
				chart.Bottom + 18,
				68,
				22,
				HorizontalAlignment.Center,
				VerticalAlignment.Center);
		}
	}

	private static void DrawSeries(ICanvas canvas, RectF chart, int max, IReadOnlyList<int> values, Color color)
	{
		if (values.Count == 0)
			return;

		var path = new PathF();
		for (var i = 0; i < values.Count; i++)
		{
			var x = XAt(chart, i, values.Count);
			var y = chart.Bottom - chart.Height * values[i] / max;
			if (i == 0)
				path.MoveTo(x, y);
			else
				path.LineTo(x, y);
		}

		canvas.StrokeColor = color;
		canvas.StrokeSize = 4;
		canvas.DrawPath(path);

		canvas.FillColor = color;
		for (var i = 0; i < values.Count; i++)
		{
			var x = XAt(chart, i, values.Count);
			var y = chart.Bottom - chart.Height * values[i] / max;
			canvas.FillCircle(x, y, 5);
		}
	}

	private static float XAt(RectF chart, int index, int count)
	{
		return count <= 1
			? chart.Left + chart.Width / 2f
			: chart.Left + chart.Width * index / (count - 1);
	}
}
