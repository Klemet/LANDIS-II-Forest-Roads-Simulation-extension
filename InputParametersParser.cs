﻿//  Author: Clément Hardy
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

			// We read the raster containing the zones where the roads can be built.
			InputVar<string> ZonesForRoadCreation = new InputVar<string>("RasterOfBuildableZones");
			ReadVar(ZonesForRoadCreation);
			parameters.ZonesForRoadCreation = ZonesForRoadCreation.Value;

			// On lit le raster initial des routes
			InputVar<string> InitialRoadNetworkMap = new InputVar<string>("InitialRoadNetworkMap");
			ReadVar(InitialRoadNetworkMap);
			parameters.InitialRoadNetworkMap = InitialRoadNetworkMap.Value;

			// We read the distance cost
			InputVar<double> DistanceCost = new InputVar<double>("DistanceCost");
			ReadVar(DistanceCost);
			parameters.DistanceCost = DistanceCost.Value;

			// We read the coarse elevation raster if he is given
			InputVar<string> CoarseElevationRaster = new InputVar<string>("CoarseElevationRaster");
			ReadVar(CoarseElevationRaster);
			parameters.CoarseElevationRaster = CoarseElevationRaster.Value;

			// We read the coarse elevation cost
			ElevationCostRanges CoarseElevationCostsTable = new ElevationCostRanges();

			const string CoarseElevationCosts = "CoarseElevationCosts";
			ReadName(CoarseElevationCosts);

			InputVar<int> LowerThresholdCoarse = new InputVar<int>("Lower Threshold for current range of elevation");
			InputVar<int> UpperThresholdCoarse = new InputVar<int>("Upper Threshold for current range of elevation");
			InputVar<double> AdditionalValueCoarse = new InputVar<double>("Additional value for this range of elevation");

			const string FineElevationRasterName = "FineElevationRaster";

			while (!AtEndOfInput && CurrentName != FineElevationRasterName)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(LowerThresholdCoarse, currentLine);
				ReadValue(UpperThresholdCoarse, currentLine);
				ReadValue(AdditionalValueCoarse, currentLine);

				CoarseElevationCostsTable.AddRange(LowerThresholdCoarse.Value, UpperThresholdCoarse.Value, AdditionalValueCoarse.Value);

				CheckNoDataAfter("the " + LowerThresholdCoarse.Name + " column",
								currentLine);

				GetNextLine();
			}

			CoarseElevationCostsTable.VerifyRanges();
			parameters.CoarseElevationCosts = CoarseElevationCostsTable;

			// We read the fine elevation raster if he is given
			InputVar<string> FineElevationRaster = new InputVar<string>("FineElevationRaster");
			ReadVar(FineElevationRaster);
			parameters.FineElevationRaster = FineElevationRaster.Value;

			// We read the fine elevation costs
			ElevationCostRanges FineElevationCostsTable = new ElevationCostRanges();

			const string FineElevationCosts = "FineElevationCosts";
			ReadName(FineElevationCosts);

			InputVar<int> LowerThresholdFine = new InputVar<int>("Lower Threshold for current range of elevation");
			InputVar<int> UpperThresholdFine = new InputVar<int>("Upper Threshold for current range of elevation");
			InputVar<double> MultiplicationValueFine = new InputVar<double>("Multiplication value for this range of elevation");

			const string CoarseWaterRasterName = "CoarseWaterRaster";

			while (!AtEndOfInput && CurrentName != CoarseWaterRasterName)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(LowerThresholdFine, currentLine);
				ReadValue(UpperThresholdFine, currentLine);
				ReadValue(MultiplicationValueFine, currentLine);

				FineElevationCostsTable.AddRange(LowerThresholdFine.Value, UpperThresholdFine.Value, MultiplicationValueFine.Value);

				CheckNoDataAfter("the " + LowerThresholdFine.Name + " column",
								currentLine);

				GetNextLine();
			}

			FineElevationCostsTable.VerifyRanges();
			parameters.FineElevationCosts = FineElevationCostsTable;

			// We read the coarse water raster if he is given
			InputVar<string> CoarseWaterRaster = new InputVar<string>("CoarseWaterRaster");
			ReadVar(CoarseWaterRaster);
			parameters.CoarseWaterRaster = CoarseWaterRaster.Value;

			// We read the coarse water cost
			InputVar<int> CoarseWaterCost = new InputVar<int>("CoarseWaterCost");
			ReadVar(CoarseWaterCost);
			parameters.CoarseWaterCost = CoarseWaterCost.Value;

			// We read the fine water raster if he is given
			InputVar<string> FineWaterRaster = new InputVar<string>("FineWaterRaster");
			ReadVar(FineWaterRaster);
			parameters.FineWaterRaster = FineWaterRaster.Value;

			// We read the fine water cost
			InputVar<int> FineWaterCost = new InputVar<int>("FineWaterCost");
			ReadVar(FineWaterCost);
			parameters.FineWaterCost = FineWaterCost.Value;

			// We read the soil raster if he is given
			InputVar<string> SoilsRaster = new InputVar<string>("SoilsRaster");
			ReadVar(SoilsRaster);
			parameters.SoilsRaster = SoilsRaster.Value;


			// ------------------------------------------------------------------------------
			// ROAD TYPE THRESHOLDS AND MULTIPLICATION VALUES

			// We read the parameters indicating if the aging and the woodflux will be simulated
			InputVar<bool> SimulationOfRoadAging = new InputVar<bool>("SimulationOfRoadAging");
			ReadVar(SimulationOfRoadAging);
			parameters.SimulationOfRoadAging = SimulationOfRoadAging.Value;
			InputVar<bool> SimulationOfWoodFlux = new InputVar<bool>("SimulationOfWoodFlux");
			ReadVar(SimulationOfWoodFlux);
			parameters.SimulationOfWoodFlux = SimulationOfWoodFlux.Value;

			if (parameters.SimulationOfRoadAging && !parameters.SimulationOfWoodFlux)
			{
				PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION EXTENSION WARNING : You chosed to simulate road aging, but not wood fluxes. However, when wood fluxes are not simulated, all the constructed roads will correspond to the lowest road type, as they will never be upgraded. Therefore, all of your roads will have a maximum age before destruction equal to the lowest road type that you entered.");
			}

			// We read the road catalogue for non-exit roads
				RoadCatalogue RoadCatalogueNonExit = new RoadCatalogue(false);

			const string RoadCatalogueName = "RoadTypes";
			ReadName(RoadCatalogueName);

			InputVar<double> LowerThresholdRoadTypes = new InputVar<double>("Lower Threshold for current road type");
			InputVar<double> UpperThresholdRoadTypes = new InputVar<double>("Upper Threshold for current road type");
			InputVar<int> RoadTypeID = new InputVar<int>("ID for the current road type");
			InputVar<double> multiplicativeCostValue = new InputVar<double>("Multiplicative cost to construct this road type");
			InputVar<int> maximumAgeBeforeDestruction = new InputVar<int>("Maximum age of use after wich the road goes back to nature");
			InputVar<string> RoadTypeName = new InputVar<string>("Name for the current road type");
			// Variables used to adapt with the use of road aging and wood flux or not.
			double dummyLowerThreshold;
			double dummyUpperThreshold;
			int dummyAgeBeforeDestruction;

			const string ExitRoadsCatalogueName = "RoadTypesForExitingWood";

			while (!AtEndOfInput && CurrentName != ExitRoadsCatalogueName)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				// We only read flux values if wood flux will be simulated
				if (parameters.SimulationOfWoodFlux)
				{
					ReadValue(LowerThresholdRoadTypes, currentLine);
					ReadValue(UpperThresholdRoadTypes, currentLine);
				}
				ReadValue(RoadTypeID, currentLine);
				ReadValue(multiplicativeCostValue, currentLine);
				// We read the age only if aging is simulated
				if (parameters.SimulationOfRoadAging)
				{
					ReadValue(maximumAgeBeforeDestruction, currentLine);
				}
				ReadValue(RoadTypeName, currentLine);

				// We fill the dummy values to adapt to the AddRange function
				if (!parameters.SimulationOfWoodFlux)
				{
					dummyLowerThreshold = 0;
					dummyUpperThreshold = 0;
				}
				else
				{
					dummyLowerThreshold = LowerThresholdRoadTypes.Value;
					dummyUpperThreshold = UpperThresholdRoadTypes.Value;
				}
				if (!parameters.SimulationOfRoadAging)
				{
					dummyAgeBeforeDestruction = int.MaxValue;
				}
				else
				{
					dummyAgeBeforeDestruction = maximumAgeBeforeDestruction.Value;
				}
				RoadCatalogueNonExit.AddRange(dummyLowerThreshold, dummyUpperThreshold, RoadTypeID.Value, multiplicativeCostValue.Value, RoadTypeName.Value, dummyAgeBeforeDestruction);

				CheckNoDataAfter("the " + LowerThresholdRoadTypes.Name + " column",
								currentLine);

				GetNextLine();
			}
			// We check to see if the user haven't entered the same road ID twice.
			RoadCatalogueNonExit.CheckRedundantRoadID();
			// We check if the roads are ordered correctly by their multiplicative value.
			RoadCatalogueNonExit.VerifyMultiplicativeValues();
			// Then, we verify the ranges for the wood flux and the ages for the road aging
			if (parameters.SimulationOfWoodFlux) { RoadCatalogueNonExit.VerifyRanges(); }
			if (parameters.SimulationOfRoadAging) { RoadCatalogueNonExit.VerifyAges(); }

			parameters.RoadCatalogueNonExit = RoadCatalogueNonExit;

			// We read the road catalogue for roads to exit the wood to
			RoadCatalogue RoadCatalogueExit = new RoadCatalogue(true);

			ReadName(ExitRoadsCatalogueName);

			InputVar<int> RoadTypeIDExit = new InputVar<int>("ID for the current road type");
			InputVar<string> RoadTypeNameExit = new InputVar<string>("Name for the current road type");

			while (!AtEndOfInput)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(RoadTypeIDExit, currentLine);
				ReadValue(RoadTypeNameExit, currentLine);


				RoadCatalogueExit.AddExitRoadType(RoadTypeIDExit.Value, RoadTypeNameExit.Value);

				CheckNoDataAfter("the " + RoadTypeIDExit.Name + " column",
								currentLine);

				GetNextLine();
			}
			// We check to see if the user haven't entered the same road ID twice.
			RoadCatalogueExit.CheckRedundantRoadID();
			parameters.RoadCatalogueExit = RoadCatalogueExit;

			// Now that everything is done, we return the parameter object.
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
		public static void DisplayParameters()
		{
			ICore ModelCore = PlugIn.ModelCore;
			IInputParameters Parameters = PlugIn.Parameters;

			ModelCore.UI.WriteLine("   Timestep : " + Parameters.Timestep);
			ModelCore.UI.WriteLine("   Heuristic : " + Parameters.HeuristicForNetworkConstruction);
			ModelCore.UI.WriteLine("   Skidding distance : " + Parameters.SkiddingDistance);
			ModelCore.UI.WriteLine("   Outputs of network maps : " + Parameters.OutputsOfRoadNetworkMaps);
			ModelCore.UI.WriteLine("   Outputs of logs : " + Parameters.OutputsOfRoadLog);

			ModelCore.UI.WriteLine("   Initial road raster : " + Parameters.InitialRoadNetworkMap);
			ModelCore.UI.WriteLine("   Distance cost : " + Parameters.DistanceCost);
			ModelCore.UI.WriteLine("   Coarse elevation raster : " + Parameters.CoarseElevationRaster);
			ModelCore.UI.WriteLine("   Coarse elevation costs : ");
			PlugIn.Parameters.CoarseElevationCosts.DisplayRangesInConsole(ModelCore);
			ModelCore.UI.WriteLine("   Fine elevation raster : " + Parameters.FineElevationRaster);
			ModelCore.UI.WriteLine("   Fine elevation costs : ");
			PlugIn.Parameters.FineElevationCosts.DisplayRangesInConsole(ModelCore);
			ModelCore.UI.WriteLine("   Coarse water raster : " + Parameters.CoarseWaterRaster);
			ModelCore.UI.WriteLine("   Coarse water cost :  : " + Parameters.CoarseWaterCost);
			ModelCore.UI.WriteLine("   Fine water raster : " + Parameters.CoarseWaterRaster);
			ModelCore.UI.WriteLine("   Fine water cost : " + Parameters.FineWaterCost);
			ModelCore.UI.WriteLine("   Soils raster : " + Parameters.SoilsRaster);
			ModelCore.UI.WriteLine("   Road types : ");
			PlugIn.Parameters.RoadCatalogueNonExit.DisplayRangesInConsole(ModelCore);
			ModelCore.UI.WriteLine("   Road types for exiting wood off the landscape : ");
			PlugIn.Parameters.RoadCatalogueExit.DisplayRangesInConsole(ModelCore);
		}

	}
}
