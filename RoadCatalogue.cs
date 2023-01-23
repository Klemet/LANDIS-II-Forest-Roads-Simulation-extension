// Author: Clément Hardy

using Landis.Library.AgeOnlyCohorts;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using System.IO;
using Landis.Library.Metadata;
using System;
using System.Diagnostics;
using System.Linq;

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
				throw new Exception("FOREST ROADS SIMULATION ERROR : You tried to input a negative value for a threshold, a roadtypeID, a multiplicative cost value, or a maximum age before destruction for a road type. Please check your parameter file." + PlugIn.errorToGithub);
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
				if (this.listOfUpperThresholds[i] <= this.listOfLowerThresholds[i]) throw new Exception("FOREST ROADS SIMULATION ERROR : one of the upper thresholds for the road types flux range is lower or equal to its associated lower threshold. This must be fixed." + PlugIn.errorToGithub);
			}

			// Then, we check if there are holes between the ranges
			for (int i = 0; i < this.numberOfRanges - 1; i++)
			{
				if ((this.listOfUpperThresholds[i] - this.listOfLowerThresholds[i + 1]) != 0) throw new Exception("FOREST ROADS SIMULATION ERROR : There is a hole between the range of wood flux " + (i+1) + " and " + (i + 2) + " in the road types. This must be fixed." + PlugIn.errorToGithub);
			}
		}

		/// <summary>
		/// A function to verify if all the ages entered are superior to 0.
		/// </summary>
		public void VerifyAges()
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (this.maximumAgeBeforeDestruction[this.listOfRoadTypesID[i]] <= 0 ) throw new Exception("FOREST ROADS SIMULATION ERROR : the age before road destruction entered for road ID " + this.listOfRoadTypesID[i] + " is null or negative. Please, make it positive and superior to 0 if you want road aging to be simulated." + PlugIn.errorToGithub);
			}
		}

		/// <summary>
		/// A function to verify that the road are indicated in the order of their multiplicative value.
		/// </summary>
		public void VerifyMultiplicativeValues()
		{
			List<double> listOfMultiplicativeValues = multiplicativeCostValue.Values.ToList();
			for (int i = 0; i < (listOfMultiplicativeValues.Count() - 1); i++)
			{
				if (listOfMultiplicativeValues[i] >= listOfMultiplicativeValues[i+1]) throw new Exception("FOREST ROADS SIMULATION ERROR : the list of your road types doesn't seem to be ordered by their multiplicative value. Please, enter them with the lowest road types (lowest multiplicative value) first. The problem seems to be with the item " + (i + 1) + " of your list of roads." + PlugIn.errorToGithub);
			}
		}

		/// <summary>
		/// A function to check that the user didn't entered the same road ID twice.
		/// </summary>
		public void CheckRedundantRoadID()
		{
			// Code for this Link function that finds duplicates in a list comes from https://stackoverflow.com/questions/3811464/how-to-get-duplicate-items-from-a-list-using-linq/3811482
			List<int> duplicatesRoadIDs = this.listOfRoadTypesID.GroupBy(s => s).SelectMany(grp => grp.Skip(1)).ToList();
			if (duplicatesRoadIDs.Count() != 0) throw new Exception("FOREST ROADS SIMULATION ERRORS: You entered the same road ID multiple times. Please check the road IDs of your road types lists. One of these IDs is : " + duplicatesRoadIDs[0] + "." + PlugIn.errorToGithub);
		}


		/// <summary>
		/// A function to get the road ID associated with a certain woodflux
		/// </summary>
		public int GetCorrespondingIDForWoodFlux(double woodFlux)
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (woodFlux < this.listOfUpperThresholds[i] && woodFlux >= this.listOfLowerThresholds[i]) return (this.listOfRoadTypesID[i]);
			}
			// Case of the woodflux being under the lowest threshold
			if (woodFlux < this.listOfLowerThresholds.First()) return (this.listOfRoadTypesID.First());
			// Case of the woodflux being above the highest threshold
			if (woodFlux > this.listOfUpperThresholds.Last()) return (this.listOfRoadTypesID.Last());

			throw new Exception("FOREST ROADS SIMULATION ERROR : Couldn't find the road type ID associated to the wood flux : " + woodFlux + ". Please check you parameter file." + PlugIn.errorToGithub);
		}

        /// <summary>
        /// Given a certain woodflux on a cell, returns the woodflux threshold for the next category of road.
        /// </summary>
        public double nextWoodFluxThreshold(double woodFlux)
        {
            for (int i = 0; i < this.numberOfRanges; i++)
            {
                if (woodFlux < this.listOfUpperThresholds[i] && woodFlux >= this.listOfLowerThresholds[i]) return (this.listOfUpperThresholds[i]);
            }
            // Case of the woodflux being under the lowest threshold
            if (woodFlux < this.listOfLowerThresholds.First()) return (this.listOfUpperThresholds.First());
            // Case of the woodflux being above the highest threshold
            if (woodFlux > this.listOfUpperThresholds.Last()) return (this.listOfUpperThresholds.Last());

            throw new Exception("FOREST ROADS SIMULATION ERROR : Couldn't find the road type ID associated to the wood flux : " + woodFlux + ". Please check you parameter file." + PlugIn.errorToGithub);
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
			throw new Exception("FOREST ROADS SIMULATION ERROR : Couldn't find the multiplicative cost value associated to the road type ID : " + roadTypeID + ". Please check you parameter file." + PlugIn.errorToGithub);
		}

		/// <summary>
		/// A function to get the ID of the road with the lowest rank.
		/// </summary>
		public int GetIDofLowestRoadType()
		{
			// The lowest road type is automatically the one at the start of the road list.
			return (listOfRoadTypesID[0]);
		}

		/// <summary>
		/// A function to find the ID of a road that is suitable for repeated entry. The road type in question must last for the given amount of year, and its
		/// multiplicative cost must be less than twice the one of the lowest road type; else, it is more optimized to create a small road and let it get destroyed before the repeated entry.
		/// If such a road does not exist, the function will return the lowest road type possible, in order to let it be upgrade by woodflux if needed later.
		/// If the user haven't registered age values, then the function will return a road type superior to the lowest road type, but with multiplicative
		/// cost inferior to 2, in the hypothesis that this will be better for a repeated entry.
		/// </summary>
		public int GetIDofPotentialRoadForRepeatedEntry(int YearsTheRoadShouldLast)
		{
			for (int i = 0; i < this.numberOfRanges; i++)
			{
				if (this.maximumAgeBeforeDestruction[listOfRoadTypesID[i]] >= YearsTheRoadShouldLast
					&& (this.multiplicativeCostValue[listOfRoadTypesID[i]]/ this.multiplicativeCostValue[this.GetIDofLowestRoadType()]) <= 2) return (this.listOfRoadTypesID[i]);
			}
			// If we haven't found a road type corresponding to those conditions, we return the ID of the lowest road type.
			return (this.GetIDofLowestRoadType());
		}

		/// <summary>
		/// Return a boolean telling if a given roadTypeID if of "higher rank" (accomodate more wood flux) than another.
		/// </summary>
		public bool IsRoadTypeOfHigherRank(int roadTypeID, int anotherRoadTypeID)
		{
			// we first have to check if the road type is associated to a threshold; it seems that sometimes, it's not the case.
			// I imagine that it's the case only 
			if (!this.meanThreshold.ContainsKey(roadTypeID) || !this.meanThreshold.ContainsKey(anotherRoadTypeID))
			{
				PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION ERROR : Tried to see if road type ID " + roadTypeID + " is of higher rank than road type ID " + anotherRoadTypeID + ". One of the two doesn't seem to be registered, thought." + PlugIn.errorToGithub);
				return (false);
			}
			if (this.meanThreshold[roadTypeID] > this.meanThreshold[anotherRoadTypeID])
			{
				return (true);
			}
			else { return (false); }
		}

		/// <summary>
		/// Return the ID corresponding to the road of highest rank amongst a list of them.
		/// </summary>
		public int whoIsRoadOfHigherRank(List<int> roadTypesIDs)
		{
			if (roadTypesIDs.Count == 0)
			{
				PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION ERROR : Tried to compare several road IDs, but the list was empty." + PlugIn.errorToGithub);
				return(this.GetIDofLowestRoadType());
			}
			else if (roadTypesIDs.Count == 1)
			{
				return (roadTypesIDs[0]);
			}
			else
			{
				int maximumRankID = roadTypesIDs[0];
				for (int i = 0; i < (roadTypesIDs.Count - 1); i++)
				{
					if (this.IsRoadTypeOfHigherRank(roadTypesIDs[i+1], maximumRankID))
					{
						maximumRankID = roadTypesIDs[i+1];
					}
				}
				return (maximumRankID);
			}
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
