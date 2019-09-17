//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
// using Landis.SpatialModeling.CoreServices;
using Landis.Utilities;
using System.Collections.Generic;

namespace Landis.Extension.ForestRoadsSimulation
{
	public class SoilRegion
	{
		private int mapCode;
		private double additionalCost;

		public SoilRegion(int mapCode, double additionalCost)
		{
			this.mapCode = mapCode;
			this.additionalCost = additionalCost;
		}

		public int MapCode
		{
			get
			{
				return (this.mapCode);
			}
		}
		public double AdditionalCost
		{
			get
			{
				return (this.additionalCost);
			}
		}
	}
}
