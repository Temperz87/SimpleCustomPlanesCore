using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.IO;
using UnityEngine.Events;
using System.Text;
using System.Globalization;

public class SimpleCustomPlane : MonoBehaviour
{
    public void LoadOntoThisPlane(string path, GameObject prefab)
    {
        GameObject newPrefab = Instantiate(prefab, transform);
        newPrefab.SetActive(true);
        Load(path);
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
        if (verboseLogs)
            Debug.Log("transform was hashed as hashed " + transform.name);
        return transform.name;
    }

    public void Load(string path)
    {
        List<string> allNames = new List<string>();
        Dictionary<string, string> hashedToOriginal = new Dictionary<string, string>();
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            string originalName = tf.name;
            allTfs.Add(HashTransform(tf, allNames), tf);
            hashedToOriginal.Add(tf.name, originalName);
        }
        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> allInstructions = SCPTextReader.GetLinesFromFile(path);
        foreach (string line in allInstructions.Keys) // we must hash them first so we actually get their names
        {
            Dictionary<string, List<SCPTextReader.Instruction>> currentObject = allInstructions[line];

            if (line.IndexOf(" ") <= 0)
            {
                Debug.LogWarning("Got an index less than 0 for space.");
                continue;
            }

            if (!allTfs.ContainsKey(NextElement(line, 0)))
            {
                Debug.LogWarning("Couldn't resolve object " + NextElement(line, 0));
                continue;
            }

            Transform currentTf = allTfs[NextElement(line, 0)];
            Transform currentParent = null;
            Transform newParent = null;

            string finderString = NextElement(line, 1);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " on " + currentTf.name);
                currentParent = allTfs[finderString];
            }
            finderString = NextElement(line, 2);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " again on " + currentTf.name);
                newParent = allTfs[finderString];
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
                if (verboseLogs)
                    Debug.Log("Try do object " + currentTf.name + " for component " + component.Trim());
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
                if (verboseLogs)
                    Debug.Log("dict    " + type.AssemblyQualifiedName + " has a length of " + instructions.Count + " on " + currentTf.name);
                foreach (SCPTextReader.Instruction instruction in instructions)
                {
                    if (instruction == null)
                        Debug.LogError("How the fuck was an instruction null?");
                    else
                        instruction.Run(currentComponent, allTfs);
                }
            }
        }
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            if (tf.name.Contains("(Clone)"))
                tf.name = tf.name.Substring(0, tf.name.IndexOf("(Clone)"));
            if (verboseLogs)
                Debug.Log("tf name was " + tf.name);
            tf.name = hashedToOriginal[tf.name];
            if (verboseLogs)
                Debug.Log("tf name is now " + tf.name);
        }
        Debug.Log("Loaded from file!");
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

    public bool verboseLogs = false;


    private Dictionary<string, Transform> allTfs = new Dictionary<string, Transform>();

    public class SCPTextReader
    {
        public static Dictionary<string, Dictionary<string, List<Instruction>>> GetLinesFromFile(string path)
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
            private List<Instruction> arrayInstructions = new List<Instruction>();
            private int size = 0;

            public InstructionType instruction;

            public void AddInstruction(string line)
            {
                line = line.Trim();
                arrayInstructions.Add(new Instruction(line, true));
                size++;
            }

            public Instruction(string newLine, bool isSubInstruction) // you can pass in anything for the second arg, i'm just overloading here
            {
                if (newLine[0] == 'a' && newLine[1] == 's')
                {
                    instruction = InstructionType.assign;
                    value = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'p')
                {
                    instruction = InstructionType.pointer;
                    newLine = newLine.Substring(8);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    value = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'n')
                {
                    instruction = InstructionType.events;
                    newLine = newLine.Substring(4);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    type = NextElement(newLine, 0);
                }
                else if (newLine[0] == 'q')
                {
                    instruction = InstructionType.qsComponent;
                    value = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
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
            }

            public void Run(Component original, Dictionary<string, Transform> allTransforms)
            {
                //Debug.Log("Running " + instruction);
                if (instruction == InstructionType.assign)
                {
                    //Debug.Log("Try do assign instruction, value is " + value + " and type is " + type + " and field is " + field);
                    FieldInfo fInfo = original.GetType().GetField(field.Trim());
                    if (fInfo == null && !(original is Renderer))
                    {
                        Debug.LogError("info was null on object " + original.name + " while trying to find field " + field + " of type " + type);
                        return;
                    }
                    switch (type.Trim())
                    {
                        case "String":
                            //Debug.Log("Setting string for " + field + " and value " + value);
                            fInfo.SetValue(original, value);
                            //Debug.Log("Set string for " + field + " and value " + value);
                            break;
                        case "Boolean":
                            //Debug.Log("Setting bool for " + field + " and value " + value + " original " + original.GetType());
                            if (original is Renderer)
                                ((Renderer)original).enabled = value.Trim() == "True";
                            else
                                fInfo.SetValue(original, value.Trim() == "True");
                            break;
                        case "Single":
                            //Debug.Log("Setting float for " + field + " and value " + value);
                            fInfo.SetValue(original, float.Parse(value));
                            break;
                        case "Int32":
                            Debug.Log("Setting int for " + field + " and value " + value);
                            fInfo.SetValue(original, Int32.Parse(value));
                            break;
                        default:
                            if (original.GetType().GetField(field) != null && original.GetType().GetField(field).FieldType.IsSubclassOf(typeof(Enum)))
                            {
                                //Debug.Log("Setting enum for " + field + " and value " + value);
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
                    //Debug.Log("Try do pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original.name);
                    Type newType = Type.GetType(type.Trim());
                    if (newType == null)
                    {
                        Debug.LogError("Couldn't get type for pointer " + type + " of value " + value);
                        return;
                    }
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
                            Debug.LogError("Couldn't resolve field " + field.Trim() + " on tf " + value.Trim() + " for " + original.name);
                        else if (value.Trim() == "null")
                            original.GetType().GetField(field).SetValue(original, null);
                        else if (!allTransforms.ContainsKey(value.Trim()))
                            Debug.LogError("Couldn't resolve tf " + value.Trim());
                        else
                        {
                            if (original.GetType().GetField(field) == null)
                                Debug.LogError("Couldn't resolve field " + field.Trim() + " on tf " + value.Trim() + " for " + original.name);
                            else
                                original.GetType().GetField(field).SetValue(original, allTransforms[value.Trim()].gameObject.GetComponent(newType));
                            Debug.Log("Did pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original.name);
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
                    //Debug.Log("Try do array instruction, original is " + original.name + " and type is " + type + " and field is " + field);
                    FieldInfo fInfo = original.GetType().GetField(field);
                    if (fInfo == null)
                    {
                        Debug.LogError("info was null on object " + original.name + " while trying to find field " + field + " of type " + type);
                        return;
                    }
                    Array newArray = ResizeArray((Array)fInfo.GetValue(original), size);
                    for (int i = 0; i < arrayInstructions.Count; i++)
                        newArray.SetValue(arrayInstructions[i].GetArrayValue(allTransforms), i);
                    original.GetType().GetField(field).SetValue(original, newArray);
                }
                else if (instruction == InstructionType.qsComponent)
                {
                    //Debug.Log("Running qsComponent instruction.");
                    List<VTOLQuickStart.QuickStartComponents.QSEngine> qsEngines = new List<VTOLQuickStart.QuickStartComponents.QSEngine>();
                    Debug.Log("new list.");
                    //EngineEffects.EngineAudioFX originalSound = ((VTOLQuickStart)original).quickStartComponents.engines[0].engine.engineEffects.audioEffects[0];
                    Debug.Log("original sound.");
                    foreach (Instruction instruction in arrayInstructions)
                    {
                        VTOLQuickStart.QuickStartComponents.QSEngine qsEngine = (VTOLQuickStart.QuickStartComponents.QSEngine)instruction.GetArrayValue(allTransforms);
                        Debug.Log("adding new qsEngine, null is " + qsEngine == null);
                        qsEngines.Add(qsEngine);
                        Debug.Log("Continuing");
                        //if (originalSound != null && qsEngine.engine.engineEffects.audioEffects == null || qsEngine.engine.engineEffects.audioEffects.Length == 0)
                        //{
                        //    Debug.Log("new");
                        //    EngineEffects.EngineAudioFX newSound = new EngineEffects.EngineAudioFX();
                        //    Debug.Log("volume");
                        //    newSound.volumeCurve = originalSound.volumeCurve;
                        //    Debug.Log("pitch");
                        //    newSound.pitchCurve = originalSound.pitchCurve;
                        //    Debug.Log("audoi");
                        //    newSound.audioSource = GameObject.Instantiate(originalSound.audioSource.gameObject, qsEngine.engine.thrustTransform).GetComponent<AudioSource>();
                        //    Debug.Log("moar aduoi");
                        //    newSound.audioSource.transform.localPosition = Vector3.zero;
                        //    Debug.Log("qsengine");
                        //    qsEngine.engine.engineEffects.audioEffects = new EngineEffects.EngineAudioFX[] { newSound };
                        //    Debug.Log("goodie");
                        //}
                        Debug.Log("doan");
                    }
                    if (type == "quickStartComponents")
                        ((VTOLQuickStart)original).quickStartComponents.engines = qsEngines.ToArray();
                    else if (type == "quickStopComponents")
                        ((VTOLQuickStart)original).quickStopComponents.engines = qsEngines.ToArray();
                }
            }
            private object GetArrayValue(Dictionary<string, Transform> allTransforms) // this is a shit implementation
            {
                if (instruction == InstructionType.pointer)
                {
                    //Debug.Log("Try do pointer sub instruction, value is " + value + " and type is " + type + " and field is " + field);
                    if (type.Contains("GameObject"))
                    {
                        Debug.Log("Type contained GameObject");
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
                            Debug.Log("Setting bool for " + field + " and value " + value);
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
                    VTOLQuickStart.QuickStartComponents.QSEngine qsEngine = new VTOLQuickStart.QuickStartComponents.QSEngine();
                    //Debug.Log("try get engine");
                    qsEngine.engine = allTransforms[value.Trim()].GetComponent<ModuleEngine>();
                    //Debug.Log("try set state");
                    qsEngine.state = int.Parse(type.Trim());
                    //Debug.Log("returning qsComponent");
                    return qsEngine;
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
                qsComponent = 16
            }
        }
    }
}
