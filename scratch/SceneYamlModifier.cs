using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class SceneYamlModifier
{
    static void Main(string[] args)
    {
        string scenePath = @"Assets/Scenes/Chapter3_MieuChanTinh.unity";
        if (!File.Exists(scenePath))
        {
            Console.WriteLine("Scene file not found: " + scenePath);
            return;
        }

        string content = File.ReadAllText(scenePath);
        Console.WriteLine("Original scene size: " + content.Length + " bytes");

        // Inject/update nodes for full cave structure & lights
        content = InjectHierarchyNodes(content);
        File.WriteAllText(scenePath, content);
        Console.WriteLine("Successfully updated scene YAML! New scene size: " + content.Length + " bytes");
    }

    static string InjectHierarchyNodes(string sceneYaml)
    {
        long nextId = 992000000;

        long idCaveStructureGO = 991000000; long idCaveStructureTr = 991000001;
        long idCaveFloorGO = 991000002; long idCaveFloorTr = 991000003;
        long idCaveWallsGO = 991000004; long idCaveWallsTr = 991000005;
        long idCaveCeilingGO = 991000006; long idCaveCeilingTr = 991000007;
        long idCaveRocksGO = 991000008; long idCaveRocksTr = 991000009;

        long idCaveDecoGO = 991000010; long idCaveDecoTr = 991000011;
        long idTorchesGO = 991000012; long idTorchesTr = 991000013;
        long idStonePropsGO = 991000014; long idStonePropsTr = 991000015;

        long idCaveLightGO = 991000016; long idCaveLightTr = 991000017;
        long idTorchLightsGO = 991000018; long idTorchLightsTr = 991000019;
        long idCaveAmbLightsGO = 991000020; long idCaveAmbLightsTr = 991000021;

        long idExitTriggerGO = 991000022; long idExitTriggerTr = 991000023;
        long idExitBoxCol = 991000024; long idExitFadeScript = 991000025;

        long idOutsideSpawnGO = 991000026; long idOutsideSpawnTr = 991000027;

        long idLightingCtrlGO = 991000028; long idLightingCtrlTr = 991000029;
        long idLightingCtrlScript = 991000030;

        // If CaveLightingController does not exist in scene YAML, add it
        if (!sceneYaml.Contains("m_Name: CaveLightingController"))
        {
            StringWriter sw = new StringWriter();

            // 1. CaveLightingController
            WriteGOWithComponents(sw, idLightingCtrlGO, "CaveLightingController", new long[] { idLightingCtrlTr, idLightingCtrlScript });
            WriteTransform(sw, idLightingCtrlTr, idLightingCtrlGO, 0, new long[0], "0, 0, 0");

            // MonoBehavior for CaveLightingController
            sw.WriteLine(string.Format("--- !u!114 &{0}", idLightingCtrlScript));
            sw.WriteLine("MonoBehaviour:");
            sw.WriteLine("  m_ObjectHideFlags: 0");
            sw.WriteLine("  m_CorrespondingSourceObject: {fileID: 0}");
            sw.WriteLine("  m_PrefabInstance: {fileID: 0}");
            sw.WriteLine("  m_PrefabAsset: {fileID: 0}");
            sw.WriteLine(string.Format("  m_GameObject: {{fileID: {0}}}", idLightingCtrlGO));
            sw.WriteLine("  m_Enabled: 1");
            sw.WriteLine("  m_EditorHideFlags: 0");
            sw.WriteLine("  m_Script: {fileID: 11500000, guid: c0000000000000000000000000000001, type: 3}");
            sw.WriteLine("  m_Name: ");
            sw.WriteLine("  m_EditorClassIdentifier: ");
            sw.WriteLine("  sunLight: {fileID: 894264107}");
            sw.WriteLine("  outdoorSunIntensity: 0.65");
            sw.WriteLine("  outdoorSunColor: {r: 1, g: 0.75, b: 0.45, a: 1}");
            sw.WriteLine("  caveSunIntensity: 0.0");
            sw.WriteLine("  caveSunColor: {r: 0.05, g: 0.08, b: 0.15, a: 1}");
            sw.WriteLine("  caveAmbientColor: {r: 0.008, g: 0.012, b: 0.025, a: 1}");
            sw.WriteLine("  caveCameraBackgroundColor: {r: 0.005, g: 0.01, b: 0.02, a: 1}");
            sw.WriteLine("  enableCaveFog: 1");
            sw.WriteLine("  caveFogColor: {r: 0.01, g: 0.03, b: 0.06, a: 1}");
            sw.WriteLine("  caveFogDensity: 0.04");

            sceneYaml += "\n" + sw.ToString();
        }

        return sceneYaml;
    }

    static void WriteGOWithComponents(StringWriter sw, long id, string name, long[] componentIds)
    {
        sw.WriteLine(string.Format("--- !u!1 &{0}", id));
        sw.WriteLine("GameObject:");
        sw.WriteLine("  m_ObjectHideFlags: 0");
        sw.WriteLine("  m_CorrespondingSourceObject: {fileID: 0}");
        sw.WriteLine("  m_PrefabInstance: {fileID: 0}");
        sw.WriteLine("  m_PrefabAsset: {fileID: 0}");
        sw.WriteLine("  serializedVersion: 6");
        sw.WriteLine("  m_Component:");
        foreach (long cid in componentIds)
        {
            sw.WriteLine(string.Format("  - component: {{fileID: {0}}}", cid));
        }
        sw.WriteLine("  m_Layer: 0");
        sw.WriteLine(string.Format("  m_Name: {0}", name));
        sw.WriteLine("  m_TagString: Untagged");
        sw.WriteLine("  m_Icon: {fileID: 0}");
        sw.WriteLine("  m_NavMeshLayer: 0");
        sw.WriteLine("  m_StaticEditorFlags: 0");
        sw.WriteLine("  m_IsActive: 1");
    }

    static void WriteTransform(StringWriter sw, long trId, long goId, long fatherTrId, long[] childrenTrIds, string posStr)
    {
        string[] pos = posStr.Split(',');
        sw.WriteLine(string.Format("--- !u!4 &{0}", trId));
        sw.WriteLine("Transform:");
        sw.WriteLine("  m_ObjectHideFlags: 0");
        sw.WriteLine("  m_CorrespondingSourceObject: {fileID: 0}");
        sw.WriteLine("  m_PrefabInstance: {fileID: 0}");
        sw.WriteLine("  m_PrefabAsset: {fileID: 0}");
        sw.WriteLine(string.Format("  m_GameObject: {{fileID: {0}}}", goId));
        sw.WriteLine("  serializedVersion: 2");
        sw.WriteLine("  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}");
        sw.WriteLine(string.Format("  m_LocalPosition: {{x: {0}, y: {1}, z: {2}}}", pos[0].Trim(), pos[1].Trim(), pos[2].Trim()));
        sw.WriteLine("  m_LocalScale: {x: 1, y: 1, z: 1}");
        sw.WriteLine("  m_ConstrainProportionsScale: 0");
        sw.WriteLine("  m_Children:");
        foreach (long cid in childrenTrIds)
        {
            sw.WriteLine(string.Format("  - {{fileID: {0}}}", cid));
        }
        sw.WriteLine(string.Format("  m_Father: {{fileID: {0}}}", fatherTrId));
        sw.WriteLine("  m_RootOrder: 0");
        sw.WriteLine("  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}");
    }
}
