//  Original authors:    Robert M. Scheller, James B. Domingo
// Re-used in this project by Clément Hardy

using Landis.SpatialModeling;

namespace Landis.Extension.ForestRoadsSimulation
{
	public class BytePixel : Pixel
	{
		public Band<byte> MapCode = "The numeric code for each raster cell";

		public BytePixel()
		{
			SetBands(MapCode);
		}
	}
}
