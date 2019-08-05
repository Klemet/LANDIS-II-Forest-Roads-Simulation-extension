//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.Utilities;
using System.Collections.Generic;
using System.Text;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// A parser that reads the plug-in's parameters from text input.
	/// </summary>
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
			InputVar<string> landisData = new InputVar<string>("LandisData");
			ReadVar(landisData);
			if (landisData.Value.Actual != PlugIn.ExtensionName)
				throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", PlugIn.ExtensionName);

			InputParameters parameters = new InputParameters();

			InputVar<int> timestep = new InputVar<int>("Timestep");
			ReadVar(timestep);
			parameters.Timestep = timestep.Value;

			//----------------------------------------------------------
			// First, read table of additional parameters for ecoregions
			PlugIn.ModelCore.UI.WriteLine("   Loading FireRegion data...");

			//IEditableFireRegionDataset dataset = new EditableFireRegionDataset();
			List<IFireRegion> dataset = new List<IFireRegion>(0);

			Dictionary<string, int> nameLineNumbers = new Dictionary<string, int>();
			Dictionary<ushort, int> mapCodeLineNumbers = new Dictionary<ushort, int>();

			InputVar<string> name = new InputVar<string>("Fire Region Name");
			InputVar<ushort> mapCode = new InputVar<ushort>("Map Code");
			InputVar<double> meanSize = new InputVar<double>("Mean Fire Size");
			InputVar<double> maxSize = new InputVar<double>("Maximum Fire Size");
			InputVar<double> minSize = new InputVar<double>("Minimum Fire Size");
			InputVar<double> ignitionProb = new InputVar<double>("Ignition Probability");
			InputVar<int> fireSpreadAge = new InputVar<int>("Fire Spread Age");

			Dictionary<string, int> lineNumbers = new Dictionary<string, int>();
			const string DynamicFireRegionTable = "DynamicFireRegionTable";
			const string InitialFireFireRegionsMap = "InitialFireRegionsMap";


			int fireRegionIndex = 0;
			while (!AtEndOfInput && CurrentName != InitialFireFireRegionsMap)
			{
				//IEditableFireRegionParameters ecoparameters = new EditableFireRegionParameters();
				IFireRegion ecoparameters = new FireRegion(fireRegionIndex);

				dataset.Add(ecoparameters);

				StringReader currentLine = new StringReader(CurrentLine);

				int lineNumber;

				ReadValue(name, currentLine);
				if (nameLineNumbers.TryGetValue(name.Value.Actual, out lineNumber))
					throw new InputValueException(name.Value.String,
												  "The name \"{0}\" was previously used on line {1}",
												  name.Value.Actual, lineNumber);
				else
					nameLineNumbers[name.Value.Actual] = LineNumber;
				ecoparameters.Name = name.Value;

				ReadValue(mapCode, currentLine);
				if (mapCodeLineNumbers.TryGetValue(mapCode.Value.Actual, out lineNumber))
					throw new InputValueException(mapCode.Value.String,
												  "The map code {0} was previously used on line {1}",
												  mapCode.Value.Actual, lineNumber);
				else
					mapCodeLineNumbers[mapCode.Value.Actual] = LineNumber;
				ecoparameters.MapCode = mapCode.Value;

				ReadValue(meanSize, currentLine);
				ecoparameters.MeanSize = meanSize.Value;

				ReadValue(minSize, currentLine);
				ecoparameters.MinSize = minSize.Value;

				ReadValue(maxSize, currentLine);
				ecoparameters.MaxSize = maxSize.Value;

				ReadValue(ignitionProb, currentLine);
				ecoparameters.IgnitionProbability = ignitionProb.Value;

				ReadValue(fireSpreadAge, currentLine);
				ecoparameters.FireSpreadAge = fireSpreadAge.Value;

				//UI.WriteLine("Max={0}, Min={1}, Mean={2}, Eco={3}.", ecoparameters.MaxSize, ecoparameters.MinSize, ecoparameters.MeanSize, ecoparameters.Name);
				fireRegionIndex++;

				CheckNoDataAfter("the " + fireSpreadAge.Name + " column",
								 currentLine);

				GetNextLine();
			}

			FireRegions.Dataset = dataset;


			//----------------------------------------------------------
			// Read in the initial fire ecoregions map:


			InputVar<string> ecoregionsMap = new InputVar<string>("InitialFireRegionsMap");
			ReadVar(ecoregionsMap);
			FireRegions.ReadMap(ecoregionsMap.Value);

			//----------------------------------------------------------
			// Read in the table of dynamic ecoregions:

			const string FuelCurves = "FuelCurveTable";

			if (ReadOptionalName(DynamicFireRegionTable))
			{
				//ReadName(DynamicFireRegionTable);

				InputVar<string> mapName = new InputVar<string>("Dynamic Map Name");
				InputVar<int> year = new InputVar<int>("Year to read in new FireRegion Map");

				double previousYear = 0;

				while (!AtEndOfInput && CurrentName != FuelCurves)
				{
					StringReader currentLine = new StringReader(CurrentLine);

					IDynamicFireRegion dynEco = new DynamicFireRegion();
					parameters.DynamicFireRegions.Add(dynEco);

					ReadValue(year, currentLine);
					dynEco.Year = year.Value;

					if (year.Value.Actual <= previousYear)
					{
						throw new InputValueException(year.Value.String,
							"Year must > the year ({0}) of the preceeding ecoregion map",
							previousYear);
					}

					previousYear = year.Value.Actual;

					ReadValue(mapName, currentLine);
					dynEco.MapName = mapName.Value;

					CheckNoDataAfter("the " + mapName.Name + " column",
									 currentLine);
					GetNextLine();
				}
			}

			//-------------------------------------------------------------
			// Second, read table of Fire curve parameters for ecoregions
			ReadName(FuelCurves);

			InputVar<string> fireRegionName = new InputVar<string>("Fire Region Name");
			InputVar<int> severity1 = new InputVar<int>("Fire Severity1 Age");
			InputVar<int> severity2 = new InputVar<int>("Fire Severity2 Age");
			InputVar<int> severity3 = new InputVar<int>("Fire Severity3 Age");
			InputVar<int> severity4 = new InputVar<int>("Fire Severity4 Age");
			InputVar<int> severity5 = new InputVar<int>("Fire Severity5 Age");

			lineNumbers.Clear();
			const string WindCurves = "WindCurveTable";

			while (!AtEndOfInput && CurrentName != WindCurves)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(fireRegionName, currentLine);
				IFireRegion ecoregion = GetFireRegion(fireRegionName.Value,
													lineNumbers);

				IFuelCurve fuelCurve = new FuelCurve();

				ReadValue(severity1, currentLine);
				fuelCurve.Severity1 = severity1.Value;

				ReadValue(severity2, currentLine);
				fuelCurve.Severity2 = severity2.Value;

				ReadValue(severity3, currentLine);
				fuelCurve.Severity3 = severity3.Value;

				ReadValue(severity4, currentLine);
				fuelCurve.Severity4 = severity4.Value;

				ReadValue(severity5, currentLine);
				fuelCurve.Severity5 = severity5.Value;

				ecoregion.FuelCurve = fuelCurve; //.GetComplete();

				CheckNoDataAfter("the " + severity1.Name + " column",
								 currentLine);
				GetNextLine();
			}

			//----------------------------------------------------------
			// Third, read table of wind curve parameters for ecoregions
			ReadName(WindCurves);

			InputVar<int> wseverity1 = new InputVar<int>("Wind Severity1 Age");
			InputVar<int> wseverity2 = new InputVar<int>("Wind Severity2 Age");
			InputVar<int> wseverity3 = new InputVar<int>("Wind Severity3 Age");
			InputVar<int> wseverity4 = new InputVar<int>("Wind Severity4 Age");
			InputVar<int> wseverity5 = new InputVar<int>("Wind Severity5 Age");

			lineNumbers.Clear();
			const string FireDamage = "FireDamageTable";

			while (!AtEndOfInput && CurrentName != FireDamage)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				ReadValue(fireRegionName, currentLine);
				IFireRegion ecoregion = GetFireRegion(fireRegionName.Value,
													lineNumbers);

				IWindCurve windCurve = new WindCurve();

				ReadValue(wseverity5, currentLine);
				windCurve.Severity5 = wseverity5.Value;

				ReadValue(wseverity4, currentLine);
				windCurve.Severity4 = wseverity4.Value;

				ReadValue(wseverity3, currentLine);
				windCurve.Severity3 = wseverity3.Value;

				ReadValue(wseverity2, currentLine);
				windCurve.Severity2 = wseverity2.Value;

				ReadValue(wseverity1, currentLine);
				windCurve.Severity1 = wseverity1.Value;

				ecoregion.WindCurve = windCurve; //.GetComplete();

				CheckNoDataAfter("the " + wseverity1.Name + " column",
								 currentLine);
				GetNextLine();
			}

			//-------------------------------------------------------------------
			//  Read table of Fire Damage classes.
			//  Damages are in increasing order.
			ReadName(FireDamage);

			InputVar<Percentage> maxAge = new InputVar<Percentage>("Max Survival Age");
			InputVar<int> severTolerDifference = new InputVar<int>("Severity Tolerance Diff");

			const string MapNames = "MapNames";
			int previousNumber = -4;
			double previousMaxAge = 0.0;

			while (!AtEndOfInput && CurrentName != MapNames
								  && previousNumber != 4)
			{
				StringReader currentLine = new StringReader(CurrentLine);

				IDamageTable damage = new DamageTable();
				parameters.FireDamages.Add(damage);

				ReadValue(maxAge, currentLine);
				damage.MaxAge = maxAge.Value;
				if (maxAge.Value.Actual <= 0)
				{
					//  Maximum age for damage must be > 0%
					throw new InputValueException(maxAge.Value.String,
									  "Must be > 0% for the all damage classes");
				}
				if (maxAge.Value.Actual > 1)
				{
					//  Maximum age for damage must be <= 100%
					throw new InputValueException(maxAge.Value.String,
									  "Must be <= 100% for the all damage classes");
				}
				//  Maximum age for every damage must be > 
				//  maximum age of previous damage.
				if (maxAge.Value.Actual <= previousMaxAge)
				{
					throw new InputValueException(maxAge.Value.String,
						"MaxAge must > the maximum age ({0}) of the preceeding damage class",
						previousMaxAge);
				}

				previousMaxAge = (double)maxAge.Value.Actual;

				ReadValue(severTolerDifference, currentLine);
				damage.SeverTolerDifference = severTolerDifference.Value;

				//Check that the current damage number is > than
				//the previous number (numbers are must be in increasing
				//order).
				if (severTolerDifference.Value.Actual <= previousNumber)
					throw new InputValueException(severTolerDifference.Value.String,
												  "Expected the damage number {0} to be greater than previous {1}",
												  damage.SeverTolerDifference, previousNumber);
				if (severTolerDifference.Value.Actual > 4)
					throw new InputValueException(severTolerDifference.Value.String,
												  "Expected the damage number {0} to be less than 5",
												  damage.SeverTolerDifference);

				previousNumber = severTolerDifference.Value.Actual;

				CheckNoDataAfter("the " + severTolerDifference.Name + " column",
								 currentLine);
				GetNextLine();
			}

			if (parameters.FireDamages.Count == 0)
				throw NewParseException("No damage classes defined.");

			InputVar<string> mapNames = new InputVar<string>(MapNames);
			ReadVar(mapNames);
			parameters.MapNamesTemplate = mapNames.Value;

			InputVar<string> logFile = new InputVar<string>("LogFile");
			ReadVar(logFile);
			parameters.LogFileName = logFile.Value;

			InputVar<string> summaryLogFile = new InputVar<string>("SummaryLogFile");
			ReadVar(summaryLogFile);
			parameters.SummaryLogFileName = summaryLogFile.Value;

			CheckNoDataAfter(string.Format("the {0} parameter", summaryLogFile.Name));

			return parameters; //.GetComplete();
		}

		//---------------------------------------------------------------------

		private void ValidatePath(InputValue<string> path)
		{
			if (path.Actual.Trim(null) == "")
				throw new InputValueException(path.String,
											  "Invalid file path: {0}",
											  path.String);
		}

		//---------------------------------------------------------------------

		private IFireRegion GetFireRegion(InputValue<string> ecoregionName,
										Dictionary<string, int> lineNumbers)
		{
			IFireRegion ecoregion = FireRegions.FindName(ecoregionName.Actual);
			if (ecoregion == null)
				throw new InputValueException(ecoregionName.String,
											  "{0} is not an ecoregion name.",
											  ecoregionName.String);
			int lineNumber;
			if (lineNumbers.TryGetValue(ecoregion.Name, out lineNumber))
				throw new InputValueException(ecoregionName.String,
											  "The ecoregion {0} was previously used on line {1}",
											  ecoregionName.String, lineNumber);
			else
				lineNumbers[ecoregion.Name] = LineNumber;

			return ecoregion;
		}
	}
}
