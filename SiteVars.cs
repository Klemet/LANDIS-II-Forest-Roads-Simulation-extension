//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.AgeOnlyCohorts;

namespace Landis.Extension.ForestRoadsSimulation
{
	public static class SiteVars
	{
		private static ISiteVar<RoadType> roadsInLandscape;
		private static ISiteVar<ISiteCohorts> cohorts;

		//---------------------------------------------------------------------

		public static void Initialize()
		{
			roadsInLandscape = PlugIn.ModelCore.Landscape.NewSiteVar<RoadType>();

			cohorts = PlugIn.ModelCore.GetSiteVar<ISiteCohorts>("Succession.AgeCohorts");
		}

		//---------------------------------------------------------------------

		public static ISiteVar<RoadType> RoadsInLandscape
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
