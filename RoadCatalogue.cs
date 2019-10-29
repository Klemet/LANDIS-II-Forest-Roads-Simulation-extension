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
	/// The road catalogue contains the different types of road that the user inputed in the parameter file for the extension.
	/// </summary>
	public class RoadCatalogue
	{
		private List<double> listOfLowerThresholds;
		private List<double> listOfUpperThresholds;
		private List<int> listOfRoadTypesID;
		public Dictionary<int, string> roadNames;
		private Dictionary<int, double> meanThreshold;
		private Dictionary<int, double> multiplicativeCostValue;
		public Dictionary<int, int> maximumAgeBeforeDestruction;
		private bool isItExitCatalogue;
		private int numberOfRanges;

		public int NumberOfRanges
		{
			get { return this.numberOfRanges; }
		}

		public RoadCatalogue(bool isItExitCatalogue)
		{
			if (!isItExitCatalogue)
			{
				this.isItExitCatalogue = false;
				this.listOfLowerThresholds = new List<double>();
				this.listOfUpperThresholds = new List<double>();
				this.meanThreshold = new Dictionary<int, double>();
				this.multiplicativeCostValue = new Dictionary<int, double>();
				this.maximumAgeBeforeDestruction = new Dictionary<int, int>();
			}
			// If this is a road catalogue only containing exit roads for the wood, no need to initialize the thresholds or the rest.
			else
			{
				this.isItExitCatalogue = true;
			}

			this.listOfRoadTypesID = new List<int>();
			this.roadNames = new Dictionary<int, string>();

			this.numberOfRanges = 0;
		}

		/// <summary>
		/// A function to add a range of elevation cost
		/// </summary>
		public void AddRange(double LowerThreshold, double UpperThreshold, int RoadTypeID, double multiplicativeCostValue, string roadTypeName, int maximumAgeBeforeDestruction)
		{
			if (LowerThreshold < 0 || UpperThreshold < 0 || RoadTypeID < 0 || multiplicativeCostValue < 0 || maximumAgeBeforeDestruction < 0)
			{
				throw new Exception("Forest Roads Simulation : You tried to input a negative value for a threshold, a roadtypeID, a multiplicative cost value, or a maximum age before destruction for a road type. Please check your parameter file.");
			}
			this.listOfLowerThresholds.Add(LowerThreshold);
			this.listOfUpperThresholds.Add(UpperThreshold);
			this.listOfRoadTypesID.Add(RoadTypeID);
			this.roadNames[RoadTypeID] = roadTypeName;
			this.meanThreshold[RoadTypeID] = (UpperThreshold + LowerThreshold) / 2;
			this.multiplicativeCostValue[RoadTypeID] = multiplicativeCostValue;
			this.maximumAgeBeforeDestruction[RoadTypeID] = maximumAgeBeforeDestruction;
			this.numberOfRanges++;
		}


		/// <summary>
		/// A function to add a type of exit road for the wood without any thresholds
		/// </summary>
		public void AddExitRoadType(int RoadTypeID, string ExitRoadTypeName)
		{
			this.listOfRoadTypesID.Add(RoadTypeID);
			this.roadNames[RoadTypeID] = ExitRoadTypeName;
			this.numberOfRanges++;
		}


		/// <summary>
		/// Checks if a road ID is contained in the catalogue
		/// </summary>
		public bool isRoadIDInCatalogue(int RoadTypeID)
		{
			if (this.listOfRoadTypesID.Contains(RoadTypeID)) { return (true); }
			else { return (false); }
		}

		/// <summary>
		/// A function to verify if the ranges are complementory, or if they are spaces between them.
		/// </summary>
		public void VerifyRanges()
		{
			// First, we check if the lower thresholds are always smaller than the UpperThresholds
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (this.listOfUpperThresholds[i] <= this.listOfLowerThresholds[i]) throw new Exception("Forest Roads Simulation : one of the upper thresholds for the road types flux range is lower or equal to its associated lower threshold. This must be fixed.");
			}

			// Then, we check if there are holes between the ranges
			for (int i = 0; i < this.numberOfRanges - 1; i++)
			{
				if ((this.listOfUpperThresholds[i] - this.listOfLowerThresholds[i + 1]) != 0) throw new Exception("Forest Roads Simulation : There is a hole between the range of wood flux " + i + " and " + (i + 1) + " in the road types. This must be fixed.");
			}
		}

		/// <summary>
		/// A function to verify if all the ages entered are superior to 0.
		/// </summary>
		public void VerifyAges()
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (this.maximumAgeBeforeDestruction[this.listOfRoadTypesID[i]] <= 0 ) throw new Exception("Forest Roads Simulation : the age before road destruction entered for road ID " + this.listOfRoadTypesID[i] + " is null or negative. Please, make it positive and superior to 0 if you want road aging to be simulated.");
			}
		}

		/// <summary>
		/// A function to get the multiplicative value associated with a certain value of fine elevation.
		/// </summary>
		public int GetCorrespondingID(double woodFlux)
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (woodFlux < this.listOfUpperThresholds[i] && woodFlux >= this.listOfLowerThresholds[i]) return (this.listOfRoadTypesID[i]);
			}

			throw new Exception("Forest Roads Simulation : Couldn't find the road type ID associated to the wood flux : " + woodFlux + ". Please check you parameter file.");
		}

		/// <summary>
		/// A function to get the multiplicative value associated with a certain value of fine elevation.
		/// </summary>
		public double GetCorrespondingMultiplicativeCostValue(int roadTypeID)
		{
			if (this.listOfRoadTypesID.Contains(roadTypeID))
			{
				return (this.multiplicativeCostValue[roadTypeID]);
			}
			throw new Exception("Forest Roads Simulation : Couldn't find the multiplicative cost value associated to the road type ID : " + roadTypeID + ". Please check you parameter file.");
		}

		/// <summary>
		/// Return a boolean telling if a given roadTypeID if of "higher rank" (accomodate more wood flux) than another.
		/// </summary>
		public bool IsRoadTypeOfHigherRank(int roadTypeID, int anotherRoadTypeID)
		{
			if (this.meanThreshold[roadTypeID] > this.meanThreshold[anotherRoadTypeID])
			{
				return (true);
			}
			else { return (false); }
		}

		/// <summary>
		/// A function for debugging purposes, to display the ranges in a console.
		/// </summary>
		public void DisplayRangesInConsole(ICore ModelCore)
		{
			ModelCore.UI.WriteLine("This catalogue contains " + this.numberOfRanges + " entries.");
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (!this.isItExitCatalogue) { ModelCore.UI.WriteLine("   Lower : " + this.listOfLowerThresholds[i] + "; Upper : " + this.listOfUpperThresholds[i]
					+ "; RoadTypeID : " + this.listOfRoadTypesID[i] + "; RoadTypeName : " + this.roadNames[this.listOfRoadTypesID[i]] 
					+ "; Multiplicative cost value : " + this.multiplicativeCostValue[this.listOfRoadTypesID[i]]
					+ "; Maximum age before destruction : " + this.maximumAgeBeforeDestruction[this.listOfRoadTypesID[i]]); }

				else { ModelCore.UI.WriteLine("RoadTypeID: " + this.listOfRoadTypesID[i] + "; RoadTypeName : " + this.roadNames[this.listOfRoadTypesID[i]]); }
			}
		}
	}
}
