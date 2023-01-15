# Input Manager

**WARNING!**: This plugin is not stable yet!!

This plugin for Unity3D is the wrapper for using Unity's New Input Manager
programatically (i.e., without using related components.)  
With this plugin, you can:

* Handle key presses as your custom `enums`
* Handle key presses in a 'frame-unlocked' manner
* "Hold frame count" with small effect from framerate fluctuation
* Rebinding the assigned key (i.e., key config.)
    * Output the custom bindings as serializable dictionary format
    * "Duplicate keys" detection; if rebind causes one key bound to two actions, the plugin will try to swap the binds between them instead

# How to use?

Please see the [README inside the package](Packages/com.clpsplug.input-manager/README.md).

# Acknowledgements

The sample project includes a TextMeshPro-rendered version of [M+ Font](https://github.com/coz-m/MPLUS_FONTS),
which is avaiable under [SIL Open Font License 1.1](https://github.com/coz-m/MPLUS_FONTS/blob/master/OFL.txt).
