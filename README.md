<p align="center">
    <img alt="GitHub Repo stars" src="https://img.shields.io/github/stars/Klemet/LANDIS-II-Forest-Roads-Simulation-extension?style=social"> <img alt="CodeFactor Grade" src="https://img.shields.io/codefactor/grade/github/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master"> <img alt="GitHub Release Date" src="https://img.shields.io/github/release-date/Klemet/LANDIS-II-Forest-Roads-Simulation-extension"> <a href="https://zenodo.org/badge/latestdoi/200656337"><img src="https://zenodo.org/badge/200656337.svg" alt="DOI"></a>
</p>
<p align="center">
  <img src="https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master/webPageContent/assets/media/FRS_module_logo_v2.svg" />

</p>


> [!WARNING]  
> With the release of [version 2.0]([url](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/releases/tag/2.0)) of the FRS extension, which aims to make the extension compatible with LANDIS-II-v8, all further releases will not be compatible with the older LANDIS-II-v7 version of LANDIS-II. If you need to use the version v7 of LANDIS-II, you can use the latest compatible version which is [FRS extension v1.3.1]([url](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/releases/tag/1.3.1)).


# 📑 Description

The FRS (Forest Roads Simulation) extension is an extension for the [LANDIS-II](http://www.landis-ii.org/) model.

It allows any user to dynamically simulate the evolution of the forest road network in the simulated landscape of LANDIS-II. It does so by creating roads to cells that are harvested by a harvest extension (such as [Base Harvest](http://www.landis-ii.org/extensions/base-harvest) or [Biomass Harvest](http://www.landis-ii.org/extensions/biomass-harvest)), while reducing the costs of construction of roads as much as possible.


# 📸 Screenshots

Here are animations representing the evolution of a forest road network simulated by the FRS extension:

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master/screenshots/animation150Years.gif)

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master/screenshots/animationCartesGuillemette.gif)

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master/screenshots/animationCartesClement.gif)


# ✨ Features

![](https://raw.githubusercontent.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/master/screenshots/EvolutionOfNetwork.png)

- [x] Build forest roads from recently harvested cells to exit points for the wood
- [x] Compute the path of new forest roads to minimize the costs of construction according to factors such as elevation, water and soils.
- [x] **[Optional]** Create loops in the network to make it more realistic
- [x] **[Optional]** Age the roads and destroy them with time
- [x] **[Optional]** Simulate the wood flux going through the roads, and upgrade their size to accomodate to this flux
- [x] **[Optional]** Take into account repeated cuts to optimize the choice of road types
- [ ] **[Optional]** **[Still to come]** Deactivation and reactivation of forest roads for conservation purposes
- [ ] **[Optional]** **[Still to come]** Estimation of CO2 emissions coming from the usage of the roads


# ⏱ Performance

Creating thousands of roads in a landscape made of millions of cells takes **less than a minute** for each time step with our extension, when using an average CPU for the time (Intel i7 CPU with 4 cores working at 2.60GHz).

Using the woodflux algorithm usually takes half the time that was needed to create the roads for the time step.

This is due to optimizations using two open-source C# Nuget packages (see Acknowledgments) that greatly improved the running time of the extension.

# 🧱 Requirements

To use the FRS extension, you need:

- The [LANDIS-II model v8.0](http://www.landis-ii.org/install) installed on your computer.
- One of the succession extensions of LANDIS-II installed on your computer.
- One of the harvest extensions ([Base Harvest](http://www.landis-ii.org/extensions/base-harvest) or [Biomass Harvest](http://www.landis-ii.org/extensions/biomass-harvest)) installed on your computer.
- The FRS extension installed on your computer (see Download section below).
- The parameter files for your scenario (see Parameterization section below).


# 💾 Download

Version 2.0 can be downloaded [here](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/releases/download/2.0/LANDIS-II-V8.Forest.Road.Simulation.module.2.0.0-setup.exe). To install it on your computer, just launch the installer.


# 🛠 Parameterization and use

To learn how to parameterize and use the FRS extension, take a look at the [FRS extension workshop](https://klemet.github.io/frs-extension-workshop/).

**The workshop will give you files, scripts, figures and examples to understand and use the FRS extension for your research**. It will also give you tips to better interpret the output data of the extension.

To know how to generate the parameter files for the succession extension and the harvest extension that you will use, please refer to their user manual.


# 🎮 Example and testing

If you want to experiment with the extension or test it, you can [download the example files](https://downgit.github.io/#/home?url=https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/tree/master/Examples), or try the [workshop](https://klemet.github.io/frs-extension-workshop/).

To launch the example scenario, you'll need the [Age-Only succession](http://www.landis-ii.org/extensions/age-only-succession) extension and the [Base Harvest](http://www.landis-ii.org/extensions/base-harvest) extension installed on your computer, in addition to the FRS extension. Just launch the `test_scenario.bat` file, and the example scenario should run.


# ☎️ Support

If you have a question, please send me an e-mail at clem.hardy@outlook.fr. I'll do my best to answer you in time.

You can also ask for help in the [LANDIS-II users group](http://www.landis-ii.org/users).

If you come across any issue or suspected bug when using the FRS extension, please post about it in the [issue section of the Github repository of the module](https://github.com/Klemet/LANDIS-II-Forest-Roads-Simulation-extension/issues).


# ✒️ Author

[Clément Hardy](http://www.cef-cfr.ca/index.php?n=Membres.ClementHardy)

PhD Student at the Université du Québec à Montréal

Mail : clem.hardy@outlook.fr

Github : [https://github.com/Klemet](https://github.com/Klemet)


# 💚 Acknowledgments

This work would not be possible without the incredible project that is LANDIS-II, and without the care and passion that the LANDIS-II fondation have to make the project as participative and accessible as it is. I thank them all tremendously.

I would also like to thank Github users [BlueRaja](https://github.com/BlueRaja) and [eregina92](https://github.com/eregina92/) for their respective packages, [High Speed Priority Queue for C#](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp) and [Supercluster.KDTree](https://github.com/eregina92/Supercluster.KDTree). Both packages were of tremendous use to improve the performance of the FRS extension, and I highly recommend you to check out their work.

The background picture for the top of the webpage is from [Tom Parsons on Unsplash](https://unsplash.com/photos/F5qVefeCrp8).
