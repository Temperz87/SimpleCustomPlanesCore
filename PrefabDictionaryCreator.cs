using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Valve.Newtonsoft.Json;

#if UNITY_EDITOR
class EditorGUIPopup : EditorWindow
{
    string[] options = { "AV-42C", "FA-26B", "F-45A" };
    int index = 0;
    string pathToDump = "Assets/";
    static Dictionary<HPEquippable, string> allStuff42;
    static Dictionary<HPEquippable, string> allStuff26;
    static Dictionary<HPEquippable, string> allStuff45;

    [MenuItem("SCP/Make Prefab Dictionary")]
    static void Init()
    {
        string path42 = "hpequips/vtol";
        string path26 = "hpequips/afighter";
        string path45 = "hpequips/f45a";
        allStuff42 = new Dictionary<HPEquippable, string>();
        allStuff26 = new Dictionary<HPEquippable, string>();
        allStuff45 = new Dictionary<HPEquippable, string>();

        foreach (GameObject go in Resources.LoadAll(path42))
        {
            allStuff42.Add(go.GetComponent<HPEquippable>(), go.GetComponent<HPEquippable>().allowedHardpoints);
        }
        foreach (GameObject go in Resources.LoadAll(path26))
        {
            allStuff26.Add(go.GetComponent<HPEquippable>(), go.GetComponent<HPEquippable>().allowedHardpoints);
        }
        foreach (GameObject go in Resources.LoadAll(path45))
        {
            allStuff45.Add(go.GetComponent<HPEquippable>(), go.GetComponent<HPEquippable>().allowedHardpoints);
        }

        EditorGUIPopup window = GetWindow<EditorGUIPopup>();
        window.position = new Rect(0, 0, 400, 1200);
        window.Show();
    }

    void OnGUI()
    {
        index = EditorGUI.Popup(
            new Rect(0, 0, position.width, 20),
            "Equips",
            index,
            options);
        EditorGUI.LabelField(new Rect(0, 20, position.width, 20), "Path to dump");
        pathToDump = EditorGUI.TextField(new Rect(0, 40, position.width, 20), pathToDump);
        Dictionary<HPEquippable, string> toUse = null;
        switch (index)
        {
            case 0:
                toUse = allStuff42;
                break;
            case 1:
                toUse = allStuff26;
                break;
            case 2:
                toUse = allStuff45;
                break;
        }
        int i = 3;
        List<HPEquippable> allKeys = toUse.Keys.ToList();
        foreach (HPEquippable equip in allKeys)
        {
            EditorGUI.LabelField(new Rect(0, 20 * i, position.width, 20), equip.gameObject.name + " : ");
            toUse[equip] = EditorGUI.TextField(new Rect(150, 20 * i, position.width, 20), toUse[equip]);
            i++;
        }
        EditorGUIPopup window = GetWindow<EditorGUIPopup>();
        window.position = new Rect(0, 0, 400, ((i * 20) + 25));
        if (GUI.Button(new Rect(0, position.height - 20, position.width, 20), "Dump File"))
        {
            using (StreamWriter file = File.CreateText(pathToDump + "/equips.json"))
            using (JsonWriter writer = new JsonTextWriter(file))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                foreach (HPEquippable equip in toUse.Keys)
                {
                    writer.WritePropertyName(equip.gameObject.name);
                    writer.WriteValue(toUse[equip]);
                }
                writer.WriteEndObject();
            }
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
        }
    }
}
#endif