# The FRS (Forest Roads Simulation) extension for LANDIS-II

***A LANDIS-II extension to simulate the dynamics of forest roads in a landscape***

![](../screenshots/PythonAnimation_Road_network_output.gif)

This extension is made for use with the LANDIS-II landscape model, available on http://www.landis-ii.org/ .
It functions in pair with a harvest extension to simulate how the forest road network of the landscape dynamically changes with wood harvesting.

## Features

- [x] Detection of an installed harvest extension (without it, the road network will not change).
- [x] Reading of a raster containing the initial road network.
- [x] Completion of the initial road network to avoid isolated roads that lead to nowhere.
- [x] At each time step, the extension gets all of the recently harvest sites, and construct a road to them unless they are close enough for to an existing road for skipping wood to them.
- [x] The building of new roads is made by an algorithm that finds the least-cost path according to elevation, lakes/rivers, vegetation, soils and distance.
- [x] Each road is assigned a type (primary, secondary, tertiary, winter roads...) according to the flux of wood that flows through them.
- [x] The cost of construction or upgrading of the roads is saved in a log.
- [x] Roads can have a lifetime, and have to be maintained with money to keep being used. 
- [x] An output raster is created at each time step to see the evolution of the road network in the landscape.
- [x] Loops are created in the network according to simple rules, in order to increase the realism of the fragmentation of the landscape.
- [x] If a repeated prescription is used where the site harvested recently will be visited again (e.g. shelterwood or uneven-aged management), the module will choose the cheapest option between constructing a long-lasting road or a cheaper road that will have to be re-constructed when accessing the site at the next harvest rotation.
- [ ] **[Optional]** Roads can be abandoned or destroyed according to several criteria. (*Still to come*)

## Screenshots

![Evolution of the forest road network throught the simulation](screenshots/EvolutionOfNetwork.png)

| Road network                                             | Wood Flux                                             |
|:--------------------------------------------------------:|:-----------------------------------------------------:|
| ![](../screenshots/PythonAnimation_Road_network_output.gif) | ![](../../screenshots/PythonAnimation_Wood_Flux_output.gif) |

## Download

Version 1.0 can be downloaded [here](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/releases/tag/1.0).

## Use

Before use, we recommend reading the user guide available for download [here](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/raw/master/LANDIS-II%20Forest%20Roads%20Simulation%20v1.0%20User%20Guide.pdf). 

To currently use the extension, you must have a parameter files fed to the extension via the main .txt file of the LANDIS-II scenario. Examples for each of these files are available in the "Example" folder.

## Author

Clément Hardy

PhD Student at the Université du Québec à Montréal

Mail : clem.hardy@outlook.fr

## Acknowledgments

This work would not be possible without the incredible project that is LANDIS-II, and without the care and passion that the LANDIS-II fondation had to make the project as participative and accessible as it is. I thank them all tremendously.

I would also like to thank Github users [BlueRaja](https://github.com/BlueRaja) and [eregina92](https://github.com/eregina92/) for their respective packages, [High Speed Priority Queue for C#](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp) and [Supercluster.KDTree](https://github.com/eregina92/Supercluster.KDTree). Both packages were of tremendous use to improve the performance of the FRS module, and I highly recommend you to check out their work.
