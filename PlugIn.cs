// Author: Clément Hardy

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
	public class PlugIn
		: ExtensionMain
	{
		// Properties of the Plugin class : type (disturbance), name (Forest Roads Simulation), 
		// and several other used for the output of data and the reading of its parameters.
		// The PlugIn class needs 4 functions : a constructor, LoadParameters, Initialize et Run
		public static readonly ExtensionType ExtType = new ExtensionType("disturbance:roads");
		public static readonly string ExtensionName = "Forest Roads Simulation";
		private bool harvestExtensionDetected = false;
		private List<RelativeLocation> skiddingNeighborhood;
		private List<RelativeLocation> minLoopingNeighborhood;
		private List<RelativeLocation> maxLoopingNeighborhood;
		public static MetadataTable<RoadLog> roadConstructionLog;
		public static string errorToGithub = " If you cannot solve the issue, please post it on the Github repository and I'll try to help : https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module";


		// Properties to contain the parameters
		private static IInputParameters parameters;

		// Properties to contain the "Core" object of LANDIS-II to reference it in other functions.
		private static ICore modelCore;

		//---------------------------------------------------------------------

		// Constructor of the PlugIn class. Heritates from the construction of the "ExtensionMain" class.
		// It just fills the properties containing the type of the extension and its name : nothing else.
		public PlugIn()
			: base(ExtensionName, ExtType)
		{

		}

		//---------------------------------------------------------------------
		// Properties to get the Model Core in read-only
		public static ICore ModelCore
		{
			get
			{
				return modelCore;
			}
		}

		//---------------------------------------------------------------------
		// Property to contain the parameters in read-only
		public static IInputParameters Parameters
		{
			get
			{
				return parameters;
			}
		}

		//---------------------------------------------------------------------

		// Function launched at the beginning of the LANDIS-II simulation to initialize the parameters of the extension.
		// It requires a reference to the .txt file where the parameters are.
		public override void LoadParameters(string dataFile, ICore mCore)
		{
			modelCore = mCore;

			// We initialize the site variables object
			SiteVars.Initialize();

			// We read the parameters in the .txt file
			InputParameterParser parser = new InputParameterParser();
			parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
			modelCore.UI.WriteLine("   Parameters of the Forest Roads Simulation Extension are loaded");

			// For debugging purposes
			// InputParameterParser.DisplayParameters();
		}

		//---------------------------------------------------------------------

		// Function launched before the simulation starts properly. Used to initialize other things.
		// In the case of this module, it's use to initialize the road network by checking different things.
		public override void Initialize()
		{
			modelCore.UI.WriteLine("  Initialization of the Forest Roads Simulation Extension...");
			Timestep = parameters.Timestep;
			modelCore.UI.WriteLine("  Reading the rasters...");
			// We read all of the maps.
			MapManager.ReadAllMaps();

			// Testing if a harvest extension is included in the scenario and thus initialized
			if (modelCore.GetSiteVar<int>("Harvest.TimeOfLastEvent") != null) { this.harvestExtensionDetected = true; modelCore.UI.WriteLine("Harvest extension correctly detected."); }

			else
			{
				modelCore.UI.WriteLine("   FOREST ROAD SIMULATION WARNING : NO HARVEST EXTENSION DETECTED");
				modelCore.UI.WriteLine("   Without a harvest extension, no roads will be created by this extension. Please include a harvest extension in your scenario, or this extension will be quite useless.");
			}

			// We check if some roads are not connected to sawmills or to a main public network.
			modelCore.UI.WriteLine("   Checking if the wood has somewhere to go...");
			if (!MapManager.IsThereAPlaceForTheWoodToGo(ModelCore))
			{
				throw new Exception("   FOREST ROAD SIMULATION WARNING : There is no site to which the road can flow to (sawmill or main road network). " +
									"   Please put at least one in the input raster containing the initial road network.");
			}
			// If some exist, we initialize the roadnetwork to indicate which road is connected to them, and to connect the roads that might be isolated.
			else
			{
				// We initialize the "cost raster" on which the path of our roads will be based.
				MapManager.CreateCostRaster();
				modelCore.UI.WriteLine("   Cost raster created. It can be visualised in the output folder of the extension.");
				// We initialize the initial road network
				modelCore.UI.WriteLine("   Initializing the road network...");
				RoadNetwork.Initialize(ModelCore, parameters.HeuristicForNetworkConstruction);
				// We initialize the relative locations that will have to be checked to see if their is a road in it at skidding distance from a site.
				skiddingNeighborhood = MapManager.CreateSearchNeighborhood(parameters.SkiddingDistance, modelCore);
				modelCore.UI.WriteLine("   Skidding neighborhood initialized. It contains " + skiddingNeighborhood.Count + " relative locations.");
				// If the loop behavior is activated, we will create a search neighborhood to create loops
				if (parameters.LoopingBehavior)
				{
					minLoopingNeighborhood = MapManager.CreateSearchNeighborhood(parameters.LoopingMinDistance, modelCore);
					maxLoopingNeighborhood = MapManager.CreateSearchNeighborhood(parameters.LoopingMaxDistance, modelCore);
					modelCore.UI.WriteLine("   Smaller looping neighborhood initialized. It contains " + minLoopingNeighborhood.Count + " relative locations.");
					modelCore.UI.WriteLine("   Bigger looping neighborhood initialized. It contains " + maxLoopingNeighborhood.Count + " relative locations.");
				}
				// We initialize the metadatas
				MetadataHandler.InitializeMetadata();
				// If we are going to simulate the wood flux, we initialize objects important for it.
				if (parameters.SimulationOfWoodFlux)
				{
					RoadNetwork.fluxPathCatalogue = new List<FluxPath>();
					RoadNetwork.fluxPathDictionary = new Dictionary<Site, FluxPath>();
				}

				// We output the map at timestep 0. Can be usefull.
				MapManager.WriteMap(parameters.OutputsOfRoadNetworkMaps, modelCore);
			}
			modelCore.UI.WriteLine("   Initialization of the Forest Roads Simulation Extension is done");

		}

		// Function called at every time step where the extension is activated. 
		// Contains the effects of the extension on the landscape.
		public override void Run()
		{
			// We give a warning back to the user if no harvest extension is detected
			if (!this.harvestExtensionDetected)
			{
				modelCore.UI.WriteLine("   FOREST ROAD SIMULATION WARNING : NO HARVEST EXTENSION DETECTED");
				modelCore.UI.WriteLine("   Without a harvest extension, no roads will be created by this extension. Please include a harvest extension in your scenario, or this extension will be quite useless.");
			}

			// If not, we do what the extension have to do at its timestep : for each recently harvested site, we'll try to build a road that lead to it if needed.
			else if (this.harvestExtensionDetected)
			{
				// 1) We age all of the roads in the landscape, if aging is simulated. Those that are too old will be considered destroyed.
				List<Site> listOfSitesWithRoads;
				if (parameters.SimulationOfRoadAging)
				{

					listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);

					foreach (Site siteWithRoad in listOfSitesWithRoads)
					{
						SiteVars.RoadsInLandscape[siteWithRoad].agingTheRoad(siteWithRoad);
					}

					// 2) We update the status of all the roads concerning their connection to an exit point (sawmill or main road network); so that the pathfinding algorithms can now when to stop afterward.
					ModelCore.UI.WriteLine("   Looking to see if the roads can go to a exit point (sawmill, main road network)...");
					listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);
					RoadNetwork.UpdateConnectionToExitPointStatus();
				}


				// 3) We get all of the sites for which a road must be constructed
				modelCore.UI.WriteLine("  Getting sites recently harvested...");
				List<Site> listOfHarvestedSites = MapManager.GetAllRecentlyHarvestedSites(ModelCore, Timestep);

				// 4) We shuffle the list according to the heuristic given by the user.
				modelCore.UI.WriteLine("  Shuffling sites according to the heuristic...");
				listOfHarvestedSites = MapManager.ShuffleAccordingToHeuristic(ModelCore, listOfHarvestedSites, parameters.HeuristicForNetworkConstruction);

				// 5) We reset the wood flux objects and data for the current timestep, if wood flux is sumlated
				if (parameters.SimulationOfWoodFlux)
				{
					modelCore.UI.WriteLine("  Reseting wood flux data...");
					RoadNetwork.RestTimestepWoodFluxData();
				}

				// 6) We initialize some UI elements because this step takes time, and set the cost of construction/repairs at this timestep to 0.
				modelCore.UI.WriteLine("  Number of recently harvested sites : " + listOfHarvestedSites.Count);
				modelCore.UI.WriteLine("  Generating roads to harvested sites...");
				var progressBar = modelCore.UI.CreateProgressMeter(listOfHarvestedSites.Count);
				var watch = System.Diagnostics.Stopwatch.StartNew();
				int roadConstructedAtThisTimestep = 0;
				RoadNetwork.costOfConstructionAndRepairsAtTimestep = 0;

				// 7) We construct the roads to each harvested site
				foreach (Site harvestedSite in listOfHarvestedSites)
				{
					// We construct the road only if the cell is at more than the given skidding distance by the user from an existing road.
					if (!MapManager.IsThereANearbyRoad(skiddingNeighborhood, harvestedSite))
					{
						// If the looping behavior is activated, we will check if we should do a loop.
						if (parameters.LoopingBehavior)
						{
							// To create a normal road if one of the conditions fail
							bool conditionsForLoop = true;

							// We don't create a loop if there are roads that are too close
							int roadsInSmallNeighborhood = MapManager.HowManyRoadsNearby(minLoopingNeighborhood, harvestedSite);
							if (roadsInSmallNeighborhood > 0) { conditionsForLoop = false; }
							else
							{
								// We don't create a loop if there are no roads close enough, at least 2 to make a loop
								int roadsInLargeNeighborhood = MapManager.HowManyRoadsNearby(maxLoopingNeighborhood, harvestedSite);
								if (roadsInLargeNeighborhood < 2) { conditionsForLoop = false; }
								else
								{
									// We don't create a loop if there are too many roads nearby
									double percentageOfRoadsAround = (double)((double)roadsInLargeNeighborhood * 100.0) / (double)maxLoopingNeighborhood.Count;
									if (percentageOfRoadsAround > parameters.LoopingMaxPercentageOfRoads) { conditionsForLoop = false; }
									else
									{
										// Now, we let the dijkstra function for the loop try to create a loop.
										// However, if the loop is too costly to build or if the probability isn't right, the loop will not be constructed (see inside this function)
										int numberOfRoadsCreated = DijkstraSearch.DijkstraLeastCostPathWithLooping(ModelCore, harvestedSite, minLoopingNeighborhood, parameters.LoopingMaxCost);
										roadConstructedAtThisTimestep += numberOfRoadsCreated;
									}
								}
							}

							// If one of the conditions to make the loop has failed, we create a normal road.
							if (!conditionsForLoop)
							{
								DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, harvestedSite);
								roadConstructedAtThisTimestep++;
							}
						}
						// If no looping behavior, we just create the least-cost road to the site.
						else
						{
							DijkstraSearch.DijkstraLeastCostPathToClosestConnectedRoad(ModelCore, harvestedSite);
							roadConstructedAtThisTimestep++;
						}

					}

					progressBar.IncrementWorkDone(1);
				}

				watch.Stop();
				modelCore.UI.WriteLine("   At this timestep, " + roadConstructedAtThisTimestep + " roads were built");
				modelCore.UI.WriteLine("   The construction took " + watch.ElapsedMilliseconds / 1000 + " seconds.\n");

				// 8) If woodflux is simulated, We add the woodflux from the harvested area to the closest road, and we then used a dijkstra 
				// search that only goes through roads in order to reach an exit point for the wood. Every road used will see its flux value 
				// for the timestep augment by one. The quantity of woodflux is based on the number of cohorts harvested in the cell.
				if (parameters.SimulationOfWoodFlux)
				{
					modelCore.UI.WriteLine("  Fluxing the wood to exit points...");

					progressBar = modelCore.UI.CreateProgressMeter(listOfHarvestedSites.Count);
					int sitesFluxedAtThisTimestep = 0;
					watch.Restart();
					ISiteVar<int> numberOfCohortsDamaged = modelCore.GetSiteVar<int>("Harvest.CohortsDamaged");

					foreach (Site harvestedSite in listOfHarvestedSites)
					{
						int siteFlux = numberOfCohortsDamaged[harvestedSite];
						Site siteWithClosestRoad = MapManager.GetClosestSiteWithRoad(skiddingNeighborhood, harvestedSite);

						// Case of the siteWithClosestRoad being a fluxpath : we just flux this path.
						if (RoadNetwork.fluxPathDictionary.ContainsKey(siteWithClosestRoad))
						{
							RoadNetwork.fluxPathDictionary[siteWithClosestRoad].FluxPathFromSite(siteWithClosestRoad, siteFlux);
						}
						// If not, we create a flux path.
						else
						{
							DijkstraSearch.DijkstraWoodFlux(PlugIn.ModelCore, siteWithClosestRoad, siteFlux);
						}
						
						sitesFluxedAtThisTimestep++;
						progressBar.IncrementWorkDone(1);
					}

					modelCore.UI.WriteLine("   At this timestep, the wood of " + sitesFluxedAtThisTimestep + " sites was fluxed.");
					modelCore.UI.WriteLine("   The fluxing took " + watch.ElapsedMilliseconds / 1000 + " seconds.\n");
				}

				// 9) We finish by upgrading the types of the roads if they have to be according to the woodflux for the timestep, and the parameters given by the user, if woodflux is simulated.
				if (parameters.SimulationOfWoodFlux)
				{
					listOfSitesWithRoads = MapManager.GetSitesWithRoads(ModelCore);

					foreach (Site siteWithRoad in listOfSitesWithRoads)
					{
						SiteVars.RoadsInLandscape[siteWithRoad].UpdateAccordingToWoodFlux(siteWithRoad);
					}

				}

				// 10) We write the output maps
				MapManager.WriteMap(parameters.OutputsOfRoadNetworkMaps, modelCore);
				if (parameters.SimulationOfWoodFlux) { MapManager.WriteMap(parameters.OutputsOfRoadNetworkMaps, modelCore, "WoodFlux"); }

				// 11) We write the log, and we're done for this timestep.
				roadConstructionLog.Clear();
				RoadLog roadLog = new RoadLog();
				roadLog.Timestep = modelCore.CurrentTime;
				roadLog.NumberOfHarvestedSitesToConnect = listOfHarvestedSites.Count;
				roadLog.NumberOfRoadsConstructed = roadConstructedAtThisTimestep;
				roadLog.TimeTaken = (int)watch.ElapsedMilliseconds / (1000 * 60);
				roadLog.CostOfConstructionAndRepairs = RoadNetwork.costOfConstructionAndRepairsAtTimestep;
				roadConstructionLog.AddObject(roadLog);
				roadConstructionLog.WriteToFile();

			} // End of if harvest extension detected

		} // End of run function

	} // End of PlugIn class

} // End of namespace
