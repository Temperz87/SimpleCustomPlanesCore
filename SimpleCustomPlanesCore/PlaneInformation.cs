using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using VTOLVR.Multiplayer;
using Valve.Newtonsoft.Json;
using System.Reflection;

public class PlaneInformation
{
    public static List<PlaneInformation> planes = new List<PlaneInformation>();


    public PlaneInformation(AssetBundle bundle, string scpPath)
    {
        Debug.Log("Try load plane from path " + scpPath);
        TextAsset manifest = bundle.LoadAsset("manifest.json") as TextAsset;
        if (manifest == null)
        {
            Debug.LogError("Couldn't find a manifest.json in an asset bundle.");
            return;
        }
        manifestDataModel info = JsonConvert.DeserializeObject<manifestDataModel>(manifest.text);

        if (info.dependencyName != null && File.Exists(scpPath + "/" + info.dependencyName + ".dll"))
        {
            Debug.Log("Trying to load dependency for " + info.playerVehicle + " at " + scpPath + "/" + info.dependencyName);
            byte[] dllBytes = File.ReadAllBytes(scpPath + "/" + info.dependencyName);
            Assembly.Load(dllBytes);
        }
        else
            Debug.Log("No depenency found for this plane at " + scpPath + "/" + ((info.dependencyName == null) ? info.dependencyName : ""));

        playerVehicle = bundle.LoadAsset(info.playerVehicle + ".asset") as PlayerVehicle;
        if (manifest == null)
        {
            Debug.LogError("Couldn't find a manifest.json in an asset bundle.");
            return;
        }

        baseVehicle = convertString(info.baseVehicle);
        //campaignsToUse = convertString(info.campaign);
        campaignsToUse = baseVehicle;
        configuratorToUse = convertString(info.configurator);
        equipsToUse = convertString(info.equips);

        playerVehicle.campaigns = VTResources.GetPlayerVehicle(convertString(campaignsToUse)).campaigns;
        playerVehicle.loadoutConfiguratorPrefab = VTResources.GetPlayerVehicle(convertString(configuratorToUse)).loadoutConfiguratorPrefab;
        playerVehicle.uiOnlyConfiguratorPrefab = VTResources.GetPlayerVehicle(convertString(configuratorToUse)).uiOnlyConfiguratorPrefab;

        if (playerVehicle.allEquipPrefabs != null && playerVehicle.allEquipPrefabs.Count > 0)
        {
            foreach (GameObject go in playerVehicle.allEquipPrefabs)
            {
                if (go == null)
                    continue;
                Debug.Log("Trying to load custom weapon " + go.name);
                try
                {
                    Armory.LoadGeneric(go, go.name, this, false, false);
                }
                catch (Exception e)
                {
                    Debug.LogError("Couldn't load custom weapon " + go.name + " stack trace: " + e.StackTrace);
                }
            }
        }

        playerVehicle.allEquipPrefabs = new List<GameObject>((GameObject[])VTResources.GetPlayerVehicle(convertString(campaignsToUse)).allEquipPrefabs.ToArray().Clone());
        if (baseVehicle != VTOLVehicles.None)
        {
            GameObject basePlane = VTResources.GetPlayerVehicle(convertString(baseVehicle)).vehiclePrefab;
            bool wasActive = basePlane.activeSelf;
            basePlane.SetActive(false);
            GameObject newF45 = GameObject.Instantiate(basePlane);
            basePlane.SetActive(wasActive);
            try
            {
                newF45.AddComponent<SimpleCustomPlane>().LoadOntoThisPlane(scpPath + "/" + playerVehicle.nickname + ".SCP", playerVehicle.vehiclePrefab);
                GameObject.DontDestroyOnLoad(newF45);
                playerVehicle.vehiclePrefab = newF45;
            }
            catch (Exception e)
            {
                Debug.LogError("Caught exception while trying to load a .SCP for custom plane " + playerVehicle.vehicleName + "; stack trace as follows.\n" + e.StackTrace);
                return;
            }
        }

        Traverse.Create<PilotSaveManager>().Method("EnsureVehicleCollections").GetValue();
        Dictionary<string, PlayerVehicle> vehicles = Traverse.Create(typeof(PilotSaveManager)).Field("vehicles").GetValue() as Dictionary<string, PlayerVehicle>;

        List<PlayerVehicle> vehicleList = Traverse.Create(typeof(PilotSaveManager)).Field("vehicleList").GetValue() as List<PlayerVehicle>;
        if (!vehicles.ContainsKey(playerVehicle.vehicleName))
        {
            vehicles.Add(playerVehicle.vehicleName, playerVehicle);
            Traverse.Create(typeof(PilotSaveManager)).Field("vehicles").SetValue(vehicles);
        }

        Traverse.Create(typeof(PilotSaveManager)).Field("vehicleList").SetValue(vehicleList);
        if (!vehicleList.Contains(playerVehicle))
        {
            vehicleList.Add(playerVehicle);
            Traverse.Create(typeof(PilotSaveManager)).Field("vehicleList").SetValue(vehicleList);
        }

        trueVehicleName = playerVehicle.vehicleName;
        planes.Add(this);
    }


    public static bool CheckCustomVehicleName(string name)
    {
        foreach (PlaneInformation info in planes)
            if (info.trueVehicleName == name)
                return true;
        return false;
    }

    public void SwapVehicleName(bool toFalseName)
    {
        if (toFalseName)
            playerVehicle.vehicleName = convertString(campaignsToUse);
        else
            playerVehicle.vehicleName = trueVehicleName;
    }

    public static VTOLVehicles convertString(string convert)
    {
        switch (convert.Trim())
        {
            case "AV42C":
                return VTOLVehicles.AV42C;
            case "FA26B":
                return VTOLVehicles.FA26B;
            case "F45A":
                return VTOLVehicles.F45A;
            case "AH94":
                return VTOLVehicles.AH94;
        }
        return VTOLVehicles.None;
    }
    public static string convertString(VTOLVehicles convert)
    {
        switch (convert)
        {
            case VTOLVehicles.AV42C:
                return "AV-42C";
            case VTOLVehicles.FA26B:
                return "F/A-26B";
            case VTOLVehicles.F45A:
                return "F-45A";
            case VTOLVehicles.AH94:
                return "AH-94";
        }
        return "None";
    }

    public PlayerVehicle playerVehicle;
    public string trueVehicleName;
    public VTOLVehicles baseVehicle;
    public VTOLVehicles campaignsToUse;
    public VTOLVehicles configuratorToUse;
    public VTOLVehicles equipsToUse;

    class manifestDataModel
    {
        public string playerVehicle;
        public string baseVehicle;
        public string campaign;
        public string configurator;
        public string equips;
        public string dependencyName;
    }
}
