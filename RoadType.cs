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
		// 6 - Main Road Network (Paved)
		// 7 - Undertermined (but there is a road)
		public int typeNumber { get; set; }
		public bool isConnectedToSawMill { get; set; }

		public RoadType(int type)
		{
			this.typeNumber = type;
			if (this.typeNumber == 5 || this.typeNumber == 6) this.isConnectedToSawMill = true;
			else this.isConnectedToSawMill = false;
		}

		public string getRoadTypeName()
		{
			string name = "";
			if (this.typeNumber == 1) name = "Primary road";
			else if (this.typeNumber == 2) name = "Secondary road";
			else if (this.typeNumber == 3) name = "Tertiary road";
			else if (this.typeNumber == 4) name = "Winter road";
			else if (this.typeNumber == 5) name = "Sawmill";
			else if (this.typeNumber == 6) name = "Main Road Network (Paved)";
			else if (this.typeNumber == 7) name = "Undertermined (but there is a road)";
			else name = "Invalid road type/No road";

			return (name);
		}

		public bool IsARoad
		{
			get
			{
				if (this.typeNumber <= 7 && this.typeNumber >= 1) return (true);
				else return (false);
			}
		}

		public bool IsAPlaceForTheWoodToGo
		{
			get
			{
				if (this.typeNumber == 5 || this.typeNumber == 6) return (true);
				else return (false);
			}
		}

	}
}
