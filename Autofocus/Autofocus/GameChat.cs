using UnityEngine;
using UnityEngine.EventSystems;

namespace Autofocus;

internal static class GameChat
{
    private const string Prefix = "<color=#b86cff>[AF]</color>";

    internal static void ShowNotification(string text)
    {
        var textChannelManager = NetworkSingleton<TextChannelManager>.I;
        textChannelManager?.AddNotification($"{Prefix} {text}");
    }

    internal static Vector3? TryGetMainPlayerPosition()
    {
        var textChannelManager = NetworkSingleton<TextChannelManager>.I;
        return textChannelManager?.MainPlayer?.position;
    }

    internal static void HideHandledCommand()
    {
        var musicManager = NetworkSingleton<MusicManager>.I;
        var lockState = musicManager != null && musicManager.IsActive ? LockState.Music : LockState.Free;

        MonoSingleton<TaskManager>.I.SetLockState(lockState);
        EventSystem.current?.SetSelectedGameObject(null);

        var input = MonoSingleton<UIManager>.I.MessageInput;
        input.text = string.Empty;
    }
}
