using SkiaSharp;

namespace Warp.Game
{
	internal class SpriteMap(Uri uri)
	{
		public SKBitmap Bitmap { get; private set; }

		internal Dictionary<string, Sprite> GetSprites()
			=> new()
			{
				{ "Player", new Sprite(Bitmap, 0, 0, 1) },
			};

		internal async Task LoadAsync(ImageLoader imageLoader, CancellationToken cancellationToken)
		{
			Bitmap = await imageLoader
				.LoadAsync(uri, cancellationToken);
		}
	}
}
