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
		public static bool DijkstraSearchForPlaceToPutWood(ICore ModelCore, Site startingSite)
		{
			// We get the open and closed lists ready
			List<SiteForPathfinding> openSearchList = new List<SiteForPathfinding>();
			List<SiteForPathfinding> exploredRoadsList = new List<SiteForPathfinding>();
			SiteForPathfinding[,] tableOfSitesForPathFinding = CreateArrayOfSitesForPathFinding(ModelCore);
			bool haveWeFoundPlaceToPutWood = false;
			// Useless assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = tableOfSitesForPathFinding[startingSite.Location.Column, startingSite.Location.Row];
			SiteForPathfinding arrivalAsPathfindingSite = startingSiteAsPathfinding;

			// We put the first site in the open list and give it the proper starting distance.
			startingSiteAsPathfinding.distanceToStart = 0;
			openSearchList.Add(startingSiteAsPathfinding);

			// We loop until the list is empty, or when we found what we're looking for
			while (openSearchList.Count > 0 && !haveWeFoundPlaceToPutWood)
			{
				// We take the site with the lowest distance to start. We don't use the "sort" function as it could take more time than needed for what we want.
				Tuple<int, SiteForPathfinding> siteToCloseWithIndex = GetOpenedSiteWithSmallestDistance(openSearchList);
				SiteForPathfinding siteToClose = siteToCloseWithIndex.Item2;

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSitesWithRoads(siteToClose.site))
				{
					// We get the neighbour as a SiteForPathfinding object
					SiteForPathfinding neighbourToOpenAsPathfinding = tableOfSitesForPathFinding[neighbourToOpen.Location.Column, neighbourToOpen.Location.Row];

					// We don't consider the neighbour if it is closed.
					if (!neighbourToOpenAsPathfinding.isClosed)
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + the cost between it and the neighbor.
						double newDistanceToStart = siteToClose.distanceToStart + siteToClose.CostOfTransition(neighbourToOpenAsPathfinding.site);

						// If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
						// this node to close will become its predecessor, and its distance to start will become this one.
						if (newDistanceToStart < neighbourToOpenAsPathfinding.distanceToStart)
						{
							neighbourToOpenAsPathfinding.distanceToStart = newDistanceToStart;
							neighbourToOpenAsPathfinding.predecessor = siteToClose;
							neighbourToOpenAsPathfinding.isOpen = true;
							openSearchList.Add(neighbourToOpenAsPathfinding);
						}

						// We check if the neighbour is a node we want to find, meaning a node with a place where the wood can flow; or, a road that 
						// is connected to such a place. If so, we can stop the search.
						haveWeFoundPlaceToPutWood = (SiteVars.RoadsInLandscape[neighbourToOpenAsPathfinding.site].isConnectedToSawMill);
						if (haveWeFoundPlaceToPutWood) { arrivalAsPathfindingSite = neighbourToOpenAsPathfinding; break; }
					}

				}

				// Now that we have checked all of its neighbours, we can close the current node.
				siteToClose.isClosed = true;
				exploredRoadsList.Add(siteToClose);
				openSearchList.RemoveAt(siteToCloseWithIndex.Item1);
				siteToClose.isOpen = false;
			}
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that are the least cost path, and make sure that all of these site are indicated as 
			// connected to a place where we can make wood go.
			if (haveWeFoundPlaceToPutWood)
			{
				exploredRoadsList.AddRange(openSearchList);
				foreach (SiteForPathfinding site in exploredRoadsList) { SiteVars.RoadsInLandscape[site.site].isConnectedToSawMill = true; }
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
		/// <param name="individualRoadDefinition">
		/// Indicate if a new IndividualRoad object should be created containing the newly created road. The "False" value is only used in the initialization of the road network.
		/// </param>
		public static void DijkstraLeastCostPathToClosestConnectedRoad(ICore ModelCore, Site startingSite, bool individualRoadDefinition = true)
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
					if ((SiteVars.CostRaster[neighbourToOpen] >= 0) && (!neighbourToOpenAsPathfinding.isClosed))
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
				// We transform the pixels into roads
				List<Site> listOfSitesInLeastCostPath = arrivalAsPathfindingSite.FindPathToStart(startingSiteAsPathfinding);
				foreach (Site site in listOfSitesInLeastCostPath)
				{
					// If there is no road on this site, we construct it.
					if (!SiteVars.RoadsInLandscape[site].IsARoad) SiteVars.RoadsInLandscape[site].typeNumber = 7;
					// Whatever it is, we indicate it as connected.
					SiteVars.RoadsInLandscape[site].isConnectedToSawMill = true;
				}

				// If it's time to define a new individual road in the process, we do it.
				if (individualRoadDefinition)
				{
					IndividualRoad newRoad = new IndividualRoad();

					foreach (Site site in listOfSitesInLeastCostPath)
					{
						// We add this site to a new road
						newRoad.sitesInTheRoad.Add(site);
						SiteVars.Roads[site].Add(newRoad);
					}

					// We add the extremities of the road.
					// First, the extremity that is now a branch of the network
					newRoad.extremities.Add(listOfSitesInLeastCostPath[0]);
					// And second, the extrimity with the connection to another road
					newRoad.extremities.Add(listOfSitesInLeastCostPath.Last());
					foreach (IndividualRoad otherRoad in SiteVars.Roads[listOfSitesInLeastCostPath.Last()])
					{
						newRoad.CreateConnection(otherRoad);
					}
				}
			}

			else throw new Exception("FOREST ROADS SIMULATION : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen.");
		}

	} // End of class
} // End of namespace
