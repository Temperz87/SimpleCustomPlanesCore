using Harmony;
using System.Collections.Generic;
using System.IO;

public static class CustomAircraftSaveManager
{
    private static bool hasLoaded = false;
    public static void LoadVSaves()
    {
        if (!File.Exists(PilotSaveManager.newSaveDataPath + "\\scpSaves.cfg"))
        {
            hasLoaded = true;
            return;
        }
        Main.Log("configNode");
        ConfigNode node = ConfigNode.LoadFromFile(PilotSaveManager.newSaveDataPath + "\\scpSaves.cfg");
        Main.Log("foreach");
        foreach (ConfigNode saveNode in node.GetNodes("SCPSAVE"))
        {
            Main.Log("name");
            string pilotName = saveNode.GetValue("pilotName");
            Main.Log("allsave");
            List<VehicleSave> allSaves = new List<VehicleSave>();
            Main.Log("save");
            if (!PilotSaveManager.pilots.ContainsKey(pilotName))
            {
                Main.Log(pilotName + " was not found.");
                continue;
            }
            PilotSave save = PilotSaveManager.pilots[pilotName];
            Main.Log("traverse");
            Traverse pilotTraverse = Traverse.Create(save);
            Main.Log("vsave");
            Dictionary<string, VehicleSave> vehicleSaves = Traverse.Create(save).Field("vehicleSaves").GetValue() as Dictionary<string, VehicleSave>;
            foreach (ConfigNode vehicleNode in node.GetNodes("VEHICLE"))
            {
                Main.Log("vsave again");
                VehicleSave vSave = VehicleSave.LoadFromConfigNode(vehicleNode);
                Main.Log("allvehicles");
                allSaves.Add(vSave);
                Main.Log("vehiclesaves.add");
                vehicleSaves.Add(vSave.vehicleName, vSave);
            }
            Main.Log("field");
            pilotTraverse.Field("vehicleSaves").SetValue(vehicleSaves);
        }
        hasLoaded = true;
    }

    public static void SaveVSaves()
    {
        if (PlaneInformation.planes.Count == 0 || !hasLoaded)
            return;
        ConfigNode node = new ConfigNode("SCPSAVES");
        foreach (PilotSave save in PilotSaveManager.pilots.Values)
        {
            ConfigNode scpSave = new ConfigNode("SCPSAVE");
            scpSave.SetValue("pilotName", save.pilotName);
            foreach (PlaneInformation info in PlaneInformation.planes)
            {
                if (PilotSaveManager.current != null)
                {
                    VehicleSave vSave = PilotSaveManager.current.GetVehicleSave(info.trueVehicleName);
                    if (vSave != null)
                    {
                        scpSave.AddNode(VehicleSave.SaveToConfigNode(vSave));
                    }
                }
            }
            node.AddNode(scpSave);
        }
        node.SaveToFile(PilotSaveManager.newSaveDataPath + "\\scpSaves.cfg");
    }
}
