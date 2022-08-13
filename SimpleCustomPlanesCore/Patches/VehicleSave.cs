using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(VehicleSave), nameof(VehicleSave.GetCampaignSave))]
public static class Ensure_VehicleHasSave
{
    public static void Postfix(VehicleSave __instance, string campaignID, ref CampaignSave __result)
    {
        if (__result == null && PlaneInformation.CheckCustomVehicleName(__instance.vehicleName))
        {
            PlaneInformation info = PlaneInformation.GetCustomVehicle(__instance.vehicleName);
            Main.Log("Adding campaign to vehicle save " + campaignID);
            VTCampaignInfo cInfo = VTResources.GetBuiltInCampaign(campaignID);
            if (cInfo == null)
            {

                cInfo = VTResources.GetCustomCampaign(campaignID);
                if (cInfo == null)
                {
                    Debug.LogError("No campaign found for id " + campaignID);
                    return;
                }
            }
            Campaign campaign = cInfo.ToIngameCampaign();
            CampaignSave campaignSave = new CampaignSave();
            campaignSave.campaignName = campaign.campaignName;
            campaignSave.campaignID = campaign.campaignID;
            campaignSave.vehicleName = info.trueVehicleName;
            campaignSave.completedScenarios = new List<CampaignSave.CompletedScenarioInfo>();
            campaignSave.availableScenarios = new List<string>();
            campaignSave.currentFuel = 1f;
            campaignSave.currentWeapons = new string[info.playerVehicle.hardpointCount];
            campaignSave.availableWeapons = new List<string>();
            foreach (string item in campaign.weaponsOnStart)
                campaignSave.availableWeapons.Add(item);
            foreach (string item2 in campaign.scenariosOnStart)
                campaignSave.availableScenarios.Add(item2);
            __instance.campaignSaves.Add(campaignSave);
            __result = campaignSave;
        }
    }
}