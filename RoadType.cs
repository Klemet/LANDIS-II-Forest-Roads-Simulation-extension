//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
// using Landis.SpatialModeling.CoreServices;
using Landis.Utilities;
using System.Collections.Generic;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// The parameters for an road type.
	/// </summary>
	public class RoadType
	{
		// The type number is :
		// 1 - Primary road
		// 2 - Secondary road
		// 3 - Tertiary road
		// 4 - Winter road
		// 5 - Sawmill
		public int typeNumber { get; set; }
		bool isConnectedToSawMill { get; set; }

		public RoadType(int type)
		{
			this.typeNumber = type;
			this.isConnectedToSawMill = false;
		}

		public string getRoadTypeName()
		{
			string name = "";
			if (this.typeNumber == 1) name = "Primary road";
			else if (this.typeNumber == 2) name = "Secondary road";
			else if (this.typeNumber == 3) name = "Tertiary road";
			else if (this.typeNumber == 4) name = "Winter road";
			else if (this.typeNumber == 5) name = "Sawmill";
			else name = "Invalid road type";

			return (name);
		}

	}
}
