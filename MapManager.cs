// Author: Clément Hardy
// With many elements copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
using System.IO;
using System.Collections.Generic;
using Landis.Core;
using Landis.Library.AgeOnlyCohorts;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;
using System.Linq;
using Landis.Landscapes;
using System.Reflection;

namespace Landis.Extension.ForestRoadsSimulation
{
	public class MapManager
	{
		/// <summary>
		/// The 8-neighbors neighborhood used in the functions to retrieve neighbors
		/// </summary>
		public static RelativeLocation[] neighborhood = new RelativeLocation[]
			{
				new RelativeLocation(-1,  0),  // north
                new RelativeLocation(-1,  1),  // northeast
                new RelativeLocation( 0,  1),  // east
                new RelativeLocation( 1,  1),  // southeast
                new RelativeLocation( 1,  0),  // south
                new RelativeLocation( 1, -1),  // southwest
                new RelativeLocation( 0, -1),  // west
                new RelativeLocation(-1, -1),  // northwest
			};

		/// <summary>
		/// Reads all of the inputed raster. Has to be called after the reading of the parameters.
		/// </summary>
		public static void ReadAllMaps()
		{
			MapManager.ReadMap(PlugIn.Parameters.ZonesForRoadCreation, "ZonesForRoadCreation");
			MapManager.ReadMap(PlugIn.Parameters.InitialRoadNetworkMap, "InitialRoadNetworkMap");
			MapManager.ReadMap(PlugIn.Parameters.CoarseElevationRaster, "CoarseElevationRaster");
			MapManager.ReadMap(PlugIn.Parameters.FineElevationRaster, "FineElevationRaster");
			MapManager.ReadMap(PlugIn.Parameters.CoarseWaterRaster, "CoarseWaterRaster");
			MapManager.ReadMap(PlugIn.Parameters.FineWaterRaster, "FineWaterRaster");
			MapManager.ReadMap(PlugIn.Parameters.SoilsRaster, "SoilsRaster");
		}

		/// <summary>
		/// If no raster have been provided for a certain map, this function we fill the variables with the default value.
		/// </summary>
		public static void FillMapWithDefaultValue(string path, string variableName)
		{
			foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
			{
				if (variableName == "CoarseElevationRaster") SiteVars.CoarseElevation[site] = 0;
				if (variableName == "FineElevationRaster") SiteVars.FineElevation[site] = 0;
				if (variableName == "CoarseWaterRaster") SiteVars.CoarseWater[site] = 0;
				if (variableName == "FineWaterRaster") SiteVars.FineWater[site] = 0;
				if (variableName == "SoilsRaster") SiteVars.Soils[site] = 0;

				// PlugIn.ModelCore.UI.WriteLine("Just put value 0 in raster " + variableName);
			}
		}

		/// <summary>
		/// Function to check if the raster map can be opened, and if it has the same dimensions as the main LANDIS-II landscape.
		/// </summary>
		public static IInputRaster<UIntPixel> TryToOpenMap(string path, string variableName)
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
			// If we were able to open the map, we return it.
			return (map);
		}

		/// <summary>
		/// Reads the raster map located at the path given to the function. It puts the information of the map in a dictionnary inside the "SiteVars" class. 
		/// </summary>
		public static void ReadMap(string path, string variableName)
		{
			// If the parameter "none" has been given, and if we are talking about an optional raster,
			// then all of the values for the site will be filled with the default value.
			if (variableName != "ZonesForRoadCreation" && variableName != "InitialRoadNetworkMap")
			{
				if (path == "none" || path == "None" || path == "" || path == null)
				{
					FillMapWithDefaultValue(path, variableName);
					// If that's the case, no need to go further in the function. we stop here.
					return;
				}
			}

			IInputRaster<UIntPixel> map = TryToOpenMap(path, variableName);

			// We read the map by looking at each pixel one by one, and putting the value of this pixel
			// in the correct site.
			// CAREFUL : Pixels and sites are read by reading a column from top to bottom, and the column from left to right.
			// NOT like a text from left to right AND top to bottom ; more rather like the japanese writing, top to bottom AND left to right.
			using (map)
			{
				UIntPixel pixel = map.BufferPixel;

				foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
				{
					// In case of a problem in the value of the pixel.
					try { map.ReadBufferPixel(); }
					catch
					{
						throw new Exception("FOREST ROADS SIMULATION ERROR : There was a problem while reading the value of raster "
											+ variableName + " at site at location : " + site.Location +
											". The value might be too big or too little. Please check again." + PlugIn.errorToGithub);
					}

					int pixelValue = (int)pixel.MapCode.Value;

					// To deal with problems of distorted No Value Data in rasters, which happen often
					if (pixelValue < 0) pixelValue = 0;

					if (variableName == "ZonesForRoadCreation") SiteVars.BuildableZones[site] = pixelValue;
					else if (variableName == "InitialRoadNetworkMap") SiteVars.RoadsInLandscape[site] = new RoadType(pixelValue);
					else if (variableName == "CoarseElevationRaster") SiteVars.CoarseElevation[site] = pixelValue;
					else if (variableName == "FineElevationRaster") SiteVars.FineElevation[site] = pixelValue;
					else if (variableName == "CoarseWaterRaster") SiteVars.CoarseWater[site] = pixelValue;
					else if (variableName == "FineWaterRaster") SiteVars.FineWater[site] = pixelValue;
					else if (variableName == "SoilsRaster") SiteVars.Soils[site] = (double)pixelValue;
					else throw new Exception("FOREST ROADS SIMULATION ERROR : Variable name not recognized for the reading of the raster map." + PlugIn.errorToGithub);

					// PlugIn.ModelCore.UI.WriteLine("Just put value "+ pixelValue + " in raster " + variableName);
				}
			}
		}

