using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(PlayerSpawn), nameof(PlayerSpawn.OnPreSpawnUnit))]
public static class Ensure_SelectedVehicleChosen
{
    public static bool Prefix()
    {
        if (Main.selectedPlane != null)
        {
            Debug.Log("Changing player vehicle to custom vehicle " + Main.selectedPlane.trueVehicleName);
            VTScenario.current.vehicle = Main.selectedPlane.playerVehicle;
        }
        return true;
    }
}
