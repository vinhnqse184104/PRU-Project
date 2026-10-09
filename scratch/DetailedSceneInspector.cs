using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class DetailedSceneInspector
{
    static void Main(string[] args)
    {
        string scenePath = @"Assets/Scenes/Chapter3_MieuChanTinh.unity";
        string[] lines = File.ReadAllLines(scenePath);

        Dictionary<string, string> goNames = new Dictionary<string, string>();
        Dictionary<string, string> transformToGO = new Dictionary<string, string>();
        Dictionary<string, string> transformParent = new Dictionary<string, string>();
        Dictionary<string, List<string>> goComponents = new Dictionary<string, List<string>>();

        string currentHeader = "";
        string currentFileID = "";
        string currentGOID = "";

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

        foreach (var kvp in goNames)
        {
            Console.WriteLine(string.Format("GO ID {0}: {1}", kvp.Key, kvp.Value));
        }
    }
}
