using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class SceneInspector
{
    static void Main(string[] args)
    {
        string scenePath = @"Assets/Scenes/Chapter3_MieuChanTinh.unity";
        if (!File.Exists(scenePath))
        {
            Console.WriteLine("Scene file not found!");
            return;
        }

        string[] lines = File.ReadAllLines(scenePath);
        Console.WriteLine(string.Format("Read {0} lines from scene.", lines.Length));

        Dictionary<string, string> goNames = new Dictionary<string, string>();
        Dictionary<string, string> transformToGO = new Dictionary<string, string>();
        Dictionary<string, string> transformParent = new Dictionary<string, string>();

        string currentHeader = "";
        string currentFileID = "";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("--- !u!"))
            {
                var match = Regex.Match(line, @"--- !u!(\d+) &(\d+)");
                if (match.Success)
                {
                    currentHeader = match.Groups[1].Value;
                    currentFileID = match.Groups[2].Value;
                }
            }
            else if (currentHeader == "1") // GameObject
            {
                if (line.Trim().StartsWith("m_Name:"))
                {
                    string name = line.Substring(line.IndexOf(':') + 1).Trim();
                    goNames[currentFileID] = name;
                }
            }
            else if (currentHeader == "4") // Transform
            {
                if (line.Trim().StartsWith("m_GameObject:"))
                {
                    var match = Regex.Match(line, @"fileID:\s*(\d+)");
                    if (match.Success)
                    {
                        transformToGO[currentFileID] = match.Groups[1].Value;
                    }
                }
                else if (line.Trim().StartsWith("m_Father:"))
                {
                    var match = Regex.Match(line, @"fileID:\s*(\d+)");
                    if (match.Success)
                    {
                        transformParent[currentFileID] = match.Groups[1].Value;
                    }
                }
            }
        }

        Console.WriteLine(string.Format("Found {0} GameObjects and {1} Transforms.", goNames.Count, transformToGO.Count));

        string caveAreaGOID = null;
        foreach (var kvp in goNames)
        {
            if (kvp.Value == "ChanTinhCaveArea")
            {
                caveAreaGOID = kvp.Key;
                break;
            }
        }

        if (caveAreaGOID != null)
        {
            Console.WriteLine(string.Format("ChanTinhCaveArea GO ID: {0}", caveAreaGOID));
            string caveTransformID = null;
            foreach (var kvp in transformToGO)
            {
                if (kvp.Value == caveAreaGOID)
                {
                    caveTransformID = kvp.Key;
                    break;
                }
            }

            if (caveTransformID != null)
            {
                Console.WriteLine(string.Format("ChanTinhCaveArea Transform ID: {0}", caveTransformID));
                PrintTree(caveTransformID, transformToGO, transformParent, goNames, lines, 0);
            }
        }
        else
        {
            Console.WriteLine("ChanTinhCaveArea not found in scene!");
        }
    }

    static void PrintTree(string currentTransformID, Dictionary<string, string> transformToGO, Dictionary<string, string> transformParent, Dictionary<string, string> goNames, string[] lines, int indent)
    {
        string goID = transformToGO.ContainsKey(currentTransformID) ? transformToGO[currentTransformID] : "?";
        string name = goNames.ContainsKey(goID) ? goNames[goID] : "Unnamed";
        Console.WriteLine(new string(' ', indent * 2) + string.Format("- {0} (Transform {1}, GO {2})", name, currentTransformID, goID));

        foreach (var kvp in transformParent)
        {
            if (kvp.Value == currentTransformID)
            {
                PrintTree(kvp.Key, transformToGO, transformParent, goNames, lines, indent + 1);
            }
        }
    }
}
