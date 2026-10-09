using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class TransformInspector
{
    static void Main(string[] args)
    {
        string scenePath = @"Assets/Scenes/Chapter3_MieuChanTinh.unity";
        string[] lines = File.ReadAllLines(scenePath);

        Dictionary<string, string> goNames = new Dictionary<string, string>();
        Dictionary<string, string> transformToGO = new Dictionary<string, string>();
        Dictionary<string, string> transformPos = new Dictionary<string, string>();

        string currentHeader = "";
        string currentFileID = "";
        string currentName = "";
        string currentPosX = "0", currentPosY = "0", currentPosZ = "0";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("--- !u!"))
            {
                if (currentHeader == "4" && !string.IsNullOrEmpty(currentFileID))
                {
                    transformPos[currentFileID] = string.Format("({0}, {1}, {2})", currentPosX, currentPosY, currentPosZ);
                }

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
                    currentName = line.Substring(line.IndexOf(':') + 1).Trim();
                    goNames[currentFileID] = currentName;
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
                else if (line.Trim().StartsWith("m_LocalPosition:"))
                {
                    // Parse m_LocalPosition: {x: 0, y: 0, z: 0}
                    var match = Regex.Match(line, @"x:\s*([-\d\.]+),\s*y:\s*([-\d\.]+),\s*z:\s*([-\d\.]+)");
                    if (match.Success)
                    {
                        currentPosX = match.Groups[1].Value;
                        currentPosY = match.Groups[2].Value;
                        currentPosZ = match.Groups[3].Value;
                    }
                }
            }
        }
        if (currentHeader == "4" && !string.IsNullOrEmpty(currentFileID))
        {
            transformPos[currentFileID] = string.Format("({0}, {1}, {2})", currentPosX, currentPosY, currentPosZ);
        }

        foreach (var kvp in transformToGO)
        {
            string transformID = kvp.Key;
            string goID = kvp.Value;
            string name = goNames.ContainsKey(goID) ? goNames[goID] : "Unnamed";
            string pos = transformPos.ContainsKey(transformID) ? transformPos[transformID] : "(0, 0, 0)";
            Console.WriteLine(string.Format("GO: {0,-25} Position: {1}", name, pos));
        }
    }
}
