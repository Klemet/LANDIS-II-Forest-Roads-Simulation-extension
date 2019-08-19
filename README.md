# A LANDIS-II extension to simulate forest roads in a landscape

CAUTION : This is a work in progress, and some functions might be currently missing or not optimized.

This extension is made for use with the LANDIS-II landscape model, available on http://www.landis-ii.org/ .
It functions in pair with a harvest extension to simulate how the forest road network of the landscape dynamically changes with wood harvesting.

## Features
- Detection of an installed harvest extension (without it, the road network will not change) [Completed]
- Reading of a raster containing the initial road network [Completed]
- Completion of the initial road network to avoid isolated roads that lead to nowhere [Completed]
- At each timestep, the extension gets all of the recently harvest sites, and construct a road to them unless they are close enough for to an existing road for skipping wood to them [Completed]
- The building of new roads is made by an algorithm that finds the least-cost path according to elevation, lakes/rivers, vegetation, soils and distance
- Each road is assigned a type (primary, secondary, tertiary, winter roads...) according to the flux of wood that flows throught them, and the possibilities of using temporary roads
- The cost of construction or upgrading of the roads is saved in a log
- An output raster is created at each timestep to see the evolution of the road network in the landscape [Completed]

## Screenshots

![Evolution of the forest road network throught the simulation](screenshots/EvolutionOfNetwork.png)
 
## Download
 
A pre-release can be downloaded [here](https://github.com/Klemet/LANDIS-II-Forest-Roads-Extension/releases/download/0.5/LANDIS-II-v7.Forest.Road.Simulation.Extension.v.0.5.Setup.exe ). Please be carefull to indicate the correct folder where LANDIS-II-v7 is installed during setup. 

## Use

To currently use the extension, you must have a parameter files fed to the extension via the main .txt file of the LANDIS-II scenario.
An example of parameter file is available in the "Example files" folder.

You also have to feed the extension a raster containing the initial road network; an example of raster can also be found in the "Example files" folder.
 
## Author

Clément Hardy

PhD Student at the Université du Québec à Montréal

Mail : clem.hardy@outlook.fr

## Acknowledgments

This work would not be possible without the incredible project that is LANDIS-II, and without the care and passion that the LANDIS-II fondation had to make the project as participative and accessible as it is. I thank them all tremendously.