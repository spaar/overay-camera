using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using spaar.ModLoader;
using UnityEngine;

namespace spaar.Mods.CameraOverlay.Patches
{
  public class FixedCameraBlockPatches
  {
    static bool IsOverlayCam(FixedCameraBlock cam)
    {
      return cam.Toggles.Find(t => t.Key == "overlay").IsActive;
    }

    static class CameraHolder
    {
      public static Dictionary<FixedCameraBlock, Camera> AllCameras = new Dictionary<FixedCameraBlock, Camera>();
      public static Dictionary<FixedCameraBlock, bool> ActivationStatus = new Dictionary<FixedCameraBlock, bool>();

      private static int _counter = 0;
      public static int Counter
      {
        get { return _counter++; }
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "Awake")]
    class Awake
    {
      static void Postfix(FixedCameraBlock __instance)
      {
        var toggle = __instance.CallPrivateMethod<MToggle>("AddToggle", new Type[]
        {
          typeof(string), typeof(string), typeof(bool)
        }, new object[]
        {
          "Overlay", "overlay", false
        });
        var xSlider = __instance.CallPrivateMethod<MSlider>("AddSlider", new Type[]
        {
          typeof(string), typeof(string), typeof(float), typeof(float), typeof(float)
        }, new object[]
        {
          "X Position", "overlay-x", 0.8f, 0.0f, 1.0f
        });
        var ySlider = __instance.CallPrivateMethod<MSlider>("AddSlider", new Type[]
        {
          typeof(string), typeof(string), typeof(float), typeof(float), typeof(float)
        }, new object[]
        {
          "Y Position", "overlay-y", 0.8f, 0.0f, 1.0f
        });
        var widthSlider = __instance.CallPrivateMethod<MSlider>("AddSlider", new Type[]
        {
          typeof(string), typeof(string), typeof(float), typeof(float), typeof(float)
        }, new object[]
        {
          "Width", "overlay-width", 0.2f, 0.0f, 1.0f
        });
        var heightSlider = __instance.CallPrivateMethod<MSlider>("AddSlider", new Type[]
        {
          typeof(string), typeof(string), typeof(float), typeof(float), typeof(float)
        }, new object[]
        {
          "Height", "overlay-height", 0.2f, 0.0f, 1.0f
        });

        xSlider.DisplayInMapper = false;
        ySlider.DisplayInMapper = false;
        widthSlider.DisplayInMapper = false;
        heightSlider.DisplayInMapper = false;

        toggle.Toggled += active =>
        {
          xSlider.DisplayInMapper = active;
          ySlider.DisplayInMapper = active;
          widthSlider.DisplayInMapper = active;
          heightSlider.DisplayInMapper = active;
        };

        if (!CameraHolder.AllCameras.ContainsKey(__instance) && StatMaster.isSimulating)
        {
          var camGO = new GameObject("Camera " + CameraHolder.Counter);
          var cam = camGO.AddComponent<Camera>();
          cam.enabled = false;
          cam.depth = 1;
          cam.rect = new Rect(0.8f, 0.8f, 0.2f, 0.2f);

          var oCam = MouseOrbit.Instance.cam;
          cam.nearClipPlane = oCam.nearClipPlane;
          cam.farClipPlane = oCam.farClipPlane;
          cam.renderingPath = oCam.renderingPath;
          cam.cullingMask = oCam.cullingMask;
          cam.useOcclusionCulling = oCam.useOcclusionCulling;
          cam.fieldOfView = oCam.fieldOfView;

          CameraHolder.AllCameras.Add(__instance, cam);

          if (__instance.GetPrivateField<bool>("simulationClone"))
            CameraHolder.ActivationStatus.Add(__instance, false);
        }
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "Start")]
    class Start
    {
      static void Postfix(FixedCameraBlock __instance)
      {
        if (IsOverlayCam(__instance))
          __instance.SetPrivateField("cameraTransform", CameraHolder.AllCameras[__instance].transform);
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "OnLoad", new Type[] { typeof(XDataHolder) })]
    class OnLoad
    {
      static void Postfix(FixedCameraBlock __instance)
      {
        if (IsOverlayCam(__instance))
        {
          var cam = CameraHolder.AllCameras[__instance];
          var x = __instance.Sliders.Find(s => s.Key == "overlay-x").Value;
          var y = __instance.Sliders.Find(s => s.Key == "overlay-y").Value;
          var width = __instance.Sliders.Find(s => s.Key == "overlay-width").Value;
          var height = __instance.Sliders.Find(s => s.Key == "overlay-height").Value;
          cam.rect = new Rect(x, y, width, height);
        }
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "Update")]
    class Update
    {
      static bool Prefix(FixedCameraBlock __instance)
      {
        // Don't modify behaviour if we aren't simulating
        if (!StatMaster.isSimulating)
          return true;

        // Don't modify behaviour if the overlay toggle is off
        if (!IsOverlayCam(__instance))
          return true;


        // Activate new Camera object
        var cam = CameraHolder.AllCameras[__instance];
        cam.enabled = __instance.isActive;

        // Don't do all the normal stuff
        return false;
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "Simulation")]
    class Simulation
    {
      static void Postfix(FixedCameraBlock __instance)
      {
        if (__instance.GetPrivateField<MKey>("activateKey").IsPressed)
        {
          if (IsOverlayCam(__instance))
          {
            // If an overlay cam is activated or deactivated, make sure it's parented correctly and prevent other cameras being affected
            var previouslyActive = FixedCameraController.Instance.cameras.Find(cam => cam.isActive && !IsOverlayCam(cam));
            CameraHolder.ActivationStatus[__instance] = !CameraHolder.ActivationStatus[__instance];

            __instance.StartCoroutine(FixPositionAndFov(__instance, MouseOrbit.Instance.cam.fieldOfView,
              previouslyActive));
          }
          else
          {
            // All cameras will be deactivated by FixedCameraControllers, restore overlay cams
            __instance.StartCoroutine(RestoreOverlayCams());
          }
        }
      }

      static IEnumerator FixPositionAndFov(FixedCameraBlock instance, float originalFov, FixedCameraBlock previouslyActiveCamera)
      {
        yield return null;

        FixedCameraController.Instance.SetPrivateField("isDirty", true);
        FixedCameraController.Instance.SetPrivateField("lastKey", instance.KeyCode);
        CameraHolder.AllCameras[instance].transform.parent =
          Game.MachineObjectTracker.ActiveMachine.SimulationMachine.GetChild(0);
        yield return null;
        if (previouslyActiveCamera == null)
        {
          MouseOrbit.Instance.isActive = true;
          MouseOrbit.Instance.cam.fieldOfView = originalFov;
        }
        else
        {
          FixedCameraController.Instance.SetPrivateField("isDirty", true);
          FixedCameraController.Instance.SetPrivateField("lastKey", previouslyActiveCamera.KeyCode);
          yield return null;
        }
        instance.StartCoroutine(RestoreOverlayCams());
      }

      static IEnumerator RestoreOverlayCams()
      {
        yield return null;
        yield return null;
        foreach (var cam in FixedCameraController.Instance.cameras.Where(IsOverlayCam))
        {
          cam.isActive = CameraHolder.ActivationStatus[cam];
        }
      }
    }

    [HarmonyPatch(typeof(FixedCameraBlock), "OnDestroy")]
    class OnDestroy
    {
      static void Prefix(FixedCameraBlock __instance)
      {
        if (__instance.GetPrivateField<bool>("simulationClone"))
        {
          GameObject.Destroy(CameraHolder.AllCameras[__instance].gameObject);
        }
      }
    }
  }

}