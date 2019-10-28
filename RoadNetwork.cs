﻿using Landis.Library.AgeOnlyCohorts;
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
	public class RoadNetwork
	{
		/// <summary>
		/// Updates the status of being connected to a place where the harvested wood can flow to (sawmill, etc.) for each of the given roads.
		/// </summary>
		/// <param name="ListOfSitesWithRoads">A list of sites with roads on them</param>
		/// <returns>A list of sites that couldn't connect.</returns>
		public static List<Site> UpdateConnectionToExitPointStatus(List<Site> ListOfSitesWithRoads)
		{
			List<Site> listOfSitesThatCantConnect = new List<Site>();
			Dictionary<Site, bool> dictonnaryOfSitesAlreadyChecked = new Dictionary<Site, bool>();

			foreach (Site site in ListOfSitesWithRoads)
			{
				// If the site is a sawmill or a main road, we indicate it as connected.
				if (SiteVars.RoadsInLandscape[site].IsAPlaceForTheWoodToGo) { dictonnaryOfSitesAlreadyChecked[site] = true; }
				// If not, we first have to check if the site can reach such a place by existing roads only if it has not been already checked.
				else if (!dictonnaryOfSitesAlreadyChecked.ContainsKey(site))
				{
					// This function will update the connection dictionnary in order to reflect which sites are connected or not to an exit point.
					DijkstraSearch.DijkstraSearchForPlaceToPutWood(PlugIn.ModelCore, site, dictonnaryOfSitesAlreadyChecked);
					// ModelCore.UI.WriteLine("A dijkstra search has just been completed.");
				}
			}

			// We update the status of the sites according to the dictionnary.
			foreach (Site site in ListOfSitesWithRoads)
			{
				SiteVars.RoadsInLandscape[site].isConnectedToSawMill = dictonnaryOfSitesAlreadyChecked[site];
				if (dictonnaryOfSitesAlreadyChecked[site] == false) { listOfSitesThatCantConnect.Add(site); }
			}

			return (listOfSitesThatCantConnect);
		}

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
			
			// We check wich ones are connected to an exit point
			ModelCore.UI.WriteLine("   Looking to see if the roads can go to a exit point (sawmill, main road network)...");
			List<Site> listOfSitesThatCantConnect = UpdateConnectionToExitPointStatus(listOfSitesWithRoads);


			// If there were sites that we couldn't not connect, we throw a warning the user
			if (listOfSitesThatCantConnect.Count != 0)
			{
				ModelCore.UI.WriteLine("   FOREST ROAD SIMULATION EXTENSION WARNING : ROADS THAT CAN'T BE CONNECTED TO SAWMILLS OR MAIN NETWORKS HAVE BEEN DETECTED IN THE INPUT MAP");
				ModelCore.UI.WriteLine("   The extension will now try to create roads to link those roads that are not connected to places where the harvest wood can flow.");

				ModelCore.UI.WriteLine("   Shuffling list of not connected sites with roads on them...");

				// Then, we shuffle the list according to the heuristic given in the .txt parameter file.
				listOfSitesThatCantConnect = MapManager.ShuffleAccordingToHeuristic(ModelCore, listOfSitesThatCantConnect, heuristic);

				ModelCore.UI.WriteLine("   Building missing roads...");

				foreach (Site site in listOfSitesThatCantConnect)
				{
					// If the site has been updated and connected, no need to look at him.
					if (!SiteVars.RoadsInLandscape[site].isConnectedToSawMill)
					{
						ModelCore.UI.WriteLine("   Getting the connected neighbours...");
						// Now, we get all of the roads that are connected to this one (and, implicitly, in need of being connected too)
						List<Site> sitesConnectedToTheLonelySite = MapManager.GetAllConnectedRoads(site);

						ModelCore.UI.WriteLine("   Building the missing road...");
						// We create a new road that will connect the given site to a road that is connected to a sawmill
						DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, site);

						// Now that it is connected, all of its connected neighbours will become automatically connected too.
						foreach (Site connectedSite in sitesConnectedToTheLonelySite) SiteVars.RoadsInLandscape[connectedSite].isConnectedToSawMill = true;
					}
				}

			}

			// All the sites are now connected. Initialization of the road network is thus complete.
			ModelCore.UI.WriteLine("   Initialization of the road network is now complete.");

		}

		/// <summary>
		/// Function to reset the woodflux going through the roads for the current timestep.
		/// </summary>
		public static void RestTimestepWoodFlux()
		{
			List<Site> listOfSitesWithRoads = MapManager.GetSitesWithRoads(PlugIn.ModelCore);

			foreach (Site siteWithRoad in listOfSitesWithRoads)
			{
				SiteVars.RoadsInLandscape[siteWithRoad].timestepWoodFlux = 0;
			}
		}

	}
}
