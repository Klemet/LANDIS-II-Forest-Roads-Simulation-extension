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
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Site = Landis.SpatialModeling.Site;
using System.ComponentModel;

namespace Landis.Extension.ForestRoadsSimulation
{
    public class EndPath
    {
        public List<Site> sitesInPath;
        public double woodFluxBeforeUpdate;
        public int lowestRoadID;
        public Dictionary<Site, Site> nextSiteForDirection;

		// Constructor for an end path : takes a list of sites, checks them to compute the properties of the path.
		// Will also edit the public dictionnary in plugin that keeps track of wherever a cell is associated to a path.
		public EndPath(List<Site> sitesInPath)
		{
			if (sitesInPath.Count == 0) { throw new Exception("FOREST ROADS SIMULATION ERROR : EndPath Error. Tried to create a path with no sites in it." + PlugIn.errorToGithub); }

			this.sitesInPath = new List<Site>();
            sitesInPath.Reverse(); // We want this path to start with the starting, and end with the exit point.
            this.woodFluxBeforeUpdate = double.PositiveInfinity;
			this.lowestRoadID = PlugIn.Parameters.RoadCatalogueNonExit.GetIDofLowestRoadType();
            nextSiteForDirection = new Dictionary<Site, Site>();

            for (int i = 0; i < sitesInPath.Count; i++)
            {
                if (!SiteVars.RoadsInLandscape[sitesInPath[i]].IsAPlaceForTheWoodToGo) // We don't put the exit point in the path; this can make for error if two paths want to end at the same exit point.
                {
                    if (PlugIn.endPathsAssociatedToSite.ContainsKey(sitesInPath[i]))
                    {
                        // If the EndPath dictionnary already contains an EndPath for the site we're trying to add, there's an
                        // issue; that means we're going to collide two EndPaths.
                        throw new Exception("FOREST ROADS SIMULATION ERROR : EndPath Error. Tried to create an EndPath with a site that already was in one. Site was : " + sitesInPath[i].Location + " . " + PlugIn.errorToGithub);
                    }
                    if (i > 0) { this.nextSiteForDirection[sitesInPath[i - 1]] = sitesInPath[i]; }
                    if (PlugIn.Parameters.SimulationOfWoodFlux)
                    {
                        this.woodFluxBeforeUpdate = Math.Min(this.woodFluxBeforeUpdate, SiteVars.RoadsInLandscape[sitesInPath[i]].woodFluxThatRoadCanHandleBeforeUpdate());
                    }
                    if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(this.lowestRoadID, SiteVars.RoadsInLandscape[sitesInPath[i]].typeNumber))
                    {
                        this.lowestRoadID = SiteVars.RoadsInLandscape[sitesInPath[i]].typeNumber;
                    }
                    PlugIn.endPathsAssociatedToSite[sitesInPath[i]] = this;
                    this.sitesInPath.Add(sitesInPath[i]);
                }
            }
            sitesInPath.Reverse(); // Just in case the real object passed as argument is edited, we put it back as it was.
        }

        // Updates the wood flux that the path can handle and the lowest road ID in the path for future checks during dijkstra searches.
        public void UpdateEndPath()
        {
            foreach (Site site in this.sitesInPath)
            {
                if (PlugIn.Parameters.SimulationOfWoodFlux)
                {
                    this.woodFluxBeforeUpdate = Math.Min(this.woodFluxBeforeUpdate, SiteVars.RoadsInLandscape[site].woodFluxThatRoadCanHandleBeforeUpdate());
                }

                if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(this.lowestRoadID, SiteVars.RoadsInLandscape[site].typeNumber))
                {
                    this.lowestRoadID = SiteVars.RoadsInLandscape[site].typeNumber;
                }
            }
        }

        // Gets the rest of a path toward the exit point for a site that has been reached. Reverses it to keep the direction of the paths in the dijstra functions.
        public List<Site> GetRestOfPath(Site startingSite)
        {
            List<Site> listToReturn = new List<Site>();
            listToReturn.Add(startingSite);
            while (nextSiteForDirection.ContainsKey(listToReturn.Last()))
            {
                listToReturn.Add(nextSiteForDirection[listToReturn.Last()]);
            }
            listToReturn.Reverse();
            return (listToReturn);
        }

        // Used when the endpath is not needed anymore. Dis-associate the site with this path, and empty everything.
        public void DissolveEndPath()
        {
            foreach (Site site in sitesInPath)
            {
                PlugIn.endPathsAssociatedToSite.Remove(site);
            }
            this.sitesInPath = null;
            this.woodFluxBeforeUpdate = 0;
            this.lowestRoadID = 0;
        }

        
	}
}
