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
	public static class ElevationCostRanges
	{
		private static List<int> listOfLowerThresholds;
		private static List<int> listOfUpperThresholds;
		private static List<double> listOfMultiplicativeValues;
		private static int numberOfRanges;

		public static int NumberOfRanges
		{
			get { return numberOfRanges; }
		}

		public static void Initialize()
		{
			listOfLowerThresholds = new List<int>();
			listOfUpperThresholds = new List<int>();
			listOfMultiplicativeValues = new List<double>();
			numberOfRanges = 0;
		}

		/// <summary>
		/// A function to add a range of elevation cost
		/// </summary>
		public static void AddRange(int LowerThreshold, int UpperThreshold, double MultiplicativeValue)
		{
			listOfLowerThresholds.Add(LowerThreshold);
			listOfUpperThresholds.Add(UpperThreshold);
			listOfMultiplicativeValues.Add(MultiplicativeValue);
			numberOfRanges++;
		}

		/// <summary>
		/// A function to verify if the ranges ae complementory, or if they are spaces between them.
		/// </summary>
		public static void VerifyRanges()
		{
			// First, we check if the lower thresholds are always smaller than the UpperThresholds
			for (int i = 0; i < numberOfRanges; i++)
			{
				if (listOfUpperThresholds[i] <= listOfLowerThresholds[i]) throw new Exception("Forest Roads Simulation : one of the upper thresholds for the elevation cost range is lower or equal to its associated lower threshold. This must be fixed.");
			}

			// Then, we check if there are holes between the ranges
			for (int i = 0; i < numberOfRanges - 1; i++)
			{
				if ((listOfUpperThresholds[i] - listOfLowerThresholds[i + 1]) != 0) throw new Exception("Forest Roads Simulation : There is a hole between the range of fine elevation " + i + " and " + (i+1) + ". This must be fixed.");
			}
		}

		/// <summary>
		/// A function to get the multiplicative value associated with a certain value of fine elevation.
		/// </summary>
		public static double GetMultiplicativeValue(int FineElevation)
		{
			for (int i = 0; i < numberOfRanges; i++)
			{
				if (FineElevation < listOfUpperThresholds[i] && FineElevation >= listOfLowerThresholds[i]) return (listOfMultiplicativeValues[i]);
			}
			throw new Exception("Forest Roads Simulation : Couldn't find the multiplicative value associated to fine elevation value : " + FineElevation + ". Please check you parameter file.");
		}

		/// <summary>
		/// A function for debugging purposes, to display the ranges in a console.
		/// </summary>
		public static void DisplayRangesInConsole(ICore ModelCore)
		{
			for (int i = 0; i < numberOfRanges; i++)
			{
				ModelCore.UI.WriteLine("Lower : " + listOfLowerThresholds[i] + "; Upper : " + listOfUpperThresholds[i] + "; Multi.Value : " + listOfMultiplicativeValues[i]);
			}
		}


	}
}
