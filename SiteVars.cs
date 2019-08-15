//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Landscapes;
using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.AgeOnlyCohorts;

namespace Landis.Extension.ForestRoadsSimulation
{
	public static class SiteVars
	{
		
		private static SiteVarDistinct<RoadType> roadsInLandscape;
		private static ISiteVar<ISiteCohorts> cohorts;

		//---------------------------------------------------------------------

		public static void Initialize()
		{
			// CAREFULL : By default, LANDIS-II creates variables where the inactive sites all have the same value !
			// For our variable to work on inactive sites, we have to tell him to create a variable with distinct values
			// for every inactive site
			roadsInLandscape = (SiteVarDistinct<RoadType>)PlugIn.ModelCore.Landscape.NewSiteVar<RoadType>(InactiveSiteMode.DistinctValues);

			cohorts = PlugIn.ModelCore.GetSiteVar<ISiteCohorts>("Succession.AgeCohorts");
		}

		//---------------------------------------------------------------------

		public static SiteVarDistinct<RoadType> RoadsInLandscape
		{
			get
			{
				return roadsInLandscape;
			}
		}

		public static ISiteVar<ISiteCohorts> Cohorts
		{
			get
			{
				return cohorts;
			}
		}

	}
}
