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
		// ------------------------------------------------------------------------------
		// BASIC PARAMETERS

		/// <summary>
		/// Timestep (years)
		/// </summary>
		int Timestep
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

		/// <summary>
		/// The distance (in meters) onto which wood can be skidded, rather than transported on a road.
		/// </summary>
		int SkiddingDistance
		{
			get; set;
		}

		/// <summary>
		/// Boolean describing if the looping behavior is activated
		/// </summary>
		bool LoopingBehavior
		{
			get; set;
		}

		/// <summary>
		/// The distance (in meters) to which we will start creating loops in the network
		/// </summary>
		int LoopingDistance
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
		/// Path of the folder where the log must be saved
		/// </summary>
		string OutputsOfRoadLog
		{
			get; set;
		}


		// ------------------------------------------------------------------------------
		// INPUT RASTERS AND COST PARAMETERS

		/// <summary>
		/// Path of the raster file containing the zones of the landscape where roads can be built
		/// </summary>
		string ZonesForRoadCreation
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the initial road network
		/// </summary>
		string InitialRoadNetworkMap
		{
			get; set;
		}

		/// <summary>
		/// The basic cost of building a forest road on the distance of a site
		/// </summary>
		double DistanceCost
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the coarse elevation values
		/// </summary>
		string CoarseElevationRaster
		{
			get; set;
		}

		/// <summary>
		/// The multiplication value used to increase cost of construction with elevation differences.
		/// </summary>
		ElevationCostRanges CoarseElevationCosts
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the fine elevation values
		/// </summary>
		string FineElevationRaster
		{
			get; set;
		}

		/// <summary>
		/// The multiplication value used to increase cost of construction with elevation differences.
		/// </summary>
		ElevationCostRanges FineElevationCosts
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the coarse water values
		/// </summary>
		string CoarseWaterRaster
		{
			get; set;
		}

		/// <summary>
		/// The cost of constructing a bridge on a body of water the size of the site
		/// </summary>
		int CoarseWaterCost
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the fine water values
		/// </summary>
		string FineWaterRaster
		{
			get; set;
		}

		/// <summary>
		/// The mean cost of constructing a culvert on one stream
		/// </summary>
		int FineWaterCost
		{
			get; set;
		}

		/// <summary>
		/// Path of the raster file containing the soil regions
		/// </summary>
		string SoilsRaster
		{
			get; set;
		}

		// ------------------------------------------------------------------------------
		// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

		bool SimulationOfRoadAging
		{
			get; set;
		}

		bool SimulationOfWoodFlux
		{
			get; set;
		}

		RoadCatalogue RoadCatalogueNonExit
		{
			get; set;
		}

		RoadCatalogue RoadCatalogueExit
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
		private string heuristicForNetworkConstruction;
		private int skiddingDistance;
		private bool loopingBehavior;
		private int loopingDistance;
		private string outputsOfRoadNetworkMaps;
		private string outputsOfRoadLog;

		private string zonesForRoadCreation;
		private string initialRoadNetworkMap;
		private double distanceCost;
		private string coarseElevationRaster;
		private ElevationCostRanges coarseElevationCosts;
		private string fineElevationRaster;
		private ElevationCostRanges fineElevationCosts;
		private string coarseWaterRaster;
		private int coarseWaterCost;
		private string fineWaterRaster;
		private int fineWaterCost;
		private string soilsRaster;

		private bool simulationOfRoadAging;
		private bool simulationOfWoodFlux;
		private RoadCatalogue roadCatalogueNonExit;
		private RoadCatalogue roadCatalogueExit;

		// ------------------------------------------------------------------------------
		// BASIC PARAMETERS

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

		/// <summary>
		/// The distance (in meters) onto which wood can be skidded, rather than transported on a road.
		/// </summary>
		public int SkiddingDistance
		{
			get
			{
				return skiddingDistance;
			}
			set
			{
				if (value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				skiddingDistance = value;
			}
		}

		/// <summary>
		/// Indicates if looping behavior is activated.
		/// </summary>
		public bool LoopingBehavior
		{
			get
			{
				return loopingBehavior;
			}
			set
			{
				loopingBehavior = value;
			}
		}

		/// <summary>
		/// The distance (in meters) to which we will start creating loops in the network
		/// </summary>
		public int LoopingDistance
		{
			get
			{
				return loopingDistance;
			}
			set
			{
				if (value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				loopingDistance = value;
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
				else throw new InputValueException(value.ToString(), "A correct path to a file must be given");
			}
		}

		/// <summary>
		/// Path of the folder where the log must be saved
		/// </summary>
		public string OutputsOfRoadLog
		{
			get
			{
				return outputsOfRoadLog;
			}
			set
			{
				if (value != null)
				{
					// FIXME: check for null or empty path (value.Actual);
					outputsOfRoadLog = value;
				}
				else throw new InputValueException(value.ToString(), "A correct path to a folder must be given");
			}
		}


		// ------------------------------------------------------------------------------
		// INPUT RASTERS AND COST PARAMETERS

		/// <summary>
		/// Path of the raster file containing the zones where the roads can be built.
		/// </summary>
		public string ZonesForRoadCreation
		{
			get
			{
				return zonesForRoadCreation;
			}
			set
			{
				if (value != null)
				{
					zonesForRoadCreation = value;
				}
				else throw new InputValueException(value.ToString(), "A correct path to a file must be given");
			}
		}

		/// <summary>
		/// Path of the raster file containing the initial road network
		/// </summary>
		public string InitialRoadNetworkMap
		{
			get
			{
				return initialRoadNetworkMap;
			}
			set
			{
				if (value != null)
				{
					initialRoadNetworkMap = value;
				}
				else throw new InputValueException(value.ToString(), "A correct path to a file must be given");
			}
		}

		/// <summary>
		/// The basic cost of building a forest road on the distance of a site
		/// </summary>
		public double DistanceCost
		{
			get
			{
				return distanceCost;
			}
			set
			{
				if (value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				distanceCost = value;
			}
		}

		/// <summary>
		/// Path of the raster file containing the coarse elevation values
		/// </summary>
		public string CoarseElevationRaster
		{
			get
			{
				return coarseElevationRaster;
			}
			set
			{
				if (value != null)
				{
					coarseElevationRaster = value;
				}
				else if (value == "None" || value == "none" || value == "" || value == null) coarseElevationRaster = "none";
			}
		}

		/// <summary>
		/// The multiplication value used to increase cost of construction with elevation differences.
		/// </summary>
		public ElevationCostRanges CoarseElevationCosts
		{
			get
			{
				return coarseElevationCosts;
			}
			set
			{
				if (this.CoarseElevationRaster == "none") coarseElevationCosts = null;
				else if (this.CoarseElevationRaster != "none" && value == null)
					throw new InputValueException(value.ToString(), "Problem with the coarse elevation costs. Please check your parameter file.");
				else coarseElevationCosts = value;
			}
		}

		/// <summary>
		/// Path of the raster file containing the fine elevation values
		/// </summary>
		public string FineElevationRaster
		{
			get
			{
				return fineElevationRaster;
			}
			set
			{
				if (value != null)
				{
					fineElevationRaster = value;
				}
				else if (value == "None" || value == "none" || value == "" || value == null) fineElevationRaster = "none";
			}
		}

		/// <summary>
		/// The multiplication value used to increase cost of construction with elevation differences.
		/// </summary>
		public ElevationCostRanges FineElevationCosts
		{
			get
			{
				return fineElevationCosts;
			}
			set
			{
				if (this.FineElevationRaster == "none") fineElevationCosts = null;
				else if (this.FineElevationRaster != "none" && value == null)
					throw new InputValueException(value.ToString(), "Problem with the fine elevation costs. Please check your parameter file.");
				else fineElevationCosts = value;
			}
		}

		/// <summary>
		/// Path of the raster file containing the coarse water values
		/// </summary>
		public string CoarseWaterRaster
		{
			get
			{
				return coarseWaterRaster;
			}
			set
			{
				if (value != null)
				{
					coarseWaterRaster = value;
				}
				else if (value == "None" || value == "none" || value == "" || value == null) coarseWaterRaster = "none";
			}
		}

		/// <summary>
		/// The cost of constructing a bridge on a body of water the size of the site
		/// </summary>
		public int CoarseWaterCost
		{
			get
			{
				return coarseWaterCost;
			}
			set
			{
				if (this.CoarseWaterRaster == "none") coarseWaterCost = 0;
				else if (this.CoarseWaterRaster != "none" && value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				else coarseWaterCost = value;
			}
		}

		/// <summary>
		/// Path of the raster file containing the fine water values
		/// </summary>
		public string FineWaterRaster
		{
			get
			{
				return fineWaterRaster;
			}
			set
			{
				if (value != null)
				{
					fineWaterRaster = value;
				}
				else if (value == "None" || value == "none" || value == "" || value == null) fineWaterRaster = "none";
			}
		}

		/// <summary>
		/// The mean cost of constructing a culvert on one stream
		/// </summary>
		public int FineWaterCost
		{
			get
			{
				return fineWaterCost;
			}
			set
			{
				if (this.FineWaterRaster == "none") fineWaterCost = 0;
				else if (this.FineWaterRaster != "none" && value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				else fineWaterCost = value;
			}
		}

		/// <summary>
		/// Path of the raster file containing the soil regions
		/// </summary>
		public string SoilsRaster
		{
			get
			{
				return soilsRaster;
			}
			set
			{
				if (value != null)
				{
					soilsRaster = value;
				}
				else if (value == "None" || value == "none" || value == "" || value == null) soilsRaster = "none";
			}
		}

		// ------------------------------------------------------------------------------
		// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

		/// <summary>
		/// Indicate if road aging will be simulated
		/// </summary>
		public bool SimulationOfRoadAging
		{
			get
			{
				return simulationOfRoadAging;
			}
			set
			{
				simulationOfRoadAging = value;
			}
		}

		/// <summary>
		/// Indicate if road aging will be simulated
		/// </summary>
		public bool SimulationOfWoodFlux
		{
			get
			{
				return simulationOfWoodFlux;
			}
			set
			{
				simulationOfWoodFlux = value;
			}
		}

		/// <summary>
		/// The object containing all of the informations on the road types.
		/// </summary>
		public RoadCatalogue RoadCatalogueNonExit
		{
			get
			{
				return roadCatalogueNonExit;
			}
			set
			{
				if (value == null)
					throw new InputValueException(value.ToString(), "Value must not be null.");
				else roadCatalogueNonExit = value;
			}
		}

		/// <summary>
		/// The object containing all of the informations on the road type where the wood can exit.
		/// </summary>
		public RoadCatalogue RoadCatalogueExit
		{
			get
			{
				return roadCatalogueExit;
			}
			set
			{
				if (value == null)
					throw new InputValueException(value.ToString(), "Value must not be null.");
				else roadCatalogueExit = value;
			}
		}

		public InputParameters()
		{
			
		}
	}
}
