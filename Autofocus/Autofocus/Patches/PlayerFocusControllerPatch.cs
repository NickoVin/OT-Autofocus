using HarmonyLib;

namespace Autofocus.Patches;

[HarmonyPatch(typeof(PlayerFocusController))]
internal static class PlayerFocusControllerPatch
{
    [HarmonyPatch("SetFocus")]
    [HarmonyPrefix]
    private static void SetFocusPrefix(int subIndex)
    {
        FocusControllerService.RememberFocusSubIndex(subIndex);
    }
}
