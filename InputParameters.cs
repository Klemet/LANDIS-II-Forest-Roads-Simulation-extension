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
		/// Path of the raster file containing the initial road network
		/// </summary>
		string InitialRoadNetworkMap
		{
			get; set;
		}

		/// <summary>
		/// The basic cost of building a forest road on the distance of a site
		/// </summary>
		int DistanceCost
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
		int CoarseElevationCost
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

		// ElevationCosts is now static to accomodate for the parameter parser
		/// <summary>
		/// The multiplication value used to increase cost of construction with elevation differences.
		/// </summary>
		// ElevationCostRanges FineElevationCosts
		// {
		// 	get; set;
		// }

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

		/// <summary>
		/// Path of the raster file containing the soil regions
		/// </summary>
		List<SoilRegion> SoilsCost
		{
			get; set;
		}

		// ------------------------------------------------------------------------------
		// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

		/// <summary>
		/// Threshold of timber flux above which the forest road must be a primary road
		/// </summary>
		int PrimaryRoadThreshold
		{
			get; set;
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a primary road.
		/// </summary>
		double PrimaryRoadMultiplication
		{
			get; set;
		}

		/// <summary>
		/// Threshold of timber flux above which the forest road must be a secondary road
		/// </summary>
		int SecondaryRoadThreshold
		{
			get; set;
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a secondary road.
		/// </summary>
		double SecondaryRoadMultiplication
		{
			get; set;
		}

		/// <summary>
		/// Percentage of constructed road that accodomate a flux for a tertiary road that will be able to become a temporary road.
		/// </summary>
		int TemporaryRoadPercentage
		{
			get; set;
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a tertiary road.
		/// </summary>
		double TertiaryRoadMultiplication
		{
			get; set;
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a temporary road.
		/// </summary>
		double TemporaryRoadMultiplication
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
		private string outputsOfRoadNetworkMaps;
		private string outputsOfRoadLog;

		private string initialRoadNetworkMap;
		private int distanceCost;
		private string coarseElevationRaster;
		private int coarseElevationCost;
		private string fineElevationRaster;
		// ElevationCosts is now static to accomodate for the parameter parser
		private string coarseWaterRaster;
		private int coarseWaterCost;
		private string fineWaterRaster;
		private int fineWaterCost;
		private string soilsRaster;
		List<SoilRegion> soilsCost;

		private int primaryRoadThreshold;
		private double primaryRoadMultiplication;
		private int secondaryRoadThreshold;
		private double secondaryRoadMultiplication;
		private int temporaryRoadPercentage;
		private double tertiaryRoadMultiplication;
		private double temporaryRoadMultiplication;

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
		/// The heuristic given by the user to determine the ordrer in which the roads are built with the least-cost path algorithm.
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
		public int DistanceCost
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
		public int CoarseElevationCost
		{
			get
			{
				return coarseElevationCost;
			}
			set
			{
				if (this.CoarseElevationRaster == "none") coarseElevationCost = 1;
				else if (this.CoarseElevationRaster == "none" && value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				else coarseElevationCost = value;
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
				else if (this.CoarseWaterRaster == "none" && value < 0)
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
				else if (this.FineWaterRaster == "none" && value < 0)
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

		/// <summary>
		/// Path of the raster file containing the soil regions
		/// </summary>
		public List<SoilRegion> SoilsCost
		{
			get
			{
				return soilsCost;
			}
			set
			{
				// If no fine elevation raster has been given, we make this parameter an empty list.
				if (this.SoilsRaster == "none") soilsCost = new List<SoilRegion>();
				// If a raster has been given, but no value has been given, we throw an exception.
				else if (this.SoilsRaster != "none" && value.Count == 0) throw new InputValueException(value.ToString(), "Need at least one soil region.");
				else soilsCost = value;
			}
		}

		// ------------------------------------------------------------------------------
		// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

		/// <summary>
		/// Threshold of timber flux above which the forest road must be a primary road
		/// </summary>
		public int PrimaryRoadThreshold
		{
			get
			{
				return primaryRoadThreshold;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else primaryRoadThreshold = value;
			}
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a primary road.
		/// </summary>
		public double PrimaryRoadMultiplication
		{
			get
			{
				return primaryRoadMultiplication;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else primaryRoadMultiplication = value;
			}
		}

		/// <summary>
		/// Threshold of timber flux above which the forest road must be a secondary road
		/// </summary>
		public int SecondaryRoadThreshold
		{
			get
			{
				return secondaryRoadThreshold;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else secondaryRoadThreshold = value;
			}
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a secondary road.
		/// </summary>
		public double SecondaryRoadMultiplication
		{
			get
			{
				return secondaryRoadMultiplication;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else secondaryRoadMultiplication = value;
			}
		}

		/// <summary>
		/// Percentage of constructed road that accodomate a flux for a tertiary road that will be able to become a temporary road.
		/// </summary>
		public int TemporaryRoadPercentage
		{
			get
			{
				return temporaryRoadPercentage;
			}
			set
			{
				if (value < 0)
					throw new InputValueException(value.ToString(), "Value must be = or > 0.");
				else temporaryRoadPercentage = value;
			}
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a tertiary road.
		/// </summary>
		public double TertiaryRoadMultiplication
		{
			get
			{
				return tertiaryRoadMultiplication;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else tertiaryRoadMultiplication = value;
			}
		}

		/// <summary>
		/// Value used to multiply the base cost of the construction of a forest road to obtain the cost of construction for a temporary road.
		/// </summary>
		public double TemporaryRoadMultiplication
		{
			get
			{
				return temporaryRoadMultiplication;
			}
			set
			{
				if (value <= 0)
					throw new InputValueException(value.ToString(), "Value must be > 0.");
				else temporaryRoadMultiplication = value;
			}
		}

		public InputParameters()
		{
			
		}
	}
}
