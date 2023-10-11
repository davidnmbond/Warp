namespace Warp.Game
{
	internal class SpriteMap(Uri uri) : Dictionary<char, Sprite>
	{
		internal async Task LoadAsync(ImageLoader imageLoader, CancellationToken cancellationToken)
		{
			using var Bitmap = await imageLoader
				.LoadAsync(uri, cancellationToken);

			// Row 0
			this['p'] = new Sprite(Bitmap, "Player", 0, 0, 1);

			// Row 1
			this['l'] = new Sprite(Bitmap, "SkyLandLeft", 0, 1, 1);
			this['m'] = new Sprite(Bitmap, "SkyLandMid", 1, 1, 1);
			this['r'] = new Sprite(Bitmap, "SkyLandRight", 2, 1, 1);
			this['\''] = new Sprite(Bitmap, "Sky", 3, 1, 1);
			this['c'] = new Sprite(Bitmap, "Cloud", 4, 1, 1);
			// Row 2
			this['L'] = new Sprite(Bitmap, "LandLeft", 0, 2, 1);
			this['M'] = new Sprite(Bitmap, "LandMid", 1, 2, 1);
			this['R'] = new Sprite(Bitmap, "LandRight", 2, 2, 1);

			// Row 3
			this['A'] = new Sprite(Bitmap, "SkyLandLeft", 0, 3, 1);
			this['B'] = new Sprite(Bitmap, "SkyLandMid", 1, 3, 1);
			this['#'] = new Sprite(Bitmap, "SkyLandMid", 1, 3, 1);
			this['C'] = new Sprite(Bitmap, "SkyLandRight", 2, 3, 1);

			// Row 7
			this[' '] = new Sprite(Bitmap, "Cross", 0, 7, 1);
		}
	}
}
