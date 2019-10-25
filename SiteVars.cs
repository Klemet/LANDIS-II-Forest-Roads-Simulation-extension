//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Landscapes;
using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.AgeOnlyCohorts;
using System.Collections.Generic;
using System.IO;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;
using System.Linq;

namespace Landis.Extension.ForestRoadsSimulation
{
	public static class SiteVars
	{
		// Variable that indicates the zones where roads can be built
		private static SiteVarDistinct<int> buildableZones;
		// Variable that indicates what road is in this site
		private static SiteVarDistinct<RoadType> roadsInLandscape;
		// Variables relative to different characteristics of sites :
		// elevation, water, etc. = 0 if it wasn't inputed by the user.
		private static SiteVarDistinct<int> coarseElevation;
		private static SiteVarDistinct<int> fineElevation;
		private static SiteVarDistinct<int> coarseWater;
		private static SiteVarDistinct<int> fineWater;
		private static SiteVarDistinct<double> soils;
		private static SiteVarDistinct<float> costRaster;
		// The cohorts on the sites.
		private static ISiteVar<ISiteCohorts> cohorts;
		// The road objects associated with each site
		private static SiteVarDistinct<List<IndividualRoad>> roads;

		//---------------------------------------------------------------------

		public static void Initialize()
		{
			// CAREFULL : By default, LANDIS-II creates variables where the inactive sites all have the same value !
			// For our variable to work on inactive sites, we have to tell him to create a variable with distinct values
			// for every inactive site
			buildableZones = (SiteVarDistinct<int>)PlugIn.ModelCore.Landscape.NewSiteVar<int>(InactiveSiteMode.DistinctValues);
			roadsInLandscape = (SiteVarDistinct<RoadType>)PlugIn.ModelCore.Landscape.NewSiteVar<RoadType>(InactiveSiteMode.DistinctValues);
			coarseElevation = (SiteVarDistinct<int>)PlugIn.ModelCore.Landscape.NewSiteVar<int>(InactiveSiteMode.DistinctValues);
			fineElevation = (SiteVarDistinct<int>)PlugIn.ModelCore.Landscape.NewSiteVar<int>(InactiveSiteMode.DistinctValues);
			coarseWater = (SiteVarDistinct<int>)PlugIn.ModelCore.Landscape.NewSiteVar<int>(InactiveSiteMode.DistinctValues);
			fineWater = (SiteVarDistinct<int>)PlugIn.ModelCore.Landscape.NewSiteVar<int>(InactiveSiteMode.DistinctValues);
			soils = (SiteVarDistinct<double>)PlugIn.ModelCore.Landscape.NewSiteVar<double>(InactiveSiteMode.DistinctValues);
			costRaster = (SiteVarDistinct<float>)PlugIn.ModelCore.Landscape.NewSiteVar<float>(InactiveSiteMode.DistinctValues);
			roads = (SiteVarDistinct<List<IndividualRoad>>)PlugIn.ModelCore.Landscape.NewSiteVar<List<IndividualRoad>>(InactiveSiteMode.DistinctValues);

			// It's not pretty, and it's going to use memory; but as things stand, I have to initialize all of the lists objects
			// in this variable to make things easier.
			foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
			{
				roads[site] = new List<IndividualRoad>();
			}

			cohorts = PlugIn.ModelCore.GetSiteVar<ISiteCohorts>("Succession.AgeCohorts");
		}

		//---------------------------------------------------------------------

		public static SiteVarDistinct<int> BuildableZones
		{
			get
			{
				return buildableZones;
			}
		}

		public static SiteVarDistinct<RoadType> RoadsInLandscape
		{
			get
			{
				return roadsInLandscape;
			}
		}

		public static SiteVarDistinct<int> CoarseElevation
		{
			get
			{
				return coarseElevation;
			}
		}

		public static SiteVarDistinct<int> FineElevation
		{
			get
			{
				return fineElevation;
			}
		}

		public static SiteVarDistinct<int> CoarseWater
		{
			get
			{
				return coarseWater;
			}
		}

		public static SiteVarDistinct<int> FineWater
		{
			get
			{
				return fineWater;
			}
		}

		public static SiteVarDistinct<double> Soils
		{
			get
			{
				return soils;
			}
		}


		public static ISiteVar<ISiteCohorts> Cohorts
		{
			get
			{
				return cohorts;
			}
		}

		public static SiteVarDistinct<float> CostRaster
		{
			get
			{
				return costRaster;
			}
		}

		public static SiteVarDistinct<List<IndividualRoad>> Roads
		{
			get
			{
				return roads;
			}
		}

	}
}
