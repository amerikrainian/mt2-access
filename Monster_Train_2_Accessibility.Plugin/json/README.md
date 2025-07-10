Use this folder to create json files specifying your mod.

The tutorial to get you up and running is located [here](https://github.com/Monster-Train-2-Modding-Group/Trainworks-Reloaded/wiki)

I find its best to organize the file structure like so.

json/
  * plugin.json - Top level json file. define your clan here, along with the banner icon, map node, card pools, subtypes, and champion specification
  * global.json - (Left as an example and the fact that AddMergedJson requires at least two json files. Currently this defines a Consume trait that can be easily reused across json files you only need to define it once. Also useful to put subtypes here as well.)
  * champions/ - Define the two champion cards data here, along with the champion upgrade paths
  * enhancers/ - Define the shop upgrades this clan gets
  * equipment/ - Equipment cards
  * rooms/ - Room cards
  * spells/ - Spell cards included in the clan
  * status_efffects/ - Custom status effect definitions if any
  * units/ - Unit cards included in the clan

Be sure when you add a json file to add it to your Plugin.cs AddMergedJsonFile line otherwise it won't be loaded!
If you add a folder make sure to include it to be copied in the csproj file. The csproj file will only copy json files in this directory and not ones found in subdirectories.

When you build your mod in the github codespace (or Visual Studio if running locally) simply copy the built files into the BepinEx/plugins directory.

