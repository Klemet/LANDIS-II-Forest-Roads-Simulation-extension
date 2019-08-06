//  Author:  Clément Hardy

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
		// Propriétés de la classe "Plugin" : Son type (disturbance), son nom (Base fire), mais aussi plusieurs propriétés utilisées
		// pour l'output des données et ses paramètres.
		// La classe PlugIn a besoin de 4 fonctions : un constructeur, LoadParameters, Initialize et Run.
		// Les propriétés en privées sont accédées en lectures via des propriétés qui masquent les privées (définies plus bas)
		public static readonly ExtensionType ExtType = new ExtensionType("disturbance:roads");
		public static readonly string ExtensionName = "Forest Roads Simulation";

		// Propriété pour contenir les paramètres
		private IInputParameters parameters;

		// Propriété qui va contenir l'object "Coeur" de LANDIS-II afin de pouvoir y faire référence dans les fonctions.
		private static ICore modelCore;

		//---------------------------------------------------------------------

		// Constructeur de la classe. Hérite du constructeur de la classe ExtensionMain. Le constructeur
		// est très simple, et remplis juste les propriétés contenant le type de l'extension et son nom. Rien d'autre.
		public PlugIn()
			: base(ExtensionName, ExtType)
		{
		}

		//---------------------------------------------------------------------

		// Propriété pour contenir le coeur du modèle en lecture seule.
		public static ICore ModelCore
		{
			get
			{
				return modelCore;
			}
		}

		//---------------------------------------------------------------------

		// Fonction qui sera appellée au début du fonctionnement de LANDIS-II pour initialiser les paramêtres
		// de l'extension. Pour ce faire, elle demande un chemin vers le fichier de paramêtres en .txt, et une référence
		// vers le coeur de LANDIS-II pour que l'extension puisse s'y lier.
		public override void LoadParameters(string dataFile, ICore mCore)
		{
			// On lie le coeur de LANDIS-II
			modelCore = mCore;

			// On initialise les variables de site, dont le type de route
			SiteVars.Initialize();

			// On charge les paramêtres du fichier .txt
			InputParameterParser parser = new InputParameterParser();
			this.parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
			modelCore.UI.WriteLine("Parameters of the Forest Roads Simulation Extension are loaded");

			// Proof that the input map of roads has been properly read
			/*
			foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
			{
				modelCore.UI.WriteLine("Site " + site.Location + " has for road type " + SiteVars.RoadsInLandscape[site].getRoadTypeName() + " (int type : " + SiteVars.RoadsInLandscape[site].typeNumber + ")");
			}
			*/
		}

		//---------------------------------------------------------------------

		// Cette fonction va aussi être appellée par le coeur de LANDIS-II avant que le scénario ne se mette à tourner.
		// Elle va préparer tout ce qu'il faut pour l'output des données.
		public override void Initialize()
		{
			Timestep = parameters.Timestep;
			modelCore.UI.WriteLine("The timestep for the Forest Road Extension is : " + Timestep);

			modelCore.UI.WriteLine("Initialization of the Forest Roads Simulation Extension is done");
		}

		public override void Run()
		{
			modelCore.UI.WriteLine("Wow ! We just activated the new plugin at the correct timestep ! Isn't it amazing ?");
		}
	}

}
