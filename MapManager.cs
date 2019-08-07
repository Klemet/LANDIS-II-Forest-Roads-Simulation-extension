//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
using System.IO;
using System.Collections.Generic;
using Landis.Core;

namespace Landis.Extension.ForestRoadsSimulation
{
	class MapManager
	{
		// Cette fonction lit la carte qui se trouve à l'endroit donné par "Path".
		// Elle va mettre cette carte dans un dictionnaire contenu dans la classe "SiteVars".
		public static void ReadMap(string path)
		{
			IInputRaster<UIntPixel> map;

			// We try to open the map; if it fails, we raise an exception
			try
			{
				map = PlugIn.ModelCore.OpenRaster<UIntPixel>(path);
			}
			catch (FileNotFoundException)
			{
				string mesg = string.Format("Error: The file {0} does not exist", path);
				throw new System.ApplicationException(mesg);
			}

			// We check if the map has the same dimensions has the main LANDIS-II landscape
			if (map.Dimensions != PlugIn.ModelCore.Landscape.Dimensions)
			{
				string mesg = string.Format("Error: The input map {0} does not have the same dimension (row, column) as the ecoregions map", path);
				throw new System.ApplicationException(mesg);
			}

			// We read the map by looking at each pixel one by one, and putting the value of this pixel
			// in the correct site.
			// CAREFUL : Pixels and sites are read by reading a column from top to bottom, and the column from left to right.
			// NOT like a text from left to right AND top to bottom ; more rather like the japanese writing, top to bottom AND left to right.
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

		public static int ReadAPixelOfTheMap(string path, Site site)
		{
			IInputRaster<UIntPixel> map;

			// We try to open the map; if it fails, we raise an exception
			try
			{
				map = PlugIn.ModelCore.OpenRaster<UIntPixel>(path);
			}
			catch (FileNotFoundException)
			{
				string mesg = string.Format("Error: The file {0} does not exist", path);
				throw new System.ApplicationException(mesg);
			}

			// We check if the map has the same dimensions has the main LANDIS-II landscape
			if (map.Dimensions != PlugIn.ModelCore.Landscape.Dimensions)
			{
				string mesg = string.Format("Error: The input map {0} does not have the same dimension (row, column) as the ecoregions map", path);
				throw new System.ApplicationException(mesg);
			}

			// We read the given pixel of the map
			using (map)
			{
				// We get the number of the site to know where the pixel to read is (from upper-left to lower-right)
				// CAREFUL : Pixels and sites are read by reading a column from top to bottom, and the column from left to right.
				// NOT like a text from left to right AND top to bottom ; more rather like the japanese writing, top to bottom AND left to right.
				int sitePixelNumber = (int)(site.Location.Column + ((site.Location.Row - 1) * map.Dimensions.Rows));
				UIntPixel pixel = map.BufferPixel;
				// We skip the pixels until finding the one we want
				for(int i = 0; i < sitePixelNumber; i++)
				{
					map.ReadBufferPixel();
				}

				return ((int)pixel.MapCode.Value);
			}
		}

		// Cette fonction va écrire la carte à l'endroit donné par "Path" (qui contient le nom du fichier .tif auquel on va rajouter le timestep).
		// Cette carte va contenir le réseau routier actuel au timestep donné
		public static void WriteMap(string path, ICore ModelCore)
		{
			// On écrit la carte output du réseau de routes
			path = (path.Remove(path.Length - 4)) + ("-" + ModelCore.CurrentTime + ".tif");
			using (IOutputRaster<BytePixel> outputRaster = ModelCore.CreateRaster<BytePixel>(path, ModelCore.Landscape.Dimensions))
			{
				BytePixel pixel = outputRaster.BufferPixel;
				foreach (Site site in ModelCore.Landscape.AllSites)
				{
					pixel.MapCode.Value = (byte)SiteVars.RoadsInLandscape[site].typeNumber;

					outputRaster.WriteBufferPixel();
				}
			}
		}

	}
}

