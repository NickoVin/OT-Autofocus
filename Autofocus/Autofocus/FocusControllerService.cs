using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace Autofocus;

internal static class FocusControllerService
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    
    private const float RestartDelaySeconds = 1f;
    private const float ExitTimeoutSeconds = 60f;
    private const float LoopIntervalSeconds = 120f;

    private static bool _forceSpaceExit;
    private static bool _awaitingProgrammaticExit;
    
    private static Coroutine? _loopCoroutine;
    private static int? _lastFocusSubIndex;

    internal static bool TryToggleOrRestartFocus(ManualLogSource logger, out string message)
    {
        var controller = NetworkSingleton<TextChannelManager>.I.MainFocusController;
        if (controller == null)
        {
            message = "Focus controller was not found yet.";
            return false;
        }

        if (controller.IsFocus)
        {
            if (_loopCoroutine != null)
            {
                message = "Autofocus loop is already running.";
                return true;
            }

            var plugin = AutofocusPlugin.Instance;
            if (plugin == null)
            {
                message = "Plugin instance is not available.";
                return false;
            }

            var mainIndex = GetCurrentMainIndex(controller);
            var subIndex = GetCurrentSubIndex();
            _loopCoroutine = plugin.RunManagedCoroutine(FocusLoopCoroutine(controller, logger, mainIndex, subIndex));
            message = $"Autofocus loop started for {controller.FocusType}.";
            return true;
        }

        message = "You must be in focus to run the command.";
        return false;
    }

    internal static bool StopLoop()
    {
        if (_loopCoroutine == null)
        {
            return false;
        }

        var plugin = AutofocusPlugin.Instance;
        if (plugin != null)
        {
            plugin.StopManagedCoroutine(_loopCoroutine);
        }

        _forceSpaceExit = false;
        _awaitingProgrammaticExit = false;
        _loopCoroutine = null;
        return true;
    }

    internal static bool ShouldForceSpaceDown()
    {
        var controller = NetworkSingleton<TextChannelManager>.I.MainFocusController;
        return _forceSpaceExit && controller != null && controller.IsFocus;
    }

    internal static void RememberFocusSubIndex(int subIndex)
    {
        if (subIndex < 0)
        {
            return;
        }

        _lastFocusSubIndex = subIndex;
    }

    private static int GetCurrentSubIndex()
    {
        return _lastFocusSubIndex ?? 0;
    }
    
    private static int GetCurrentMainIndex(PlayerFocusController controller)
    {
        var currentMainIndexField = typeof(PlayerFocusController).GetField("_currentFocusMainIndex", InstanceFlags);
        if (currentMainIndexField?.GetValue(controller) is int currentMainIndex and >= 0)
        {
            return currentMainIndex;
        }

        var currentFocusType = controller.FocusType;
        var focusTypes = (
                controller.CurrentFocusAreaController?.FocusAreaSettings?.FocusTypes
                ?? ScriptableSingleton<GameSettings>.I.FocusTypes
                ?? []
            )
            .Distinct().ToList();

        for (var index = 0; index < focusTypes.Count; index++)
        {
            if (Equals(focusTypes[index], currentFocusType))
                return index;
        }

        return 0;
    }

    private static IEnumerator FocusLoopCoroutine(
        PlayerFocusController controller,
        ManualLogSource logger,
        int mainIndex,
        int subIndex)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(LoopIntervalSeconds);

            if (_loopCoroutine == null)
            {
                break;
            }

            if (controller == null)
            {
                logger.LogWarning("Autofocus loop stopped because the focus controller disappeared.");
                GameChat.ShowNotification("Autofocus loop stopped: focus controller is gone.");
                break;
            }

            if (!controller.IsFocus)
            {
                if (!_awaitingProgrammaticExit)
                {
                    logger.LogInfo("Autofocus loop stopped because focus was ended manually.");
                    GameChat.ShowNotification("Autofocus loop stopped because you left focus.");
                    break;
                }

                if (!TryStartFocus(controller, logger, mainIndex, subIndex, out var startMessage))
                {
                    logger.LogWarning(startMessage);
                    GameChat.ShowNotification(startMessage);
                    break;
                }

                _awaitingProgrammaticExit = false;
                continue;
            }
            
            yield return RestartFocusCoroutine(controller, logger, mainIndex, subIndex);
        }

        _forceSpaceExit = false;
        _awaitingProgrammaticExit = false;
        _loopCoroutine = null;
    }
    
    private static bool TryStartFocus(
        PlayerFocusController controller,
        ManualLogSource logger,
        int mainIndex,
        int subIndex,
        out string message)
    {
        try
        {
            var openSelector = typeof(PlayerFocusController).GetMethod("OpenSelector", InstanceFlags);
            openSelector?.Invoke(controller, null);

            var buttonFocusItem = typeof(PlayerFocusController).GetMethod(
                "ButtonFocusItem",
                InstanceFlags,
                null,
                [typeof(int)],
                null
            );
            buttonFocusItem?.Invoke(controller, [mainIndex]);

            controller.SetFocus(subIndex);
            RememberFocusSubIndex(subIndex);

            message = "Focus mode was requested.";
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            message = $"Focus start failed: {ex.Message}";
            return false;
        }
    }

    private static IEnumerator RestartFocusCoroutine(
        PlayerFocusController controller,
        ManualLogSource logger,
        int mainIndex,
        int subIndex)
    {
        _awaitingProgrammaticExit = true;
        _forceSpaceExit = true;

        var elapsed = 0f;
        while (controller != null && controller.IsFocus && elapsed < ExitTimeoutSeconds)
        {
            if (_loopCoroutine == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _forceSpaceExit = false;
        _awaitingProgrammaticExit = false;

        if (controller == null || controller.IsFocus)
        {
            logger.LogWarning("Focus restart failed because the game did not exit focus in time.");
            GameChat.ShowNotification("Could not exit focus automatically. Chat input may be active.");
            yield break;
        }

        yield return new WaitForSecondsRealtime(RestartDelaySeconds);

        if (!TryStartFocus(controller, logger, mainIndex, subIndex, out var restartMessage))
        {
            logger.LogWarning(restartMessage);
            GameChat.ShowNotification(restartMessage);
            _loopCoroutine = null;
        }
    }
}
