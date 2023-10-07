namespace Warp.Game
{
	public class Renderer
	{
		private const int WarpSize = 10;

		private readonly uint[] _buffer;
		private readonly MorphSpec[] _morphMap;
		public int _viewportHeight;
		public int _viewportWidth;
		private int t;

		public Renderer(int viewportWidth, int viewportHeight)
		{
			_viewportWidth = viewportWidth;
			_viewportHeight = viewportHeight;
			_buffer = new uint[_viewportHeight * _viewportWidth];
			_morphMap = GetMorphMap();
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
				for (int x = 0; x < _viewportWidth; x++)
				{
					var xPositionVector = (float)(x - halfViewportWidth) / halfViewportWidth;
					var yPositionVector = (float)(y - _viewportHeight) / _viewportHeight;

					var xOffset = Math.Pow(WarpSize * xPositionVector, 3);
					var yOffset = Math.Pow(WarpSize * yPositionVector, 4);
					morphMap[bufferIndex++] = new MorphSpec
					{
						XPixelOffset = (int)xOffset,
						YPixelOffset = (int)yOffset
					};
				}
			}

			return morphMap;
		}

		internal unsafe uint[] UpdateFrameBuffer()
		{
			var bufferIndex = 0;
			t += 4;
			for (int y = 0; y < _viewportHeight; y++)
			{
				for (int x = 0; x < _viewportWidth; x++)
				{
					var warpSpec = _morphMap[bufferIndex];
					var warpedX = x + warpSpec.XPixelOffset + t;
					var warpedY = y + warpSpec.YPixelOffset;
					_buffer[bufferIndex++] = ((warpedX >> 6) & 1) == ((warpedY >> 6) & 1)
						? 0xff000000
						: 0xffffffff;
				}
			}

			return _buffer;
		}
	}
}