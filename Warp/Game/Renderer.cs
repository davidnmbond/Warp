namespace Warp.Game;

internal class Renderer
{
	private const int WarpSize = 10;

	private readonly uint[] _buffer;
	private readonly MorphSpec[] _morphMap;
	private readonly uint[] _playerFrame;
	public int _viewportHeight;
	public int _viewportWidth;

	public Renderer(SpriteMap spriteMap, int viewportWidth, int viewportHeight)
	{
		_viewportWidth = viewportWidth;
		_viewportHeight = viewportHeight;
		_buffer = new uint[_viewportHeight * _viewportWidth];
		_morphMap = GetMorphMap();
		_playerFrame = spriteMap['p'].GetFrame(0);
	}

	private MorphSpec[] GetMorphMap()
	{
		// This morph map is a 2D array of MorphSpecs.
		// Each MorphSpec contains an X and Y value.
		// X and Y range from -WarpSize to WarpSize, linearly from the centre of the viewport.
		var morphMap = new MorphSpec[_viewportHeight * _viewportWidth];
		int halfViewportWidth = _viewportWidth / 2;
		int halfViewportHeight = _viewportHeight / 2;
		var bufferIndex = 0;
		for (int y = 0; y < _viewportHeight; y++)
		{
			var yPositionVector = (float)(y - halfViewportHeight) / halfViewportHeight;
			for (int x = 0; x < _viewportWidth; x++)
			{
				var xPositionVector = (float)(x - halfViewportWidth) / halfViewportWidth;

				var xOffset = Math.Pow(WarpSize * xPositionVector, 3);
				var intXOffset = xOffset < 0 ? (int)(xOffset - 1) : (int)xOffset;
				var xOffsetFraction = (int)((xOffset - Math.Truncate(xOffset)) * 100);

				var yOffset = Math.Pow(WarpSize * yPositionVector, 3);
				var intYOffset = yOffset < 0 ? (int)(yOffset - 1) : (int)yOffset;
				var yOffsetFraction = (int)((yOffset - Math.Truncate(yOffset)) * 100);

				morphMap[bufferIndex++] = new MorphSpec
				{
					XPixelOffset = intXOffset + 1000,
					YPixelOffset = intYOffset + 1500,
					X0BytePart = xOffsetFraction,
					X1BytePart = 100 - xOffsetFraction,
					Y0BytePart = yOffsetFraction,
					Y1BytePart = 100 - yOffsetFraction
				};
			}
		}

		return morphMap;
	}

	internal unsafe uint[] UpdateFrameBuffer(
		TileMap tileMap,
		int xPixelOffset,
		int yPixelOffset
		)
	{
		var bufferIndex = 0;
		for (int y = 0; y < _viewportHeight; y++)
		{
			for (int x = 0; x < _viewportWidth; x++)
			{
				var warpSpec = _morphMap[bufferIndex];
				var warpedX = x + warpSpec.XPixelOffset + xPixelOffset;
				var warpedY = y + warpSpec.YPixelOffset + yPixelOffset;

				var warpedTileX = warpedX / 32;
				var warpedTileY = warpedY / 32;

				if (warpedTileX < 0 || warpedTileX >= tileMap.LineLength || warpedTileY < 0 || warpedTileY >= tileMap.LineCount)
				{
					_buffer[bufferIndex++] = ((warpedX >> 6) & 1) == ((warpedY >> 6) & 1)
						? 0xff000000
						: 0xffffffff; // Other (checkerboard);
					continue;
				}

				var backgroundTile = tileMap.Background[warpedTileY, warpedTileX];

				_buffer[bufferIndex++] = backgroundTile.Pixels[warpedX % 32 + (warpedY % 32) * 32];
			}
		}

		// Draw player
		var midViewPortXStart = _viewportWidth / 2 - 16;
		var midViewPortYStart = _viewportHeight / 2 - 16;

		var playerPixelIndex = 0;

		for (int playerPixelY = midViewPortYStart; playerPixelY < midViewPortYStart + 32; playerPixelY++)
		{
			for (int playerPixelX = midViewPortXStart; playerPixelX < midViewPortXStart + 32; playerPixelX++)
			{
				uint pixel = _playerFrame[playerPixelIndex++];
				// Is this a transparent pixel?
				if ((pixel & 0xff000000) == 0)
				{
					// Yes, skip it
					continue;
				}

				_buffer[playerPixelY * _viewportWidth + playerPixelX] = pixel;
			}
		}

		return _buffer;
	}
}