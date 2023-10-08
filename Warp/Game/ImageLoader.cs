using SkiaSharp;

namespace Warp.Game;

internal class ImageLoader(HttpClient httpClient)
{
	internal async Task<SKBitmap> LoadAsync(Uri uri, CancellationToken cancellationToken)
	{
		var response = await httpClient!.GetAsync(uri, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to load image from {uri}");
		}

		var imageBytes = await response
			.Content
			.ReadAsByteArrayAsync(cancellationToken);

		return SKBitmap.Decode(imageBytes);
	}
}
