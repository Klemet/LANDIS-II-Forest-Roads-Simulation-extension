//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;

namespace Landis.Extension.ForestRoadsSimulation
{
	public class UIntPixel : Pixel
	{
		public Band<uint> MapCode = "The numeric code for each raster cell";

		public UIntPixel()
		{
			SetBands(MapCode);
		}
	}
}