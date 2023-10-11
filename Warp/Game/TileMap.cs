
namespace Warp.Game;

internal class TileMap(Uri uri, SpriteMap spriteMap)
{
	private const int MinLineLength = 60;

	internal Sprite[,] Background { get; private set; } = new Sprite[0, 0];

	internal Sprite?[,] Foreground { get; private set; } = new Sprite?[0, 0];

	internal bool IsLoaded { get; private set; }

	internal int LineCount { get; private set; }

	internal int LineLength { get; private set; }

	internal async Task LoadAsync(HttpClient httpClient, CancellationToken cancellationToken)
	{
		var response = await httpClient!.GetAsync(uri, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to load map from {uri}");
		}

		var mapText = await response
			.Content
			.ReadAsStringAsync(cancellationToken);

		var lines = mapText
			.Split(Environment.NewLine)
			.Select(x => x.TrimEnd());

		var lineIndex = 0;
		foreach (var line in lines)
		{
			if (lineIndex == 0)
			{
				if (line.Length < MinLineLength)
				{
					throw new Exception($"Map lines must be at least {MinLineLength} characters long");
				}

				if (line.Length % 2 != 0)
				{
					throw new Exception("Map lines must be even length");
				}

				LineLength = line.Length;
				LineCount = lines.Count();

				Background = new Sprite[LineCount, LineLength / 2];
				Foreground = new Sprite[LineCount, LineLength / 2];
			}
			else if (line.Length != LineLength)
			{
				throw new Exception("Map lines must be the same length");
			}
			// Line length is OK

			var i = 0;
			var x = 0;
			while (i < line.Length)
			{
				var backgroundCharacter = line[i++];
				var foregroundCharacter = line[i++];
				try
				{
					Background![lineIndex, x] = spriteMap[backgroundCharacter];
					Foreground![lineIndex, x++] = foregroundCharacter == '.' ? null : spriteMap[foregroundCharacter];
				}
				catch (Exception ex)
				{
					throw new Exception($"Failed to parse map line {lineIndex} at character {i}", ex);
				}
			}

			lineIndex++;
		}

		IsLoaded = true;
	}
}
