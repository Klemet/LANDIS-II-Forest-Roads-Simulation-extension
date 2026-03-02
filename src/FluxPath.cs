// Author: Clément Hardy

using Landis.Library.UniversalCohorts;
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
	public class FluxPath
	{
		public FluxPath nextPath;
		// Indicates if this fluxPath is an end of the network; meaning if it leads to an exit point for the wood.
		public bool isAnEnd;
		public List<Site> sitesInPath;
		// Warning : connectionToNext is the site contained in the nextPath, but not in this path where the connection is made.
		public Site connectionToNext;

		// Constructor for the end of the dijkstra search, when we have found a connection to an exit point
		public FluxPath(List<Site> sitesInPath)
		{
			if (sitesInPath.Count == 0) { throw new Exception("FOREST ROADS SIMULATION ERROR : Flux Path Error. Tried to create a path with no sites in it." + PlugIn.errorToGithub); }

			this.sitesInPath = sitesInPath;
			this.isAnEnd = true;
			RoadNetwork.fluxPathCatalogue.Add(this);
			foreach (Site site in sitesInPath)
			{
				RoadNetwork.fluxPathDictionary[site] = this;
			}
		}

		// Constructor for the end of the dijkstra search, when we have found a connection to another path
		public FluxPath(List<Site> sitesInPath, Site connectionToNext)
		{
			if (sitesInPath.Count == 0) { throw new Exception("FOREST ROADS SIMULATION ERROR : Tried to create a path with no sites in it." + PlugIn.errorToGithub); }

			// We create this path properly
			this.sitesInPath = sitesInPath;
			this.connectionToNext = connectionToNext;
			this.nextPath = RoadNetwork.fluxPathDictionary[connectionToNext];
			this.isAnEnd = false;
			RoadNetwork.fluxPathCatalogue.Add(this);
			foreach (Site site in sitesInPath)
			{
				RoadNetwork.fluxPathDictionary[site] = this;
			}
		}

		/// <summary>
		/// This function will "flux" the wood from path to path using connection points and list of sites in the paths until it meets an exit path.
		/// </summary>
		/// <param name="connectionToThisSite">The site from which we will beguin to flux on this current path. Has to be in the current path.</param>
		/// <param name="woodFlux">The amount of wood to be fluxed along the paths to an exit point</param>
		public void FluxPathFromSite(Site connectionToThisPath, double woodFlux)
		{
			FluxPath currentPath = this;
			Site currentConnectionPoint = connectionToThisPath;
			int connectionIndex;
			List<Site> sitesTofFlux;

			while (!currentPath.isAnEnd)
			{
				// Case of micro-paths (1 site), in order to avoid errors.
				if (currentPath.sitesInPath.Count == 1)
				{
					SiteVars.RoadsInLandscape[currentPath.sitesInPath[0]].timestepWoodFlux += woodFlux;
				}
				else
				{
					connectionIndex = currentPath.sitesInPath.IndexOf(currentConnectionPoint);
					if (connectionIndex == -1) { throw new Exception("FOREST ROADS SIMULATION ERROR : During the computing of the flux of wood, site " + currentConnectionPoint + " was not found in the corresponding path that contained " + currentPath.sitesInPath.Count + " sites. This is not normal." + PlugIn.errorToGithub); }
					sitesTofFlux = currentPath.sitesInPath.GetRange(connectionIndex, (currentPath.sitesInPath.Count - connectionIndex));

					foreach (Site siteToFlux in sitesTofFlux)
					{
						SiteVars.RoadsInLandscape[siteToFlux].timestepWoodFlux += woodFlux;
					}
				}

				currentConnectionPoint = currentPath.connectionToNext;
				currentPath = currentPath.nextPath;
			}

			// Ending when we reached a currentPath that is an end

			// Case of micro-paths (1 site), in order to avoid errors.
			if (currentPath.sitesInPath.Count == 1)
			{
				SiteVars.RoadsInLandscape[currentPath.sitesInPath[0]].timestepWoodFlux += woodFlux;
			}
			else
			{
				connectionIndex = currentPath.sitesInPath.IndexOf(currentConnectionPoint);
				if (connectionIndex == -1) { throw new Exception("FOREST ROADS SIMULATION ERROR : During the computing of the flux of wood, site " + currentConnectionPoint + " was not found in the corresponding path that contained " + currentPath.sitesInPath.Count + " sites. This is not normal." + PlugIn.errorToGithub); }
				sitesTofFlux = currentPath.sitesInPath.GetRange(connectionIndex, (currentPath.sitesInPath.Count - connectionIndex));

				foreach (Site siteToFlux in sitesTofFlux)
				{
					SiteVars.RoadsInLandscape[siteToFlux].timestepWoodFlux += woodFlux;
				}
			}

		}






	}
}
