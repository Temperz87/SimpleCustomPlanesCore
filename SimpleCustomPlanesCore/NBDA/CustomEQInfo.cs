using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VTNetworking;

public class CustomEqInfo
{
    public GameObject weaponObject;
    public PlaneInformation pInfo;
    public HPEquippable equip;
    public bool isExclude;
    public bool isWMD;

    public CustomEqInfo(GameObject weaponObject, PlaneInformation pInfo, bool exclude, bool WMD, string equips = null)
    {
        this.weaponObject = weaponObject;
        equip = weaponObject.GetComponent<HPEquippable>();
        this.pInfo = pInfo;
        this.isExclude = exclude;
        this.isWMD = WMD;
        //this.mpReady = mpReady;
        if (equips != null)
            weaponObject.GetComponent<HPEquippable>().allowedHardpoints = equips;
        string name = weaponObject.name;
        if (name.Contains("(Clone)"))
            name = weaponObject.name.Substring(0, weaponObject.name.IndexOf("(Clone)"));
        VTNetworkManager.RegisterOverrideResource("SimpleCustomPlanesCore/" + name, weaponObject);
        if (equip is HPEquipMissileLauncher)
        {
            try
            {
                HPEquipMissileLauncher launcher = equip as HPEquipMissileLauncher;
                if (!launcher.missileResourcePath.Contains("SimpleCustomPlanesCore/"))
                    launcher.missileResourcePath = "SimpleCustomPlanesCore/" + name + "Missile";
                VTNetworkManager.RegisterOverrideResource(launcher.missileResourcePath, launcher.ml.missilePrefab);
            }
            catch (NullReferenceException e)
            {
                Debug.Log("Caught NRE while trying to add missile launcher path to override resources, of launcher " + name + ".");
            }
        }
    }
    public CustomEqInfo(GameObject weaponObject) : this(weaponObject, null, false, true, null) { }

    public bool CompareTo(PlaneInformation vehicle)
    {
        return pInfo == null || pInfo.trueVehicleName == vehicle.trueVehicleName;
    }
}