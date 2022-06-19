using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

[HarmonyPatch(typeof(LoadoutConfigurator), nameof(LoadoutConfigurator.AttachImmediate))]
public class EnsureCanAttachImmediate
{
    public static bool Prefix(LoadoutConfigurator __instance, int hpIdx)
    {
        return hpIdx < __instance.equips.Length;
    }
}

[HarmonyPatch(typeof(LoadoutConfigurator), nameof(LoadoutConfigurator.Initialize))]
public class Inject_WyvernLC
{
    [HarmonyPrefix]
    public static bool Prefix(LoadoutConfigurator __instance)
    {
        if (Main.selectedPlane != null && Main.selectedPlane.trueVehicleName == "X-02S")
        {
            __instance.OnAttachHPIdx += delegate (int hpIdx)
            {
                if (hpIdx == 11)
                {
                    __instance.Detach(1);
                    __instance.Detach(2);
                    __instance.Detach(3);
                    __instance.Detach(4);
                }
                else if (hpIdx >= 1 && hpIdx <= 4)
                {
                    __instance.Detach(11);
                }
            };
            Debug.Log("Starting config setup for wyvern");
            __instance.hpNodes[0].transform.localPosition = new Vector3(0, -81.2f, -2.4f);
            __instance.hpNodes[1].transform.localPosition = new Vector3(-105.2f, -121.9f, -2.4f);
            __instance.hpNodes[2].transform.localPosition = new Vector3(-105.2f, -166.1f, -2.4f);
            __instance.hpNodes[3].transform.localPosition = new Vector3(105.2f, -121.9f, -2.4f);
            __instance.hpNodes[4].transform.localPosition = new Vector3(105.2f, -166.1f, -2.4f);
            __instance.hpNodes[5].transform.localPosition = new Vector3(-110.6f, 49.5f, -2.4f);
            __instance.hpNodes[6].transform.localPosition = new Vector3(110.6f, 49.5f, -2.4f);
            __instance.hpNodes[7].transform.localPosition = new Vector3(-201.4f, 49.5f, -2.4f);
            __instance.hpNodes[8].transform.localPosition = new Vector3(201.4f, 49.5f, -2.4f);
            __instance.hpNodes[9].transform.localPosition = new Vector3(182.5f, -218.8f, -2.4f);
            __instance.hpNodes[10].transform.localPosition = new Vector3(-182.5f, -218.8f, -2.4f);
            if (__instance.hpNodes[11] != null)
            {
                __instance.hpNodes[11].transform.localPosition = new Vector3(0f, -218.8f, -2.4f);
                __instance.hpNodes[11].transform.Find("EXT FUEL").GetComponent<Text>().text = "RAILGUN";
            }

            Debug.Log("Setting image");
            //RawImage image = __instance.hpNodes[11].transform.parent.GetComponent<RawImage>();
            //image.texture = Main.LoadoutImage;
            //image.rectTransform.sizeDelta = new Vector2(460, 105.6f);

            Debug.Log("Destroying extra nodes and lines.");
            GameObject.Destroy(__instance.hpNodes[12].gameObject);
            GameObject.Destroy(__instance.hpNodes[13].gameObject);
            GameObject.Destroy(__instance.hpNodes[14].gameObject);
            GameObject.Destroy(__instance.hpNodes[15].gameObject);
            foreach (UILineRenderer renderer in __instance.gameObject.GetComponentsInChildren<UILineRenderer>(true))
                GameObject.Destroy(renderer.gameObject);

            Debug.Log("Removing extra nodes from the loadout configurator.");
            HPConfiguratorNode[] newNodes = new HPConfiguratorNode[12];
            for (int i = 0; i < 12; i++)
            {
                newNodes[i] = __instance.hpNodes[i];
            }
            __instance.hpNodes = newNodes;



            Debug.Log("__instance setup complete.");
        }
        return true;
    }

}
[HarmonyPatch(typeof(LoadoutConfigurator), "EquipCompatibilityMask")]
public static class EquipComaptibilityPatch // I stole this from c I stole this from C
{
    public static bool Prefix(LoadoutConfigurator __instance, HPEquippable equip)
    {
        Dictionary<string, string> allowedhardpointbyweapon = new Dictionary<string, string>();
        if (Main.selectedPlane != null && Main.selectedPlane.trueVehicleName == "X-02S")
        {
            allowedhardpointbyweapon.Add("f45_gun", "0");
            allowedhardpointbyweapon.Add("f45_aim9x1", "5,6,7,8,9,10");
            allowedhardpointbyweapon.Add("f45_amraamInternal", "1,2,3,4,9,10");
            allowedhardpointbyweapon.Add("f45_amraamRail", "5,6,7,8");
            allowedhardpointbyweapon.Add("f45_sidewinderx2", "1,2,3,4,9,10");
            allowedhardpointbyweapon.Add("f45_droptank", "5,6");
            allowedhardpointbyweapon.Add("f45_gbu38x1", "1,2,3,4");
            allowedhardpointbyweapon.Add("f45_gbu38x2Internal", "1,2,3,4");
            allowedhardpointbyweapon.Add("f45_gbu38x4Internal", "1,2,3,4");
            allowedhardpointbyweapon.Add("f45-gbu53", "1,2,3,4");

            allowedhardpointbyweapon.Add("f45_gbu12x1", "");
            allowedhardpointbyweapon.Add("f45_gbu12x2Internal", "");
            allowedhardpointbyweapon.Add("f45-agm145I", "");
            allowedhardpointbyweapon.Add("f45-agm145It", "");
            allowedhardpointbyweapon.Add("f45-agm145ISide", "");
            allowedhardpointbyweapon.Add("f45-agm145x3", "");
            allowedhardpointbyweapon.Add("f45-gbu39", "");
            allowedhardpointbyweapon.Add("f45_mk82Internal", "");
            allowedhardpointbyweapon.Add("f45_mk82x1", "");
            allowedhardpointbyweapon.Add("f45_mk82x4Internal", "");
            allowedhardpointbyweapon.Add("f45_mk83x1", "");
            allowedhardpointbyweapon.Add("f45_mk83x1Internal", "");
            allowedhardpointbyweapon.Add("f45_agm161", "");
            allowedhardpointbyweapon.Add("f45_agm161Internal", "");
        }
        else if (Main.selectedPlane != null && Main.selectedPlane.allEquips != null)
            allowedhardpointbyweapon = Main.selectedPlane.allEquips;

        Debug.Log("Equipment: " + equip.name + " previously allowed on" + equip.allowedHardpoints);

        if (allowedhardpointbyweapon.ContainsKey(equip.name))
        {
            equip.allowedHardpoints = (string)allowedhardpointbyweapon[equip.name];
            Debug.Log("Equipment: " + equip.name + " now allowed on" + equip.allowedHardpoints);
        }
        else
        {
            Debug.Log("Equipment: " + equip.name + ", not in dictionary");
        }
        return true;
    }
}