using Landis.Library.AgeOnlyCohorts;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using System.IO;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;
using System.Linq;


namespace Landis.Extension.ForestRoadsSimulation
{
	class RoadNetwork
	{
		/// <summary>
		/// This function initialize the road network by checking if every road is connected one way or another to a place where the harvested wood can flow to (sawmill, etc.). If not, it construct the road.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="heuristic">
		/// The heuristic used to determine in which order the roads must be built by the least-cost path algorithm.
		/// </param>
		public static void Initialize(ICore ModelCore, string heuristic)
		{
			// We get all of the sites with a road on it
			List<Site> listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
			List<Site> listOfSitesThatCantConnect = new List<Site>();

			ModelCore.UI.WriteLine("Looking to see if the roads can go to a sawmill...");
			foreach (Site site in listOfSitesWithRoads)
			{
				// If the site is a sawmill or a main road, no need to do anything further. We indicate it as connected.
				if (SiteVars.RoadsInLandscape[site].IsAPlaceForTheWoodToGo || SiteVars.RoadsInLandscape[site].isConnectedToSawMill) SiteVars.RoadsInLandscape[site].isConnectedToSawMill = true;
				// If not, we first have to check if the site can reach such a place by existing roads.
				else
				{
					bool haveWeFoundAPlaceToPutWood = DijkstraSearch.DijkstraSearchForPlaceToPutWood(ModelCore, site);
					// ModelCore.UI.WriteLine("A dijkstra search has just been completed.");

					// If it can't, we add this site to the list of sites for which we will have to build a road.
					if(!haveWeFoundAPlaceToPutWood) listOfSitesThatCantConnect.Add(site);
				}
			}

			// If there were sites that we couldn't not connect, we throw a warning the user
			if (listOfSitesThatCantConnect.Count != 0)
			{
				ModelCore.UI.WriteLine("FOREST ROAD SIMULATION EXTENSION WARNING : ROADS THAT CAN'T BE CONNECTED TO SAWMILLS OR MAIN NETWORKS HAVE BEEN DETECTED IN THE INPUT MAP");
				ModelCore.UI.WriteLine("The extension will now try to create roads to link those roads that are not connected to places where the harvest wood can flow.");

				ModelCore.UI.WriteLine("Shuffling list of not connected sites with roads on them...");
				// Then, we shuffle the list according to the heuristic given in the .txt parameter file.
				if (heuristic == "Random")
				{
					Random random = new Random();
					listOfSitesThatCantConnect = listOfSitesThatCantConnect.OrderBy(site => random.Next()).ToList();
				}
				else if (heuristic == "Closestfirst")
				{
					// We update the list of sites with roads that we used before; but we want only the connected sites inside of it.
					listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
					// We use it to find the distance to the nearest road for each cell
					listOfSitesThatCantConnect = listOfSitesThatCantConnect.OrderBy(site => MapManager.DistanceToNearestRoad(listOfSitesWithRoads, site, true)).ToList();
				}
				else if (heuristic == "Farthestfirst")
				{
					// Same thing, but in reverse order.
					listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
					listOfSitesThatCantConnect = listOfSitesThatCantConnect.OrderByDescending(site => MapManager.DistanceToNearestRoad(listOfSitesWithRoads, site, true)).ToList();
				}
				else throw new Exception("Heuristic non recognized");

				ModelCore.UI.WriteLine("Building missing roads...");
				foreach (Site site in listOfSitesThatCantConnect)
				{
					// If the site has been updated and connected, no need to look at him.
					if (!SiteVars.RoadsInLandscape[site].isConnectedToSawMill)
					{
						// ModelCore.UI.WriteLine("Getting the connected neighbours...");
						// Now, we get all of the roads that are connected to this one (and, implicitly, in need of being connected too)
						List<Site> sitesConnectedToTheLonelySite = MapManager.GetAllConnectedRoads(site);

						// ModelCore.UI.WriteLine("Building the missing road...");
						// We create a new road that will connect the given site to a road that is connected to a sawmill
						DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, site);

						// Now that it is connected, all of its connected neighbours will become automatically connected too.
						foreach (Site connectedSite in sitesConnectedToTheLonelySite) SiteVars.RoadsInLandscape[connectedSite].isConnectedToSawMill = true;
					}
				}

				// All the sites are now connected. Initialization of the road network is thus complete.
				ModelCore.UI.WriteLine("Initialization of the road network is now complete.");
			}

		}

	}
}
