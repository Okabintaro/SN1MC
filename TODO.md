Current Tasks/Todos I want to try tackle in the SN1MC Mod
=========================================================

## Fix UI Raycasts

Currently the raycasts feel weirdly offset and not precise enough compared to SteamVR or games like VRChat.
Ideally I want the PDA to feel like the VRChat Quick Menu.

The first step is to fix the offset of the current raycasting by doing the raycast properly from the controler.
Currently it seems that the UI and Headset camera is getting used for that which is not ideal.
But I need more practice to tackle that problem first.

### TODOs

- [O] [Study Unity Event System](https://docs.unity3d.com/2019.1/Documentation/ScriptReference/EventSystems.EventSystem.html)
    - There is one Event System
    - Multiple Input Modules attached to that, but there can only be one active
- [X] Implement the laserpointer in unity test scene
- [O] What input modules like FPSInput are there in subnautica, take a look using Debugger

## Add Gloves/Hands instead of body

The body of the player is not really made for VR, so instead of having the full body with weird arm IK I think it would be good to switch to bare hands like every other VR Game at the moment.
In the future it should probably be possible to use the characters body, but most likely there will be modifications needed to be done

- Work already started, see `AssetLoader.cs`

## UI Input using fingers/touch?

Even though laser pointers are nice when they work correctly, it would also be nice to be able to use the virtual fingers once we have them, to e.g. interact with the PDA.

- References
    - [UlitmateXR](https://www.ultimatexr.io) handles [UI Input](https://www.ultimatexr.io/guides/ui-interaction) using fingers and laserpointer
        - Source: https://github.com/VRMADA/ultimatexr-unity/
        - Permissive MIT License

## More Immersive HUDs

The UI and HUDs are not made for VR at all. IMHO having the UI infront of your face all the time ruins the immersion quite a bit.
Instead one could put the HUD on the left wrist like a watch or something.

Same goes for the vehicles. The seamoth has a couple of Displays in the cockpit which don't display anything.
Wouldn't it be cool if you could read the health, energy and more from there directly?
When the finger input is done, one could probably also interact with those displays to switch and activate equipment/upgrade slots.

## Late/Maybe Tasks

- Another Inventory or way to get the PDA(maybe like a backpack)
- Proper Full Body IK
    - Subnautica seems to use FinalIK already, so could use VRIK or something