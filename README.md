# ActionLanguage
Action Language as used by EDD

It includes the following projects:

* ActionLanguage - Project https://github.com/EDDiscovery/BaseUtilities / BaseUtils + Audio, Project https://github.com/EDDiscovery/ExtendedControls / ExtendedControls + ExtendedForms

The vsproj has had its ProjectReference for Baseutilities and ExtendedControls manually changed to use $(SolutionDir) as the base folder to find them. So it expects them to be checked out at c:\code\parent\<submodule>.

Note when you include this module in a parent module, you should not expand the baseutilities or extendedcontrols submodule.  Instead include it in the parent module itself.

