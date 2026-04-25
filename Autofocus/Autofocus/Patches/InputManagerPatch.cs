using HarmonyLib;

namespace Autofocus.Patches;

[HarmonyPatch(typeof(InputManager))]
internal static class InputManagerPatch
{
    [HarmonyPatch("get_IsSpaceKeyDown")]
    [HarmonyPostfix]
    private static void GetIsSpaceKeyDownPostfix(ref bool __result)
    {
        if (!__result && FocusControllerService.ShouldForceSpaceDown())
        {
            __result = true;
        }
    }
}
