// Author: Clément Hardy

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
        /// Big dijkstra function to replace the several function that was before the update to 1.4.
        /// Can make a path between two sites, or try to make two to create a loop.
        /// Try to make two least cost path to two sites; the second path is only constructed if the site reached is far enough, and if the cost of the path isn't too high.
        /// Optimizes both the construction of new roads, and the upgrades of existing roads. Deals with the wood flux directly (before, another function did it)
        /// </summary>
        /// /// <param name="ModelCore">
        /// The model's core framework.
        /// </param>
        /// /// <param name="startingSite">
        /// The starting site of the search.
        /// </param>
        /// /// <param name="searchNeighborhood">
        /// The search neighborhood used to check for sites in the looping algorithm.
        /// </param>
        /// /// <param name="loopingActivated">
        /// Activates the looping behaviour, which will try to create a second path.
        /// </param>
        /// /// <param name="loopingMaxCost">
        /// The maximal cost that the second road must cost.
        /// </param>
        /// /// <param name="woodFluxActivated">
        /// Takes into account the wood to flux down the path if activated. In the case of a loop, the wood is only fluxed on the first path
        /// </param>
        /// /// <param name="woodfluxNumber">
        /// The amount of wood to flux down the path.
        /// </param>
        public static int DijkstraPathFinding(ICore ModelCore, Site startingSite, List<RelativeLocation> searchNeighborhood = null, bool loopingActivated = false, double loopingMaxCost = 10, bool woodFluxActivated = false, double woodfluxNumber = 0)
		{
			// We initialize the frontier and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			Dictionary<Site, int> updatePlannedForSite = new Dictionary<Site, int>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool IsFirstSiteReached = false;
			bool IsSecondSiteReached = false;
			List<Site> forbiddenSites = new List<Site>();
			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);
			Site siteToClose;
			double newDistanceToStart;
			int yearsBeforeReturn = MapManager.GetTimeBeforeNextHarvest(ModelCore, startingSite);
			int IDOfRoadToConstruct = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofPotentialRoadForRepeatedEntry(yearsBeforeReturn);
			bool debugMessages = false;
			bool restrictToExistingRoads = false;
			bool endingFoundWithEndPathInFirstSearch = false;
            bool endingFoundWithEndPathInSecondSearch = false;

            // Useless assignment to please the gods of C#
            Site firstSiteReached = startingSite;
			Site secondSiteReached = startingSite;
            List<Site> listOfSitesInFirstLeastCostPath = new List<Site>();
            List<Site> listOfSitesInSecondLeastCostPath = new List<Site>();
            EndPath endPathForFirstPath = null;
            EndPath endPathForSecondPath = null;

			restartLoop:

            //if (startingSite.Location.ToString() == "(74, 58)") { debugMessages = true; MapManager.WriteMap(PlugIn.Parameters.OutputsOfRoadNetworkMaps + "-DEBUG", ModelCore); }
            //else { debugMessages = false; }

            // We loop until the list is empty
            while (frontier.Count > 0)
			{
                if (debugMessages) { ModelCore.UI.WriteLine("Number of sites in Dijkstra queue = " + frontier.Count); }
                siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose))
				{

                    if (debugMessages) { ModelCore.UI.WriteLine("Checking to open neighbor" + neighbourToOpen.Location); }
                    if (debugMessages) { ModelCore.UI.WriteLine("Neighbor has type : " + SiteVars.RoadsInLandscape[neighbourToOpen].typeNumber); }
                    if (debugMessages) { ModelCore.UI.WriteLine("Neighbor has age : " + SiteVars.RoadsInLandscape[neighbourToOpen].roadAge); }
                    // We don't consider the neighbour if it is closed or if it's non-constructible, or if it's the forbiden list of sites for making a proper loop.
                    if ((SiteVars.CostRasterWithRoads[neighbourToOpen] >= 0) && (!isClosed.Contains(neighbourToOpen) && (!forbiddenSites.Contains(neighbourToOpen))))
					{
						// If neighbor is associated to an endpath, we can pause the search and see if we can end it here and there.
						if (PlugIn.endPathsAssociatedToSite.ContainsKey(neighbourToOpen))
						{
                            if (debugMessages) { ModelCore.UI.WriteLine("Neighbor is associated to EndPath."); }
                            bool destroyPath = false;
							// We check if the road ID to build and the wood to flux are not too much for this endpath.
							// WARNING : If it's a second path for a loop, no need to check the flux. We don't flux down this path.
							if (woodFluxActivated && !firstSiteReached && (woodfluxNumber > PlugIn.endPathsAssociatedToSite[neighbourToOpen].woodFluxBeforeUpdate))
							{
								destroyPath = true;
                                if (debugMessages) { ModelCore.UI.WriteLine("Destroying EndPath because of woodflux."); }
                            }
							// If the road ID to build with this road is higher than the lowest road ID in the end path, this means we'll need to do updates
							// in this end path. Hence, we'll destroy it and search instead.
							if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(IDOfRoadToConstruct, PlugIn.endPathsAssociatedToSite[neighbourToOpen].lowestRoadID))
							{
								destroyPath = true;
                                if (debugMessages) { ModelCore.UI.WriteLine("Destroying EndPath because of ID to construct.."); }
                            }
							// If it's not OK, we dissolve the flux path, and we keep going with the search.
							if (destroyPath) { PlugIn.endPathsAssociatedToSite[neighbourToOpen].DissolveEndPath(); }

							else
							{
                                if (debugMessages) { ModelCore.UI.WriteLine("EndPath not destroyed : ending the search."); }

                                if (!IsFirstSiteReached)
                                {
                                    listOfSitesInFirstLeastCostPath = PlugIn.endPathsAssociatedToSite[neighbourToOpen].GetRestOfPath(neighbourToOpen);
                                    List<Site> restOfPath = MapManager.FindPathToStart(startingSite, siteToClose, predecessors, true);
                                    listOfSitesInFirstLeastCostPath.AddRange(restOfPath);
                                    firstSiteReached = listOfSitesInFirstLeastCostPath.First();
                                    endingFoundWithEndPathInFirstSearch = true;
                                    endPathForFirstPath = PlugIn.endPathsAssociatedToSite[neighbourToOpen];
                                    // We go to the rest of the function : have we found a first path, or a second path if we're looping ?
                                    goto endingChecks;
                                }

                                else // if first site has been reached and we're still there, we have to be looping.
                                {
                                    listOfSitesInSecondLeastCostPath = PlugIn.endPathsAssociatedToSite[neighbourToOpen].GetRestOfPath(neighbourToOpen);
                                    List<Site> restOfPath = MapManager.FindPathToStart(startingSite, siteToClose, predecessors, true);
                                    listOfSitesInSecondLeastCostPath.AddRange(restOfPath);
                                    secondSiteReached = listOfSitesInSecondLeastCostPath.First();
                                    endingFoundWithEndPathInSecondSearch = true;
                                    endPathForSecondPath = PlugIn.endPathsAssociatedToSite[neighbourToOpen];
                                    goto endingChecks;
                                }
                            }
                        }

                        // if (debugMessages) { ModelCore.UI.WriteLine("Conditions to open neighbor are OK."); }
                        // If the neighbor has no road, we consider the construction of a road to it.
                        if (!SiteVars.RoadsInLandscape[neighbourToOpen].IsARoad)
                        {
                            // if (debugMessages) { ModelCore.UI.WriteLine("Neighbor has no road, computing cost normally."); }
                            // We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
                            // from the node to close + the cost between it and the neighbor.
                            newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen);
						}
						// If the neighbor has a road, we consider if we have to upgrade it.
						else
						{
                            if (debugMessages) { ModelCore.UI.WriteLine("Neighbor has road, computing upgrade."); }
                            // We check the current ID of the road, versus the one if we need to return to the cell, versus the one we need because of the woodflux.
                            // The highest/costlier ID is the one we need.
                            // First, check if it is not an exit point; we can't update those.
                            if (!SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo)
							{
								// If we've found a road connected to an exit point, we will restrict the search to navigating inside the road network.
								// This prevents complete optimisation of both road construction and road upgrades; but it's much quicker.
								if (SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill)
								{
                                    if (debugMessages) { ModelCore.UI.WriteLine("Neighbor has road and is connected to exist point. Restricting to roads; restriction before was : " + restrictToExistingRoads); }
                                    restrictToExistingRoads = true; 
								}

                                int currentRoadID = SiteVars.RoadsInLandscape[neighbourToOpen].typeNumber;
								int highestRankID = 0;

								if (woodFluxActivated)
								{
                                    highestRankID = PlugIn.Parameters.RoadCatalogueNonExit.whoIsRoadOfHigherRank(new List<int>() { SiteVars.RoadsInLandscape[neighbourToOpen].UpdateNeedIfWoodFluxIncrease(woodfluxNumber), IDOfRoadToConstruct, currentRoadID });
                                }
								else
								{
									highestRankID = PlugIn.Parameters.RoadCatalogueNonExit.whoIsRoadOfHigherRank(new List<int>() { IDOfRoadToConstruct, currentRoadID });
								}

								// If highest rank = currentRoadID, no update
								if (highestRankID != currentRoadID)
								{
                                    // if (debugMessages) { ModelCore.UI.WriteLine("Checking updatePlannedForSite dictionnary."); }
                                    // We register the update to do it later when the path is found, but only if there's not an upgrade to a bigger level already planned
                                    if (updatePlannedForSite.ContainsKey(neighbourToOpen))
									{
										if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(highestRankID, updatePlannedForSite[neighbourToOpen])) { updatePlannedForSite[neighbourToOpen] = highestRankID; }
									}
									else
									{
                                        updatePlannedForSite[neighbourToOpen] = highestRankID;
                                    }

                                    // if (debugMessages) { ModelCore.UI.WriteLine("Updating cost so far with update."); }
                                    // We compute the cost : any transition cost from the two due to construction, the cost of update.
                                    // Things are a bit ambiguous here; it was easier to compute the costs of construction going from centroid to centroid (so, half of the cells) to account for
                                    // movements in diagonal being more expensive.
                                    // However, it's easier to compute road upgrades from pixel to pixels, as it's difficult to upgrade half a pixel.
                                    // Therefore, the cost for construction computed with MapManager.CostOfTransition is going to be between centroid; but if we upgrade, it's going to be pixel by pixel.
                                    newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen) + (SiteVars.RoadsInLandscape[neighbourToOpen].ComputeUpdateCost(neighbourToOpen, currentRoadID, highestRankID));
								}
								else // If no update, cost is as usual; 
								{
									newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen);
								}
							}
							else // If neighbor is exit point, no upgrade, so cost as usual.
							{
								newDistanceToStart = costSoFar[siteToClose] + MapManager.CostOfTransition(siteToClose, neighbourToOpen);
							}
						}

                        // If the node isn't opened yet, or if it is opened and going to start throught the current node to close is closer; then, 
                        // this node to close will become its predecessor, and its distance to start will become this one.
                        // We will not add the neighbor if it's not part of the existing road network and we're restricting ourselves to the road network.
                        if ((!costSoFar.ContainsKey(neighbourToOpen) || newDistanceToStart < costSoFar[neighbourToOpen]) && (SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill || !restrictToExistingRoads))
						{
                            // if (debugMessages) { ModelCore.UI.WriteLine("Opening neighbour in frontier."); }
                            // if (debugMessages) { ModelCore.UI.WriteLine("Neighbour is officially opened."); }
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
                            // if (debugMessages) { ModelCore.UI.WriteLine("Frontier count before end of loop = " + frontier.Count); }
                        }

						endingChecks:

                        // Conditions for stopping
                        // 1. We're not doing a loop, and we've reached a exit point.
                        // We simply finish the search.
                        if (!IsFirstSiteReached && (SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo || endingFoundWithEndPathInFirstSearch))
						{
                            IsFirstSiteReached = true;

                            if (!endingFoundWithEndPathInFirstSearch)
							{
                                if (debugMessages) { ModelCore.UI.WriteLine("First path has been found."); }
                                // We register the arrival and the path
                                firstSiteReached = neighbourToOpen;
                                listOfSitesInFirstLeastCostPath = MapManager.FindPathToStart(startingSite, firstSiteReached, predecessors);
                            }

							// If no loop, we're done.
							if (!loopingActivated) {goto End;}

							// If loop, we keep going for the second search.
							else
							{
								// To create the loop in the same way as in version 1.3, we will find the first cell the algorithm connected to that had
								// a road connected to an exit point; we will create a forbiden zone around this cell to force the loop to be done in
								// the second path.
								listOfSitesInFirstLeastCostPath.Reverse(); // We reverse because we need to start from the first site in the line afterward
								foreach (Site site in listOfSitesInFirstLeastCostPath)
								{
									if (SiteVars.RoadsInLandscape[site].isConnectedToSawMill)
									{
										forbiddenSites = MapManager.GetNearbySites(searchNeighborhood, site);
										// We remove the roads from the forbidden sites; if not, the current algorithm can get stuck.
										forbiddenSites.RemoveAll(forbidenSite => SiteVars.RoadsInLandscape[forbidenSite].isConnectedToSawMill);
                                        goto continuingLoop;
                                    }
								}
								// Reverse again because the lines at the end of the function need it
								listOfSitesInFirstLeastCostPath.Reverse();

								continuingLoop:
								// We reset the search to start again, but find the second path
								predecessors = new Dictionary<Site, Site>();
								costSoFar = new Dictionary<Site, Double>();
								isClosed = new HashSet<Site>();
								frontier = new Priority_Queue.SimplePriorityQueue<Site>();
								costSoFar[startingSite] = 0;
								frontier.Enqueue(startingSite, 0);
                                restrictToExistingRoads = false;
                                // ModelCore.UI.WriteLine("Searching second path...");

                                goto restartLoop;
                            }
						}

						// 2. We're doing a loop, and we've reached an exit point for the second time.
						else if (loopingActivated && IsFirstSiteReached && (SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo || endingFoundWithEndPathInSecondSearch))
						{
                            IsSecondSiteReached = true;

                            if (debugMessages) { ModelCore.UI.WriteLine("Second path found."); }
                            // We simply close everything.
                            if (!endingFoundWithEndPathInSecondSearch)
							{
                                secondSiteReached = neighbourToOpen;
                                listOfSitesInSecondLeastCostPath = MapManager.FindPathToStart(startingSite, secondSiteReached, predecessors);
                            }

							goto End; 
						}
					} // End of looking at one neighbour
				} // End of looking at all neighbours in neighboorhood of a site
				isClosed.Add(siteToClose);
			}// End of while loop for the frontier

			End:
			
			// We start by computing the cost of the first road, and constructing it
			if (IsFirstSiteReached)
			{
				double costOfConstructionInFirstPath = 0;
				double costOfUpgradesInFirstPath = 0;
				for (int i = 0; i < listOfSitesInFirstLeastCostPath.Count; i++)
				{
                    // Werever we update it or not, this pixel is now going to be connected to an exit point.
                    SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].isConnectedToSawMill = true;

                    // If there is no road on this site, we construct it.
                    if (!SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].IsARoad)
					{
						SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].typeNumber = IDOfRoadToConstruct;
						// We update the cost raster that contains the roads.
						SiteVars.CostRasterWithRoads[listOfSitesInFirstLeastCostPath[i]] = 0;
						// We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
						if (i < listOfSitesInFirstLeastCostPath.Count - 1) costOfConstructionInFirstPath += MapManager.CostOfTransition(listOfSitesInFirstLeastCostPath[i], listOfSitesInFirstLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
					}
					// If there is a road, we check if we should update it.
					else
					{
						if (updatePlannedForSite.ContainsKey(listOfSitesInFirstLeastCostPath[i]))
						{
							int oldTypeNumber = SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].typeNumber;
							costOfUpgradesInFirstPath += SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].ComputeUpdateCost(listOfSitesInFirstLeastCostPath[i], oldTypeNumber, updatePlannedForSite[listOfSitesInFirstLeastCostPath[i]]);
							SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].typeNumber = updatePlannedForSite[listOfSitesInFirstLeastCostPath[i]];
                            SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].roadAge = 0; // We reset the age
							// We destroy the key in the dictionnary; in this way, the update is considered to be done. It will not need to be done for the second path later.
							updatePlannedForSite.Remove(listOfSitesInFirstLeastCostPath[i]);
                        }

                    }

                    // If the wood flux is simulated, we add it along the cells of this path.
                    // If an upgrade due to the woodflux is needed, it has been done already because it was recorded in updatePlannedForSite.
                    if (woodFluxActivated)
					{
						SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].timestepWoodFlux += woodfluxNumber;
                    }
					
				}

				// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
				RoadNetwork.lastArrivalSiteOfDijkstraSearch = firstSiteReached;
				RoadNetwork.costOfLastPath = costOfConstructionInFirstPath + costOfUpgradesInFirstPath;
				RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfConstructionInFirstPath + costOfUpgradesInFirstPath;

				// Now that everything if done and that we have at least one road, we can construct an endPath object to make future searches quicker.
				// We initialize it.
				if (!endingFoundWithEndPathInFirstSearch)
				{
                    // Also, we don't make an endpath if the path is too short.
                    if (listOfSitesInFirstLeastCostPath.Count > 3) { EndPath newEndPath = new EndPath(listOfSitesInFirstLeastCostPath); }
                }
				else // If we found one, we update it.
				{
					endPathForFirstPath.UpdateEndPath();
                }

                // Now, if a second site was reached, we check how much it cost. If it's not too costly AND a probabilities are OK (see probability of loop construction parameter), we build it.
                if (loopingActivated && IsSecondSiteReached)
				{
					double costOfConstructionInSecondPath = 0;
                    double costOfUpgradesInSecondPath = 0;
                    for (int i = 0; i < listOfSitesInSecondLeastCostPath.Count; i++)
					{
						if (!SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].IsARoad)
						{
                            if (i < listOfSitesInSecondLeastCostPath.Count - 1) costOfConstructionInSecondPath += MapManager.CostOfTransition(listOfSitesInSecondLeastCostPath[i], listOfSitesInSecondLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
                        }
						else
						{
                            if (updatePlannedForSite.ContainsKey(listOfSitesInSecondLeastCostPath[i]))
                            {
                                int oldTypeNumber = SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].typeNumber;
                                costOfUpgradesInSecondPath += SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].ComputeUpdateCost(listOfSitesInSecondLeastCostPath[i], oldTypeNumber, updatePlannedForSite[listOfSitesInSecondLeastCostPath[i]]);
                            }
                        }
                    }
					// If this second least cost path is not too costly AND a probabilities are OK (see probability of loop construction parameter), we build it., then we'll build it too.
					// The random number will be between 1 and 100, and it must be inferior to 100 - the probability parameter. This way, the higher the probability parameter, 
					// the higher the chance that the random number will be above the threshold.
					// We use the random number generator from the LANDIS-II core, which implies that results will always be the same as long as the random number seed given with the scenario is the same.
					if (((costOfConstructionInSecondPath + costOfUpgradesInSecondPath) / loopingMaxCost) < (costOfConstructionInFirstPath + costOfUpgradesInFirstPath) && (PlugIn.ModelCore.GenerateUniform()*100) > (100 - PlugIn.Parameters.LoopingProbability))
					{
						for (int i = 0; i < listOfSitesInSecondLeastCostPath.Count; i++)
						{
                            // Werever we update it or not, this pixel is now going to be connected to an exit point.
                            SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].isConnectedToSawMill = true;

                            // If there is no road on this site, we construct it.
                            if (!SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].IsARoad)
                            {
                                SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].typeNumber = IDOfRoadToConstruct;
                                // Whatever it is, we indicate it as connected.
                                SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].isConnectedToSawMill = true;
                                // We update the cost raster that contains the roads.
                                SiteVars.CostRasterWithRoads[listOfSitesInSecondLeastCostPath[i]] = 0;
                                // We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
                                if (i < listOfSitesInSecondLeastCostPath.Count - 1) costOfConstructionInSecondPath += MapManager.CostOfTransition(listOfSitesInSecondLeastCostPath[i], listOfSitesInSecondLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
                            }
                            // If there is a road, we check if we should update it.
                            else
                            {
                                if (updatePlannedForSite.ContainsKey(listOfSitesInSecondLeastCostPath[i]))
                                {
                                    int oldTypeNumber = SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].typeNumber;
                                    costOfUpgradesInSecondPath += SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].ComputeUpdateCost(listOfSitesInSecondLeastCostPath[i], oldTypeNumber, updatePlannedForSite[listOfSitesInSecondLeastCostPath[i]]);
                                    SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].typeNumber = updatePlannedForSite[listOfSitesInSecondLeastCostPath[i]];
                                    SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].roadAge = 0; // We reset the age
                                    updatePlannedForSite.Remove(listOfSitesInSecondLeastCostPath[i]);
                                }

                            }

                            // If the wood flux is simulated, we add it along the cells of this path.
                            // If an upgrade due to the woodflux is needed, it has been done already because it was recorded in updatePlannedForSite.
							// EDIT : Actually, we don't; because if we do, we're fluxing the same wood twice. The hypothesis will be that the wood
							// goes through the first road, and that's it.
                            //if (woodFluxActivated)
                            //{
                            //    SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].timestepWoodFlux += woodfluxNumber;
                            //}
						}

						// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
						RoadNetwork.lastArrivalSiteOfDijkstraSearch = secondSiteReached;
						RoadNetwork.costOfLastPath = costOfConstructionInSecondPath + costOfUpgradesInSecondPath;
						RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfConstructionInSecondPath + costOfUpgradesInSecondPath;

						// If we have found the first path using an endpath, but that we didn't run into any
						// endpath for the second path, we can create one. It's because in that way, we know
						// that we haven't created a new EndPath with the first path; and we know we haven't
						// found the same EndPath as the one before, thus avoiding to collide two EndPaths.
						if (endingFoundWithEndPathInFirstSearch && !endingFoundWithEndPathInSecondSearch)
						{
                            if (listOfSitesInSecondLeastCostPath.Count > 3) { EndPath secondNewEndPath = new EndPath(listOfSitesInSecondLeastCostPath); }
                        }
						else if(endingFoundWithEndPathInSecondSearch) // If we found one, we update it.
						{
							endPathForSecondPath.UpdateEndPath();
						}

                        // If both roads have been constructed, we return that it's the case
                        return (2);

					}
				}
				// If only one road has been constructed, we return that that's the case.
				return (1);

                // We update the woodflux and lowest ID that of the endpath we met if we did meet one.

            }
            else throw new Exception("FOREST ROADS SIMULATION ERROR : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen. Check if there are exit points in your landscape, and if they are reachable by the pathfinding algorithm (e.g. not surrounded by areas we roads can't be built)." + PlugIn.errorToGithub);
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
		/// /// <param name="initialisation">
		/// True if this is done for the initialisation of the module; false if not.
		/// </param>
		public static void DijkstraLeastCostPathToClosestConnectedRoad(ICore ModelCore, Site startingSite, bool initialisation = false)
		{
			// We initialize the frontier of the algorithm and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool haveWeFoundARoadToConnectTo = false;
			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);
			Site siteToClose;
			double newDistanceToStart;

			// Useless assignement to please the gods of C#
			Site arrivalSite = startingSite;

			// We loop until the list is empty (all cells of the landscape have been considered), or until we find an arrival cell
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
				double costOfPath = 0;
				// If we are in the initiation phase, there is no need to check the strategy of the type of road we are going to construct.
				if (initialisation)
				{
					for (int i = 0; i < listOfSitesInLeastCostPath.Count; i++)
					{
						// If there is no road on this site, we construct it. It'll be the smallest type of road.
						if (!SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType();
						// Whatever it is, we indicate it as connected.
						SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].isConnectedToSawMill = true;
						// We update the cost raster that contains the roads.
						SiteVars.CostRasterWithRoads[listOfSitesInLeastCostPath[i]] = 0;
						// We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
						if (i < listOfSitesInLeastCostPath.Count - 1) costOfPath += MapManager.CostOfTransition(listOfSitesInLeastCostPath[i], listOfSitesInLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType());
					}
				}
				// If we are building a road during the simulation, we are going to check wherever it's better to build a simple road or not.
				else
				{
					// Before we decide what type of road to construct, we need information on the stand in which the starting site is. In particular,
					// we want to know if there is going to be a repeated entry in this stand soon.
					// To know that, we need to know which is the last presciption applied to this stand; if it's a multipleRepeat, we need to know the period.
					// If not, it's a single repeat; if so, we need to know the time for which the stand is set aside.
					int yearsBeforeReturn = MapManager.GetTimeBeforeNextHarvest(ModelCore, startingSite);
					int IDOfRoadToConstruct = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofPotentialRoadForRepeatedEntry(yearsBeforeReturn);
					for (int i = 0; i < listOfSitesInLeastCostPath.Count; i++)
					{
						// If there is no road on this site, we construct it.
						if (!SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber = IDOfRoadToConstruct;
						// Whatever it is, we indicate it as connected.
						SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].isConnectedToSawMill = true;
						// We update the cost raster that contains the roads.
						SiteVars.CostRasterWithRoads[listOfSitesInLeastCostPath[i]] = 0;
						// We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
						if (i < listOfSitesInLeastCostPath.Count - 1) costOfPath += MapManager.CostOfTransition(listOfSitesInLeastCostPath[i], listOfSitesInLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
					}
					// Finally, we upgrade the rest of the way towards an exit point if needed (if we constructed something else than the lowest type of roads)
					if (IDOfRoadToConstruct != PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType())
					{
						DijkstraLeastCostPathUpgradeRoadForRepeatedEntry(ModelCore, arrivalSite, IDOfRoadToConstruct);
					}
				}
				// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
				RoadNetwork.lastArrivalSiteOfDijkstraSearch = arrivalSite;
				RoadNetwork.costOfLastPath = costOfPath;
				RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfPath;
			}
			else throw new Exception("FOREST ROADS SIMULATION ERROR : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen. Check if there are exit points in your landscape, and if they are reachable by the pathfinding algorithm (e.g. not surrounded by areas we roads can't be built)." + PlugIn.errorToGithub);
		}

		/// <summary>
		/// Finds the least cost path from roads to roads to a exit point for the wood in the landscape, and add the given wood flux to every road visited.
		/// Warning : The starting site must not be inside an existing fluxpath.
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
			double newDistanceToStart;

			// Useless assignement to please the gods of C#
			Site arrivalSite = startingSite;

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
			// ModelCore.UI.WriteLine("Dijkstra search for wood flux is over.");

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

					// We don't put the arrival site in the fluxpath, because it is part of another fluxpath.
					listOfSitesInLeastCostPath.Remove(arrivalSite);

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
			else throw new Exception("FOREST ROADS SIMULATION ERROR : A Dijkstra search wasn't able to flux the wood from site " + startingSite.Location + " to any exit point. This isn't supposed to happen. Please check the output raster containg the road network for the current timestep (" + ModelCore.CurrentTime + " years) and see if a road is interrupted somewhere." + PlugIn.errorToGithub);
		}

		/// <summary>
		/// Try to make two least cost path to two sites; the second path is only constructed if the site reached is far enough, and if the cost of the path isn't too high.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		/// /// <param name="loopingDistance">
		/// The minimal distance that the second site must be from the first one.
		/// </param>
		/// /// <param name="loopingMaxCost">
		/// The maximal cost that the second road must cost.
		/// </param>
		public static int DijkstraLeastCostPathWithLooping(ICore ModelCore, Site startingSite, List<RelativeLocation> searchNeighborhood, double loopingMaxCost)
		{
			// We initialize the frontier and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool IsFirstSiteReached = false;
			bool IsSecondSiteReached = false;
			List<Site> forbiddenSites = new List<Site>();
			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);
			Site siteToClose;
			double newDistanceToStart;

			// Useless assignment to please the gods of C#
			Site firstSiteReached = startingSite;
			Site secondSiteReached = startingSite;

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSites(siteToClose))
				{
					// We don't consider the neighbour if it is closed or if it's non-constructible, or if it's the forbiden list of sites for making a proper loop.
					if ((SiteVars.CostRasterWithRoads[neighbourToOpen] >= 0) && (!isClosed.Contains(neighbourToOpen) && (!forbiddenSites.Contains(neighbourToOpen))))
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
						// If a first site is reached, we register it, we create the list of forbiden sites not to reach or usen as path, and we remove those sites from the frontier.
						if (!IsFirstSiteReached && SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill) { IsFirstSiteReached = true; firstSiteReached = neighbourToOpen; forbiddenSites = MapManager.GetNearbySites(searchNeighborhood, firstSiteReached);  foreach (Site road in forbiddenSites) frontier.TryRemove(road); }
						// If a second site is reached, we register it, and we end the search.
						if (IsFirstSiteReached) if (SiteVars.RoadsInLandscape[neighbourToOpen].isConnectedToSawMill) if (firstSiteReached != neighbourToOpen) { IsSecondSiteReached = true; secondSiteReached = neighbourToOpen; goto End; }
					}
				}
				isClosed.Add(siteToClose);
			}

			End:
			
			// We start by computing the cost of the first road, and constructing it
			if (IsFirstSiteReached)
			{
				List<Site> listOfSitesInFirstLeastCostPath = MapManager.FindPathToStart(startingSite, firstSiteReached, predecessors);
				double costOfFirstPath = 0;
				int yearsBeforeReturn = MapManager.GetTimeBeforeNextHarvest(ModelCore, startingSite);
				int IDOfRoadToConstruct = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofPotentialRoadForRepeatedEntry(yearsBeforeReturn);
				for (int i = 0; i < listOfSitesInFirstLeastCostPath.Count; i++)
				{
					// If there is no road on this site, we construct it.
					if (!SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].typeNumber = IDOfRoadToConstruct;
					// Whatever it is, we indicate it as connected.
					SiteVars.RoadsInLandscape[listOfSitesInFirstLeastCostPath[i]].isConnectedToSawMill = true;
					// We update the cost raster that contains the roads.
					SiteVars.CostRasterWithRoads[listOfSitesInFirstLeastCostPath[i]] = 0;
					// We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
					if (i < listOfSitesInFirstLeastCostPath.Count - 1) costOfFirstPath += MapManager.CostOfTransition(listOfSitesInFirstLeastCostPath[i], listOfSitesInFirstLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
				}

				// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
				RoadNetwork.lastArrivalSiteOfDijkstraSearch = firstSiteReached;
				RoadNetwork.costOfLastPath = costOfFirstPath;
				RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfFirstPath;

				// Finally, we upgrade the rest of the way towards an exit point if needed (if we constructed something else than the lowest type of roads)
				if (IDOfRoadToConstruct != PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType())
				{
					DijkstraLeastCostPathUpgradeRoadForRepeatedEntry(ModelCore, firstSiteReached, IDOfRoadToConstruct);
				}

				// Now, if a second site was reached, we check how much it cost. Of it's not too costly AND a probabilities are OK (see probability of loop construction parameter), we build it.
				if (IsSecondSiteReached)
				{
					List<Site> listOfSitesInSecondLeastCostPath = MapManager.FindPathToStart(startingSite, secondSiteReached, predecessors);
					double costOfSecondPath = 0;
					for (int i = 0; i < listOfSitesInSecondLeastCostPath.Count; i++)
					{
						if (i < listOfSitesInSecondLeastCostPath.Count - 1) costOfSecondPath += MapManager.CostOfTransition(listOfSitesInSecondLeastCostPath[i], listOfSitesInSecondLeastCostPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
					}
					// If this second least cost path is not too costly AND a probabilities are OK (see probability of loop construction parameter), we build it., then we'll build it too.
					// The random number will be between 1 and 100, and it must be inferior to 100 - the probability parameter. This way, the higher the probability parameter, 
					// the higher the chance that the random number will be above the threshold.
					// We use the random number generator from the LANDIS-II core, which implies that results will always be the same as long as the random number seed given with the scenario is the same.
					if ((costOfSecondPath / loopingMaxCost) < costOfFirstPath && (PlugIn.ModelCore.GenerateUniform()*100) > (100 - PlugIn.Parameters.LoopingProbability))
					{
						for (int i = 0; i < listOfSitesInSecondLeastCostPath.Count; i++)
						{
							// If there is no road on this site, we construct it.
							if (!SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].typeNumber = IDOfRoadToConstruct;
							// Whatever it is, we indicate it as connected.
							SiteVars.RoadsInLandscape[listOfSitesInSecondLeastCostPath[i]].isConnectedToSawMill = true;
							// We update the cost raster that contains the roads.
							SiteVars.CostRasterWithRoads[listOfSitesInSecondLeastCostPath[i]] = 0;
						}

						// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
						RoadNetwork.lastArrivalSiteOfDijkstraSearch = secondSiteReached;
						RoadNetwork.costOfLastPath = costOfSecondPath;
						RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfSecondPath;

						// Finally, we upgrade the rest of the way towards an exit point if needed (if we constructed something else than the lowest type of roads)
						if (IDOfRoadToConstruct != PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType())
						{
							DijkstraLeastCostPathUpgradeRoadForRepeatedEntry(ModelCore, secondSiteReached, IDOfRoadToConstruct);
						}
						// If both roads have been constructed, we return that it's the case
						return (2);
					}
				}
				// If only one road has been constructed, we return that that's the case.
				return (1);
			}
			else throw new Exception("FOREST ROADS SIMULATION ERROR : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen. Check if there are exit points in your landscape, and if they are reachable by the pathfinding algorithm (e.g. not surrounded by areas we roads can't be built)." + PlugIn.errorToGithub);
		}

		/// <summary>
		/// Finds the least cost path from a given road pixel (that has to be connected to an exit point) to the nearest exit point pixel using roads,
		/// and tries to upgrade the road pixels of this path to the given road type ID. This function is used if there will be another harvest in a given
		/// harvested cell later, and if the module has decided to construct a sturdy road that will be there when the harvest will be done a second time.
		/// If that's the case, this function will make sure that there will be a path from the harvested cell to an exit point that will last until then.
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="startingSite">
		/// The starting site of the search. Has to be a road pixel connected to an exit point.
		/// </param>
		/// /// <param name="roadTypeIdToUpgradeTo">
		/// True if this is done for the initialisation of the module; false if not.
		/// </param>
		public static void DijkstraLeastCostPathUpgradeRoadForRepeatedEntry(ICore ModelCore, Site startingSite, int roadTypeIdToUpgradeTo)
		{
			// We initialize the frontier and everything else
			Priority_Queue.SimplePriorityQueue<Site> frontier = new Priority_Queue.SimplePriorityQueue<Site>();
			Dictionary<Site, Site> predecessors = new Dictionary<Site, Site>();
			Dictionary<Site, double> costSoFar = new Dictionary<Site, Double>();
			HashSet<Site> isClosed = new HashSet<Site>();
			bool haveWeFoundAnExitPoint = false;
			costSoFar[startingSite] = 0;
			frontier.Enqueue(startingSite, 0);
			Site siteToClose;
			double newDistanceToStart;

			// Useless assignement to please the gods of C#
			Site arrivalSite = startingSite;

			// We loop until the list is empty
			while (frontier.Count > 0)
			{
				siteToClose = frontier.Dequeue();

				// We look at each of its neighbours, road on them or not.
				foreach (Site neighbourToOpen in MapManager.GetNeighbouringSitesWithRoads(siteToClose))
				{
					// We don't consider the neighbour if it is closed or if it's non-constructible.
					if ((SiteVars.CostRasterWithRoads[neighbourToOpen] >= 0) && (!isClosed.Contains(neighbourToOpen)))
					{
						// We get the value of the distance to start by using the current node to close, which is just an addition of the distance to the start 
						// from the node to close + 1 since we are only considering existing roads
						newDistanceToStart = costSoFar[siteToClose] + 1;

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
						if (SiteVars.RoadsInLandscape[neighbourToOpen].IsAPlaceForTheWoodToGo) { arrivalSite = neighbourToOpen; haveWeFoundAnExitPoint = true; goto End; }
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
			if (haveWeFoundAnExitPoint)
			{
				List<Site> listOfSitesInLeastCostPath = MapManager.FindPathToStart(startingSite, arrivalSite, predecessors);
				// We will now try to update the roads in the path.
				double costOfUpgrades = 0;
				// We don't take the last site of the path, as it will be an exit point, and cannot be upgraded.
				for (int i = 0; i < listOfSitesInLeastCostPath.Count -1; i++)
				{
					// We check if the type we want to upgrade it too is of higher rank than the road type on the pixel
					if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(roadTypeIdToUpgradeTo, SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber))
					{
						// If so, we upgrade it !
						int oldTypeNumber = SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber;
						costOfUpgrades += SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].ComputeUpdateCost(listOfSitesInLeastCostPath[i], oldTypeNumber, roadTypeIdToUpgradeTo);
						SiteVars.RoadsInLandscape[listOfSitesInLeastCostPath[i]].typeNumber = roadTypeIdToUpgradeTo;
					}
				}

				// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
				RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfUpgrades;
			}
			else throw new Exception("FOREST ROADS SIMULATION ERROR : A Dijkstra search wasn't able to connect the site " + startingSite.Location + " to any site. This isn't supposed to happen." + PlugIn.errorToGithub);
		}

		/// <summary>
		/// Construct a path that was determined in "DijkstraLoopingTestPath"
		/// </summary>
		/// /// <param name="ModelCore">
		/// The model's core framework.
		/// </param>
		/// /// <param name="listOfSitesInPath">
		/// The list of paths in the sites, normally taken from "DijkstraLoopingTestPath"
		/// </param>
		public static void ConstructLoopingTestPath(ICore ModelCore, List<Site> listOfSitesInPath, Site startingSite)
		{
			double costOfPath = 0;
			int yearsBeforeReturn = MapManager.GetTimeBeforeNextHarvest(ModelCore, startingSite);
			int IDOfRoadToConstruct = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofPotentialRoadForRepeatedEntry(yearsBeforeReturn);
			for (int i = 0; i < listOfSitesInPath.Count; i++)
			{
				// If there is no road on this site, we construct it.
				if (!SiteVars.RoadsInLandscape[listOfSitesInPath[i]].IsARoad) SiteVars.RoadsInLandscape[listOfSitesInPath[i]].typeNumber = IDOfRoadToConstruct;
				// Whatever it is, we indicate it as connected.
				SiteVars.RoadsInLandscape[listOfSitesInPath[i]].isConnectedToSawMill = true;
				// We update the cost raster that contains the roads.
				SiteVars.CostRasterWithRoads[listOfSitesInPath[i]] = 0;
				// We also add the cost of transition to the costs of construction and repair for this timestep : it's the cost of transition multiplied by the type of the road that we are constructing. If there are already roads of other types on these cells, it doesn't change anything, as the value in the cost raster is 0 for them.
				if (i < listOfSitesInPath.Count - 1) costOfPath += MapManager.CostOfTransition(listOfSitesInPath[i], listOfSitesInPath[i + 1]) * PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(IDOfRoadToConstruct);
			}
			// We register the informations relative to the arrival site and the path in the RoadNetwork static objects
			RoadNetwork.costOfLastPath = costOfPath;
			RoadNetwork.costOfConstructionAndRepairsAtTimestep += costOfPath;
		}
	} // End of DijkstraSearch class
} // End of namespace
