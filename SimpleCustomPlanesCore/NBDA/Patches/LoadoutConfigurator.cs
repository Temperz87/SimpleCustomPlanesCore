using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[HarmonyPatch(typeof(LoadoutConfigurator), nameof(LoadoutConfigurator.Initialize))]
public class Inject_CustomWeapons
{
    [HarmonyPostfix]
    public static void Postfix(LoadoutConfigurator __instance, bool useMidflightEquips)
    {
        Traverse traverse = Traverse.Create(__instance);
        Dictionary<string, EqInfo> unlockedWeaponPrefabs = (Dictionary<string, EqInfo>)traverse.Field("unlockedWeaponPrefabs").GetValue();
        foreach (string name in Armory.allCustomWeapons.Keys)
        {
            if (name == null || Armory.allCustomWeapons[name] == null)
                continue;
            try
            {
                Debug.Log("Try add " + name + " to loadout configurator.");
                CustomEqInfo info = Armory.allCustomWeapons[name];
                if (!info.CompareTo(Main.selectedPlane))
                {
                    Debug.Log("Compare to somehow failed?");
                    //continue; 
                }
                if (info == null)
                {
                    Debug.LogError(name + " was not found in all custom weaopns.");
                    continue;
                }
                if (info.weaponObject == null)
                {
                    Debug.LogError("info's weapon object is null.");
                    continue;
                }
                GameObject customWeapon = info.weaponObject;
                EqInfo eq = new EqInfo(GameObject.Instantiate(customWeapon), name);
                unlockedWeaponPrefabs.Add(name, eq);
                __instance.availableEquipStrings.Add(name);

                Debug.Log("Added weapon to list " + name);
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError("Couldn't find weapon " + name);
                continue;
            }
        }

        if (useMidflightEquips && __instance.equips != null) // ensure that reloading works
        {
            foreach (HPEquippable equip in __instance.equips)
            {
                if (equip == null)
                    continue;
                if (!unlockedWeaponPrefabs.ContainsKey(equip.gameObject.name))
                    unlockedWeaponPrefabs.Add(equip.gameObject.name, new EqInfo(GameObject.Instantiate(Armory.TryGetWeapon(equip.gameObject.name).equip.gameObject), equip.gameObject.name));
            }
        }
        traverse.Field("unlockedWeaponPrefabs").SetValue(unlockedWeaponPrefabs);
        traverse.Field("allWeaponPrefabs").SetValue(unlockedWeaponPrefabs);
    }
}

[HarmonyPatch(typeof(LoadoutConfigurator), "Attach")]
public static class Patch_AttachCustomWeapon
{
    public static bool Prefix(string weaponName, int hpIdx, LoadoutConfigurator __instance) // this entire functions existance stems from not being able to patch out get instantiated
    {
        if (helper == null)
        {
            helper = __instance.gameObject.AddComponent<MonoHelper>();
        }
        if (Armory.CheckCustomWeapon(weaponName))
        {
            __instance.Detach(hpIdx);
            Debug.Log("attaching custom weapon " + weaponName);
            if (!__instance.uiOnly)
                helper.StartConfigRoutine(hpIdx, weaponName, __instance);
            else
                helper.AttachImmediate(hpIdx, weaponName, __instance);
            return false;
        }
        else
            Debug.Log(weaponName + " is not a custom weapon.");
        return true;
    }
    public static MonoHelper helper = null;
}

[HarmonyPatch(typeof(LoadoutConfigurator), "AttachImmediate")]
public static class Patch_AttachCustomWeaponImmediate
{
    public static bool Prefix(string weaponName, int hpIdx, LoadoutConfigurator __instance) // this entire functions existance stems from not being able to patch out get instantiated
    {
        if (Patch_AttachCustomWeapon.helper == null)
            Patch_AttachCustomWeapon.helper = __instance.gameObject.AddComponent<MonoHelper>();
        if (Armory.CheckCustomWeapon(weaponName))
        {
            __instance.Detach(hpIdx);
            Debug.Log("attaching immediate custom weapon " + weaponName);
            Patch_AttachCustomWeapon.helper.AttachImmediate(hpIdx, weaponName, __instance);
            return false;
        }
        else
            Debug.Log(weaponName + " is not a custom weapon.");
        return true;
    }
}

[HarmonyPatch(typeof(HPConfiguratorFullInfo), nameof(HPConfiguratorFullInfo.AttachSymmetry))]
public static class Ensure_Symmetry
{
    public static bool Prefix(string weaponName, int hpIdx, HPConfiguratorFullInfo __instance)
    {
        if (!Armory.CheckCustomWeapon(weaponName))
            return true;
        GameObject equip = Armory.TryGetWeapon(weaponName).weaponObject;
        if (equip == null)
            return false;
        int weirdbitwiseshit = LoadoutConfigurator.EquipCompatibilityMask(equip.GetComponent<HPEquippable>());
        if (__instance.configurator.wm.symmetryIndices != null && hpIdx < __instance.configurator.wm.symmetryIndices.Length) // I have no idea how baha did this at all
        {
            int num = __instance.configurator.wm.symmetryIndices[hpIdx];
            if (num < 0 || __instance.configurator.lockedHardpoints.Contains(num))
            {
                return false;
            }
            int WHATISTHISVARIABLE = 1 << num;

            if ((WHATISTHISVARIABLE & weirdbitwiseshit) == WHATISTHISVARIABLE)
            {
                __instance.configurator.Attach(weaponName, num);
            }
        }
        return false;
    }
}

