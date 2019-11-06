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
			// We initialize the frontier and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool haveWeFoundARoadToConnectTo = false;

			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);

			Site siteToClose;
			// Useless assignement to please the gods of C#
			Site arrivalSite = startingSite;
			double newDistanceToStart;

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose))
				{
					// We don't consider the neighbour if it is closed or if it's non-constructible.
					if ((SiteVars.CostRasterWithRoads[neighbourToOpen] >= 0) && (!isClosed.Contains(neighbourToOpen)))
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + the cost between it and the neighbor.
						newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen);

						// If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
						// this node to close will become its predecessor, and its distance to start will become this one.
						if (!costSoFar.ContainsKey(neighbourToOpen) || newDistanceToStart < costSoFar[neighbourToOpen])
						{
							costSoFar[neighbourToOpen] = newDistanceToStart;
							predecessors[neighbourToOpen] = siteToClose;
							// Case of the node not being opened
							if (!frontier.Contains(neighbourToOpen))
							{
								frontier.Enqueue(neighbourToOpen, (float)costSoFar[neighbourToOpen]);
							}
							// Case of the node being already open (if it is already in the frontier, we have to update)
							else
							{
								frontier.UpdatePriority(neighbourToOpen, (float)costSoFar[neighbourToOpen]);
							}
						}

						// We check if the neighbour is a node we want to find, meaning a node with a place where the wood can flow; or, a road that 
						// is connected to such a place. If so, we can stop the search.
						if (SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill) { arrivalSite = neighbourToOpen; haveWeFoundARoadToConnectTo = true; goto End; }
					}

				}

				isClosed.Add(siteToClose);
			}

			End:
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that
			// are the least cost path, and make sure that all of these site now have a road constructed on them, and that it is
			// indicated as connected to a place where we can make wood go.
			if (haveWeFoundARoadToConnectTo)
			{
				List<Site> listOfSitesInLeastCostPath = MapManager.FindPathToStart(startingSite, arrivalSite, predecessors);
				for (int i = 0; i < listOfSitesInLeastCostPath.Count; i++)
				{
					// If there is no road on this site, we construct it. We give it the code of an undefiened road for now.
					if (!SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber = -1;
					// Whatever it is, we indicate it as connected.
					SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].isConnectedToSawMill = true;
					// We update the cost raster that contains the roads.
					SiteVars.CostRasterWithRoads[listOfSitesInLeastCostPath[i]] = 0;
					// We also add the cost of transition to the costs of construction and repair for this timestep
					if (i < listOfSitesInLeastCostPath.Count - 1) RoadNetwork.costOfConstructionAndRepairsAtTimestep += MapManager.CostOfTransition(listOfSitesInLeastCostPath[i], listOfSitesInLeastCostPath[i + 1]);
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
			// We initialize the frontier and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool haveWeFoundARoadToConnectTo = false;

			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);

			Site siteToClose;
			// Useless assignement to please the gods of C#
			Site arrivalSite = startingSite;
			double newDistanceToStart;

			// First, we got to check the possibility that the starting site IS the arrival site. This is possible in very rare situations,
			// as the given starting site can be an exit point...That is not surounded by roads. If that happens, the dijkstra will not be able to
			// open any neighbors, and will fail. Again, very rare, but still important.
			if (RoadNetwork.fluxPathDictionary.ContainsKey(startingSite) || SiteVars.RoadsInLandscape[startingSite].IsAPlaceForTheWoodToGo)
			{
				haveWeFoundARoadToConnectTo = true; goto End;
			}

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				// We take the site with the lowest distance to start.
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, but only those with roads on them.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSitesWithRoads(siteToClose))
				{
					// We don't consider the neighbour if it is closed
					if (!isClosed.Contains(neighbourToOpen))
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + the cost between it and the neighbor.
						newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen);

						// If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
						// this node to close will become its predecessor, and its distance to start will become this one.
						if (!costSoFar.ContainsKey(neighbourToOpen) || newDistanceToStart < costSoFar[neighbourToOpen])
						{
							costSoFar[neighbourToOpen] = newDistanceToStart;
							predecessors[neighbourToOpen] = siteToClose;
							// Case of the node not being opened
							if (!frontier.Contains(neighbourToOpen))
							{
								frontier.Enqueue(neighbourToOpen, (float)costSoFar[neighbourToOpen]);
							}
							// Case of the node being already open
							else
							{
								frontier.UpdatePriority(neighbourToOpen, (float)costSoFar[neighbourToOpen]);
							}
						}

						// We check if the neighbour is a exit node for the wood. If that's the case, search is other.
						if (RoadNetwork.fluxPathDictionary.ContainsKey(neighbourToOpen) || SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo) { arrivalSite = neighbourToOpen; haveWeFoundARoadToConnectTo = true; goto End; }
					}

				}

				// Now that we have checked all of its neighbours, we can close the current node.
				isClosed.Add(siteToClose);

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
				if (RoadNetwork.fluxPathDictionary.ContainsKey(arrivalSite))
				{
					List<Site> listOfSitesInLeastCostPath = MapManager.FindPathToStart(startingSite, arrivalSite, predecessors);

					// We have to reverse the list, because we want to go from the harvested zones to the connection point, and not the opposite.
					listOfSitesInLeastCostPath.Reverse();

					FluxPath newFluxPath = new FluxPath(listOfSitesInLeastCostPath, arrivalSite);

					// Now that this new path is created, we flux the wood.
					newFluxPath.FluxPathFromSite(startingSite, woodFlux);
				}
				// Else, if we didn't found a fluxPath to connect to but an exit point for the wood, we create a new "isAnEnd" path.
				else
				{
					List<Site> listOfSitesInLeastCostPath = MapManager.FindPathToStart(startingSite, arrivalSite, predecessors);

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
