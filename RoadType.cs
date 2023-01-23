// Author: Clément Hardy

using Landis.Ecoregions;
using Landis.SpatialModeling;
// using Landis.SpatialModeling.CoreServices;
using Landis.Utilities;
using System;
using System.Collections.Generic;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// The properties of a road type.
	/// </summary>
	public class RoadType
	{
		public int typeNumber { get; set; }
		public double timestepWoodFlux { get; set; }
		public bool isConnectedToSawMill { get; set; }
		public int roadAge { get; set; }

		public RoadType(int type)
		{
			this.typeNumber = type;
			this.timestepWoodFlux = 0;
			this.roadAge = 0;
			if (PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber)) this.isConnectedToSawMill = true;
			else this.isConnectedToSawMill = false;
		}

		/// <summary>
		/// Gets the name of a given road type.
		/// </summary>
		/// <returns></returns>
		public string getRoadTypeName()
		{
			if (PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber))
			{
				return (PlugIn.Parameters.RoadCatalogueNonExit.roadNames[this.typeNumber]);
			}
			else if (PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber))
			{
				return (PlugIn.Parameters.RoadCatalogueExit.roadNames[this.typeNumber]);
			}
			else if (this.typeNumber == 0)
			{
				return ("No road");
			}
			else
			{
				throw new Exception("FOREST ROADS SIMULATION ERROR : A road type name couldn't be found. This is an internal problem, or a parameter file issue. The unknown road type ID was: " + this.typeNumber);
			}
		}

		/// <summary>
		/// Checks if the road type for this site/pixel correspond to a road. This is the case if the road type is contained in one of the road catalogues; if not, it's not a road.
		/// </summary>
		public bool IsARoad
		{
			get
			{
				if (PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber) || PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber)) return (true);
				else return (false);
			}
		}

		/// <summary>
		/// Returns "true" if the road on the site corresponds to an exit point for the wood to go to.
		/// </summary>
		public bool IsAPlaceForTheWoodToGo
		{
			get
			{
				if (PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber)) return (true);
				else return (false);
			}
		}

		/// <summary>
		/// Makes the road age according to the timestep of the extension. If the age of the road is above the limit of age for her type before it gets destroyed, then the road gets destroyed (back to type 0)
		/// </summary>
		/// <returns>true if the road has been destroyed by age</returns>
		public bool agingTheRoad(Site site)
		{
			this.roadAge += PlugIn.Parameters.Timestep;

			// We only take into account the roads that are not exit points for the wood. Those are immortal.
			if (PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber))
			{
				if (this.roadAge > PlugIn.Parameters.RoadCatalogueNonExit.maximumAgeBeforeDestruction[this.typeNumber])
				{
					this.typeNumber = 0;
					this.isConnectedToSawMill = false;
					// We indicate to the cost raster with roads that there is no more road in this site.
					SiteVars.CostRasterWithRoads[site] = SiteVars.BaseCostRaster[site];
					// We destroy any endpath this road was part of.
					if (PlugIn.endPathsAssociatedToSite.ContainsKey(site)) { PlugIn.endPathsAssociatedToSite[site].DissolveEndPath(); }
					return (true);
				}
			}

			return (false);
		}

		/// <summary>
		/// Updates the type of the road on the pixel according to the flux of wood going on it and the parameters of the extension
		/// </summary>
		public void UpdateAccordingToWoodFlux(Site site)
		{
			int oldTypeNumber = this.typeNumber;
			// If the road ID of this road corresponds to an exit point for the road, we won't update it; If it's not in the Road catalogue for non-exits, there's an issue.
			if (PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber))
			{
				int roadIDCorrespondingToFlux = PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingIDForWoodFlux(this.timestepWoodFlux);
				// We make the update only if the current road is not of a higher rank, wood-flux-wise
				if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(roadIDCorrespondingToFlux, this.typeNumber))
				{
					this.typeNumber = roadIDCorrespondingToFlux;
					// As this is a road update, the age of the road goes back to 0.
					this.roadAge = 0;
					// We add the cost of upgrade to the road costs at this timestep. The cost of the upgrade is the multplication of the cost raster value for this pixel by the difference between the multiplicative cost values
					// of before the upgrade, and fater the upgrade.
					RoadNetwork.costOfConstructionAndRepairsAtTimestep += ComputeUpdateCost(site, oldTypeNumber, this.typeNumber);
				}
			}
			// If it's in neither road catalogue, there's a problem.
			else if (!PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber) && !PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber))
			{
				PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION ERROR : Tried to update " + this.typeNumber + " , but it's not registered as a valid RoadID.");
			}
		}

        /// <summary>
        /// Returns the road type ID that this current road will be updated to if we increase the road flux on it by a particular amount
        /// </summary>
        public int UpdateNeedIfWoodFluxIncrease(double woodFluxNumber)
        {
			int oldTypeNumber = this.typeNumber;

            // If the road ID of this road corresponds to an exit point for the road, we won't update it; If it's not in the Road catalogue for non-exits, there's an issue.
            if (PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber))
            {
                int roadIDCorrespondingToFlux = PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingIDForWoodFlux(this.timestepWoodFlux + woodFluxNumber);
                // We make the update only if the current road is not of a higher rank, wood-flux-wise
                if (PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(roadIDCorrespondingToFlux, this.typeNumber))
                {
					return (roadIDCorrespondingToFlux);
                }
				else { return (oldTypeNumber); }
            }
            // If it's in neither road catalogue, there's a problem.
            else if (!PlugIn.Parameters.RoadCatalogueNonExit.isRoadIDInCatalogue(this.typeNumber) && !PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber))
            {
                PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION ERROR : Tried to update " + this.typeNumber + " , but it's not registered as a valid RoadID.");
				return (oldTypeNumber);
            }
			else { return (oldTypeNumber); }
        }

        /// <summary>
        /// Returns the amount of wood flux our road type can deal with before needing to be updated.
        /// </summary>
        public double woodFluxThatRoadCanHandleBeforeUpdate()
        {
			if (PlugIn.Parameters.RoadCatalogueNonExit.nextWoodFluxThreshold(this.timestepWoodFlux) < this.timestepWoodFlux)
			{
				PlugIn.ModelCore.UI.WriteLine("FOREST ROADS SIMULATION ERROR : Got a upper wood flux threshold that was superior to current woodflux for cell.");
				return (0);
			}
			else
			{
                return (PlugIn.Parameters.RoadCatalogueNonExit.nextWoodFluxThreshold(this.timestepWoodFlux) - this.timestepWoodFlux);
            }
        }

        /// <summary>
        /// Computes the cost of a road update. It corresponds to the costs of construction on the cost raster (without taking roads into account), times
		/// the difference between the multiplicative costs of the two types of roads, times a parameter to reduce the cost.
        /// </summary>
        public double ComputeUpdateCost(Site site, int OldTypeNumber, int NewTypeNumber)
		{
			return (SiteVars.BaseCostRaster[site] *
				   (PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(NewTypeNumber) -
				   PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingMultiplicativeCostValue(OldTypeNumber)) * PlugIn.Parameters.UpgradeCostReduction);

		}

	}
}
