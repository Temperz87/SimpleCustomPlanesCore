using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;

[HarmonyPatch(typeof(CampaignSelectorUI), nameof(CampaignSelectorUI.OpenCampaignSelector))]
public static class Inject_Scenarios
{
    public static bool Prefix()
    {
        foreach (PlaneInformation info in PlaneInformation.planes)
            info.SwapVehicleName(true);
        return true;
    }
}
