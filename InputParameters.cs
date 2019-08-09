//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Utilities;
using System.Collections.Generic;
using System.Text;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// Parameters for the plug-in.
	/// </summary>
	public interface IInputParameters
	{
		/// <summary>
		/// Timestep (years)
		/// </summary>
		int Timestep
		{
			get; set;
		}

		/// <summary>
		/// Path for input map
		/// </summary>
		string RoadNetworkMap
		{
			get; set;
		}

		/// <summary>
		/// Path to save the output maps of the forest road network
		/// </summary>
		string OutputsOfRoadNetworkMaps
		{
			get; set;
		}

		/// <summary>
		/// The heuristic given by the user to determine the ordrer in which the roads are built with the least-cost path algorithm.
		/// </summary>
		string HeuristicForNetworkConstruction
		{
			get; set;
		}

	}
}


namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// Parameters for the plug-in.
	/// </summary>
	public class InputParameters
		: IInputParameters
	{
		private int timestep;
		private string roadNetworkMap;
		private string outputsOfRoadNetworkMaps;
		private string heuristicForNetworkConstruction;

		//---------------------------------------------------------------------

		/// <summary>
		/// Timestep (years)
		/// </summary>
		public int Timestep
		{
			get
			{
				return timestep;
			}
			set
			{
				if (value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				timestep = value;
			}
		}

		/// <summary>
		/// Path for input map
		/// </summary>
		public string RoadNetworkMap
		{
			get
			{
				return roadNetworkMap;
			}
			set
			{
				if (value != null)
				{
					// FIXME: check for null or empty path (value.Actual);
					roadNetworkMap = value;
				}
			}
		}

		/// <summary>
		/// Path to save the output maps of the forest road network
		/// </summary>
		public string OutputsOfRoadNetworkMaps
		{
			get
			{
				return outputsOfRoadNetworkMaps;
			}
			set
			{
				if (value != null)
				{
					// FIXME: check for null or empty path (value.Actual);
					outputsOfRoadNetworkMaps = value;
				}
			}
		}

		/// <summary>
		/// The heuristic given by the user to determine the ordrer in which the roads are built with the least-cost path algorithm.
		/// </summary>
		public string HeuristicForNetworkConstruction
		{
			get
			{
				return heuristicForNetworkConstruction;
			}
			set
			{
				if (value != "Random" && value != "Closestfirst" && value != "Farthestfirst")
				{
					throw new InputValueException(value.ToString(), "Value must be \"Random\", \"Closestfirst\" or \"Farthestfirst\".");
				}
				else heuristicForNetworkConstruction = value;
			}
		}

		

		public InputParameters()
		{
			
		}
	}
}
