using Landis.Library.AgeOnlyCohorts;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using System.IO;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;

namespace Landis.Extension.ForestRoadsSimulation 
{
	/// <summary>
	/// A class made to help the Dijkstra algorithm to be simpler. See "DijkstraSearch" class for use.
	/// </summary>
	public class SiteForPathfinding : IComparable<SiteForPathfinding>
	{
		public Site site;
		public double distanceToStart;
		public SiteForPathfinding predecessor;
		public bool isClosed;
		public bool isOpen;

		public SiteForPathfinding(Site site)
		{
			this.site = site;
			this.distanceToStart = double.PositiveInfinity;
			this.predecessor = null;
			this.isClosed = false;
			this.isOpen = false;
		}

		/// <summary>
		/// Finds the distance to the starting point of the current search via
		/// the predecessors of this site and register it in the object.
		/// WARNING : The function will throw an exception
		/// if the predecessors do not lead to the starting site one way or another.
		/// </summary>
		/// <returns>
		/// A double which is the distance (cost) to the starting site.
		/// </returns>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		public double FindDistanceToStart(SiteForPathfinding startingSite)
		{
			double distanceToStart = 0;
			SiteForPathfinding currentSite = this;
			SiteForPathfinding nextPredecessor;
			bool foundStartingSite = false;
			// Case of this node being the starting site (you never know)
			// so as to avoid potential errors.
			if (this.site.Location == startingSite.site.Location) {foundStartingSite = true; nextPredecessor = currentSite; }
			else nextPredecessor = this.predecessor;

			while (!foundStartingSite)
			{

				distanceToStart = distanceToStart + this.CostOfTransition(nextPredecessor.site);
				if (nextPredecessor.site.Location == startingSite.site.Location) foundStartingSite = true;
				else
				{
					currentSite = nextPredecessor;
					nextPredecessor = currentSite.predecessor;
				}
			}

			return(distanceToStart);
		}

		/// <summary>
		/// This function retrieves the list of sites that are used in a least-cost path
		/// to go back to the starting site. Best used if the current site is the goal that
		/// have been reached.
		/// </summary>
		/// <returns>
		/// A list of sites that are the least-cost path. The list will not contain the current
		/// site, as it is the goal.
		/// </returns>
		/// /// <param name="startingSite">
		/// The starting site of the search.
		/// </param>
		public List<Site> FindPathToStart(SiteForPathfinding startingSite)
		{
			List<Site> ListOfSitesInThePath = new List<Site>();
			SiteForPathfinding currentSite = this;
			SiteForPathfinding nextPredecessor;
			bool foundStartingSite = false;
			// Case of this node being the starting site (you never know)
			// so as to avoid potential errors.
			if (this.site.Location == startingSite.site.Location) { foundStartingSite = true; nextPredecessor = currentSite; }
			else nextPredecessor = this.predecessor;

			while (!foundStartingSite)
			{

				ListOfSitesInThePath.Add(nextPredecessor.site);
				if (nextPredecessor.site.Location == startingSite.site.Location) foundStartingSite = true;
				else
				{
					currentSite = nextPredecessor;
					nextPredecessor = currentSite.predecessor;
				}
			}

			return (ListOfSitesInThePath);
		}

		/// <summary>
		/// Calculate the cost of going from this site to another.
		/// </summary>
		/// <returns>
		/// A double which is the cost of transition.
		/// </returns>
		/// /// <param name="otherSite">
		/// The other site where we want to go to.
		/// </param>
		public double CostOfTransition(Site otherSite)
		{
			// The cost of transition is half the transition in this pixel, and half the transition in the other, as we're going from centroid to centroid.
			double cost = (SiteVars.CostRaster[this.site] + SiteVars.CostRaster[otherSite])/2 ;

			// We multiply the cost according to the distance (diagonal or not)
			if (otherSite.Location.Row != this.site.Location.Row && otherSite.Location.Column == this.site.Location.Column) cost = cost * Math.Sqrt(2.0);

			return (cost);
		}

		/// <summary>
		/// A function to compare two sites for the priority queue in the dijkstra algorithm. If one of the two sites has a smaller distance to start than the other, then it has a bigger priority.
		/// </summary>
		/// <param name="other">The other site to compare this one too.</param>
		public int CompareTo(SiteForPathfinding other)
		{
			if (other.distanceToStart > this.distanceToStart) { return (-1); }
			else if (other.distanceToStart < this.distanceToStart) { return (1); }
			else { return (0); }
		}
	}
}
