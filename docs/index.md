<p align="center">
  <img src="https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/docs/Logo%20Module%20FRS.png" />
</p>


# What is the FRS module ?

The FRS (Forest Roads Simulation) module is an extension for the [LANDIS-II](http://www.landis-ii.org/) model.

It allows any user to dynamically simulate the evolution of the forest road network in the simulated landscape of LANDIS-II. It does so by creating roads to cells that are harvested by a harvest module (such as [Base Harvest](http://www.landis-ii.org/extensions/base-harvest) or [Biomass Harvest](http://www.landis-ii.org/extensions/biomass-harvest)), while reducing the costs of construction of roads as much as possible.


# What does it look like ? (Screenshots)

Here are animations representing the evolution of a forest road network simulated by the FRS module:

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/screenshots/PythonAnimation_Road_network_output.gif)

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/screenshots/animationCartesGuillemette.gif)

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/screenshots/animationCartesClement.gif)


# What can it do ? (Features)

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/screenshots/EvolutionOfNetwork.png)

- [x] Build forest roads from recently harvested cells to exit points for the wood
- [x] Compute the path of new forest roads to minimize the costs of construction according to factors such as elevation, water and soils.
- [x] **[Optional]** Create loops in the network to make it more realistic
- [x] **[Optional]** Age the roads and destroy them with time
- [x] **[Optional]** Simulate the wood flux going through the roads, and upgrade their size to accomodate to this flux
- [x] **[Optional]** Take into account repeated cuts to optimize the choice of road types
- [ ] **[Optional]** **[Still to come]** Deactivation and reactivation of forest roads for conservation purposes
- [ ] **[Optional]** **[Still to come]** Estimation of CO2 emissions coming from the usage of the roads


# Is it fast ? (Performance)

The FRS module is pretty fast, from my own point of view. Indeed, creating thousands of roads in a landscape made of millions of cells took **less than a minute** for each time step with our module, when using an average CPU for the time (Intel i7 CPU with 4 cores working at 2.60GHz). Using the woodflux algorithm is also really quick, often flushing the wood half the time that was needed to create the roads for the time step.

This is due to the fact that I made sure to optimize the algorithms of the module so that they would take as little time to run as possible. To that end, I used two open-source C# Nuget packages (see Acknowledgments) that greatly improved the running time of the module.

Additional data from other users is needed to be sure; but currently, I would say that **you can expect to see no real difference between running your simulations with the FRS module or without**, in terms of performance and time of simulation. 


# What do I need to use it ? (Requirements)

To use the FRS module, you need:

- The [LANDIS-II model v7.0](http://www.landis-ii.org/install) installed on your computer.
- One of the succession extensions of LANDIS-II installed on your computer.
- One of the harvest extensions ([Base Harvest](http://www.landis-ii.org/extensions/base-harvest) or [Biomass Harvest](http://www.landis-ii.org/extensions/biomass-harvest)) installed on your computer.
- The FRS module installed on your computer (see Download section below).
- The parameter files for your scenario (see Parameterization section below).


# Where do I download it ? (Download)

Version 1.0 can be downloaded [here](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/releases/download/1.0/LANDIS-II-V7.Forest.Road.Simulation.module.1.0-setup.exe). To install it on your computer, just launch the installer.


# Where do I get the parameter files ? (Parameterization)

LANDIS-II requires a global parameter file for your scenario, and then different parameter files for each extension that you use.

To know how to generate the parameter files for the succession extension and the harvest extension that you will use, please refer to their user manual.

**To generate the parameter files needed for the FRS module, please read the [user guide of the module](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/master/LANDIS-II%20Forest%20Roads%20Simulation%20v1.0%20User%20Guide.pdf).** It will help you throught the process in detail !


# Can I test it ? Can I have an example of parameter files ?

Yes, and yes ! Just [download the example files](https://downgit.github.io/#/home?url=https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/tree/master/Examples), and you'll be set !

To launch the example scenario, you'll need the [Age-Only succession](http://www.landis-ii.org/extensions/age-only-succession) extension and the [Base Harvest](http://www.landis-ii.org/extensions/base-harvest) extension installed on your computer, in addition to the FRS module. Just launch the `test_scenario.bat` file, and the example scenario should run.


# What do I do if I have questions, or if I need help ? (Support)

If you have a question, please send me an e-mail at clem.hardy@outlook.fr. I'll do my best to answer you in time. 
You can also ask for help in the [LANDIS-II users group](http://www.landis-ii.org/users).

If you come across any issue or suspected bug when using the FRS module, please post about it in the [issue section of the Github repository of the module](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-module/issues).


# Author

[Clément Hardy](http://www.cef-cfr.ca/index.php?n=Membres.ClementHardy)

PhD Student at the Université du Québec à Montréal

Mail : clem.hardy@outlook.fr

Github : [https://github.com/Klemet](https://github.com/Klemet)


# Acknowledgments

This work would not be possible without the incredible project that is LANDIS-II, and without the care and passion that the LANDIS-II fondation have to make the project as participative and accessible as it is. I thank them all tremendously.

I would also like to thank Github users [BlueRaja](https://github.com/BlueRaja) and [eregina92](https://github.com/eregina92/) for their respective packages, [High Speed Priority Queue for C#](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp) and [Supercluster.KDTree](https://github.com/eregina92/Supercluster.KDTree). Both packages were of tremendous use to improve the performance of the FRS module, and I highly recommend you to check out their work.

I also thank users Art Shop and Jacqueline Fernandez from [the Noun Project](https://thenounproject.com/) for their ressources that helped me to create the logo for the module. The background picture for this website is from [Tom Parsons on Unsplash](https://unsplash.com/photos/F5qVefeCrp8).
