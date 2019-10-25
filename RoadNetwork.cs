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
		// An object keep track of road ID numbers.
		public static int roadIDCounter;
		// Another to keep the roads in a catalog
		public static List<IndividualRoad> roadCatalog;

		/// <summary>
		/// This function initialize the road network by checking if every road is connected one way or another to a place where the harvested wood can flow to (sawmill, etc.). If not, it construct the road. It also separate roads to identifiy them individualy.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="heuristic">
		/// The heuristic used to determine in which order the roads must be built by the least-cost path algorithm.
		/// </param>
		public static void Initialize(ICore ModelCore, string heuristic)
		{
			// We also initialize the road ID, since we're going to start giving IDs to roads.
			roadIDCounter = 1;
			roadCatalog = new List<IndividualRoad>();

			// We get all of the sites with a road on it
			List<Site> listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
			List<Site> listOfSitesThatCantConnect = new List<Site>();

			ModelCore.UI.WriteLine("   Looking to see if the roads can go to a sawmill...");
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
						DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, site, false);

						// Now that it is connected, all of its connected neighbours will become automatically connected too.
						foreach (Site connectedSite in sitesConnectedToTheLonelySite) SiteVars.RoadsInLandscape[connectedSite].isConnectedToSawMill = true;
					}
				}

			}

			ModelCore.UI.WriteLine("   Spliting initial roads for identification...");

			// Now, we have to split the existing roads at their intersections.
			// First, we make a sorted list of sites so that we can examine the sites in the right order.
			listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
			Dictionary<Site, int> numberOfNeighborsWithRoads = new Dictionary<Site, int>();

			foreach (Site site in listOfSitesWithRoads)
			{
				numberOfNeighborsWithRoads[site] = MapManager.GetNeighbouringSitesWithRoads(site).Count;
			}

			List<Site> orderedListOfSitesToExamine = listOfSitesWithRoads.OrderBy(site => numberOfNeighborsWithRoads[site]).ToList();


			// Then, we will look at each site in order
			foreach (Site site in orderedListOfSitesToExamine)
			{
				// We will do a kind of dijkstra search from this site if it isn't already associated to a road ID already.
				if (SiteVars.Roads[site].Count == 0)
				{
					// The search will propagate a new road from neighbouring site with a road on it to the other, until it finds
					// an intersection to stop. The rules for detecting an intersection are complex, as they interact with the fact
					// that some intersections might be messy. I will not describe them all here.
					List<Site> sitesAlreadyChecked = new List<Site>();
					List<Site> openListOfSites = new List<Site>();
					// We create the new road object that we are going to fill with the search
					IndividualRoad newRoad = new IndividualRoad();
					// Other initializations of the loop
					openListOfSites.Add(site);
					newRoad.sitesInTheRoad.Add(site);
					SiteVars.Roads[site].Add(newRoad);

					ModelCore.UI.WriteLine("   Looping on a site for a new road - Ini complete.");
					ModelCore.UI.WriteLine("   Road number : " + newRoad.ID);
					ModelCore.UI.WriteLine("   Road count in road catalog : " + roadCatalog.Count);

					// We stop when there is no more neighbours to check, or when the extremities of the roads have been filled
					while (openListOfSites.Count > 0 && newRoad.extremities.Count < 2)
					{
						Site siteToClose = openListOfSites[0];
						openListOfSites.RemoveAt(0);

						ModelCore.UI.WriteLine("   Looking at neighbors of a site.");
						foreach (Site neighbor in MapManager.GetNeighbouringSitesWithRoads(siteToClose))
						{
							// We only considerer the neighbor if it is checked.
							if (!sitesAlreadyChecked.Contains(neighbor) && !newRoad.sitesInTheRoad.Contains(neighbor))
							{
								// Case where the neighbor is an extrimity of the network (it has only one neighbor himself)
								if (numberOfNeighborsWithRoads[neighbor] == 1)
								{
									ModelCore.UI.WriteLine("   Case of neighbor is extremity of network.");
									// We add it as an extrimity, and we stop the search this way
									if (!sitesAlreadyChecked.Contains(neighbor) && !newRoad.sitesInTheRoad.Contains(neighbor))
									{
										newRoad.sitesInTheRoad.Add(neighbor);
										SiteVars.Roads[neighbor].Add(newRoad);
										newRoad.extremities.Add(neighbor);
										ModelCore.UI.WriteLine("   Extrimity added. Number of extremities : " + newRoad.extremities.Count);
									}

									if (newRoad.extremities.Count == 2) { break; }
								}
								// Case when the neighbor is not assigned, and it doesn't look like an intersection (less than 3 neighbors with roads)
								else if (numberOfNeighborsWithRoads[neighbor] < 3 && SiteVars.Roads[neighbor].Count == 0)
								{
									ModelCore.UI.WriteLine("   Case of neighbor not assigned and not intersection.");
									// We add the neighbor to the current road and we keep going IF it hasn't been added in the search already
									if (!sitesAlreadyChecked.Contains(neighbor) && !newRoad.sitesInTheRoad.Contains(neighbor))
									{
										newRoad.sitesInTheRoad.Add(neighbor);
										SiteVars.Roads[neighbor].Add(newRoad);
										openListOfSites.Add(neighbor);
									}

									if (newRoad.extremities.Count == 2) { break; }
								}
								// Case when the neighbor is not assigned, and it does look like an intersection
								else if (numberOfNeighborsWithRoads[neighbor] >= 3 && SiteVars.Roads[neighbor].Count == 0)
								{
									ModelCore.UI.WriteLine("   Case of neighbor not assigned but intersection.");
									// A variable that will be used to wrap up this section
									bool wasItConnected = false;

									// We add the neighbor to the current road if it hasn't been added in the search already, and we stop
									if (!sitesAlreadyChecked.Contains(neighbor) && !newRoad.sitesInTheRoad.Contains(neighbor))
									{
										newRoad.sitesInTheRoad.Add(neighbor);
										SiteVars.Roads[neighbor].Add(newRoad);
									}
									// We also have to check if there is another road that arrived at this intersection already; if that's the case, we got to connect to it.
									List<Site> neighborsOfneighborThatAreAlreadyAssigned = new List<Site>();
									foreach (Site neighborOfNeighbor in MapManager.GetNeighbouringSitesWithRoads(neighbor))
									{
										if (SiteVars.Roads[neighborOfNeighbor].Count != 0 && !newRoad.sitesInTheRoad.Contains(neighborOfNeighbor))
										{
											neighborsOfneighborThatAreAlreadyAssigned.Add(neighborOfNeighbor);
										}
									}
									// We check those neighbors to see if they are extrimities of the roads they are into. For the first road for wich the neighbor is an extremity, 
									// we connect to it, and only it. Then, we're done. It will be enough.
									foreach (Site neighborOfneighborAlreadyAssigned in neighborsOfneighborThatAreAlreadyAssigned)
									{
										foreach (IndividualRoad roadOnNeighborOfNeighbor in SiteVars.Roads[neighborOfneighborAlreadyAssigned].ToList())
										{
											if (roadOnNeighborOfNeighbor.extremities.Contains(neighborOfneighborAlreadyAssigned))
											{
												// We put in in the current road only if it haven't been put into it already
												if (newRoad.sitesInTheRoad.Contains(neighborOfneighborAlreadyAssigned))
												{
													newRoad.sitesInTheRoad.Add(neighborsOfneighborThatAreAlreadyAssigned[0]);
													newRoad.extremities.Add(neighborsOfneighborThatAreAlreadyAssigned[0]);
													ModelCore.UI.WriteLine("   Extrimity added. Number of extremities : " + newRoad.extremities.Count);
													SiteVars.Roads[neighborsOfneighborThatAreAlreadyAssigned[0]].Add(newRoad);
													wasItConnected = true;
												}
												newRoad.CreateConnection(roadOnNeighborOfNeighbor);
											}
										}
										// We will not connect to multiple neighbors; if we already did it with one, it's over.
										if (wasItConnected) { break; }
									}
									// If there weren't any road to connect to, then this neighbor must become an extremity, waiting for another road to connect to it.
									if (!wasItConnected)
									{
										newRoad.extremities.Add(neighbor); ModelCore.UI.WriteLine("   Extremity added. Number of extremities : " + newRoad.extremities.Count);
									}

									if (newRoad.extremities.Count == 2) { break; }
								}
								// Case when neighbor is already assigned
								else if (SiteVars.Roads[neighbor].Count > 0)
								{
									ModelCore.UI.WriteLine("   Case of neighbor assigned.");
									// If a connection between this current road and the one this neighbor is assigned to does not exist, we create one.
									// This will become an extremity.
									// We add the neighbor to the current road if it hasn't been added in the search already, and we stop
									if (!sitesAlreadyChecked.Contains(neighbor) && !newRoad.sitesInTheRoad.Contains(neighbor))
									{
										bool atLeastOneConnectionToAnotherRoad = false;
										foreach (IndividualRoad otherRoad in SiteVars.Roads[neighbor].ToList())
										{
											if (!newRoad.connectedRoads.Contains(otherRoad))
											{
												newRoad.CreateConnection(otherRoad);
												// If it's the first connection, we add this site as an extrimity of the current road.
												if (!atLeastOneConnectionToAnotherRoad)
												{
													newRoad.sitesInTheRoad.Add(neighbor);
													newRoad.extremities.Add(neighbor);
													ModelCore.UI.WriteLine("   Extrimity added. Number of extremities : " + newRoad.extremities.Count);

													SiteVars.Roads[neighbor].Add(newRoad);
												}
												atLeastOneConnectionToAnotherRoad = true;
											}
										}
									}

									if (newRoad.extremities.Count == 2) { break; }
								}

								sitesAlreadyChecked.Add(neighbor);

							} // End of if neighbor is not already checked
							

						} // End of foreach neighbors


					} // End of while loop
					

				} // And of if the site is not a part of a road


			} // End of foreach site with roads

			// Before we finish the initialization, we output a raster of road IDs
			MapManager.WriteMap(PlugIn.Parameters.OutputsOfRoadNetworkMaps, ModelCore, "RoadIDIni");

			// All the sites are now connected. Initialization of the road network is thus complete.
			ModelCore.UI.WriteLine("   Initialization of the road network is now complete.");

		}

	}
}
