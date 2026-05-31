namespace SyncChain.Desktop.Services;

public static class SigninBackground
{
	private static readonly string[] CandidateRelativePaths =
	[
		"background_signin.jpg",
		"background_signin.scale-100.jpg",
		Path.Combine("sampleUI", "background_signin.jpg"),
		Path.Combine("app", "SyncChain.Desktop", "Resources", "Images", "background_signin.jpg")
	];

	public static string? ImagePath { get; } = ResolvePath();

	public static ImageSource? CreateSource()
	{
		return ImagePath is null
			? null
			: ImageSource.FromStream(() => File.OpenRead(ImagePath));
	}

	private static string? ResolvePath()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);

		while (current is not null)
		{
			foreach (var relativePath in CandidateRelativePaths)
			{
				var fullPath = Path.Combine(current.FullName, relativePath);
				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}

			current = current.Parent;
		}

		return null;
	}
}
