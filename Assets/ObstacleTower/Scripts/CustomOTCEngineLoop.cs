using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;


/// <summary>
/// Obstacle Tower uses a custom engine loop to speed up the simulation during training.
/// Removed subsystems are looked up by fully-qualified type name so that the code
/// compiles even when a subsystem no longer exists in the current Unity version
/// (e.g. UNet systems were removed in Unity 6, ARCore / Kinect removed earlier).
/// </summary>
public class CustomOTCEngineLoop : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void RuntimeStart()
    {
        var defaultPlayerLoop = PlayerLoop.GetDefaultPlayerLoop();

        // Assumptions: project does not use:
        // XR, Analytics, WebRequest, Kinect, TangoUpdate, iOS, TextureStreaming, Audio, Physics2D, Wind,
        // Video, Pathfinding, runtime Substance, Enlighten, VFX, PhysicsCloth,
        // ParticleSystems, ScreenCapture, UNet networking.
        // Systems that no longer exist in the current Unity version are silently skipped.

        // Initialization
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.Initialization+XREarlyUpdate");

        // EarlyUpdate
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+AnalyticsCoreStatsUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+UnityWebRequestUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+XRUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+ProcessRemoteInput");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+ARCoreUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+UpdateKinect");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+DeliverIosPlatformEvents");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+SpriteAtlasManagerUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+UpdateStreamingManager");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.EarlyUpdate+UpdateTextureStreamingManager");

        // FixedUpdate
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.FixedUpdate+AudioFixedUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.FixedUpdate+XRFixedUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.FixedUpdate+Physics2DFixedUpdate");

        // PreUpdate
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreUpdate+Physics2DUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreUpdate+AIUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreUpdate+WindUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreUpdate+UpdateVideo");

        // PreLateUpdate — UNet types removed in Unity 6
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreLateUpdate+AIUpdatePostScript");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreLateUpdate+UNetUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreLateUpdate+UpdateMasterServerInterface");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreLateUpdate+UpdateNetworkManager");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PreLateUpdate+ParticleSystemBeginUpdateAll");

        // PostLateUpdate
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+UpdateAudio");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+UpdateVideo");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+UpdateSubstance");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+UpdateVideoTextures");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+EnlightenRuntimeUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+VFXUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+XRPostPresent");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+ProcessWebSendMessages");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+ExecuteGameCenterCallbacks");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+PhysicsSkinnedClothBeginUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+PhysicsSkinnedClothFinishUpdate");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+UpdateCaptureScreenshot");
        TryRemove(ref defaultPlayerLoop, "UnityEngine.PlayerLoop.PostLateUpdate+ParticleSystemEndUpdateAll");

        PlayerLoop.SetPlayerLoop(defaultPlayerLoop);

        Debug.Log("Setup of CustomOTCEngineLoop done.");
    }

    /// <summary>
    /// Removes the subsystem with the given fully-qualified type name from the player loop.
    /// Uses string matching so that the code compiles even when the type no longer exists.
    /// </summary>
    private static void TryRemove(ref PlayerLoopSystem system, string fullTypeName)
    {
        if (!RemoveByName(ref system, fullTypeName))
        {
            Debug.Log($"CustomOTCEngineLoop: subsystem '{fullTypeName}' not found in player loop (may have been removed in this Unity version) — skipping.");
        }
    }

    private static bool RemoveByName(ref PlayerLoopSystem system, string fullTypeName)
    {
        if (system.subSystemList == null)
            return false;

        for (int idx = 0; idx < system.subSystemList.Length; idx++)
        {
            var sub = system.subSystemList[idx];
            if (sub.type != null && sub.type.FullName == fullTypeName)
            {
                var list = new List<PlayerLoopSystem>(system.subSystemList);
                list.RemoveAt(idx);
                system.subSystemList = list.ToArray();
                return true;
            }

            if (RemoveByName(ref system.subSystemList[idx], fullTypeName))
                return true;
        }

        return false;
    }
}
