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
	class SiteForPathfinding
	{
		public Site site;
		public double distanceToStart;
		public SiteForPathfinding predecessor;

		public SiteForPathfinding(Site site)
		{
			this.site = site;
			this.distanceToStart = double.PositiveInfinity;
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
			// First, we initialize the cost
			double cost = 0;

			// we add the base cost of crossing the sites
			cost += PlugIn.Parameters.DistanceCost;

			// We add the cost associated with coarse elevation if there was an input of a coarse elevation raster
			if (PlugIn.Parameters.CoarseElevationRaster != "none")
				cost += (Math.Abs(SiteVars.CoarseElevation[this.site] - SiteVars.CoarseElevation[otherSite]) * PlugIn.Parameters.CoarseElevationCost);

			// We multiply with the fine elevation cost if there was a input of fine elevation raster The fine elevation value is the mean of the value for both sites.
			if (PlugIn.Parameters.FineElevationRaster != "none")
				cost = cost * ElevationCostRanges.GetMultiplicativeValue((SiteVars.FineElevation[this.site] + SiteVars.FineElevation[otherSite])/2);

			// We add the coarse water cost if there was an input for the coarse water raster
			if (PlugIn.Parameters.CoarseWaterRaster != "none")
				cost += PlugIn.Parameters.CoarseWaterCost;

			// We add the fine water cost, that will depend on the number of stream crossed, if there was an input of the fine water raster. The number of stream crossed in the mean
			// for the two cells.
			if (PlugIn.Parameters.FineWaterRaster != "none")
				cost += (((SiteVars.FineWater[this.site] + SiteVars.FineWater[otherSite])/2) / PlugIn.ModelCore.CellLength) * PlugIn.Parameters.FineWaterCost;

			// Finally, we add the cost associated with the soil, if there was an input for the soil raster. The cost is the mean of the cost for the two sites.
			if (PlugIn.Parameters.SoilsRaster != "none")
				cost += (SoilRegions.GetAdditionalValue(SiteVars.Soils[this.site]) + SoilRegions.GetAdditionalValue(SiteVars.Soils[otherSite])) / 2;

			// But if the other site has a road a it, then it's half price ! And if the current site has a road on it, it's free !
			if (SiteVars.RoadsInLandscape[otherSite].IsARoad)
			{
				if (SiteVars.RoadsInLandscape[this.site].IsARoad) cost = 0;
				else cost = cost * 0.5;
			}
			// We influence the cost of transition according to the position of the othersite
			// If they share the same row or the same column, then the othersite isn't in a diagonal
			// If not, they are in diagonal, and the score is multiplied by the square root of 2.
			if (otherSite.Location.Row != this.site.Location.Row && otherSite.Location.Column == this.site.Location.Column) cost = cost * Math.Sqrt(2.0);
			
			return (cost);
		}



	}
}
