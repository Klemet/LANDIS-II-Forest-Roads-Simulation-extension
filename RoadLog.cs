using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Landis.Library.Metadata;

namespace Landis.Extension.ForestRoadsSimulation
{
	public class RoadLog
	{
		[DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Simulation Year")]
		public int Timestep { set; get; }

		[DataFieldAttribute(Unit = FieldUnits.Count, Desc = "Number of harvested sites to connect")]
		public int NumberOfHarvestedSitesToConnect { set; get; }

		[DataFieldAttribute(Unit = FieldUnits.Count, Desc = "Number of roads constructed")]
		public int NumberOfRoadsConstructed { set; get; }

		[DataFieldAttribute(Unit = FieldUnits.minutes, Desc = "Time taken for the construction")]
		public int TimeTaken { set; get; }

		[DataFieldAttribute(Unit = FieldUnits.Count, Desc = "Cost of road construction or repair in monetary units")]
		public double CostOfConstructionAndRepairs { set; get; }
	}
}
