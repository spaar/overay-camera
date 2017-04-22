using System;
using System.Reflection;
using Harmony;
using spaar.ModLoader;
using UnityEngine;

namespace spaar.Mods.CameraOverlay
{
  public class CameraOverlayMod : ModLoader.Mod
  {
    public override string Name { get; } = "camera-overlay";
    public override string DisplayName { get; } = "Camera Overlay";
    public override string Author { get; } = "spaar";
    public override Version Version { get; } = new Version(1, 0, 0);

    public override void OnLoad()
    {
      // Your initialization code here
      var harmony = HarmonyInstance.Create("spaar.Mods.CameraOverlay");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void OnUnload()
    {
      // Your code here
      // e.g. save configuration, destroy your objects if CanBeUnloaded is true etc
    }
  }
}
