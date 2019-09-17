//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Utilities;
using System.Collections.Generic;
using Landis.Core;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// A parser that reads the plug-in's parameters from text input.
	/// </summary>
	// La classe hérite du text parser de LANDIS-II, qui contient de propriétés qui lui permettent de savoir ou il se trouve dans le fichier.
	public class InputParameterParser
		: TextParser<IInputParameters>
	{

		//---------------------------------------------------------------------
		public override string LandisDataValue
		{
			get
			{
				return PlugIn.ExtensionName;
			}
		}
		public InputParameterParser()
		{
			// FIXME: Hack to ensure that Percentage is registered with InputValues
			Landis.Utilities.Percentage p = new Landis.Utilities.Percentage();
		}

		//---------------------------------------------------------------------

		protected override IInputParameters Parse()
		{
			// ------------------------------------------------------------------------------
			// BASIC PARAMETERS

			// Pour commencer, on regarde le paramêtre "LandisData" qui se trouve au début du fichier .txt
			// de paramêtre. Si il ne correspond pas au nom de l'extension, on lève une exception.
			InputVar<string> landisData = new InputVar<string>("LandisData");
			ReadVar(landisData);
			if (landisData.Value.Actual != PlugIn.ExtensionName)
				throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", PlugIn.ExtensionName);

			// On creer l'object qu'on va renvoyer, qui va continir nos paramêtres
			InputParameters parameters = new InputParameters();

			// On lit le timestep
			InputVar<int> timestep = new InputVar<int>("Timestep");
			ReadVar(timestep);
			parameters.Timestep = timestep.Value;

			// On lit l'heuristique pour la création du réseau de routes
			InputVar<string> HeuristicForNetworkConstruction = new InputVar<string>("HeuristicForNetworkConstruction");
			ReadVar(HeuristicForNetworkConstruction);
			parameters.HeuristicForNetworkConstruction = HeuristicForNetworkConstruction.Value;

			// On lit la distance de débardage
			InputVar<int> SkiddingDistance = new InputVar<int>("SkiddingDistance");
			ReadVar(SkiddingDistance);
			parameters.SkiddingDistance = SkiddingDistance.Value;

			// On lit le chemin ou enregistrer les cartes d'output
			InputVar<string> OutputsOfRoadNetworkMaps = new InputVar<string>("OutputsOfRoadNetworkMaps");
			ReadVar(OutputsOfRoadNetworkMaps);
			parameters.OutputsOfRoadNetworkMaps = OutputsOfRoadNetworkMaps.Value;

			// We read the path where we need to save the log
			InputVar<string> OutputsOfRoadLog = new InputVar<string>("OutputsOfRoadLog");
			ReadVar(OutputsOfRoadLog);
			parameters.OutputsOfRoadLog = OutputsOfRoadLog.Value;

			// ------------------------------------------------------------------------------
			// INPUT RASTERS AND COST PARAMETERS

			// On lit le raster initial des routes
			InputVar<string> InitialRoadNetworkMap = new InputVar<string>("InitialRoadNetworkMap");
			ReadVar(InitialRoadNetworkMap);
			parameters.InitialRoadNetworkMap = InitialRoadNetworkMap.Value;
			MapManager.ReadMap(InitialRoadNetworkMap.Value, "InitialRoadNetworkMap");

			// We read the distance cost
			InputVar<int> DistanceCost = new InputVar<int>("DistanceCost");
			ReadVar(DistanceCost);
			parameters.DistanceCost = DistanceCost.Value;

			// We read the coarse elevation raster if he is given
			InputVar<string> CoarseElevationRaster = new InputVar<string>("CoarseElevationRaster");
			ReadVar(CoarseElevationRaster);
			parameters.CoarseElevationRaster = CoarseElevationRaster.Value;
			MapManager.ReadMap(CoarseElevationRaster.Value, "CoarseElevationRaster");

			// We read the coarse elevation cost
			InputVar<int> CoarseElevationCost = new InputVar<int>("CoarseElevationCost");
			ReadVar(CoarseElevationCost);
			parameters.CoarseElevationCost = CoarseElevationCost.Value;

			// We read the fine elevation raster if he is given
			InputVar<string> FineElevationRaster = new InputVar<string>("FineElevationRaster");
			ReadVar(FineElevationRaster);
			parameters.FineElevationRaster = FineElevationRaster.Value;
			MapManager.ReadMap(FineElevationRaster.Value, "FineElevationRaster");

			// We read the fine elevation costs
			// WARNING : Because the parser is quite annoying with the construction of objects,
			// The fine elevation cost does not go by the "parameters" object. It directly goes
			// to the ElevationCostRanges static class.
			ElevationCostRanges.Initialize();

			const string FineElevationCosts = "FineElevationCosts";
			ReadName(FineElevationCosts);

			InputVar<int> LowerThreshold = new InputVar<int>("Lower Threshold for current range of elevation");
			InputVar<int> UpperThreshold = new InputVar<int>("Upper Threshold for current range of elevation");
			InputVar<double> MultiplicationValue = new InputVar<double>("Multiplication value for this range of elevation");

			const string CoarseWaterRasterName = "CoarseWaterRaster";

			while (!AtEndOfInput && CurrentName != CoarseWaterRasterName)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(LowerThreshold, currentLine);
				ReadValue(UpperThreshold, currentLine);
				ReadValue(MultiplicationValue, currentLine);

				ElevationCostRanges.AddRange(LowerThreshold.Value, UpperThreshold.Value, MultiplicationValue.Value);

				CheckNoDataAfter("the " + LowerThreshold.Name + " column",
								currentLine);

				GetNextLine();
			}

			ElevationCostRanges.VerifyRanges();

			// We read the coarse water raster if he is given
			InputVar<string> CoarseWaterRaster = new InputVar<string>("CoarseWaterRaster");
			ReadVar(CoarseWaterRaster);
			parameters.CoarseWaterRaster = CoarseWaterRaster.Value;
			MapManager.ReadMap(CoarseWaterRaster.Value, "CoarseWaterRaster");

			// We read the coarse water cost
			InputVar<int> CoarseWaterCost = new InputVar<int>("CoarseWaterCost");
			ReadVar(CoarseWaterCost);
			parameters.CoarseWaterCost = CoarseWaterCost.Value;

			// We read the fine water raster if he is given
			InputVar<string> FineWaterRaster = new InputVar<string>("FineWaterRaster");
			ReadVar(FineWaterRaster);
			parameters.FineWaterRaster = FineWaterRaster.Value;
			MapManager.ReadMap(FineWaterRaster.Value, "FineWaterRaster");

			// We read the fine water cost
			InputVar<int> FineWaterCost = new InputVar<int>("FineWaterCost");
			ReadVar(FineWaterCost);
			parameters.FineWaterCost = FineWaterCost.Value;

			// We read the soil raster if he is given
			InputVar<string> SoilsRaster = new InputVar<string>("SoilsRaster");
			ReadVar(SoilsRaster);
			parameters.SoilsRaster = SoilsRaster.Value;

			// We read the soil additive costs
			// WARNING : Again, because the parser is quite annoying with the construction of objects,
			// The soil region cost does not go by the "parameters" object. It directly goes
			// to the SoilRegions static class.
			SoilRegions.Initialize();

			const string SoilsCost = "SoilsCost";
			ReadName(SoilsCost);

			InputVar<int> MapCode = new InputVar<int>("MapCode of soil region");
			InputVar<int> AdditionalCost = new InputVar<int>("Additional Cost for this soil region");

			const string PrimaryRoadThresholdName = "PrimaryRoadThreshold";

			while (!AtEndOfInput && CurrentName != PrimaryRoadThresholdName)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(MapCode, currentLine);
				ReadValue(AdditionalCost, currentLine);

				SoilRegions.AddRegion(new SoilRegion(MapCode.Value, AdditionalCost.Value));

				CheckNoDataAfter("the " + MapCode.Name + " column",
								 currentLine);
				GetNextLine();
			}

			// And THEN we read the soil map; so that this way, we were able to initialize the soil regions first.
			// we give the soil costs so that we can properly insert the soilregions of the user into the sites.
			MapManager.ReadMap(SoilsRaster.Value, "SoilsRaster");


			// ------------------------------------------------------------------------------
			// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

			// We read the primary road threshold
			InputVar<int> PrimaryRoadThreshold = new InputVar<int>("PrimaryRoadThreshold");
			ReadVar(PrimaryRoadThreshold);
			parameters.PrimaryRoadThreshold = PrimaryRoadThreshold.Value;

			// We read the primary multiplicative value
			InputVar<double> PrimaryRoadMultiplication = new InputVar<double>("PrimaryRoadMultiplication");
			ReadVar(PrimaryRoadMultiplication);
			parameters.PrimaryRoadMultiplication = PrimaryRoadMultiplication.Value;

			// We read the secondary road threshold
			InputVar<int> SecondaryRoadThreshold = new InputVar<int>("SecondaryRoadThreshold");
			ReadVar(SecondaryRoadThreshold);
			parameters.SecondaryRoadThreshold = SecondaryRoadThreshold.Value;

			// We read the secondary multiplicative value
			InputVar<double> SecondaryRoadMultiplication = new InputVar<double>("SecondaryRoadMultiplication");
			ReadVar(SecondaryRoadMultiplication);
			parameters.SecondaryRoadMultiplication = SecondaryRoadMultiplication.Value;

			// We read the temporary road percentage
			InputVar<int> TemporaryRoadPercentage = new InputVar<int>("TemporaryRoadPercentage");
			ReadVar(TemporaryRoadPercentage);
			parameters.TemporaryRoadPercentage = TemporaryRoadPercentage.Value;

			// We read the tertiary multiplicative value
			InputVar<double> TertiaryRoadMultiplication = new InputVar<double>("TertiaryRoadMultiplication");
			ReadVar(TertiaryRoadMultiplication);
			parameters.TertiaryRoadMultiplication = TertiaryRoadMultiplication.Value;

			// We read the temporary multiplicative value
			InputVar<double> TemporaryRoadMultiplication = new InputVar<double>("TemporaryRoadMultiplication");
			ReadVar(TemporaryRoadMultiplication);
			parameters.TemporaryRoadMultiplication = TemporaryRoadMultiplication.Value;

			return (parameters);
		}

		//---------------------------------------------------------------------

		private void ValidatePath(InputValue<string> path)
		{
			if (path.Actual.Trim(null) == "")
				throw new InputValueException(path.String,
											  "Invalid file path: {0}",
											  path.String);
		}

		/// <summary>
		/// A function for debugging purposes. It display all of the parameters in a parameter object
		/// in the LANDIS-II console.
		/// </summary>
		public static void DisplayParameters(ICore ModelCore, IInputParameters Parameters)
		{
			ModelCore.UI.WriteLine("   Timestep : " + Parameters.Timestep);
			ModelCore.UI.WriteLine("   Heuristic : " + Parameters.HeuristicForNetworkConstruction);
			ModelCore.UI.WriteLine("   Skidding distance : " + Parameters.SkiddingDistance);
			ModelCore.UI.WriteLine("   Outputs of network maps : " + Parameters.OutputsOfRoadNetworkMaps);
			ModelCore.UI.WriteLine("   Outputs of logs : " + Parameters.OutputsOfRoadLog);

			ModelCore.UI.WriteLine("   Initial road raster : " + Parameters.InitialRoadNetworkMap);
			ModelCore.UI.WriteLine("   Distance cost : " + Parameters.DistanceCost);
			ModelCore.UI.WriteLine("   Coarse elevation raster : " + Parameters.CoarseElevationRaster);
			ModelCore.UI.WriteLine("   Coarse elevation cost : " + Parameters.CoarseElevationCost);
			ModelCore.UI.WriteLine("   Fine elevation raster : " + Parameters.FineElevationRaster);
			ModelCore.UI.WriteLine("   Fine elevation costs : ");
			ElevationCostRanges.DisplayRangesInConsole(ModelCore);
			ModelCore.UI.WriteLine("   Coarse water raster : " + Parameters.CoarseWaterRaster);
			ModelCore.UI.WriteLine("   Coarse water cost :  : " + Parameters.CoarseWaterCost);
			ModelCore.UI.WriteLine("   Fine water raster : " + Parameters.CoarseWaterRaster);
			ModelCore.UI.WriteLine("   Fine water cost : " + Parameters.FineWaterCost);
			ModelCore.UI.WriteLine("   Soils raster : " + Parameters.SoilsRaster);
			ModelCore.UI.WriteLine("   Soil costs : ");
			SoilRegions.DisplayRegionsInConsole(ModelCore);

			ModelCore.UI.WriteLine("   Primary Road Threshold : " + Parameters.PrimaryRoadThreshold);
			ModelCore.UI.WriteLine("   Primary Road Multiplication Value : " + Parameters.PrimaryRoadMultiplication);
			ModelCore.UI.WriteLine("   Secondary Road Threshold : " + Parameters.SecondaryRoadThreshold);
			ModelCore.UI.WriteLine("   Secondary Road Multiplication Value : " + Parameters.SecondaryRoadMultiplication);
			ModelCore.UI.WriteLine("   Percentage of temporary roads : " + Parameters.TemporaryRoadPercentage);
			ModelCore.UI.WriteLine("   Tertiary Road Multiplication Value : " + Parameters.TertiaryRoadMultiplication);
			ModelCore.UI.WriteLine("   Temporary Road Multiplication Value : " + Parameters.TemporaryRoadMultiplication);
		}

	}
}
