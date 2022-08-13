using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;

[HarmonyPatch(typeof(CampaignInfoUI), nameof(CampaignInfoUI.UpdateDisplay))]
public static class Ensure_PlayerVehiclesNamedRight
{
    public static bool Prefix()
    {
        foreach (PlaneInformation info in PlaneInformation.planes)
            info.SwapVehicleName(false);
        return true;
    }
}