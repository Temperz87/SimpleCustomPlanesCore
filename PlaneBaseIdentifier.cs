using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.IO;
using UnityEngine.Events;
using Valve.Newtonsoft.Json;
using System.Text;
using System.Globalization;

public class PlaneBaseIdentifier : MonoBehaviour
{
    [ContextMenu("Initialize")]
    public void Initialize()
    {
        allObjects = new List<ObjectInformation>();

        List<string> allNames = new List<string>();
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            if (!Initialized)
                allTfs.Add(HashTransform(tf, allNames), transform);
            else
            {
                allNames.Add(transform.name);
                if (!allTfs.ContainsKey(tf.name))
                    allTfs.Add(tf.name, tf);
            }
        }

        void RecursiveGetTf(Transform tf)
        {
            foreach (Transform child in tf)
            {
                //if (child.name == "seatAdjustTf-1659611427" || child.name == "touchScreenArea1907453234")
                //{
                //    Debug.Log("Skipping blacklisted child");
                //    continue;
                //}
                allObjects.Add(new ObjectInformation(child.gameObject));
                RecursiveGetTf(child);
            }
        }

        allObjects.Add(new ObjectInformation(transform.gameObject));
        RecursiveGetTf(transform);

        Initialized = true;
    }

    private string HashTransform(Transform transform, List<string> allNames)
    {
        if (transform.name.Contains("(Clone)"))
            transform.name = transform.name.Substring(0, transform.name.IndexOf("(Clone)"));
        string toHash = transform.name;

        Transform parent = transform;
        while (parent.parent != null)
        {
            parent = parent.parent;
            toHash += parent.name;
        }

        transform.name = transform.name + toHash.GetHashCode();
        while (allNames.Contains(transform.name))
            transform.name += "1";
        allNames.Add(transform.name);
        //if (verboseLogs)
        //    Debug.Log("hashed " + transform.name);
        return transform.name;
    }

    [ContextMenu("Dump file")]
    public void Dump()
    {
        /* 1: Get File Location
         * 2: Spawn in custom plane first 
         * 3: Yell at users to NEVER add components on anything that isn't a custom object
         * 4: Get ALL values of components and dump them to a file, do pointers for object scripts (ex: take this health on this vehicle part and assign it to this other one on another vehicle, and store them into a giant ass list
         * 5: dump the file to the specified path, format should be gameobject name, position, original parent, reparent 
        */
        List<string> toDump = new List<string>();
        foreach (ObjectInformation info in allObjects)
        {
            if (info == null)
                Debug.Log("Null object?");
            toDump.Add(info.GetDumpLine());
            //Debug.Log("Added " + info.originalObject.name + " to be dumped.");
        }
        Debug.Log("Dumping file.");
        using (StreamWriter writer = new StreamWriter(pathToDump + nickname + ".SCP", false, Encoding.Default, 4096))
        {
            foreach (string line in toDump)
            {
                writer.WriteLine(line);
                //Debug.Log("Dumped " + line);
                writer.Flush();
            }
        }
        PlayerVehicle newVehicle = PlayerVehicle.CreateInstance<PlayerVehicle>();
        newVehicle.vehicleName = vehicleName;
        newVehicle.description = description;
        newVehicle.nickname = nickname;
        newVehicle.vehicleImage = vehicleImage;
        newVehicle.vehiclePrefab = vehiclePrefab;
        newVehicle.quicksaveReady = true;
        switch (equipsToUse)
        {
            case vehicleEnum.AH94:
                newVehicle.equipsResourcePath = "HPEquips/ah-94";
                break;
            case vehicleEnum.AV42C:
                newVehicle.equipsResourcePath = "HPEquips/vtol";
                break;
            case vehicleEnum.FA26B:
                newVehicle.equipsResourcePath = "HPEquips/afighter";
                break;
            case vehicleEnum.F45A:
                newVehicle.equipsResourcePath = "HPEquips/f45a";
                break;
        }

        switch (configuratorToUse)
        {
            case vehicleEnum.AH94:
                newVehicle.loadoutSpawnOffset = new Vector3(0, 1.79999995f, -0.5f);
                newVehicle.playerSpawnOffset = new Vector3(0, 1.79999995f, 6.78999996f);
                break;
            case vehicleEnum.AV42C:
                newVehicle.loadoutSpawnOffset = new Vector3(0, 1.36300004f, 0);
                newVehicle.playerSpawnOffset = new Vector3(0, 1.36300004f, 4.26999998f);
                break;
            case vehicleEnum.FA26B:
                newVehicle.loadoutSpawnOffset = new Vector3(0, 2.6500001f, -7.01999998f);
                newVehicle.playerSpawnOffset = new Vector3(0, 2.66000009f, 0);
                break;
            case vehicleEnum.F45A:
                newVehicle.loadoutSpawnOffset = new Vector3(0, 1.90999997f, -6);
                newVehicle.playerSpawnOffset = new Vector3(0, 1.90999997f, -1.34000003f);
                break;
        }
#if UNITY_EDITOR 
        newVehicle.hardpointCount = hardpointCount;
        newVehicle.allEquipPrefabs = new List<GameObject>(customWeapons);
        AssetDatabase.CreateAsset(newVehicle, pathToDump + nickname + ".asset");
        AssetDatabase.SaveAssets();

        string formattedDependency = dependencyName;

        if (!dependencyName.Contains(".dll"))
            dependencyName += ".dll";

        using (StreamWriter file = File.CreateText(pathToDump + " /manifest.json"))
        using (JsonWriter writer = new JsonTextWriter(file))
        {
            writer.Formatting = Formatting.Indented;

            writer.WriteStartObject();
            writer.WritePropertyName("PlayerVehicle");
            writer.WriteValue(nickname);
            writer.WritePropertyName("BaseVehicle");
            writer.WriteValue(baseVehicle.ToString());
            writer.WritePropertyName("Campaign");
            writer.WriteValue(campaignsToUse.ToString());
            writer.WritePropertyName("Configurator");
            writer.WriteValue(configuratorToUse.ToString());
            writer.WritePropertyName("Equips");
            writer.WriteValue(equipsToUse.ToString());
            writer.WritePropertyName("DependencyName");
            writer.WriteValue(dependencyName.ToString());
            writer.WriteEndObject();
        }

        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
#endif
    }

    [ContextMenu("Load from path")]
    public void Load()
    {
        List<string> allNames = new List<string>();
        if (!Initialized)
        {
            foreach (Transform tf in GetComponentsInChildren<Transform>(true))
                allTfs.Add(HashTransform(tf, allNames), tf);
            Initialized = true;
        }
        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> allInstructions = SCPTextReader.GetLinesFromFile(pathToDump + "/" + nickname + ".SCP");
        foreach (string line in allInstructions.Keys) // we must hash them first so we actually get their names
        {
            Dictionary<string, List<SCPTextReader.Instruction>> currentObject = allInstructions[line];

            if (line.IndexOf(" ") <= 0)
            {
                Debug.LogWarning("Got an index less than 0 for space.");
                continue;
            }
            try
            {
                if (!allTfs.ContainsKey(NextElement(line, 0)))
                {
                    Debug.LogWarning("Couldn't resolve object " + NextElement(line, 0));
                    continue;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogWarning("Couldn't resolve object");
                continue;
            }

            Transform currentTf = allTfs[NextElement(line, 0)];
            //Transform currentParent = null;
            Transform newParent = null;

            string finderString = NextElement(line, 1);
            //if (!finderString.Contains("NULL"))
            //{
            //    Debug.Log("Got string " + finderString + " on " + currentTf.name);
            //    //currentParent = allTfs[finderString.Trim()];
            //}
            finderString = NextElement(line, 2);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " again on " + currentTf.name);
                try
                {
                    newParent = allTfs[finderString.Trim()];
                }
                catch (KeyNotFoundException e)
                {
                    newParent = null;
                }
            }
            if (newParent != null)
            {
                currentTf.SetParent(newParent);
                //Debug.Log("reparenting " + currentTf + " from " + currentParent?.name + " to " + newParent.name);
            }
            if (currentTf.parent != null)
            {
                Vector3 localPos;
                Vector3 localEulers;
                Vector3 localScale;

                localPos = StringToVector3(NextElement(line, 3));
                localEulers = StringToVector3(NextElement(line, 4));
                localScale = StringToVector3(NextElement(line, 5));

                currentTf.localPosition = localPos;
                currentTf.localEulerAngles = localEulers;
                currentTf.localScale = localScale;
                currentTf.gameObject.SetActive(line[line.Length - 2] == 'u');
                //Debug.Log("Set " + currentTf.name + " to " + localPos);
            }
            foreach (string component in currentObject.Keys)
            {
                //verboseLogs = true;
                //if (verboseLogs)
                //    Debug.Log("Try do object " + currentTf.name + " for component " + component.Trim());
                Type type = Type.GetType(component.Trim());
                if (type == null)
                {
                    Debug.LogError("Couldn't resolve component " + component.Trim());
                    continue;
                }
                Component currentComponent = currentTf.GetComponent(type);
                if (currentComponent == null)
                {
                    Debug.LogError("Current component of type " + type.Name + " was null on " + currentTf.name);
                    continue;
                }
                List<SCPTextReader.Instruction> instructions = currentObject["    " + type.AssemblyQualifiedName];
                //if (verboseLogs)
                //    Debug.Log("dict    " + type.AssemblyQualifiedName + " has a length of " + instructions.Count + " on " + currentTf.name);
                foreach (SCPTextReader.Instruction instruction in instructions)
                {
                    if (instruction == null)
                        Debug.LogError("How the fuck was an instruction null?");
                    else
                        instruction.Run(currentComponent, allTfs, false);
                }
            }
        }
        if (File.Exists(pathToDump + "manifest.json"))
        {
            StreamReader r = new StreamReader(pathToDump + "manifest.json");
            string jsonString = r.ReadToEnd();

            manifestDataModel model = JsonConvert.DeserializeObject<manifestDataModel>(jsonString);

            baseVehicle = convertString(model.baseVehicle);
            campaignsToUse = convertString(model.campaign);
            configuratorToUse = convertString(model.configurator);
            equipsToUse = convertString(model.equips);
            dependencyName = model.dependencyName;
#if UNITY_EDITOR
            PlayerVehicle asset = AssetDatabase.LoadAssetAtPath<PlayerVehicle>(pathToDump + model.playerVehicle + ".asset");
            if (asset == null)
                Debug.LogWarning("Couldn't find a player vehicle to load from.");
            else
            {
                vehicleName = asset.vehicleName;
                description = asset.description;
                nickname = asset.nickname;
                hardpointCount = asset.hardpointCount;
                vehicleImage = asset.vehicleImage;
                vehiclePrefab = asset.vehiclePrefab;
                customWeapons = asset.allEquipPrefabs.ToArray();
            }
#endif
        }
        Debug.Log("Loaded from file!");
    }


    class manifestDataModel
    {
        public string playerVehicle;
        public string baseVehicle;
        public string campaign;
        public string configurator;
        public string equips;
        public string dependencyName;
    }

    public static vehicleEnum convertString(string convert)
    {
        switch (convert.Trim())
        {
            case "AV42C":
                return vehicleEnum.AV42C;
            case "FA26B":
                return vehicleEnum.FA26B;
            case "F45A":
                return vehicleEnum.F45A;
            case "AH94":
                return vehicleEnum.AH94;
        }
        return vehicleEnum.None;
    }

    public static string NextElement(string line, int index)
    {
        //Debug.Log("Getting element " + index + " from line " + line);
        return line.Substring(SubStringOfIndex(line, "[{(", index) + 3, SubStringOfIndex(line, ")}]", index) - SubStringOfIndex(line, "[{(", index) - 3).Trim();
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    public static int SubStringOfIndex(string str, string value, int position = 0)
    {
        if (position < 0)
            return -1;

        int offset = str.IndexOf(value);
        for (int i = 0; i < position; i++)
        {
            if (offset == -1) return -1;
            offset = str.IndexOf(value, offset + 1);
        }

        return offset;
    }

    public string pathToDump = "Assets/Simple Custom Planes/SubFolder/";
    [Header("PlayerVehicle Stuff")]
    public string vehicleName;
    public string description;
    public string nickname;
    public int hardpointCount;
    public Texture2D vehicleImage;
    public vehicleEnum baseVehicle;
    public vehicleEnum campaignsToUse;
    public vehicleEnum configuratorToUse;
    public vehicleEnum equipsToUse;
    [Header("Your vehicle prefab, not the SCP amalgamation you made")]
    public GameObject vehiclePrefab;
    [Header("A .dll dependency if you have one")]
    public string dependencyName;
    [Header("If you have a HPEquippable prefab, put it here")]
    public GameObject[] customWeapons;

    public enum vehicleEnum
    {
        None,
        AV42C,
        FA26B,
        F45A,
        AH94
    }

    //public bool verboseLogs = false;

    [Header("This is to show if the object has been intialized, don't click this")]
    public bool Initialized = false;

    public List<ObjectInformation> allObjects;
    private Dictionary<string, Transform> allTfs = new Dictionary<string, Transform>();

    public class ObjectInformation
    {
        public GameObject originalObject;
        public Transform originalParent;
        public Transform newParent;
        public List<Component> allComponents = new List<Component>();
        public Dictionary<Component, Type> componentsByType = new Dictionary<Component, Type>();
        //private int initialDumpHash;

        public ObjectInformation(GameObject go)
        {
            //if (verboseLogs)
            //    Debug.Log("Making new Object information for " + go.name);
            originalObject = go;
            originalParent = go.transform.parent;

            int i = 0;
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    Debug.LogWarning("Somehow got a null component?");
                    continue;
                }
                allComponents.Add(component);
                componentsByType.Add(component, component.GetType());
                //if (verboseLogs)
                //    Debug.Log("Added component to object " + go.name + " of " + component.GetType().Name);
                i++;
            }
            //if (verboseLogs)
            //    Debug.Log("Added " + i + " components to object " + go.name);
            //initialDumpHash = GetProperties().GetHashCode();
        }

        public string GetDumpLine()
        {
            newParent = originalObject.transform.parent;
            string active = originalObject.activeSelf.ToString();
            if (originalObject.name.Contains("Local") && originalObject.GetComponent<VTOLVR.Multiplayer.MultiplayerObjectSwitch>())
                active = "False";
            string result = "[{(" + originalObject.name + ")}] [{(" + (originalParent ? originalParent.name : "NULL") + ")}] [{(" + (newParent ? newParent.name : "NULL") + ")}] [{(" + Vector3ToString(originalObject.transform.localPosition) + ")}] [{(" + Vector3ToString(originalObject.transform.localEulerAngles) + ")}] [{(" + Vector3ToString(originalObject.transform.localScale) + ")}] " + active;
            string toAdd = GetProperties();
            //Debug.Log("Initial hash is " + initialDumpHash);
            if (toAdd.Length != 0)
                result += toAdd + "\nEND";
            return result;
        }

        private string Vector3ToString(Vector3 vector)
        {
            return "(" + vector.x + ", " + vector.y + ", " + vector.z + ")";
        }

        private string GetProperties()
        {
            string result = "";
            foreach (Component component in allComponents)
            {
                if (component is PlaneBaseIdentifier)
                    continue;
                if (component == null)
                {
                    Debug.Log("Null component?");
                    continue;
                }

                if (component is Renderer)
                {
                    result += "\n    " + componentsByType[component].AssemblyQualifiedName + "\n        assign enabled [{(Boolean)}] [{(" + ((Renderer)component).enabled + ")}]\n    END";
                    continue;
                }

                FieldInfo[] fInfo = componentsByType[component].GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fInfo.Length != 0)
                {
                    string componentStr = "\n    " + componentsByType[component].AssemblyQualifiedName;
                    foreach (FieldInfo prop in fInfo)
                    {
                        try
                        {
                            object value = prop.GetValue(component);
                            //if (value == null)
                            //    continue;
                            //Debug.Log(trueType.Name + " on " + component.gameObject.name + " found prop: " + prop.Name + ": " + value + " of type " + value.GetType().Name);
                            if (value != null && value is Array)
                            {
                                Array newArr = (Array)value;
                                string arrayStr = "\n        array [{(" + newArr.GetValue(0).GetType().AssemblyQualifiedName + ")}] [{(" + prop.Name + ")}] " + newArr.Length;
                                bool addArray = false;
                                if (newArr.Length > 0)
                                {
                                    if (newArr is Component[])
                                    {
                                        //Debug.Log("Got a component[]");
                                        for (int i = 0; i < newArr.Length; i++)
                                        {
                                            Component arrValue = ((Component)newArr.GetValue(i));
                                            arrayStr += "\n            pointer " + i + " [{(" + arrValue.GetType().AssemblyQualifiedName + ")}] [{(" + arrValue.name + ")}]";
                                            addArray = true;
                                        }
                                    }
                                    else
                                    {
                                        Type type = newArr.GetValue(0).GetType();
                                        if (type.Name == "String" || type.Name == "Int32" || type.Name == "Single" || type.Name == "Boolean")
                                        {
                                            //Debug.Log("Got a primitive[]");
                                            for (int i = 0; i < newArr.Length; i++)
                                            {
                                                arrayStr += "\n            assign " + i + " [{(" + type.Name + ")}] [{(" + newArr.GetValue(i) + ")}]";
                                                addArray = true;
                                            }
                                        }
                                        else if (newArr is GameObject[]) // i don't know why i have to do this, should be literally a component
                                        {
                                            //Debug.Log("Got a gameObject[]");
                                            for (int i = 0; i < newArr.Length; i++)
                                            {
                                                arrayStr += "\n            pointer " + i + " [{(" + typeof(GameObject).AssemblyQualifiedName + ")}] [{(" + ((GameObject)newArr.GetValue(i)).name + ")}]";
                                                addArray = true;
                                            }
                                        }
                                        else
                                        {
                                            //Debug.Log("Got a weird[]");
                                            for (int i = 0; i < newArr.Length; i++)
                                            {
                                                object element = newArr.GetValue(i);
                                                arrayStr += "\n                " + convertField(i.ToString(), element, element.GetType().AssemblyQualifiedName, "        ");
                                                addArray = true;
                                            }
                                        }
                                    }
                                    if (addArray)
                                        componentStr += arrayStr;
                                }
                            }
                            else
                                componentStr += "\n        " + convertField(prop.Name, value, prop.FieldType.AssemblyQualifiedName);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("caught exception, " + e.StackTrace);
                        }
                    }
                    componentStr += "\n    END";
                    result += componentStr;
                }
            }
            return result;
        }

        private string convertField(string fieldName, object field, string typeName, string newObjectIndent = "")
        {
            string result;
            Type fieldType = Type.GetType(typeName);
            if (fieldType.IsSubclassOf(typeof(Component)))
            {
                if (field != null)
                    result = "pointer " + fieldName + " [{(" + ((Component)field).name + ")}] [{(" + typeName + " )}]";
                else
                {
                    //Debug.Log("Writing null");
                    result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                }
            }
            else if (fieldType.IsSubclassOf(typeof(Animator)))
            {
                if (field != null)
                    result = "pointer " + fieldName + " [{(" + ((Animator)field).name + ")}] [{(" + typeName + " )}]";
                else
                    result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
            }
            else if (fieldType.IsSubclassOf(typeof(UnityEventBase)))
            {
                return "ignore " + fieldName;
                string events = "";
                UnityEventBase baseEvent = field as UnityEventBase;
                int size = baseEvent.GetPersistentEventCount();
                if (size == 0)
                    return "ignore " + fieldName;
                string eventType = "none";
                if (baseEvent is IntEvent)
                    eventType = "int";
                else if (baseEvent is BoolEvent)
                    eventType = "bool";
                else if (baseEvent is StringEvent)
                    eventType = "string";
                for (int i = 0; i < size; i++)
                {
                    events += "\n[{(" + baseEvent.GetPersistentTarget(i) + ")}] [{(" + baseEvent.GetPersistentMethodName(i) + ")}]";
                }
                result = "events " + eventType + events;
            }
            else
            {
                if (fieldType.IsPrimitive || field is string || field is Enum)
                {
                    if (field is Enum)
                        field = (int)field;
                    result = "assign " + fieldName + " [{(" + fieldType.Name + ")}] [{(" + field.ToString().Replace("\n", "\\n") + ")}]";
                }
                //else if (field is VTOLQuickStart.QuickStartComponents) // this is a very shit implementation
                //{
                //    //Debug.Log("Got quickstart components");
                //    result = "qsComponents " + fieldName;
                //    foreach (VTOLQuickStart.QuickStartComponents.QSEngine qsEngine in ((VTOLQuickStart.QuickStartComponents)field).engines)
                //    {
                //        result += "\n            qsEngine[{(" + qsEngine.engine.name + ")}] [{(" + qsEngine.state + ")}]";
                //    }
                //}
                else
                {
                    result = "ignore " + fieldName;
                    Type newType = Type.GetType(typeName);
                    FieldInfo[] fInfo = newType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    if (field is Array)
                    {
                        result = "array [{(" + typeName + ")}] " + fieldName + "\n";
                        Array newArr = (Array)field;
                        string arrayStr = ""/*"\n        array [{(" + newArr.GetValue(0).GetType().AssemblyQualifiedName + ")}] [{(" + prop.Name + ")}] " + newArr.Length*/;
                        bool addArray = false;
                        if (newArr.Length > 0)
                        {
                            if (newArr is Component[])
                            {
                                //Debug.Log("Got a component[]");
                                for (int i = 0; i < newArr.Length; i++)
                                {
                                    Component arrValue = ((Component)newArr.GetValue(i));
                                    arrayStr += "\n            pointer " + i + " [{(" + arrValue.name + ")}] [{(" + arrValue.GetType().AssemblyQualifiedName + ")}]";
                                    addArray = true;
                                }
                            }
                            else
                            {
                                Type type = newArr.GetValue(0).GetType();
                                if (type.Name == "String" || type.Name == "Int32" || type.Name == "Single" || type.Name == "Boolean")
                                {
                                    //Debug.Log("Got a primitive[]");
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        arrayStr += "\n            assign " + i + " [{(" + newArr.GetValue(i) + ")}] [{(" + type.Name + ")}]";
                                        addArray = true;
                                    }
                                }
                                else if (newArr is GameObject[]) // i don't know why i have to do this, should be literally a component
                                {
                                    //Debug.Log("Got a gameObject[]");
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        arrayStr += "\n            pointer " + i + " [{(" + ((GameObject)newArr.GetValue(i)).name + ")}] [{(" + typeof(GameObject).AssemblyQualifiedName + ")}]";
                                        addArray = true;
                                    }
                                }
                                else
                                {
                                    //Debug.Log("Got a weird[]");
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        object element = newArr.GetValue(i);
                                        arrayStr += "\n            newObject " + i + " [{(" + element.GetType().AssemblyQualifiedName + ")}]";
                                        FieldInfo[] info = element.GetType().GetFields();
                                        foreach (FieldInfo eInfo in info)
                                        {
                                            arrayStr += "\n                " + convertField(eInfo.Name, eInfo.GetValue(element), eInfo.FieldType.Name);
                                        }
                                    }
                                }
                            }
                            if (addArray)
                                result += arrayStr;
                        }
                    }
                    else if (fInfo.Length != 0 && !fInfo.GetType().IsAssignableFrom(typeof(PlayerVehicle))) // yes this is necasary :P
                    {
                        ConstructorInfo cInfo = Type.GetType(typeName).GetConstructors()[Type.GetType(typeName).GetConstructors().Length - 1];
                        List<string> allTypes = new List<string>();
                        foreach (ParameterInfo pInfo in cInfo.GetParameters())
                            allTypes.Add(pInfo.ParameterType.AssemblyQualifiedName);
                        result = "newObject [{(" + fieldName + ")}] [{(" + typeName + " )}] [{(" + allTypes.Count + ")}] [{(" + ConfigNodeUtils.WriteList(allTypes) + ")}]";
                        foreach (FieldInfo prop in fInfo)
                        {
                            result += "\n            " + newObjectIndent + convertField(prop.Name, prop.GetValue(field), prop.FieldType.AssemblyQualifiedName, newObjectIndent + "    ");
                        }
                    }
                }
            }
            return result;
        }

        //public string ToLiteral(string input) // from https://stackoverflow.com/a/14502246
        //{
        //    var literal = new StringBuilder(input.Length + 2);
        //    literal.Append("\"");
        //    foreach (var c in input)
        //    {
        //        switch (c)
        //        {
        //            case '\'': literal.Append(@"\'"); break;
        //            case '\"': literal.Append("\\\""); break;
        //            case '\\': literal.Append(@"\\"); break;
        //            case '\0': literal.Append(@"\0"); break;
        //            case '\a': literal.Append(@"\a"); break;
        //            case '\b': literal.Append(@"\b"); break;
        //            case '\f': literal.Append(@"\f"); break;
        //            case '\n': literal.Append(@"\n"); break;
        //            case '\r': literal.Append(@"\r"); break;
        //            case '\t': literal.Append(@"\t"); break;
        //            case '\v': literal.Append(@"\v"); break;
        //            default:
        //                if (Char.GetUnicodeCategory(c) != UnicodeCategory.Control)
        //                {
        //                    literal.Append(c);
        //                }
        //                else
        //                {
        //                    literal.Append(@"\u");
        //                    literal.Append(((ushort)c).ToString("x4"));
        //                }
        //                break;
        //        }
        //    }
        //    literal.Append("\"");
        //    return literal.ToString().Substring(1, literal.ToString().Length - 2);
        //}
    }

    public class SCPTextReader
    {
        public static Dictionary<string, Dictionary<string, List<Instruction>>> GetLinesFromFile(string path = @"C:\Users\tempe\Desktop\Ripped\VTOLVR\Assets\Simple Custom Planes\MyCoolPlane.SCP")
        {
            StreamReader reader = new StreamReader(path);
            Dictionary<string, Dictionary<string, List<Instruction>>> result = new Dictionary<string, Dictionary<string, List<Instruction>>>();

            Dictionary<string, List<Instruction>> currentDict = null;
            List<Instruction> currentList = null;

            string currentLine;
            Instruction currentInstruction = null;
            while ((currentLine = reader.ReadLine()) != null)
            {
                if (currentLine == null || currentLine.Length <= 0 || currentLine.Trim() == "END")
                    continue;
                if (currentLine[0] != ' ')
                {
                    currentDict = new Dictionary<string, List<Instruction>>();
                    //Debug.Log("adding line " + currentLine);
                    if (!result.ContainsKey(currentLine))
                        result.Add(currentLine, currentDict);
                }
                else if (currentLine[5] != ' ')
                {
                    currentList = new List<Instruction>();
                    while (currentDict.ContainsKey(currentLine))
                        currentLine += "(Copy)";
                    currentDict.Add(currentLine, currentList);
                    //Debug.Log("New dict: " + currentLine);
                }
                else if (currentLine.Substring(0, 12) == "            ")
                {
                    //Debug.Log("adding new sub instruction to instruction " + currentLine);
                    currentInstruction.AddInstruction(currentLine);
                }
                else
                {
                    currentInstruction = new Instruction(currentLine);
                    currentList.Add(currentInstruction);
                }
            }

            return result;
        }
        public class Instruction
        {
            private string field;
            private string value;
            private string type;
            private List<string> allTypes = new List<string>();
            private List<Instruction> subInstructions = new List<Instruction>();
            private Instruction lastNewObject = null;
            private int size = 0;

            public InstructionType instruction;

            public void AddInstruction(string line)
            {
                Instruction toAdd = new Instruction(line.Trim(), true);
                if (toAdd.instruction == InstructionType.newObject)
                    lastNewObject = toAdd;
                if ((instruction != InstructionType.array && line.Length >= 15 && line.Substring(0, 15) == "               ") || (instruction == InstructionType.array && line.Substring(0, 23) == "                        "))
                    lastNewObject.subInstructions.Add(toAdd);
                else
                    subInstructions.Add(new Instruction(line.Trim(), true));
                size++;
            }

            public Instruction(string newLine, bool isSubinstruction) // bool here is present for "fun" overloading
            {
                if (newLine[0] == 'a' && newLine[1] == 's')
                {
                    instruction = InstructionType.assign;
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                    field = newLine.Substring(7, newLine.Substring(7).IndexOf(" "));
                }
                else if (newLine[0] == 'p')
                {
                    instruction = InstructionType.pointer;
                    newLine = newLine.Substring(8);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'q')
                {
                    instruction = InstructionType.qsComponent;
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'n')
                {
                    instruction = InstructionType.newObject;
                    field = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    if (int.Parse(NextElement(newLine, 2)) > 0)
                    {
                        allTypes = ConfigNodeUtils.ParseList(NextElement(newLine, 3));
                    }
                }
            }

            public Instruction(string newLine)
            {
                newLine = newLine.Trim();
                if (newLine[0] == 'a' && newLine[1] == 's')
                {
                    instruction = InstructionType.assign;
                    newLine = newLine.Substring(7);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'p')
                {
                    instruction = InstructionType.pointer;
                    newLine = newLine.Substring(8);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    newLine = newLine.Substring(newLine.IndexOf(" ") + 1);
                    value = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    //Debug.Log("New sub type: " + type + " value: " + value);
                }
                else if (newLine[0] == 'e')
                {
                    instruction = InstructionType.events;
                    newLine = newLine.Substring(7);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    newLine = newLine.Substring(newLine.IndexOf(" ") + 1);
                    type = newLine.Substring(0, newLine.IndexOf(" ") + 1);
                    value = newLine.Substring(newLine.IndexOf(" ") + 1);
                }
                else if (newLine[0] == 'a' && newLine[1] == 'r')
                {
                    instruction = InstructionType.array;
                    type = NextElement(newLine, 0);
                    field = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'q')
                {
                    instruction = InstructionType.qsComponent;
                    type = newLine.Substring(newLine.IndexOf(" ") + 1).Trim();
                }
                else if (newLine[0] == 'n')
                {
                    instruction = InstructionType.newObject;
                    field = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    if (int.Parse(NextElement(newLine, 2)) > 0)
                    {
                        allTypes = ConfigNodeUtils.ParseList(NextElement(newLine, 3));
                    }
                }
            }

            public void Run(object original, Dictionary<string, Transform> allTransforms, bool log)
            {
                //Debug.Log("Running " + instruction);
                if (instruction == InstructionType.assign)
                {
                    if (log)
                        Debug.Log("Try do assign instruction, value is " + value + " and type is " + type + " and field is " + field);
                    switch (type.Trim())
                    {
                        case "String":
                            if (log)
                                Debug.Log("Setting string for " + field + " and value " + value);
                            original.GetType().GetField(field).SetValue(original, value);
                            //Debug.Log("Set string for " + field + " and value " + value);
                            break;
                        case "Boolean":
                            if (log)
                                Debug.Log("Setting bool for field " + field + " and value " + value + " original " + original.GetType());
                            if (original is Renderer)
                                ((Renderer)original).enabled = value.Trim() == "True";
                            else
                                original.GetType().GetField(field).SetValue(original, value.Trim() == "True");
                            break;
                        case "Single":
                            if (log)
                                Debug.Log("Setting float for " + field + " and value " + value + " and original type is " + original.GetType().Name);
                            if (original == null)
                                Debug.LogError("Original is null");
                            original.GetType().GetField(field).SetValue(original, float.Parse(value));
                            break;
                        case "Int32":
                            if (log)
                                Debug.Log("Setting int for " + field + " and value " + value);
                            original.GetType().GetField(field).SetValue(original, Int32.Parse(value));
                            break;
                        default:
                            if (original.GetType().GetField(field) != null && original.GetType().GetField(field).FieldType.IsSubclassOf(typeof(Enum)))
                            {
                                if (log)
                                    Debug.Log("Setting enum for " + field + " and value " + value);
                                original.GetType().GetField(field).SetValue(original, int.Parse(value));
                            }
                            else
                                Debug.LogError("Couldn't resolve type for assign instruction of field " + field);
                            break;
                    }
                }
                else if (instruction == InstructionType.pointer)
                {
                    // holy shit this is going to give me a fucking aneurism implementing i am not looking forward to this
                    // Debug.Log("Try do pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original.name);
                    Type newType = Type.GetType(type.Trim());
                    if (newType == null)
                        Debug.LogError("Couldn't get type for pointer " + type + " of value " + value);
                    else if (type.Contains("UnityEngine.GameObject"))
                    {
                        if (value.Trim() == "null")
                            original.GetType().GetField(field).SetValue(original, null);
                        else if (!allTransforms.ContainsKey(value.Trim()))
                            Debug.LogError("Couldn't resolve tf " + value.Trim());
                        else
                            original.GetType().GetField(field).SetValue(original, allTransforms[value.Trim()].gameObject);
                    }
                    else
                    {
                        if (original.GetType().GetField(field) == null)
                            Debug.LogError("Couldn't resolve field " + field.Trim() + " on tf " + value.Trim());
                        else
                        {
                            if (value.Trim() == "null")
                                original.GetType().GetField(field).SetValue(original, null);
                            else
                            {
                                try
                                {
                                    if (allTransforms[value.Trim()].gameObject.GetComponent(newType) != null)
                                        original.GetType().GetField(field).SetValue(original, allTransforms[value.Trim()].gameObject.GetComponent(newType));
                                }
                                catch (KeyNotFoundException e)
                                {
                                    Debug.LogError("Couldn't get value " + value.Trim() + " on field " + field);
                                }
                            }
                            //Debug.Log("Did pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original.name);
                        }
                    }
                }
                else if (instruction == InstructionType.events)
                {
                    // DEEP VEIN THROMBOSIS (dvt) IS WHAT I'LL HAVE AFTER DOING THIS
                    //Debug.Log("Try do event, value is " + value + " and type is " + type + " and field is " + field);
                }
                else if (instruction == InstructionType.array)
                {
                    // This one was fun :) no it wasn't i wrote the comment when it was 95% done i am having a mental breakdown rn
                    Array newArray = ResizeArray((Array)original.GetType().GetField(field).GetValue(original), size);
                    for (int i = 0; i < subInstructions.Count; i++)
                        newArray.SetValue(subInstructions[i].GetArrayValue(allTransforms), i);
                    original.GetType().GetField(field).SetValue(original, newArray);
                }
                else if (instruction == InstructionType.qsComponent)
                {
                    //Debug.Log("Running qsComponent instruction.");
                    List<VTOLQuickStart.QuickStartComponents.QSEngine> qsEngines = new List<VTOLQuickStart.QuickStartComponents.QSEngine>();
                    foreach (Instruction instruction in subInstructions)
                        qsEngines.Add((VTOLQuickStart.QuickStartComponents.QSEngine)instruction.GetArrayValue(allTransforms));
                    if (type == "quickStartComponents")
                        ((VTOLQuickStart)original).quickStartComponents.engines = qsEngines.ToArray();
                    else if (type == "quickStopComponents")
                        ((VTOLQuickStart)original).quickStopComponents.engines = qsEngines.ToArray();
                }
                else if (instruction == InstructionType.newObject)
                {
                    object newObject = null;
                    if (type.Contains("PlayerVehicle"))
                        return;
                    List<Type> ctorTypes = new List<Type>();
                    foreach (string type in allTypes)
                    {
                        ctorTypes.Add(Type.GetType(type));
                    }
                    List<object> ctorObj = new List<object>();
                    foreach (Type newType in ctorTypes)
                    {
                        if (log)
                            Debug.Log("adding type " + newType.Name);
                        if (newType.IsValueType)
                            ctorObj.Add(Activator.CreateInstance(newType));
                        else
                            ctorObj.Add(null);
                    }
                    newObject = Type.GetType(type).GetConstructor(ctorTypes.ToArray()).Invoke(ctorObj.ToArray());
                    foreach (Instruction instruction in subInstructions)
                    {
                        instruction.Run(newObject, allTransforms, false);
                    }
                    original.GetType().GetField(field).SetValue(original, newObject);
                }
            }

            private object GetArrayValue(Dictionary<string, Transform> allTransforms) // this is a shit implementation
            {
                if (instruction == InstructionType.pointer)
                {
                    try
                    {
                        if (!allTransforms[value.Trim()])
                        {
                            Debug.LogError("Couldn't find transform for sub pointer instruction " + value.Trim());
                            return null;
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        Debug.LogError("Couldn't find transform for sub pointer instruction of type " + type + " and value " + value.Trim());
                        return null;
                    }
                    //Debug.Log("Try do pointer sub instruction, value is " + value + " and type is " + type + " and field is " + field);
                    if (type.Contains("GameObject"))
                    {
                        //Debug.Log("Type contained GameObject");
                        return allTransforms[value.Trim()].gameObject;
                    }
                    Type newType = Type.GetType(type.Trim());
                    if (newType == null)
                    {
                        Debug.LogError("Type was null for type " + type);
                        return null;
                    }
                    return allTransforms[value.Trim()].gameObject.GetComponent(newType);
                }
                else if (instruction == InstructionType.assign)
                {
                    //Debug.Log("Try do assign sub instruction, value is " + value + " and type is " + type + " and field is " + field);
                    switch (type)
                    {
                        case "String":
                            return value;
                        case "Boolean":
                            //Debug.Log("Setting bool for " + field + " and value " + value + " original " + original.GetType());
                            return value.Trim() == "True";
                        case "Single":
                            //Debug.Log("Setting float for " + field + " and value " + value);
                            return float.Parse(value);
                        case "Int32":
                            //Debug.Log("Setting int for " + field + " and value " + value);
                            return Int32.Parse(value);
                        default:
                            Debug.LogWarning("default: couldn't resolve object for sub array " + type);
                            return null;
                    }
                }
                else if (instruction == InstructionType.events) // enums can be whatever the fuck i want :P
                {
                    // array code for weird classes at some point :(
                }
                else if (instruction == InstructionType.qsComponent)
                {
                    //Debug.Log("Returning array value for qsComponent");
                    try
                    {
                        VTOLQuickStart.QuickStartComponents.QSEngine qsEngine = new VTOLQuickStart.QuickStartComponents.QSEngine();
                        qsEngine.engine = allTransforms[value.Trim()].GetComponent<ModuleEngine>();
                        qsEngine.state = int.Parse(type.Trim());
                        return qsEngine;
                    }
                    catch (KeyNotFoundException e)
                    {
                        Debug.LogError("Couldn't find value " + value + " for qsEngine");
                        return null;
                    }
                }
                Debug.LogWarning("Couldn't resolve type for sub array.");
                return null;
            }

            public static System.Array ResizeArray(System.Array oldArray, int newSize) // from https://stackoverflow.com/a/4840857
            {
                System.Type elementType = oldArray.GetType().GetElementType();
                System.Array newArray = System.Array.CreateInstance(elementType, newSize);
                return newArray;
            }

            public enum InstructionType
            {
                ignore = 0,
                assign = 1,
                pointer = 2,
                events = 4,
                array = 8,
                qsComponent = 16,
                newObject = 32
            }
        }
    }
}