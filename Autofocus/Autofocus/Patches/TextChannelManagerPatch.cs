using HarmonyLib;

namespace Autofocus.Patches;

[HarmonyPatch(typeof(TextChannelManager))]
internal static class TextChannelManagerPatch
{
    [HarmonyPatch("OnEnterPressed")]
    [HarmonyPrefix]
    private static void OnEnterPressedPrefix()
    {
        var plugin = AutofocusPlugin.Instance;
        if (plugin == null)
        {
            return;
        }

        var input = MonoSingleton<UIManager>.I.MessageInput.text;
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/'))
        {
            return;
        }

        if (plugin.TryHandleCommand(input))
        {
            GameChat.HideHandledCommand();
        }
    }
}
