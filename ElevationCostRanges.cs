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
	public class ElevationCostRanges
	{
		private List<int> listOfLowerThresholds;
		private List<int> listOfUpperThresholds;
		private List<double> listOfCorrespondingValues;
		private int numberOfRanges;

		public int NumberOfRanges
		{
			get { return this.numberOfRanges; }
		}

		public ElevationCostRanges()
		{
			this.listOfLowerThresholds = new List<int>();
			this.listOfUpperThresholds = new List<int>();
			this.listOfCorrespondingValues = new List<double>();
			this.numberOfRanges = 0;
		}

		/// <summary>
		/// A function to add a range of elevation cost
		/// </summary>
		public void AddRange(int LowerThreshold, int UpperThreshold, double MultiplicativeValue)
		{
			this.listOfLowerThresholds.Add(LowerThreshold);
			this.listOfUpperThresholds.Add(UpperThreshold);
			this.listOfCorrespondingValues.Add(MultiplicativeValue);
			this.numberOfRanges++;
		}

		/// <summary>
		/// A function to verify if the ranges ae complementory, or if they are spaces between them.
		/// </summary>
		public void VerifyRanges()
		{
			// First, we check if the lower thresholds are always smaller than the UpperThresholds
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (this.listOfUpperThresholds[i] <= this.listOfLowerThresholds[i]) throw new Exception("Forest Roads Simulation : one of the upper thresholds for the elevation cost range is lower or equal to its associated lower threshold. This must be fixed.");
			}

			// Then, we check if there are holes between the ranges
			for (int i = 0; i < this.numberOfRanges - 1; i++)
			{
				if ((this.listOfUpperThresholds[i] - this.listOfLowerThresholds[i + 1]) != 0) throw new Exception("Forest Roads Simulation : There is a hole between the range of elevation " + i + " and " + (i+1) + ". This must be fixed.");
			}
		}

		/// <summary>
		/// A function to get the multiplicative value associated with a certain value of fine elevation.
		/// </summary>
		public double GetCorrespondingValue(double Elevation)
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (Elevation < this.listOfUpperThresholds[i] && Elevation >= this.listOfLowerThresholds[i]) return (this.listOfCorrespondingValues[i]);
			}
			
			throw new Exception("Forest Roads Simulation : Couldn't find the value associated to the elevation value : " + Elevation + ". Please check you parameter file.");
		}

		/// <summary>
		/// A function for debugging purposes, to display the ranges in a console.
		/// </summary>
		public void DisplayRangesInConsole(ICore ModelCore)
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				ModelCore.UI.WriteLine("   Lower : " + this.listOfLowerThresholds[i] + "; Upper : " + this.listOfUpperThresholds[i] + "; Associated Value : " + this.listOfCorrespondingValues[i]);
			}
		}


	}
}
