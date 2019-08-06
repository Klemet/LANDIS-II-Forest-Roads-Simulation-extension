//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
using System.IO;
using System.Collections.Generic;

namespace Landis.Extension.ForestRoadsSimulation
{
	class MapReader
	{
		public static void ReadMap(string path)
		{
			IInputRaster<UIntPixel> map;

			try
			{
				map = PlugIn.ModelCore.OpenRaster<UIntPixel>(path);
			}
			catch (FileNotFoundException)
			{
				string mesg = string.Format("Error: The file {0} does not exist", path);
				throw new System.ApplicationException(mesg);
			}

			if (map.Dimensions != PlugIn.ModelCore.Landscape.Dimensions)
			{
				string mesg = string.Format("Error: The input map {0} does not have the same dimension (row, column) as the ecoregions map", path);
				throw new System.ApplicationException(mesg);
			}

			using (map)
			{
				UIntPixel pixel = map.BufferPixel;
				foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
				{
					map.ReadBufferPixel();
					int mapCode = (int)pixel.MapCode.Value;

					SiteVars.RoadsInLandscape[site] = new RoadType(mapCode);
				}
			}
		}
	}
}

