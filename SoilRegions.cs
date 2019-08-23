using Landis.Library.AgeOnlyCohorts;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using System.IO;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// A class to contain the parameters relative to the elevation cost ranges
	/// </summary>
	public static class SoilRegions
	{
		private static List<SoilRegion> listOfSoilRegions;

		public static void Initialize()
		{
			listOfSoilRegions = new List<SoilRegion>();
		}

		/// <summary>
		/// A function to add a range of soil region
		/// </summary>
		public static void AddRegion(SoilRegion region)
		{
			listOfSoilRegions.Add(region);
		}

		/// <summary>
		/// A function to get the corresponding SoilRegion object from a given mapcode.
		/// </summary>
		public static SoilRegion GetSoilRegion(int MapCode)
		{
			foreach (SoilRegion regionInSoilsRegions in listOfSoilRegions)
			{
				if (MapCode == regionInSoilsRegions.MapCode) return (regionInSoilsRegions);
			}
			throw new Exception("Forest Roads Simulation : Couldn't find the soil region with mapcode : " + MapCode + ". Please check you parameter file.");
		}

		/// <summary>
		/// A function to get the additional cost associated with a certain soil region.
		/// </summary>
		public static double GetAdditionalValue(SoilRegion region)
		{
			if (listOfSoilRegions.Count == 0) throw new Exception("Forest Roads Simulation : ERROR : Attempted to read the soil regions without any soil region registered.");
			foreach (SoilRegion regionInSoilsRegions in listOfSoilRegions)
			{
				if (region.MapCode == regionInSoilsRegions.MapCode) return (regionInSoilsRegions.AdditionalCost);
			}
			throw new Exception("Forest Roads Simulation : Couldn't find the additional value associated to the soil region with mapcode : " + region.MapCode + ". Please check you parameter file.");
		}

		/// <summary>
		/// A function for debugging purposes, to display the regions in a console.
		/// </summary>
		public static void DisplayRegionsInConsole(ICore ModelCore)
		{
			foreach (SoilRegion regionInSoilsRegions in listOfSoilRegions)
			{
				ModelCore.UI.WriteLine("Region : " + regionInSoilsRegions.MapCode + "; Additional cost : " + regionInSoilsRegions.AdditionalCost);
			}
		}


	}
}
