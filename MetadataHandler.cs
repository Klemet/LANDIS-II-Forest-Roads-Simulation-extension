using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Landis.Library.Metadata;
using Landis.Core;
using Landis.Utilities;
using System.IO;
using Flel = Landis.Utilities;

namespace Landis.Extension.ForestRoadsSimulation
{
	public static class MetadataHandler
	{

		public static ExtensionMetadata Extension { get; set; }

		public static void InitializeMetadata()
		{

			ScenarioReplicationMetadata scenRep = new ScenarioReplicationMetadata()
			{
				RasterOutCellArea = PlugIn.ModelCore.CellArea,
				TimeMin = PlugIn.ModelCore.StartTime,
				TimeMax = PlugIn.ModelCore.EndTime,
			};

			Extension = new ExtensionMetadata(PlugIn.ModelCore)
			//Extension = new ExtensionMetadata()
			{
				Name = PlugIn.ExtensionName,
				TimeInterval = PlugIn.Parameters.Timestep,
				ScenarioReplicationMetadata = scenRep
			};

			//---------------------------------------
			//          Table output:   
			//---------------------------------------

			CreateDirectory("Forest Roads Construction Log.csv");
			PlugIn.roadConstructionLog = new MetadataTable<RoadLog>("Forest Roads Construction Log.csv");


			PlugIn.ModelCore.UI.WriteLine("   Generating event table...");
			OutputMetadata tblOut_constructionLog = new OutputMetadata()
			{
				Type = OutputType.Table,
				Name = "Forest Roads Construction Log.csv",
				FilePath = "./",
				Visualize = true,
			};
			tblOut_constructionLog.RetriveFields(typeof(RoadLog));
			Extension.OutputMetadatas.Add(tblOut_constructionLog);

			//---------------------------------------
			MetadataProvider mp = new MetadataProvider(Extension);
			mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);

		}
		public static void CreateDirectory(string path)
		{
			//Require.ArgumentNotNull(path);
			path = path.Trim(null);
			if (path.Length == 0)
				throw new ArgumentException("path is empty or just whitespace");

			string dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir))
			{
				Flel.Directory.EnsureExists(dir);
			}

			//return new StreamWriter(path);
			return;
		}
	}
}
