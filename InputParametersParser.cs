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

			// On lit le raster initial des routes
			InputVar<string> roadNetworkMap = new InputVar<string>("InitialRoadNetworkMap");
			ReadVar(roadNetworkMap);
			MapReader.ReadMap(roadNetworkMap.Value);

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

	}
}
