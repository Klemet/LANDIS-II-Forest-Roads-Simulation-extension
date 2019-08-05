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
		// Les propriétés en privées sont accédées en lectures via des propriétés qui masquent les privées (définies plus bas)
		public static readonly ExtensionType ExtType = new ExtensionType("disturbance:fire");
		public static readonly string ExtensionName = "Base Fire";

		// Propriétés pour métadonnées
		private string mapNameTemplate;
		// public static MetadataTable<SummaryLog> summaryLog;
		// public static MetadataTable<EventsLog> eventLog;
		private int[] summaryFireRegionEventCount;
		private int summaryTotalSites;
		private int summaryEventCount;
		// Propriétés pour paramêtres
		// private List<IDynamicFireRegion> dynamicEcos;
		// private IInputParameters parameters;
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
			// On initialise les variables des sites; voir "SiteVars". On va creer 5 nouvelles variables, 
			// récuperer les cohortes et enregistrer deux des 5 (sévérité, temps depuis dernier feu) dans le coeur (?)
			// SiteVars.Initialize();
			// On utilise le parser de LANDIS-II pour lire le fichier de paramêtres, et mettre les paramêtres dans une propriété
			// de la classe.
			// InputParameterParser parser = new InputParameterParser();
			// parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
		}

		//---------------------------------------------------------------------

		// Cette fonction va aussi être appellée par le coeur de LANDIS-II avant que le scénario ne se mette à tourner.
		// Elle va préparer tout ce qu'il faut pour l'output des données.
		public override void Initialize()
		{
			// On lit les paramêtres nécéssaires pour l'output des données
			// Timestep = parameters.Timestep;
			// mapNameTemplate = parameters.MapNamesTemplate;
			// dynamicEcos = parameters.DynamicFireRegions;
			// string logFileName = parameters.LogFileName;
			// string summaryLogFileName = parameters.SummaryLogFileName;

			// summaryFireRegionEventCount = new int[FireRegions.Dataset.Count];
			// On initialize la table de dommages dans l'object qui gère en détail l'évenement de feu
			// Event.Initialize(parameters.FireDamages);

			// On affiche la ou vont s'enregistrer les fichiers de logs (logs des feux, et résumé des feux)
			// modelCore.UI.WriteLine("   Opening and Initializing Fire log files \"{0}\" and \"{1}\"...", parameters.LogFileName, parameters.SummaryLogFileName);

			// On initialize les colonnes des logs : un pour chaque région de feu
			List<string> colnames = new List<string>();
			// foreach (IFireRegion fireregion in FireRegions.Dataset)
			// {
			// colnames.Add(fireregion.Name);
			// }
			// On enregistre ces colomnes dans l'object "ExtensionMetadata" qui sert pour l'output
			// ExtensionMetadata.ColumnNames = colnames;
			// On initialize toute l'écriture dans les fichiers d'output 
			// MetadataHandler.InitializeMetadata(Timestep, mapNameTemplate, logFileName, summaryLogFileName);


			//if (isDebugEnabled)
			// modelCore.UI.WriteLine("Initialization done");
		}

		public override void Run()
		{
			throw new NotImplementedException();
		}
	}

}
