//  Author: Clément Hardy
// With mant elements shamelessely copied from the corresponding class
// in the "Base Fire" extension by Robert M. Scheller and James B. Domingo

using Landis.SpatialModeling;
// using Landis.SpatialModeling.CoreServices;
using Landis.Utilities;
using System;
using System.Collections.Generic;

namespace Landis.Extension.ForestRoadsSimulation
{
	/// <summary>
	/// The parameters for an road type.
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
				throw new Exception("Forest Roads Simulation : A road type name couldn't be found. This is an internal problem, or a parameter file issue.");
			}
		}

		public bool IsARoad
		{
			get
			{
				if (this.typeNumber != 0) return (true);
				else return (false);
			}
		}

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
					return (true);
				}
			}

			return (false);
		}

		/// <summary>
		/// Updates the type of the road on the pixel according to the flux of wood going on it and the parameters of the extension
		/// </summary>
		public void UpdateAccordingToWoodFlux()
		{
			// If the road ID of this road corresponds to an exit point for the road, we won't update it.
			if (!PlugIn.Parameters.RoadCatalogueExit.isRoadIDInCatalogue(this.typeNumber))
			{
				int roadIDCorrespondingToFlux = PlugIn.Parameters.RoadCatalogueNonExit.GetCorrespondingID(this.timestepWoodFlux);
				// We make the update only if the current road is not of a higher rank, wood-flux-wise, or if it is an undertimined type of road (code : -1)
				if (this.typeNumber == -1 || PlugIn.Parameters.RoadCatalogueNonExit.IsRoadTypeOfHigherRank(roadIDCorrespondingToFlux, this.typeNumber))
				{
					this.typeNumber = roadIDCorrespondingToFlux;
					// As this is a road update, the age of the road goes back to 0.
					this.roadAge = 0;
				}
			}
		}

	}
}
