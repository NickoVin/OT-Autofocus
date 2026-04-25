using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace Autofocus;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class AutofocusPlugin : BaseUnityPlugin
{
    private const string ModGuid = "com.nickovin.ontogether.autofocus";
    private const string ModName = "Autofocus";
    private const string ModVersion = "1.0.0";

    internal static AutofocusPlugin? Instance { get; private set; }
    
    private ConfigEntry<bool>? _enableMod;
    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;

        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
        
        _enableMod = Config.Bind(
            "General",
            "EnableMod",
            true,
            "Enable or disable the Autofocus mod."
        );

        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();

        Logger.LogInfo($"{ModName} v{ModVersion} loaded.");
        Logger.LogInfo("Type /afhelp in the in-game chat to test the mod.");
    }

    private void OnDestroy()
    {
        FocusControllerService.StopLoop();
        _harmony?.UnpatchSelf();
        Instance = null;
    }

    internal Coroutine RunManagedCoroutine(IEnumerator routine) => StartCoroutine(routine);

    internal void StopManagedCoroutine(Coroutine routine) => StopCoroutine(routine);

    internal bool TryHandleCommand(string input)
    {
        if (_enableMod?.Value != true)
        {
            return false;
        }

        var command = input.Trim().ToLowerInvariant();
        switch (command)
        {
            case "/afhelp":
            case "/autofocus":
                GameChat.ShowNotification("Autofocus loaded. Commands: /afhelp, /afstart, /afstop");
                return true;

            case "/afstart":
                if (FocusControllerService.TryToggleOrRestartFocus(Logger, out var focusMessage))
                {
                    GameChat.ShowNotification(focusMessage);
                    return true;
                }

                Logger.LogWarning(focusMessage);
                GameChat.ShowNotification(focusMessage);
                return true;

            case "/afstop":
                if (FocusControllerService.StopLoop())
                {
                    GameChat.ShowNotification("Autofocus loop stopped.");
                    return true;
                }

                GameChat.ShowNotification("Autofocus loop is not running.");
                return true;

            default:
                return false;
        }
    }
}
