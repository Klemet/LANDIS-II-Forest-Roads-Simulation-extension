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
	/// An road object. It is described by an ID, a list of sites, and two sites as extrimities. The ID is automatically generated when creating a new road thanks to a static ID object.
	/// </summary>
	public class IndividualRoad
	{
		public int ID;
		public List<Site> sitesInTheRoad;
		public List<Site> extremities;
		public List<IndividualRoad> connectedRoads;


		public IndividualRoad()
		{
			this.ID = RoadNetwork.roadIDCounter;
			RoadNetwork.roadIDCounter++;
			// Road is automatically added in the road catalog at the same index as its ID
			// WARNING : Has to be changed !!!
			RoadNetwork.roadCatalog.Add(this);
			this.sitesInTheRoad = new List<Site>();
			this.extremities = new List<Site>();
			this.connectedRoads = new List<IndividualRoad>();
		}

		/// <summary>
		/// Create a connection to another road by registering each in the others list of connected roads.
		/// </summary>
		/// <param name="otherRoad"> Another road to connect to.</param>
		public void CreateConnection(IndividualRoad otherRoad)
		{
			this.connectedRoads.Add(otherRoad);
			otherRoad.connectedRoads.Add(this);
		}

	}
}