		/// <summary>
		/// Function used for debugging purposes. Directly read into the input map given by the user. Obsolete.
		/// </summary>
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
				// CAREFUL : Pixels and sites are read by reading a row from top to bottom, and the column from left to right.
				// NOT like a text from left to right AND top to bottom ; more rather like the japanese writing, top to bottom AND left to right.
				int sitePixelNumber = (int)(site.Location.Column + ((site.Location.Row - 1) * map.Dimensions.Rows));
				UIntPixel pixel = map.BufferPixel;
				// We skip the pixels until finding the one we want
				for (int i = 0; i < sitePixelNumber; i++)
				{
					map.ReadBufferPixel();
				}

				return ((int)pixel.MapCode.Value);
			}
		}

		/// <summary>
		/// This function writes a map at the given path that contains either the existing road network, or the cost raster, or the wood fluxes.
		/// </summary>
		public static void WriteMap(string path, ICore ModelCore, string mapType = "roads")
		{
			// We register the right name for the maps
			if (mapType == "roads") { path = (path.Remove(path.Length - 4)) + ("-" + ModelCore.CurrentTime + ".tif"); }
			else if (mapType == "costRaster") { path = (path.Remove(path.Length - 4)) + ("-" + "Cost Raster" + ".tif"); }
			else if (mapType == "WoodFlux") { path = (path.Remove(path.Length - 4)) + ("-" + "Wood Flux" + ModelCore.CurrentTime + ".tif"); }
			// If this is a map for the road network, we write it in each pixel.
			if (mapType == "roads")
			{
				try
				{
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
				catch
				{
					PlugIn.ModelCore.UI.WriteLine("Couldn't create map " + path + ". Please check that it is accessible, and not in read-only mode.");
				}
			}
			// If it is the costRaster we want to write, we write the cost of construction in each pixel.
			else if (mapType == "costRaster")
			{
				try
				{
					using (IOutputRaster<UIntPixel> outputRaster = ModelCore.CreateRaster<UIntPixel>(path, ModelCore.Landscape.Dimensions))
					{
						UIntPixel pixel = outputRaster.BufferPixel;
						foreach (Site site in ModelCore.Landscape.AllSites)
						{
							pixel.MapCode.Value = (int)SiteVars.BaseCostRaster[site];
							outputRaster.WriteBufferPixel();
						}
					}
				}
				catch
				{
					PlugIn.ModelCore.UI.WriteLine("Couldn't create map " + path + ". Please check that it is accessible, and not in read-only mode.");
				}
			}
			// If it is the woodFlux map we want to write, that's what we create.
			else if (mapType == "WoodFlux")
			{
				try
				{
					using (IOutputRaster<UIntPixel> outputRaster = ModelCore.CreateRaster<UIntPixel>(path, ModelCore.Landscape.Dimensions))
					{
						UIntPixel pixel = outputRaster.BufferPixel;
						foreach (Site site in ModelCore.Landscape.AllSites)
						{
							if (SiteVars.RoadsInLandscape[site].IsARoad)
							{
								pixel.MapCode.Value = (int)SiteVars.RoadsInLandscape[site].timestepWoodFlux;
								outputRaster.WriteBufferPixel();
							}
							else
							{
								pixel.MapCode.Value = 0;
								outputRaster.WriteBufferPixel();
							}
						}
					}
				}
				catch
				{
					PlugIn.ModelCore.UI.WriteLine("Couldn't create map " + path + ". Please check that it is accessible, and not in read-only mode.");
				}
			}
		}

		/// <summary>
		/// This function gets the highest slope between a site and its neighbors. WARNING : Should be used only for sites that have an elevation value associated to them.
		/// </summary>
		public static double GetHighestSlopeAmongstNeighbors(Site site)
		{
			double highestSlope = 0;

			foreach (Site neighbor in MapManager.GetNeighbouringSites(site))
			{
				// The distance between cells is unitary; we have to multiply it by the length of a pixel.
				double horizontalDistance = Math.Sqrt(Math.Pow((site.Location.Row - neighbor.Location.Row), 2) + Math.Pow((site.Location.Column - neighbor.Location.Column), 2)) * PlugIn.ModelCore.CellLength;

				double elevationDifference = Math.Abs(SiteVars.CoarseElevation[site] - SiteVars.CoarseElevation[neighbor]);

				double slope = (elevationDifference / horizontalDistance) * 100;

				if (slope > highestSlope) { highestSlope = slope; }
			}
			return (highestSlope);
		}

		/// <summary>
		/// This function exists to create a cost raster based on each site of the landscape that contains a part of the information of the cost of construction of roads in the landscape.
		/// It is made so as to optimize the pathfinding process, because it has to calculate cost of transition from one cell to another a very large number of time. The more those calculation
		/// are made in advance, the better.
		/// </summary>
		public static void CreateCostRaster()
		{
			foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
			{
				// First of all; if the pixel is in a non-buildable zone, the cost is negative. No road will be created on this cell.
				if (SiteVars.BuildableZones[site] == 0) { SiteVars.BaseCostRaster[site] = -1; SiteVars.CostRasterWithRoads[site] = -1; }
				else
				{
					// First, we initialize the cost
					double cost = 0;

					// Else, if there is a body of water on this site, the cost is the cost of building a bridge.
					if (PlugIn.Parameters.CoarseWaterRaster != "none" && SiteVars.CoarseWater[site] != 0)
					{
						cost = PlugIn.Parameters.CoarseWaterCost;
					}

					// Else, we incorporate all of the other costs into the mix.
					else
					{
						// We add the base cost of crossing the sites
						cost += PlugIn.Parameters.DistanceCost;

						// We add the slope cost, which depends on the highest slope towards one of the neighbours of the site.
						if (PlugIn.Parameters.CoarseElevationRaster != "none")
						{
							cost += PlugIn.Parameters.CoarseElevationCosts.GetCorrespondingValue(MapManager.GetHighestSlopeAmongstNeighbors(site));
						}

						// We add the fine water cost, that will depend on the number of stream crossed, if there was an input of the fine water raster. The number of stream crossed is expressed as a probability, depending on the length
						// of streams in the site, and the length of a cell/site.
						if (PlugIn.Parameters.FineWaterRaster != "none")
						{
							cost += (SiteVars.FineWater[site] / PlugIn.ModelCore.CellLength) * PlugIn.Parameters.FineWaterCost;
						}

						// We add the cost associated with the soil, if there was an input for the soil raster. The cost is the mean of the cost for the two sites.
						if (PlugIn.Parameters.SoilsRaster != "none")
						{
							cost += SiteVars.Soils[site];
						}

						// Finally, we multiply with the fine elevation cost if there was a input of fine elevation rasters. This multiplication represents a detour taken to avoid the fine topography.
						if (PlugIn.Parameters.FineElevationRaster != "none")
						{
							cost = cost * PlugIn.Parameters.FineElevationCosts.GetCorrespondingValue(SiteVars.FineElevation[site]);
						}
					}
					// We associate this cost to the site
					SiteVars.BaseCostRaster[site] = (float)cost;

					// If there is a road on the site, then the cost of crossing this site is 0.
					if (SiteVars.RoadsInLandscape[site].IsARoad) cost = 0;

					SiteVars.CostRasterWithRoads[site] = (float)cost;
				}
			}
			// Once all of the sites are treated, we export the cost raster so that it can be seen by the user.
			// For now, we will put it in the same folder as the ouput raster for the model
			MapManager.WriteMap(PlugIn.Parameters.OutputsOfRoadNetworkMaps, PlugIn.ModelCore, "costRaster");
		}

		/// <summary>
		/// This function returns a boolean that indicates if the given site is reacheable. It is unreacheable if it is surrounded only by non-constructible sites.
		/// </summary>
		public static bool IsSiteReacheable(Site givenSite)
		{
			List<Site> neighbouringSites = GetNeighbouringSites(givenSite);
			bool isSiteReacheable = false;

			foreach (Site neighbouringSite in neighbouringSites)
			{
				if (SiteVars.BaseCostRaster[neighbouringSite] >= 0) isSiteReacheable = true; break;
			}
			return (isSiteReacheable);
		}

		/// <summary>
		/// Gets the 8 neighbours surounding a site as a list of sites.
		/// </summary>
		/// <returns>
		/// A list of sites containing the neighbouring sites of the given site. 
		/// </returns>
		/// /// <param name="givenSite">
		/// A site for which the neighbouring sites must be found.
		/// </param>
		public static List<Site> GetNeighbouringSites(Site givenSite)
		{
			List<Site> listOfNeighbouringSites = new List<Site>();
			Site neighbour;

			foreach (RelativeLocation relativeLocation in MapManager.neighborhood)
			{
				neighbour = givenSite.GetNeighbor(relativeLocation);
				// Checks if the site is rightly in the landscape. See https://github.com/LANDIS-II-Foundation/Library-Spatial/blob/master/src/api/Site.cs for more infos.
				if (neighbour) listOfNeighbouringSites.Add(neighbour);
			}
			return (listOfNeighbouringSites);
		}

		/// <summary>
		/// Gets the 8 neighbours that have roads on them surounding a site as a list of sites
		/// </summary>
		/// <returns>
		/// A list of sites containing the neighbouring sites of the given site. 
		/// </returns>
		/// /// <param name="givenSite">
		/// A site for which the neighbouring sites must be found.
		/// </param>
		public static List<Site> GetNeighbouringSitesWithRoads(Site givenSite)
		{
			List<Site> listOfNeighbouringSites = new List<Site>();
			Site neighbour;

			foreach (RelativeLocation relativeLocation in neighborhood)
			{
				neighbour = givenSite.GetNeighbor(relativeLocation);
				if (neighbour && SiteVars.RoadsInLandscape[neighbour].IsARoad) listOfNeighbouringSites.Add(neighbour);
			}
			return (listOfNeighbouringSites);
		}

		/// <summary>
		/// Creates a list of relative locations around a site that will be checked to see if their is a road at a given distance
		/// from a given site.
		/// </summary>
		/// <returns>
		/// A list of relative locations.
		/// </returns>
		/// <param name="Distance">
		/// The skidding distance given as a parameter to the plugin.
		/// </param>
		/// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		public static List<RelativeLocation> CreateSearchNeighborhood(int Distance, ICore ModelCore)
		{
			List<RelativeLocation> relativeCircleNeighborhood = new List<RelativeLocation>();

			float landscapeResolution = ModelCore.CellLength;

			int squareSizeOfNeighborhood = (int)Math.Floor(Distance / landscapeResolution) + 1;

			for (int col = -squareSizeOfNeighborhood; col < squareSizeOfNeighborhood + 1; col++)
			{
				for (int row = -squareSizeOfNeighborhood; row < squareSizeOfNeighborhood + 1; row++)
				{
					// We avoid checking the reference cell.
					if (col != 0 || row != 0)
					{
						// We check the euclidian distance between centroids to know if a site is with the skidding distance of the reference site.
						if (Math.Sqrt(Math.Pow((row * landscapeResolution), 2) + Math.Pow((col * landscapeResolution), 2)) <= Distance)
						{
							// If it is, it will be part of the skidding neighborhood.
							relativeCircleNeighborhood.Add(new RelativeLocation(row, col));
						}
					}
				}
			}
			return (relativeCircleNeighborhood);
		}

		/// <summary>
		/// Checks if a given site is in a landscape
		/// </summary>
		public static bool IsItInLandscape(Site site)
		{
			if (site.Location.Row < PlugIn.ModelCore.Landscape.Dimensions.Rows && site.Location.Column < PlugIn.ModelCore.Landscape.Dimensions.Columns)
			{
				if (site.Location.Column >= 0 && site.Location.Row >= 0)
				{
					return (true);
				}
			}
			return (false);
		}

		/// <summary>
		/// Checks in a search neighborhood of a site if there is an existing road connected to a sawmill.
		/// </summary>
		/// <returns>
		/// True if there is an existing road nearby; False overwise.
		/// </returns>
		/// <param name="searchNeighborhood">
		/// A list of relativeLocations that we will explore to check if there is a road on the corresponding sites.
		/// </param>
		/// <param name="site">
		/// A site for which we want to check if there is a road nearby.
		/// </param>
		public static bool IsThereANearbyRoad(List<RelativeLocation> searchNeighborhood, Site site)
		{
			bool isThereANearbyRoad = false;
			Site neighbor;

			foreach (RelativeLocation relativeNeighbour in searchNeighborhood)
			{
				neighbor = site.GetNeighbor(relativeNeighbour);
				// PlugIn.ModelCore.UI.WriteLine("Examining road on neighbour " + neighbor);

				// First, we gotta check if the neighbour is indeed inside the landscape to avoid index errors.
				if (neighbor && IsItInLandscape(neighbor))
				{
					if (SiteVars.RoadsInLandscape[neighbor].isConnectedToSawMill)
					{
						// If the neighbour site has a road in it, we can stop right there.
						isThereANearbyRoad = true;
						break;
					}
				}
			}
			return (isThereANearbyRoad);
		}

		/// <summary>
		/// Checks in the search neighborhood of a site if there is an existing road connected to a sawmill, and return a list of them.
		/// </summary>
		/// <returns>
		/// A list of sites with a road on them that is connected to a sawmill.
		/// </returns>
		/// <param name="searchNeighborhood">
		/// A list of relativeLocations that we will explore to check if there is a road on the corresponding sites.
		/// </param>
		/// <param name="site">
		/// A site for which we want to check if there is a road nearby.
		/// </param>
		public static List<Site> GetNearbySites(List<RelativeLocation> searchNeighborhood, Site site)
		{
			List<Site> listOfNearbySitesWithRoads = new List<Site>();
			Site neighbor;

			foreach (RelativeLocation relativeNeighbour in searchNeighborhood)
			{
				neighbor = site.GetNeighbor(relativeNeighbour);
				// PlugIn.ModelCore.UI.WriteLine("Examining road on neighbour " + neighbor);

				// First, we gotta check if the neighbour is indeed inside the landscape to avoid index errors.
				if (neighbor && IsItInLandscape(neighbor))
				{
					listOfNearbySitesWithRoads.Add(neighbor);
				}
			}
			return (listOfNearbySitesWithRoads);
		}

		/// <summary>
		/// Checks in the search neighborhood of a site to see how many roads connected to a sawmill there are nearby.
		/// </summary>
		/// <returns>
		/// A list of sites with a road on them that is connected to a sawmill.
		/// </returns>
		/// <param name="searchNeighborhood">
		/// A list of relativeLocations that we will explore to check if there is a road on the corresponding sites.
		/// </param>
		/// <param name="site">
		/// A site for which we want to check if there is a road nearby.
		/// </param>
		public static int HowManyRoadsNearby(List<RelativeLocation> searchNeighborhood, Site site)
		{
			List<Site> listOfNearbySitesWithRoads = new List<Site>();
			Site neighbor;
			int numberOfRoadsNearby = 0;

			foreach (RelativeLocation relativeNeighbour in searchNeighborhood)
			{
				neighbor = site.GetNeighbor(relativeNeighbour);
				// PlugIn.ModelCore.UI.WriteLine("Examining road on neighbour " + neighbor);

				// First, we gotta check if the neighbour is indeed inside the landscape to avoid index errors.
				if (neighbor && IsItInLandscape(neighbor))
				{
					if (SiteVars.RoadsInLandscape[neighbor].isConnectedToSawMill)
					{
						numberOfRoadsNearby++;
					}
				}
			}
			return (numberOfRoadsNearby);
		}

		/// <summary>
		/// Get all of the sites that have a road (forest road, sawmill, etc.) on them.
		/// </summary>
		/// <returns>
		/// A list of "Site" objects that are associated with a road in the variable RoadsInLandscape.
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		public static List<Site> GetSitesWithRoads(ICore ModelCore)
		{
			List<Site> listOfSitesWithRoads = new List<Site>();

			foreach (Site site in ModelCore.Landscape.AllSites)
			{
				if (SiteVars.RoadsInLandscape[site].IsARoad)
				{
					listOfSitesWithRoads.Add(site);
				}
			}
			return (listOfSitesWithRoads);
		}

		/// <summary>
		/// Get all of the sites that have an exit point (sawmill, main road network, etc.) on them.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		public static List<Site> GetSitesWithExitPoints(ICore ModelCore)
		{
			List<Site> listOfSitesWithExitPoints = new List<Site>();

			foreach (Site site in ModelCore.Landscape.AllSites)
			{
				if (SiteVars.RoadsInLandscape[site].IsAPlaceForTheWoodToGo)
				{
					listOfSitesWithExitPoints.Add(site);
				}
			}
			return (listOfSitesWithExitPoints);
		}

		/// <summary>
		/// Check if there is at least a site with a main public road or a sawmill so that the harvested wood has a place to flow to, and the road network places to connect to.
		/// </summary>
		/// <returns>
		/// A boolean indicating if there is at least such a site.
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		public static bool IsThereAPlaceForTheWoodToGo(ICore ModelCore)
		{
			bool placeDetected = false;

			foreach (Site site in ModelCore.Landscape.AllSites)
			{
				if (SiteVars.RoadsInLandscape[site].IsAPlaceForTheWoodToGo)
				{
					placeDetected = true;
					break;
				}
			}
			return (placeDetected);
		}

		/// <summary>
		/// Calculate the manhattan distance between two given sites
		/// </summary>
		/// <returns>
		/// A double giving the value of this distance. The distance has no units (1 = side of a pixel or site)
		/// </returns>
		public static double GetDistance(Site givenSite, Site otherSite)
		{
			return (Math.Sqrt(Math.Pow((givenSite.Location.Row - otherSite.Location.Row), 2.0) + Math.Pow((givenSite.Location.Column - otherSite.Location.Column), 2.0)));
			// Manhanttan distance - For different neighbourhood type ?
			// return (Math.Abs(givenSite.Location.Row - otherSite.Location.Row) + Math.Abs(givenSite.Location.Column - otherSite.Location.Column));
		}

		/// <summary>
		/// Calculate the manhattan distance to the closest road from the given site
		/// </summary>
		/// <returns>
		/// A double giving the value of this distance. The distance has no units (1 = side of a pixel or site)
		/// </returns>
		/// /// <param name="sitesWithRoads">
		/// A list of sites containing roads that we want to check. Used for better performance, instead of launching .GetSitesWithRoads every time the function is called.
		/// </param>
		/// /// <param name="givenSite">
		/// The site for which we want to have the distance to the nearest road.
		/// </param>
		/// /// <param name="connected">
		/// If true, the function will look for the nearest road indicated as "connected" to a place where the wood can be transported.
		/// </param>
		public static double DistanceToNearestRoad(List<Site> sitesWithRoads, Site givenSite, bool connected = true)
		{
			double minDistance = double.PositiveInfinity;

			foreach (Site otherSite in sitesWithRoads)
			{
				if (connected) if (!SiteVars.RoadsInLandscape[otherSite].isConnectedToSawMill) continue;

				double distanceBetweenSites = GetDistance(givenSite, otherSite);

				if (distanceBetweenSites > 0 && distanceBetweenSites < minDistance)
				{
					minDistance = distanceBetweenSites;
				}
			}
			return (minDistance);
		}

		/// <summary>
		/// Get the farthest site in a list of site for a given site.
		/// </summary>
		/// <returns>
		/// The farthest site from the given site.
		/// </returns>
		/// /// <param name="givenSite">
		/// The site for which we want to have the farthest site.
		/// </param>
		/// /// <param name="listOfSites">
		/// The list of site in which to search for the farthest site.
		/// </param>
		public static Site GetFarthestSite(Site givenSite, List<Site> listOfSites)
		{
			double maxDistance = 0;
			// Stupid assignation to please the gods of C#
			Site farthestSite = givenSite;

			foreach (Site otherSite in listOfSites)
			{
				double distanceBetweenSites = GetDistance(givenSite, otherSite);

				if (distanceBetweenSites > maxDistance)
				{
					maxDistance = distanceBetweenSites;
					farthestSite = otherSite;
				}
			}
			return (farthestSite);
		}

		/// <summary>
		/// Get all of the sites that have a road, and that are connected (8-site neighbourhood) to the given site.
		/// </summary>
		/// <returns>
		/// A list of sites with a road on them, and that are connected to this one.
		/// </returns>
		public static List<Site> GetAllConnectedRoads(Site givenSite)
		{
			HashSet<Site> listOfConnectedNeighborsWithRoads = new HashSet<Site>();

			HashSet<Site> openSearchList = new HashSet<Site>();
			HashSet<Site> closedSearchList = new HashSet<Site>();

			openSearchList.Add(givenSite);

			while (openSearchList.Count != 0)
			{
				Site currentSite = openSearchList.First();
				openSearchList.Remove(currentSite);

				foreach (Site neighbour in GetNeighbouringSitesWithRoads(currentSite))
				{
					listOfConnectedNeighborsWithRoads.Add(neighbour);
					if (!closedSearchList.Contains(neighbour)) openSearchList.Add(neighbour);
				}
				closedSearchList.Add(currentSite);
			}
			return (listOfConnectedNeighborsWithRoads.ToList());
		}

		/// <summary>
		/// Gets the closest existing site with a road on it that is connected to an exit point for the wood. It has to be at a skidding distance; if not, the road will be built by another function during the road of the plugin.
		/// </summary>
		/// <returns>
		/// A site with the closest road on it. 
		/// </returns>
		public static Site GetClosestSiteWithRoad(List<RelativeLocation> skiddingNeighborhood, Site site)
		{
			// If the given site is a connected road, then it is the one we want.
			if (SiteVars.RoadsInLandscape[site].isConnectedToSawMill)
			{
				return (site);
			}
			// If not, we check the skidding neighborhood
			else
			{
				Site neighbor;
				// Useless assignation to please the gods of C#
				Site siteToReturn = site;
				double minimumDistance = double.PositiveInfinity;

				foreach (RelativeLocation relativeNeighbour in skiddingNeighborhood)
				{
					neighbor = site.GetNeighbor(relativeNeighbour);
					// First, we gotta check if the neighbour is indeed inside the landscape to avoid index errors.
					if (neighbor && IsItInLandscape(neighbor))
					{
						if (SiteVars.RoadsInLandscape[neighbor].isConnectedToSawMill && MapManager.GetDistance(site, neighbor) < minimumDistance)
						{
							siteToReturn = neighbor;
							minimumDistance = MapManager.GetDistance(site, neighbor);
						}
					}
				}
				return (siteToReturn);
			}
		}

		/// <summary>
		/// Get all of the sites that have been harvested recently by the harvest extension, and to which we need to create a road to.
		/// </summary>
		/// <returns>
		/// A list of sites to which a road must be built.
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="Timestep">
		/// The timestep of the extension.
		/// </param>
		public static List<Site> GetAllRecentlyHarvestedSites(ICore ModelCore, int Timestep)
		{
			List<Site> listOfHarvestedSites = new List<Site>();
			ISiteVar<int> timeSinceLastEventVar = ModelCore.GetSiteVar<int>("Harvest.TimeOfLastEvent");
			ISiteVar<string> prescription = ModelCore.GetSiteVar<string>("Harvest.PrescriptionName");
			ISiteVar<Landis.Library.HarvestManagement.Stand> standOfSite = Landis.Library.HarvestManagement.SiteVars.Stand;

			foreach (Site site in ModelCore.Landscape.AllSites)
			{
				// Carefull : the time of last event is the timestep when the last harvest event happened; not the number of years SINCE the last event.
				int timeOfLastEvent = timeSinceLastEventVar[site];

				if (site.IsActive && (ModelCore.CurrentTime - timeOfLastEvent) < Timestep && timeOfLastEvent != -100)
				{
					listOfHarvestedSites.Add(site);
				}
			}
			return (listOfHarvestedSites);
		}

		/// <summary>
		/// Get the time (in years) before the next harvest in this cell, if (and only if) the cell in question is harvested with a repeated harvest (or single repeat).
		/// </summary>
		/// <returns>
		/// The time (in years) before the repeated harvest will harvest this cell again; if no repeated harvest is on the cell, it returns 0.
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="site">
		/// The site for which to get the time before the next harvest.
		/// </param>
		public static int GetTimeBeforeNextHarvest(ICore ModelCore, Site site)
		{
			// We get the stand of the cell
			ISiteVar<Landis.Library.HarvestManagement.Stand> standOfSite = Landis.Library.HarvestManagement.SiteVars.Stand;
			Landis.Library.HarvestManagement.Stand standOfTheSite = standOfSite[site];

			// We check if the stand where the cell is is harvested by a repeated prescription
			// If it is not, we return 0.
			if (!(standOfTheSite.LastPrescription is Landis.Library.HarvestManagement.RepeatHarvest)) { return (0); }

			// If it is a repeated prescription, we check if it is a single repeat
			else if ((standOfTheSite.LastPrescription is Landis.Library.HarvestManagement.SingleRepeatHarvest))
			{
				// We check until when the stand is reserved
				int timestepOfReservation = (int)standOfTheSite.GetType().GetField("setAsideUntil", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standOfTheSite);

				// The time to the next rotation is then returned
				int difference = timestepOfReservation - ModelCore.CurrentTime;
				if (difference < 0) { return (0); }
				else { return (difference); }
			}

			// If it is a repeated prescription...
			else
			{
				// First, we check if its reservation has ended
				int timestepOfReservation = (int)standOfTheSite.GetType().GetField("setAsideUntil", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standOfTheSite);

				if (timestepOfReservation == ModelCore.CurrentTime) { return (0); }
				else
				{
					Landis.Library.HarvestManagement.RepeatHarvest repeatPrescription = (Landis.Library.HarvestManagement.RepeatHarvest)standOfSite[site].LastPrescription;
					return (repeatPrescription.Interval);
				}
			}
		}

		/// <summary>
		/// Gets the coordinates of sites and put it into an array of coordinates. This function is used to feed the kdTree search functions.
		/// </summary>
		/// /// <param name="listOfSites">
		/// The list of sites to get the coordinates from.
		/// </param>
		public static double[][] GetSitesCoordinates(List<Site> listOfSites)
		{
			var data = new List<double[]>();

			foreach (Site site in listOfSites)
			{
				// The coordinates will have the format row, column; that's the one LANDIS-II uses for some of its functions.
				data.Add(new double[] { (double)site.Location.Row, (double)site.Location.Column });
			}

			return (data.ToArray());
		}

		/// <summary>
		/// Shuffle a list of given sites according to their distance to the closest road, and the heuristic given by the user.
		/// </summary>
		/// <returns>
		/// The shuffled list of sites.
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="listOfSites">
		/// The list of sites to shuffle.
		/// </param>
		/// /// <param name="heuristic">
		/// The heuristic given by the user as a parameter to the extension.
		/// </param>
		public static List<Site> ShuffleAccordingToHeuristic(ICore ModelCore, List<Site> listOfSites, string heuristic)
		{
			List<Site> shuffledListOfSites = new List<Site>();

			// If the heuristic is random, we just randomly shuffle the list of sites.
			if (heuristic == "Random")
			{
				Random random = new Random();
				listOfSites = listOfSites.OrderBy(site => random.Next()).ToList();
			}
			// If not, we are going to construct a kdTree to make quick searches about the closest road to each given site, in order to order them afterward.
			else
			{
				// To create the tree, we need the sites with roads on them; their coordinates in a array; and the function used to compare
				// two sets of coordinates, which will be the euclidian distance.
				List<Site> listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
				double[][] coordinnatesOfSitesWithRoads = MapManager.GetSitesCoordinates(listOfSitesWithRoads);
				Func<double[], double[], double> L2Norm = (x, y) =>
				{
					double dist = 0;
					for (int i = 0; i < x.Length; i++)
					{
						dist += (x[i] - y[i]) * (x[i] - y[i]);
					}

					return dist;
				};
				var searchTree = new Supercluster.KDTree.KDTree<double, Site>(2, coordinnatesOfSitesWithRoads, listOfSitesWithRoads.ToArray(), L2Norm);

				// Now that the tree is made, we calculate the distances and sort according to each heuristic.
				if (heuristic == "Closestfirst")
				{
					// Buckle up, cause this is a complex Link function. We ask to order the sites according to a value linked to a given site of the list;
					// This value is the euclidian distance (L2Norm function) between a double[] containing the coordinates of the sites, and a double[] containing
					// the coordinates of the closest site found via the kdtree search. The search needs the same double[] coordinates of the given site, and return
					// a list of tuples where each tuple contains the coordinates of the closest site, and the site object. Complex, but it works fast.
					shuffledListOfSites = listOfSites.OrderBy(site => L2Norm(new double[] { (double)site.Location.Row, (double)site.Location.Column },
						searchTree.NearestNeighbors(new double[] { (double)site.Location.Row, (double)site.Location.Column }, 1)[0].Item1)).ToList();
				}
				else if (heuristic == "Farthestfirst")
				{
					// Same thing, but in reverse order.
					shuffledListOfSites = listOfSites.OrderByDescending(site => L2Norm(new double[] { (double)site.Location.Row, (double)site.Location.Column },
						searchTree.NearestNeighbors(new double[] { (double)site.Location.Row, (double)site.Location.Column }, 1)[0].Item1)).ToList();
				}
				else throw new Exception("Heuristic non recognized");
			}
			return (shuffledListOfSites);
		}

		/// <summary>
		/// Calculate the cost of going from this site to another.
		/// </summary>
		/// <returns>
		/// A double which is the cost of transition.
		/// </returns>
		/// /// <param name="otherSite">
		/// The other site where we want to go to.
		/// </param>
		public static double CostOfTransition(Site givenSite, Site otherSite)
		{
			// The cost of transition is half the transition in this pixel, and half the transition in the other, as we're going from centroid to centroid.
			double cost = (SiteVars.CostRasterWithRoads[givenSite] + SiteVars.CostRasterWithRoads[otherSite]) / 2;

			// We multiply the cost according to the distance (diagonal or not)
			if (otherSite.Location.Row != givenSite.Location.Row && otherSite.Location.Column != givenSite.Location.Column) cost = cost * Math.Sqrt(2.0);

			return (cost);
		}

		/// <summary>
		/// Gives the list of sites of a path by using a list of predecessors, an arrival, and a starting site. The predecessors list is generated by the dijkstra
		/// algorithm, for example.
		/// </summary>
		/// <returns>
		/// A list that beguins with the arrival site, and ends with the starting site.
		/// </returns>
		public static List<Site> FindPathToStart(Site startingSite, Site arrivalSite, Dictionary<Site, Site> predecessors)
		{
			List<Site> ListOfSitesInThePath = new List<Site>();
			Site currentSite = arrivalSite;
			Site nextPredecessor;
			bool foundStartingSite = false;
			// Case of this node being the starting site (you never know)
			// so as to avoid potential errors.
			if (arrivalSite.Location == startingSite.Location) { foundStartingSite = true; nextPredecessor = currentSite; ListOfSitesInThePath.Add(currentSite); }
			else nextPredecessor = predecessors[arrivalSite];

			while (!foundStartingSite)
			{
				ListOfSitesInThePath.Add(nextPredecessor);
				if (nextPredecessor.Location == startingSite.Location) foundStartingSite = true;
				else
				{
					currentSite = nextPredecessor;
					nextPredecessor = predecessors[currentSite];
				}
			}

			return (ListOfSitesInThePath);
		}
	}
}