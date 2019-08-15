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
		/// Searches for a place where the harvested wood can flow (sawmill, main road network) starting from a given site with a road via the Dijkstra Algorithm. 
		/// It only looks for a path through neighbors (8) that have a road on them.
		/// </summary>
		/// <returns>
		/// A boolean indicating if a site has been found; if not, that means that the road on the site isn't connected to a place where the wood must flow. If it's found,
		/// the road on the starting site - and also of all of the sites that have been opened during the search - are considered "connected". 
		/// </returns>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		public static bool DijkstraSearchForPlaceToPutWood(ICore ModelCore, Site startingSite)
		{
			// We get the open and closed lists ready
			List<SiteForPathfinding> openSearchList = new List<SiteForPathfinding>();
			List<SiteForPathfinding> closedSearchList = new List<SiteForPathfinding>();
			bool haveWeFoundPlaceToPutWood = false;
			// Assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = new SiteForPathfinding(startingSite);
			SiteForPathfinding arrivalAsPathfindingSite = startingSiteAsPathfinding;

			// We put the first site in the open list
			openSearchList.Add(startingSiteAsPathfinding);

			// We loop until the list is ready, or when we found what we're looking for
			while (openSearchList.Count > 0 && !haveWeFoundPlaceToPutWood)
			{
				// We sort the sites on the open list according to their distance to start
				openSearchList = openSearchList.OrderBy(SiteForPathfinding => SiteForPathfinding.distanceToStart).ToList();

				// We select the site that is closest, and get him out of the list; and we add him to the "closed" list.
				SiteForPathfinding siteToClose = openSearchList[0];
				openSearchList.RemoveAt(0);
				closedSearchList.Add(siteToClose);

				// ModelCore.UI.WriteLine("Looking at site " + siteToClose.site.Location);

				// We look at each of its neighbours that have a road on them.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose.site, true))
				{
					// ModelCore.UI.WriteLine("Looking at neighbour " + neighbourToOpen.Location);
					// For each, we check if they are not already in the open/closed list. If so, we select them.
					SiteForPathfinding neighbourToOpenAsPathfinding = new SiteForPathfinding(neighbourToOpen);
					if (closedSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen) != null)
					{
						neighbourToOpenAsPathfinding = closedSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen);
					}
					else if (openSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen) != null)
					{
						neighbourToOpenAsPathfinding = openSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen);
					}
					// If they are not already in the open/closed list, we put them in the open list.
					else { openSearchList.Add(neighbourToOpenAsPathfinding); }
					// If the distance to start have never been calculated (= double.PositiveInfinity), we calculate it
					// We also add the current site as predecessor.
					if (neighbourToOpenAsPathfinding.distanceToStart == double.PositiveInfinity)
					{
						neighbourToOpenAsPathfinding.predecessor = siteToClose;
						// ModelCore.UI.WriteLine("Checking a distance to start (Site not opened)...");
						neighbourToOpenAsPathfinding.distanceToStart = neighbourToOpenAsPathfinding.FindDistanceToStart(startingSiteAsPathfinding);
						// ModelCore.UI.WriteLine("Distance checked.");
					}
					// If this neighbour was already open, and if he is not the predecessor of this very site
					else if (neighbourToOpenAsPathfinding != siteToClose.predecessor)
					{
						// We calculate the distance to start if the current site was used as predecessor.
						SiteForPathfinding predecessorWeMightKeep = neighbourToOpenAsPathfinding.predecessor;
						neighbourToOpenAsPathfinding.predecessor = siteToClose;
						// ModelCore.UI.WriteLine("Checking a distance to start (site already opened)...");
						double distanceThroughtNodeToClose = neighbourToOpenAsPathfinding.FindDistanceToStart(startingSiteAsPathfinding);
						// ModelCore.UI.WriteLine("Distance checked.");
						// If this distance is smaller, he then becomes the predecessor.
						if (distanceThroughtNodeToClose < neighbourToOpenAsPathfinding.distanceToStart) { neighbourToOpenAsPathfinding.distanceToStart = distanceThroughtNodeToClose; }
						else { neighbourToOpenAsPathfinding.predecessor = predecessorWeMightKeep; }
					}

					// We check if the neighbour is a node we want to find, meaning a node with a place where the wood can flow; or, a road that 
					// is connected to such a place. If so, we can stop the search.
					haveWeFoundPlaceToPutWood = (SiteVars.RoadsInLandscape[neighbourToOpenAsPathfinding.site].isConnectedToSawMill);
					if (haveWeFoundPlaceToPutWood) { arrivalAsPathfindingSite = neighbourToOpenAsPathfinding; break; }
				}
				// ModelCore.UI.WriteLine("Closing the site.");
				// Now that we have checked all of its neighbours, we can close the current node.
				closedSearchList.Add(siteToClose);
			}
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that
			// are the least cost path, and make sure that all of these site are indicated as connected to a place where we can make wood go.
			if (haveWeFoundPlaceToPutWood)
			{
				closedSearchList.AddRange(openSearchList);
				foreach (SiteForPathfinding site in closedSearchList) { SiteVars.RoadsInLandscape[site.site].isConnectedToSawMill = true; }
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
			List<SiteForPathfinding> openSearchList = new List<SiteForPathfinding>();
			List<SiteForPathfinding> closedSearchList = new List<SiteForPathfinding>();
			bool haveWeFoundARoadToConnectTo = false;
			// Assignation made to please the gods of C# and their rules that prevent a clean initialization.
			SiteForPathfinding startingSiteAsPathfinding = new SiteForPathfinding(startingSite);
			SiteForPathfinding arrivalAsPathfindingSite = startingSiteAsPathfinding;

			// We put the first site in the open list
			openSearchList.Add(startingSiteAsPathfinding);

			// We loop until the list is ready, or when we found what we're looking for
			while (openSearchList.Count > 0 && !haveWeFoundARoadToConnectTo)
			{
				// We sort the sites on the open list according to their distance to start
				openSearchList = openSearchList.OrderBy(SiteForPathfinding => SiteForPathfinding.distanceToStart).ToList();

				// We select the site that is closest, and get him out of the list; and we add him to the "closed" list.
				SiteForPathfinding siteToClose = openSearchList[0];
				openSearchList.RemoveAt(0);
				closedSearchList.Add(siteToClose);

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose.site, false))
				{
					// For each, we check if they are not already in the open/closed list. If so, we select them.
					SiteForPathfinding neighbourToOpenAsPathfinding = new SiteForPathfinding(neighbourToOpen);
					if (closedSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen) != null)
					{
						neighbourToOpenAsPathfinding = closedSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen);
					}
					else if (openSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen) != null)
					{
						neighbourToOpenAsPathfinding = openSearchList.Find(SiteForPathfinding => SiteForPathfinding.site == neighbourToOpen);
					}
					// If they are not already in the open/closed list, we put them in the open list.
					else { openSearchList.Add(neighbourToOpenAsPathfinding); }
					// If the distance to start have never been calculated (= double.PositiveInfinity), we calculate it
					// We also add the current site as predecessor.
					if (neighbourToOpenAsPathfinding.distanceToStart == double.PositiveInfinity)
					{
						neighbourToOpenAsPathfinding.predecessor = siteToClose;
						neighbourToOpenAsPathfinding.distanceToStart = neighbourToOpenAsPathfinding.FindDistanceToStart(startingSiteAsPathfinding);
					}
					// If this neighbour was already open, and if he is not the predecessor of this very site
					else if (neighbourToOpenAsPathfinding != siteToClose.predecessor)
					{
						// We calculate the distance to start if the current site was used as predecessor.
						SiteForPathfinding predecessorWeMightKeep = neighbourToOpenAsPathfinding.predecessor;
						neighbourToOpenAsPathfinding.predecessor = siteToClose;
						double distanceThroughtNodeToClose = neighbourToOpenAsPathfinding.FindDistanceToStart(startingSiteAsPathfinding);
						// If this distance is smaller, he then becomes the predecessor.
						if (distanceThroughtNodeToClose < neighbourToOpenAsPathfinding.distanceToStart) { neighbourToOpenAsPathfinding.distanceToStart = distanceThroughtNodeToClose; }
						else { neighbourToOpenAsPathfinding.predecessor = predecessorWeMightKeep; }
					}

					// We check if the neighbour is a node we want to find, meaning a node with a place where the wood can flow; or, a road that 
					// is connected to such a place. If so, we can stop the search.
					haveWeFoundARoadToConnectTo = (SiteVars.RoadsInLandscape[neighbourToOpenAsPathfinding.site].isConnectedToSawMill);
					if (haveWeFoundARoadToConnectTo) { arrivalAsPathfindingSite = neighbourToOpenAsPathfinding; break; }
				}

				// Now that we have checked all of its neighbours, we can close the current node.
				closedSearchList.Add(siteToClose);
			}
			// ModelCore.UI.WriteLine("Dijkstra search is over.");

			// If we're out of the loop, that means that the search is over. If it was successfull before we ran out of neighbours to check, 
			// We can now retrieve the list of the sites that
			// are the least cost path, and make sure that all of these site now have a road constructed on them, and that it is
			// indicated as connected to a place where we can make wood go.
			if (haveWeFoundARoadToConnectTo)
			{
				List<Site> listOfSitesInLeastCostPath = arrivalAsPathfindingSite.FindPathToStart(startingSiteAsPathfinding);
				foreach (Site site in listOfSitesInLeastCostPath)
				{
					// If there is no road on this site, we construct it.
					if (!SiteVars.RoadsInLandscape[site].IsARoad) SiteVars.RoadsInLandscape[site].typeNumber = 7;
					// Whatever it is, we indicate it as connected.
					SiteVars.RoadsInLandscape[site].isConnectedToSawMill = true;
				}
			}
			else throw new Exception("FOREST ROADS SIMULATION : A Dijkstra search wasn't able to connect two sites. This isn't supposed to happen.");
		}


	}
}
