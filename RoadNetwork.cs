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
	public class RoadNetwork
	{
		public static double costOfConstructionAndRepairsAtTimestep;
		public static double costOfLastPath;
		public static Site lastArrivalSiteOfDijkstraSearch;
		public static List<FluxPath> fluxPathCatalogue;
		public static Dictionary<Site, FluxPath> fluxPathDictionary;


		/// <summary>
		/// Initialy updates the status of being connected to a place where the harvested wood can flow to (sawmill, etc.) for each of the given roads. 
		/// It is different from the other updates, because it has to look at all of the sites, and not just the ones that changed status recently.
		/// </summary>
		/// <param name="ListOfSitesWithRoads">A list of sites with roads on them</param>
		/// <returns>A list of sites that couldn't connect.</returns>
		public static List<Site> UpdateConnectionToExitPointStatus()
		{
			Dictionary<Site, bool> dictonnaryOfSitesAlreadyChecked = new Dictionary<Site, bool>();

			// First, we get all of the roads that correspond to exit points.
			List<Site> sitesWithExitPoints = MapManager.GetSitesWithExitPoints(PlugIn.ModelCore);
			PlugIn.ModelCore.UI.WriteLine(sitesWithExitPoints.Count + " sites with exit points detected.");

			var progressBar = PlugIn.ModelCore.UI.CreateProgressMeter(sitesWithExitPoints.Count);
			List<Site> listOfConnectedSites;

			foreach (Site siteWithExit in sitesWithExitPoints)
			{
				// If the site hasn't been already checked
				if (!dictonnaryOfSitesAlreadyChecked.ContainsKey(siteWithExit))
				{
					dictonnaryOfSitesAlreadyChecked[siteWithExit] = true;
					SiteVars.RoadsInLandscape[siteWithExit].isConnectedToSawMill = true;

					// We get all of the sites connected to this exit point
					listOfConnectedSites = MapManager.GetAllConnectedRoads(siteWithExit);

					// We defined each of them as checked and we update the connection status of those connected sites
					foreach (Site siteConnected in listOfConnectedSites)
					{
						dictonnaryOfSitesAlreadyChecked[siteConnected] = true;
						SiteVars.RoadsInLandscape[siteConnected].isConnectedToSawMill = true;
					}
				}

				progressBar.IncrementWorkDone(1);
			}

			// To finish, we generate a list of sites that are not connected to an exit point.
			List<Site> nonConnectedSites = new List<Site>();

			foreach (Site siteWithRoad in MapManager.GetSitesWithRoads(PlugIn.ModelCore))
			{
				if (!dictonnaryOfSitesAlreadyChecked.ContainsKey(siteWithRoad)) { nonConnectedSites.Add(siteWithRoad); SiteVars.RoadsInLandscape[siteWithRoad].isConnectedToSawMill = false; }
			}

			return (nonConnectedSites);

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
			PlugIn.ModelCore.UI.WriteLine(listOfSitesWithRoads.Count + " sites with a road on them have been detected.");

			// We check wich ones are connected to an exit point
			ModelCore.UI.WriteLine("   Looking to see if the roads can go to a exit point (sawmill, main road network)...");
			List<Site> listOfSitesThatCantConnect = UpdateConnectionToExitPointStatus();


			// If there were sites that we couldn't not connect, we throw a warning the user
			if (listOfSitesThatCantConnect.Count != 0)
			{
				ModelCore.UI.WriteLine("   FOREST ROAD SIMULATION EXTENSION WARNING : ROADS THAT CAN'T BE CONNECTED TO SAWMILLS OR MAIN NETWORKS HAVE BEEN DETECTED IN THE INPUT MAP");
				ModelCore.UI.WriteLine("   The extension will now try to create roads to link those roads that are not connected to places where the harvest wood can flow.");
				PlugIn.ModelCore.UI.WriteLine(listOfSitesThatCantConnect.Count + " sites are in need of a connection to an exit point.");

				ModelCore.UI.WriteLine("   Shuffling list of not connected sites with roads on them...");

				// Then, we shuffle the list according to the heuristic given in the .txt parameter file.
				listOfSitesThatCantConnect = MapManager.ShuffleAccordingToHeuristic(ModelCore, listOfSitesThatCantConnect, heuristic);

				ModelCore.UI.WriteLine("   Building missing roads...");
				var progressBar = PlugIn.ModelCore.UI.CreateProgressMeter(listOfSitesThatCantConnect.Count);


				foreach (Site site in listOfSitesThatCantConnect)
				{
					// If the site has been updated and connected, no need to look at him.
					if (!SiteVars.RoadsInLandscape[site].isConnectedToSawMill)
					{
						// ModelCore.UI.WriteLine("   Getting the connected neighbours...");
						// Now, we get all of the roads that are connected to this one (and, implicitly, in need of being connected too)
						List<Site> sitesConnectedToTheLonelySite = MapManager.GetAllConnectedRoads(site);

						// ModelCore.UI.WriteLine("   Building the missing road...");
						// We create a new road that will connect the given site to a road that is connected to a sawmill
						DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, site);

						// Now that it is connected, all of its connected neighbours will become automatically connected too.
						foreach (Site connectedSite in sitesConnectedToTheLonelySite) SiteVars.RoadsInLandscape[connectedSite].isConnectedToSawMill = true;
					}

					progressBar.IncrementWorkDone(1);
				}

			}

			// All the sites are now connected. Initialization of the road network is thus complete.
			ModelCore.UI.WriteLine("   Initialization of the road network is now complete.");

		}

		/// <summary>
		/// Function to reset the woodflux going through the roads for the current timestep.
		/// </summary>
		public static void RestTimestepWoodFluxData()
		{
			List<Site> listOfSitesWithRoads = MapManager.GetSitesWithRoads(PlugIn.ModelCore);

			foreach (Site siteWithRoad in listOfSitesWithRoads)
			{
				SiteVars.RoadsInLandscape[siteWithRoad].timestepWoodFlux = 0;
			}

			fluxPathCatalogue.Clear();
			fluxPathDictionary.Clear();

		}

	}
}
