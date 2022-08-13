using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(PilotSelectUI), nameof(PilotSelectUI.StartSelectedPilotButton))]
public class Ensure_VehiclesLoaded
{
    public static bool Prefix()
    {
        if (LoadingTemplate.instance != null && LoadingTemplate.instance.hasVehicle)
        {
            LoadingTemplate.instance.Open();
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PilotSelectUI), nameof(PilotSelectUI.SetupVehicleScreen))]
public class SetupVehicleScreenPatch
{
    public static bool Prefix(ref PilotSelectUI __instance)
    {
        Debug.Log("SCP: Setting vehicle screen.");
        Traverse.Create(__instance).Field("vehicles").SetValue(PilotSaveManager.GetVehicleList());
        return true;
    }
}

[HarmonyPatch(typeof(PilotSelectUI), nameof(PilotSelectUI.SelectVehicle))]
public static class Ensure_VehicleSelected
{
    public static void Postfix(PlayerVehicle vehicle)
    {
        Debug.Log("SCP: Setting custom vehicle " + vehicle.nickname);
        Main.selectedPlane = PlaneInformation.GetCustomVehicleFromNickName(vehicle.nickname);
    }
}
