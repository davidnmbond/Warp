using SkiaSharp;

namespace Warp.Game;

internal class Sprite
{
	private readonly uint[] _pixels;

	internal int FrameCount { get; }

	internal Sprite(SKBitmap bitmap, int tileX, int tileY, int frameCount)
	{
		_pixels = new uint[32 * 32 * frameCount];

		for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
		{
			var xOffset = tileX * 32 + frameIndex * 32;
			for (int y = 0; y < 32; y++)
			{
				var yOffset = tileY * 32;
				for (int x = 0; x < 32; x++)
				{
					uint tileColour = MakePixel(bitmap.GetPixel(x + xOffset, y + yOffset));
					_pixels[x + y * 32 + frameIndex * 32 * 32] = tileColour;
				}
			}
		}

		FrameCount = frameCount;
	}

	private static uint MakePixel(SKColor colour)
		=> (uint)((colour.Alpha << 24) | (colour.Blue << 16) | (colour.Green << 8) | colour.Red);

	internal uint[] GetFrame(int frameIndex)
	{
		var frame = new uint[32 * 32];
		Array.Copy(_pixels, frameIndex * 32 * 32, frame, 0, 32 * 32);
		return frame;
	}
}