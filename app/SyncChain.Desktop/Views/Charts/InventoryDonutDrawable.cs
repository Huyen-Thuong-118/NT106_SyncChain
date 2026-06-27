using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Charts;

public sealed class InventoryDonutDrawable : IDrawable
{
	public IReadOnlyList<InventorySlice> Slices { get; init; } = Array.Empty<InventorySlice>();
	public Color CenterColor { get; init; } = Color.FromArgb("#213145");

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var total = Slices.Sum(x => x.Quantity);
		if (total <= 0)
			return;

		var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
		var centerX = dirtyRect.Left + dirtyRect.Width / 2f;
		var centerY = dirtyRect.Top + dirtyRect.Height / 2f;
		var radius = size / 2f;
		var innerRadius = radius * 0.58f;
		var startAngle = -90d;

		foreach (var slice in Slices)
		{
			var sweep = slice.Quantity * 360d / total;
			if (sweep <= 0)
				continue;

			canvas.FillColor = slice.Color;
			canvas.FillPath(BuildSlice(centerX, centerY, radius, startAngle, sweep));
			startAngle += sweep;
		}

		canvas.FillColor = CenterColor;
		canvas.FillCircle(centerX, centerY, innerRadius);
	}

	private static PathF BuildSlice(float centerX, float centerY, float radius, double startAngle, double sweep)
	{
		const int minSegments = 8;
		var segments = Math.Max(minSegments, (int)Math.Ceiling(sweep / 6d));
		var path = new PathF();
		path.MoveTo(centerX, centerY);

		for (var i = 0; i <= segments; i++)
		{
			var angle = (startAngle + sweep * i / segments) * Math.PI / 180d;
			var x = centerX + radius * (float)Math.Cos(angle);
			var y = centerY + radius * (float)Math.Sin(angle);
			path.LineTo(x, y);
		}

		path.Close();
		return path;
	}
}