public class MonoHelper : MonoBehaviour
{
    private static ExternalOptionalHardpoints extHardpoint;
    private void Awake()
    {
        if (extHardpoint == null)
        {
            GameObject playerObject = VTOLAPI.GetPlayersVehicleGameObject();
            if (playerObject)
                extHardpoint = playerObject.GetComponent<ExternalOptionalHardpoints>();
        }
    }
    public void StartConfigRoutine(int hpIdx, string weaponName, LoadoutConfigurator __instance)
    {
        try
        {
            StartCoroutine(attachRoutine(hpIdx, weaponName, __instance));
        }
        catch (NullReferenceException e)
        {
            if (weaponName != null && __instance != null)
                Debug.LogError("TEMPERZ YOU PARENTED IT TO THE WRONG THING AGAIN YOU NIMWIT");
            throw e;
        }
    }

    public void AttachImmediate(int hpIdx, string weaponName, LoadoutConfigurator __instance)
    {
        __instance.DetachImmediate(hpIdx);
        if (__instance.uiOnly)
        {
            CustomEqInfo weapon = Armory.TryGetWeapon(weaponName);
            __instance.equips[hpIdx] = weapon.equip;
            __instance.equips[hpIdx].OnConfigAttach(__instance);
            if (__instance.equips[hpIdx] is HPEquipIRML iRML)
                iRML.irml = (IRMissileLauncher)iRML.ml;
        }
        else
        {
            GameObject instantiated = Instantiate(Armory.TryGetWeapon(weaponName).weaponObject);
            instantiated.name = weaponName;
            instantiated.SetActive(true);
            Transform transform = instantiated.transform;
            __instance.equips[hpIdx] = transform.GetComponent<HPEquippable>();
            Traverse LCPTraverse = Traverse.Create(__instance);
            Transform[] allTfs = (Transform[])(LCPTraverse.Field("hpTransforms").GetValue());
            Transform hpTf = allTfs[hpIdx];
            transform.SetParent(hpTf);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            __instance.equips[hpIdx].OnConfigAttach(__instance);
            if (extHardpoint != null)
                extHardpoint.Wm_OnWeaponEquippedHPIdx(hpIdx);
        }
        __instance.UpdateNodes();
    }

    private static IEnumerator attachRoutine(int hpIdx, string weaponName, LoadoutConfigurator __instance)
    {
        Debug.Log("Starting attach routine.");
        Traverse LCPTraverse = Traverse.Create(__instance);
        Coroutine[] detachRoutines = (Coroutine[])LCPTraverse.Field("detachRoutines").GetValue();
        if (detachRoutines[hpIdx] != null)
        {
            yield return detachRoutines[hpIdx];
            detachRoutines = (Coroutine[])LCPTraverse.Field("detachRoutines").GetValue();
        }
        GameObject instantiated = Instantiate(Armory.TryGetWeapon(weaponName).weaponObject);
        instantiated.name = weaponName;
        instantiated.SetActive(true);
        if (extHardpoint != null)
            extHardpoint.Wm_OnWeaponEquippedHPIdx(hpIdx);
        InternalWeaponBay iwb = __instance.GetWeaponBay(hpIdx);
        if (iwb)
            iwb.Open();

        Transform weaponTf = instantiated.transform;
        Transform[] allTfs = (Transform[])(LCPTraverse.Field("hpTransforms").GetValue());
        Transform hpTf = allTfs[hpIdx];
        __instance.equips[hpIdx] = weaponTf.GetComponent<HPEquippable>();
        __instance.equips[hpIdx].OnConfigAttach(__instance);
        weaponTf.rotation = hpTf.rotation;
        Vector3 localPos = new Vector3(0f, -4f, 0f);
        weaponTf.position = hpTf.TransformPoint(localPos);
        __instance.UpdateNodes();
        Vector3 tgt = new Vector3(0f, 0f, 0.5f);
        while ((localPos - tgt).sqrMagnitude > 0.01f)
        {
            localPos = Vector3.Lerp(localPos, tgt, 5f * Time.deltaTime);
            weaponTf.position = hpTf.TransformPoint(localPos);
            yield return null;
        }
        weaponTf.parent = hpTf;
        weaponTf.localPosition = tgt;
        weaponTf.localRotation = Quaternion.identity;
        __instance.vehicleRb.AddForceAtPosition(Vector3.up * __instance.equipImpulse, __instance.wm.hardpointTransforms[hpIdx].position, ForceMode.Impulse);
        AudioSource[] allAudioTfs = (AudioSource[])(LCPTraverse.Field("hpAudioSources").GetValue());
        allAudioTfs[hpIdx].PlayOneShot(__instance.attachAudioClip);
        __instance.attachPs.transform.position = hpTf.position;
        __instance.attachPs.FireBurst();
        yield return new WaitForSeconds(0.2f);
        while (weaponTf.localPosition.sqrMagnitude > 0.001f)
        {
            weaponTf.localPosition = Vector3.MoveTowards(weaponTf.localPosition, Vector3.zero, 4f * Time.deltaTime);
            yield return null;
        }
        weaponTf.localPosition = Vector3.zero;
        __instance.UpdateNodes();
        if (iwb)
            iwb.Close();
        Coroutine[] attachRoutines = (Coroutine[])LCPTraverse.Field("attachRoutines").GetValue();
        attachRoutines[hpIdx] = null;
        LCPTraverse.Field("attachRoutines").SetValue(attachRoutines);
        Debug.Log("Ended attach routine.");
        //FindAllChildrenRecursively(instantiated.transform);
        yield break;
    }

    public static void FindAllChildrenRecursively(Transform parent) // debug function :P
    {
        return;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
                continue;
            Debug.Log("Found child of parent " + parent.name + " called " + child);
            FindAllChildrenRecursively(child);
        }
    }
}
