# Input Manager

**WARNING!**: This plugin is not stable yet!!

This plugin for Unity3D is the wrapper for Unity's New Input Manager.  
With this plugin, you can:

* Handle key presses as your custom `enums`
* Handle key presses in a 'frame-unlocked' manner
* "Hold frame count" with small effect from framerate fluctuation
* Rebinding the assigned key (i.e., key config.)
    * Output the custom bindings as serializable dictionary format
    * "Duplicate keys" detection; if rebind causes one key bound to two actions, the plugin will try to swap the binds between them instead

## Installation

### Prerequisits
**Using New Input System is a MUST!**  
If your game currently use the legacy input system, installing this plugin **will also pull `"com.unity.inputsystem": "1.4.4"` as its dependency** and you will have to perform extra steps.  
Also, you must acknowledge that the classes from the legacy input system (e.g., the `Input` class) will be unavailable.
Depending on how you were handling the user input, you may need to modify a large chunk of your code.

### Add the package
Write the following in your manifest.json:

```json
{
    "dependencies": {
      "com.clpsplug.input-manager": "https://github.com/clpsplug/inputmanager.git?path=Packages/com.clpsplug.input-manager/#base"
    }
}
```

### Disable legacy `Input`-related classes

`com.unity.inputsystem` and the legacy `Input`-related classes cannot coexist, and Unity will prompt you to disable it.

1. Restart your Unity Editor as told. This is required to disable the legacy system.
2. After that, **for all the scenes where you have EventSystem,** replace Standalone Input Module with the new Input System UI Input Module.
* The Editor provides you with the easy migration button, but you will need to do it for each Standalone Input Module component you have in your project.


## Run the sample

Clone the project

```bash
git clone https://github.com/clpsplug/inputmanager.git
```

Play the "Scenes/SampleScene.unity".


## Documentation

It is under construction, but it will be available at [Wiki](https://github.com/Clpsplug/InputManager/wiki)!

## License

[MIT](https://choosealicense.com/licenses/mit/)

