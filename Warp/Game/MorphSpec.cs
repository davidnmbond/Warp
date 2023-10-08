namespace Warp.Game
{
	public record MorphSpec
	{
		public required int XPixelOffset { get; set; }
		public required int YPixelOffset { get; set; }
		public int X0BytePart { get; internal set; }
		public int X1BytePart { get; internal set; }
		public int Y0BytePart { get; internal set; }
		public int Y1BytePart { get; internal set; }
	}
}
