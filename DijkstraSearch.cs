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
	class DijkstraSearch
	{
		/// <summary>
		/// Create an array of SiteForPathfinding objects, which are sites with additonal information to facilitate pathfinding.
		/// This resulting array can be used in dijkstra searches to avoid looking into lists to check if sites are "opened" or "closeed" by the algorithm.
		/// The resulting array have for dimensions [columns (x axis), rows (y axis)] of the LANDIS-II landscape.
		/// </summary>
		/// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		public static SiteForPathfinding[,] CreateArrayOfSitesForPathFinding(ICore ModelCore)
		{
			// We have to use dimensions+1 because for C#, things start at 0; for LANDIS-II, rows and columns start at 1. This means there will be empty slots in the array, but this has no impacts.
			SiteForPathfinding[,] tableOfSitesForPathfinding = new SiteForPathfinding[PlugIn.ModelCore.Landscape.Dimensions.Columns+1, PlugIn.ModelCore.Landscape.Dimensions.Rows+1];

			foreach (Site site in ModelCore.Landscape.AllSites)
			{
				tableOfSitesForPathfinding[site.Location.Column, site.Location.Row] = new SiteForPathfinding(site);
			}

			return (tableOfSitesForPathfinding);
		}

		/// <summary>
		/// Gives back the SiteForPathfinding object that has the lowest distance to start value. Exists to avoid the "sort" function of C# that might take more time than this one.
		/// It return it as a tuple containing the index of the returned SiteForPathfinding in the enumerable, and the SiteForPathfinding object in itself.
		/// </summary>
		/// <param name="enumerableOfSitesForPathfinding">
		/// A list or an array containing the SiteForPathfinding objects.
		/// </param>
		public static Tuple<int, SiteForPathfinding> GetOpenedSiteWithSmallestDistance(IEnumerable<SiteForPathfinding> enumerableOfSitesForPathfinding)
		{
			SiteForPathfinding siteToReturn = enumerableOfSitesForPathfinding.First();
			int indexOfSiteToReturn = 0;
			int i = 0;

			foreach (SiteForPathfinding site in enumerableOfSitesForPathfinding)
			{
				if (site.isOpen && site.distanceToStart < siteToReturn.distanceToStart)
				{
					siteToReturn = site;
					indexOfSiteToReturn = i;
				}
				i++;
			}

			return (new Tuple<int, SiteForPathfinding>(indexOfSiteToReturn, siteToReturn));
		}

		/// <summary>
		/// Searches for a place where the harvested wood can flow (sawmill, main road network) starting from a given site with a road via the Dijkstra Algorithm. 
		/// It only looks for a path through neighbors (8) that have a road on them.
		/// </summary>
		/// <returns>
		/// A boolean indicating if a site has been found; if not, that means that the road on the site isn't connected to a place where the wood must flow. If it's found,
		/// the road on the starting site - and also of all of the sites that have been opened during the search - are considered "connected". 
		/// </returns>
		/// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		public static bool DijkstraSearchForPlaceToPutWood(ICore ModelCore, Site startingSite, Dictionary<Site, bool> connectionDictonnary)
		{
			// We get the open and closed lists ready
			HashSet<SiteForPathfinding> openSearchList = new HashSet<SiteForPathfinding>();
			List<SiteForPathfinding> exploredRoadsList = new List<SiteForPathfinding>();
			SiteForPathfinding[,] tableOfSitesForPathFinding = new SiteForPathfinding[ModelCore.Landscape.Dimensions.Columns + 1, ModelCore.Landscape.Dimensions.Rows + 1];
			bool haveWeFoundPlaceToPutWood = false;
			// Useless assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = new SiteForPathfinding(startingSite);
			tableOfSitesForPathFinding[startingSite.Location.Column, startingSite.Location.Row] = startingSiteAsPathfinding;

			// We put the first site in the open list and give it the proper starting distance.
			startingSiteAsPathfinding.distanceToStart = 0;
			openSearchList.Add(startingSiteAsPathfinding);

			// Pre-allocation of objects to be faster.
			SiteForPathfinding siteToClose;
			SiteForPathfinding neighbourToOpenAsPathfinding;

			// We loop until the list is empty, or when we found what we're looking for
			while (openSearchList.Count > 0)
			{
				siteToClose = openSearchList.First();
				openSearchList.Remove(siteToClose);
				exploredRoadsList.Add(siteToClose);

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSitesWithRoads(siteToClose.site))
				{
					// We get the neighbour as a SiteForPathfinding object
					neighbourToOpenAsPathfinding = tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row];
					if (neighbourToOpenAsPathfinding == null)
					{
						neighbourToOpenAsPathfinding = new SiteForPathfinding(neighbourToOpen);
						tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row] = neighbourToOpenAsPathfinding;
					}

					// We don't consider the neighbour if it is closed.
					if (!neighbourToOpenAsPathfinding.isClosed)
					{
						// We are not looking for a path; so, no need to bother with distances or predecessors.
						neighbourToOpenAsPathfinding.isOpen = true;
						openSearchList.Add(neighbourToOpenAsPathfinding);

						// We stop if we reach a site that has already been checked by the grander algorithm of the road network, as indicated
						// by the presence of the neighbor in the connectionDictonnary.
						if (connectionDictonnary.ContainsKey(neighbourToOpenAsPathfinding.site) || SiteVars.RoadsInLandscape[neighbourToOpenAsPathfinding.site].IsAPlaceForTheWoodToGo)
						{
							// If the site was an exit point, everybody was connected.
							if (SiteVars.RoadsInLandscape[neighbourToOpenAsPathfinding.site].IsAPlaceForTheWoodToGo)
							{
								haveWeFoundPlaceToPutWood = true;
							}
							// If the reached site has been checked as not connected, then all of the roads we have been exploring are not connected too.
							// We stop the loop, and indicate that we have not found a place to put the wood.
							else if (!connectionDictonnary[neighbourToOpenAsPathfinding.site])
							{
								haveWeFoundPlaceToPutWood = false;
							}
							// If the site reached is indicated as connected, then we stop the loop and indicate that we found a exit point to connect.
							else
							{
								haveWeFoundPlaceToPutWood = true;
							}
							goto End;
						}

					}

				}

				// Now that we have checked all of its neighbours, we can close the current node.
				siteToClose.isClosed = true;
				siteToClose.isOpen = false;
			}
		// ModelCore.UI.WriteLine("Dijkstra search is over.");

		End:
			// Now that we reached the end, we are going to look at all the explored site, and update their status in the connectionDictonnary
			// according to if we found an exit point to connect to or not.
			foreach (SiteForPathfinding exploredSite in exploredRoadsList)
			{
				if (haveWeFoundPlaceToPutWood) { connectionDictonnary[exploredSite.site] = true; }
				else { connectionDictonnary[exploredSite.site] = false; }
			}

			// Whatever happens, we give back an indication that we found - or not - a place to go through the neighbors.
			return (haveWeFoundPlaceToPutWood);
		}

		/// <summary>
		/// Finds the least cost path from a given site to a site with a road already connected to somewhere where the wood can flow.
		/// Different from DijkstraSearchForPlaceToPutWood in the sense that we do not only look for sites with paths, but all sites (even with no paths),
		/// and that we build new roads where there was no path.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		public static void DijkstraLeastCostPathToClosestConnectedRoad(ICore ModelCore, Site startingSite)
		{
			// We get the open and closed lists ready
			// List<SiteForPathfinding> openSearchList = new List<SiteForPathfinding>();
			// C5.IntervalHeap<SiteForPathfinding> frontier = new C5.IntervalHeap<SiteForPathfinding>();
			Priority_Queue.FastPriorityQueue<SiteForPathfinding> frontier = new Priority_Queue.FastPriorityQueue<SiteForPathfinding>(PlugIn.ModelCore.Landscape.SiteCount);
			SiteForPathfinding[,] tableOfSitesForPathFinding = new SiteForPathfinding[ModelCore.Landscape.Dimensions.Columns + 1, ModelCore.Landscape.Dimensions.Rows + 1];
			bool haveWeFoundARoadToConnectTo = false;
			// Useless assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = new SiteForPathfinding(startingSite);
			tableOfSitesForPathFinding[startingSite.Location.Column, startingSite.Location.Row] = startingSiteAsPathfinding;
			SiteForPathfinding arrivalAsPathfindingSite = startingSiteAsPathfinding;

			// We put the first site in the open list and give it the proper starting distance.
			startingSiteAsPathfinding.distanceToStart = 0;
			// openSearchList.Add(startingSiteAsPathfinding);
			// frontier.Add(startingSiteAsPathfinding);
			frontier.Enqueue(startingSiteAsPathfinding, (float)startingSiteAsPathfinding.distanceToStart);

			// Pre-allocation of objects to be faster.
			SiteForPathfinding siteToClose;
			SiteForPathfinding neighbourToOpenAsPathfinding;
			double newDistanceToStart;

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				// We take the site with the lowest distance to start. We don't use the "sort" function as it could take more time than needed for what we want.
				// Tuple<int, SiteForPathfinding> siteToCloseWithIndex = GetOpenedSiteWithSmallestDistance(openSearchList);
				// SiteForPathfinding siteToClose = siteToCloseWithIndex.Item2;
				// var siteToClose = frontier.FindMin();
				// frontier.DeleteMin();
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose.site))
				{
					neighbourToOpenAsPathfinding = tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row];
					if (neighbourToOpenAsPathfinding == null)
					{
						neighbourToOpenAsPathfinding = new SiteForPathfinding(neighbourToOpen);
						tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row] = neighbourToOpenAsPathfinding;
					}

					// We don't consider the neighbour if it is closed or if it's non-constructible.
					if ((SiteVars.CostRasterWithRoads[neighbourToOpen] >= 0) && (!neighbourToOpenAsPathfinding.isClosed))
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + the cost between it and the neighbor.
						newDistanceToStart = siteToClose.distanceToStart + siteToClose.CostOfTransition(neighbourToOpenAsPathfinding.site);

						// If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
						// this node to close will become its predecessor, and its distance to start will become this one.
						if (newDistanceToStart < neighbourToOpenAsPathfinding.distanceToStart)
						{
							neighbourToOpenAsPathfinding.distanceToStart = newDistanceToStart;
							// Case of the node not being opened
							if (neighbourToOpenAsPathfinding.predecessor == null)
							{
								neighbourToOpenAsPathfinding.isOpen = true;
								neighbourToOpenAsPathfinding.predecessor = siteToClose;
								frontier.Enqueue(neighbourToOpenAsPathfinding, (float)neighbourToOpenAsPathfinding.distanceToStart);
							}
							// Case of the node being already open
							else
							{
								neighbourToOpenAsPathfinding.predecessor = siteToClose;
								frontier.UpdatePriority(neighbourToOpenAsPathfinding, (float)neighbourToOpenAsPathfinding.distanceToStart);
							}
							// openSearchList.Add(neighbourToOpenAsPathfinding);
							// frontier.Add(neighbourToOpenAsPathfinding);
						}

						// We check if the neighbour is a node we want to find, meaning a node with a place where the wood can flow; or, a road that 
						// is connected to such a place. If so, we can stop the search.
						if (SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill) { arrivalAsPathfindingSite = neighbourToOpenAsPathfinding; haveWeFoundARoadToConnectTo = true; goto End; }
					}

				}

				// Now that we have checked all of its neighbours, we can close the current node.
				siteToClose.isClosed = true;
				// openSearchList.RemoveAt(siteToCloseWithIndex.Item1);
				siteToClose.isOpen = false;
			}

			End:
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that
			// are the least cost path, and make sure that all of these site now have a road constructed on them, and that it is
			// indicated as connected to a place where we can make wood go.
			if (haveWeFoundARoadToConnectTo)
			{
				List<SiteForPathfinding> listOfSitesInLeastCostPath = arrivalAsPathfindingSite.FindPathToStart(startingSiteAsPathfinding);
				for (int i = 0; i < listOfSitesInLeastCostPath.Count; i++)
				{
					// If there is no road on this site, we construct it. We give it the code of an undefiened road for now.
					if (!SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i].site].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i].site].typeNumber = -1;
					// Whatever it is, we indicate it as connected.
					SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i].site].isConnectedToSawMill = true;
					// We update the cost raster that contains the roads.
					SiteVars.CostRasterWithRoads[listOfSitesInLeastCostPath[i].site] = 0;
					// We also add the cost of transition to the costs of construction and repair for this timestep
					if (i < listOfSitesInLeastCostPath.Count - 1) RoadNetwork.costOfConstructionAndRepairsAtTimestep += listOfSitesInLeastCostPath[i].CostOfTransition(listOfSitesInLeastCostPath[i + 1].site);
				}
			}
			else throw new Exception("FOREST ROADS SIMULATION : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen.");
		}


		/// <summary>
		/// Finds the least cost path from roads to roads to a exit point for the wood in the landscape, and add the given wood flux to every road visited.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		/// /// <param name="Woodflux">
		/// The flux of wood that is going to flow to the exit point.
		/// </param>
		public static void DijkstraWoodFlux(ICore ModelCore, Site startingSite, double woodFlux)
		{
			// We get the open and closed lists ready
			// List<SiteForPathfinding> openSearchList = new List<SiteForPathfinding>();
			Priority_Queue.FastPriorityQueue<SiteForPathfinding> frontier = new Priority_Queue.FastPriorityQueue<SiteForPathfinding>(PlugIn.ModelCore.Landscape.SiteCount);
			SiteForPathfinding[,] tableOfSitesForPathFinding = new SiteForPathfinding[ModelCore.Landscape.Dimensions.Columns + 1, ModelCore.Landscape.Dimensions.Rows + 1];
			bool haveWeFoundARoadToConnectTo = false;
			// Useless assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = new SiteForPathfinding(startingSite);
			tableOfSitesForPathFinding[startingSite.Location.Column, startingSite.Location.Row] = startingSiteAsPathfinding;
			SiteForPathfinding arrivalAsPathfindingSite = startingSiteAsPathfinding;

			// We put the first site in the open list and give it the proper starting distance.
			startingSiteAsPathfinding.distanceToStart = 0;
			frontier.Enqueue(startingSiteAsPathfinding, (float)startingSiteAsPathfinding.distanceToStart);

			// Pre-allocation of objects to be faster.
			SiteForPathfinding siteToClose;
			SiteForPathfinding neighbourToOpenAsPathfinding;
			double newDistanceToStart;

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				// We take the site with the lowest distance to start.
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, but only those with roads on them.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSitesWithRoads(siteToClose.site))
				{
					neighbourToOpenAsPathfinding = tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row];
					if (neighbourToOpenAsPathfinding == null)
					{
						neighbourToOpenAsPathfinding = new SiteForPathfinding(neighbourToOpen);
						tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row] = neighbourToOpenAsPathfinding;
					}

					// We don't consider the neighbour if it is closed
					if (!neighbourToOpenAsPathfinding.isClosed)
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + the arbitrary cost of using one road pixel (1). No need to use the detailed cost informations here, as we're just going from road to road.
						newDistanceToStart = siteToClose.distanceToStart + 1;

						// If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
						// this node to close will become its predecessor, and its distance to start will become this one.
						if (newDistanceToStart < neighbourToOpenAsPathfinding.distanceToStart)
						{
							neighbourToOpenAsPathfinding.distanceToStart = newDistanceToStart;
							// Case of the node not being opened
							if (neighbourToOpenAsPathfinding.predecessor == null)
							{
								neighbourToOpenAsPathfinding.isOpen = true;
								neighbourToOpenAsPathfinding.predecessor = siteToClose;
								frontier.Enqueue(neighbourToOpenAsPathfinding, (float)neighbourToOpenAsPathfinding.distanceToStart);
							}
							// Case of the node being already open
							else
							{
								neighbourToOpenAsPathfinding.predecessor = siteToClose;
								frontier.UpdatePriority(neighbourToOpenAsPathfinding, (float)neighbourToOpenAsPathfinding.distanceToStart);
							}

						}

						// We check if the neighbour is a exit node for the wood. If that's the case, search is other.
						if (RoadNetwork.fluxPathDictionary.ContainsKey(neighbourToOpen) || SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo) { arrivalAsPathfindingSite = neighbourToOpenAsPathfinding; haveWeFoundARoadToConnectTo = true; goto End; }
					}

				}

				// Now that we have checked all of its neighbours, we can close the current node.
				siteToClose.isClosed = true;
				// openSearchList.RemoveAt(siteToCloseWithIndex.Item1);
				siteToClose.isOpen = false;
			}

		End:
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that
			// are the least cost path, and make sure that all of these site now have a road constructed on them, and that it is
			// indicated as connected to a place where we can make wood go.
			if (haveWeFoundARoadToConnectTo)
			{
				// If we found a fluxPath to connect to, we create a new one starting from the starting site, and stopping just before the arrival (which is our connection site to the fluxpath, and thus already belongs to a fluxpath)
				if (RoadNetwork.fluxPathDictionary.ContainsKey(arrivalAsPathfindingSite.site))
				{
					List<Site> listOfSitesInLeastCostPath = arrivalAsPathfindingSite.FindPathToStartAsSites(startingSiteAsPathfinding);

					// We have to reverse the list, because we want to go from the harvested zones to the connection point, and not the opposite.
					listOfSitesInLeastCostPath.Reverse();

					FluxPath newFluxPath = new FluxPath(listOfSitesInLeastCostPath, arrivalAsPathfindingSite.site);

					// Now that this new path is created, we flux the wood.
					newFluxPath.FluxPathFromSite(startingSite, woodFlux);
				}
				// Else, if we didn't found a fluxPath to connect to but an exit point for the wood, we create a new "isAnEnd" path.
				else
				{
					List<Site> listOfSitesInLeastCostPath = arrivalAsPathfindingSite.FindPathToStartAsSites(startingSiteAsPathfinding);

					// We have to reverse the list, because we want to go from the harvested zones to the exit point, and not the opposite.
					listOfSitesInLeastCostPath.Reverse();

					FluxPath newFluxPathIsAnEnd = new FluxPath(listOfSitesInLeastCostPath);

					// Then, we flux the wood down this path.
					newFluxPathIsAnEnd.FluxPathFromSite(startingSite, woodFlux);
				}

			}

			else throw new Exception("FOREST ROADS SIMULATION : A Dijkstra search wasn't able to flux the wood from site " + startingSite.Location + " to any exit point. This isn't supposed to happen.");
		}


	} // End of DijkstraSearch class

} // End of namespace
