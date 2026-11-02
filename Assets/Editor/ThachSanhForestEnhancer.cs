using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

// Editor-only, explicitly invoked. No runtime behaviour or shared source asset mutation.
public static class ThachSanhForestEnhancer
{
    const string Folder="Assets/ForestGameViewEnhancement";
    const string Output="Backups/Forest_GameView_Redesign_20261102/";
    static Terrain ground;
    static Transform group;
    static Material leaf,stem,moss;
    static readonly List<Vector3> vertices=new List<Vector3>();
    static readonly List<int> triangles=new List<int>();
    static readonly List<Vector3> protectedPositions=new List<Vector3>();
    static readonly List<string> notes=new List<string>();
    static Dictionary<Material,Material> copies=new Dictionary<Material,Material>();

    [MenuItem("Tools/Thach Sanh/Enhance Forest")]
    public static void Enhance()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/RungThachSanh.unity")throw new Exception("Open RungThachSanh in Edit mode first");
        var previous=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Forest_Environment_Enhancement");
        if(previous && previous.transform.Find("GameView_Redesign")){Debug.Log("Already enhanced; no duplicates added");Verify();return;}
        if(scene.isDirty)throw new Exception("Save existing scene edits first");
        protectedPositions.Clear();notes.Clear();
        Directory.CreateDirectory(Output);
        if(!File.Exists(Output+"RungThachSanh.before.unity.txt"))File.Copy(scene.path,Output+"RungThachSanh.before.unity.txt");
        var protectedBefore=ProtectedSnapshot();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Enhance Thach Sanh Forest");
        try {
        Directory.CreateDirectory(Folder);
        var roots=scene.GetRootGameObjects();
        ground=roots.SelectMany(x=>x.GetComponentsInChildren<Terrain>()).First();
        foreach(var r in roots.Where(x=>x.GetComponentInChildren<Animator>() || x.GetComponentInChildren<Camera>() || x.name.ToLower().Contains("lythong"))) protectedPositions.Add(r.transform.position);
        var cam=roots.Single(x=>x.name=="Main Camera").GetComponent<Camera>();
        Capture(cam,"before");
        var root=previous;
        if(!root){root=new GameObject("Forest_Environment_Enhancement");Undo.RegisterCreatedObjectUndo(root,"Forest environment root");}
        group=new GameObject("GameView_Redesign").transform;Undo.RegisterCreatedObjectUndo(group.gameObject,"Forest redesign");group.SetParent(root.transform,false);
        SaveState(roots);
        foreach(var original in Resources.FindObjectsOfTypeAll<RenderSettings>())Undo.RecordObject(original,"Forest ambience");
        foreach(var light in roots.SelectMany(x=>x.GetComponentsInChildren<Light>()))Undo.RecordObject(light,"Forest sunlight");
        Undo.RecordObject(ground,"Forest grass visuals");Undo.RecordObject(ground.GetComponent<TerrainCollider>(),"Terrain visual copy");
        group.gameObject.layer=2;
        var nav=group.gameObject.AddComponent<NavMeshModifier>();nav.ignoreFromBuild=true;nav.applyToChildren=true;
        leaf=Material("Fern_Jade",new Color(.22f,.39f,.085f));
        stem=Material("Bamboo_Olive",new Color(.32f,.40f,.11f));
        moss=Material("Moss_WarmGreen",new Color(.30f,.34f,.16f));
        var banyan=roots.Single(x=>x.name=="cayda");
        int n=0;
        foreach(var p in new[]{new Vector2(83,101),new Vector2(110,102),new Vector2(78,128),new Vector2(119,135)})
        {
            var t=Copy(banyan.transform,group);t.name="Ancient_Banyan_"+(++n);t.localScale=banyan.transform.localScale*.85f;t.rotation=Quaternion.Euler(0,n*67,0)*banyan.transform.rotation;Place(t,p,true);
        }
        var shrub=AssetDatabase.FindAssets("ShrubBush_B t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>x.EndsWith("/ShrubBush_B.prefab"));
        var shrubSource=AssetDatabase.LoadAssetAtPath<GameObject>(shrub);
        n=0;
        foreach(var p in new[]{new Vector2(86,98),new Vector2(107,99),new Vector2(84,108),new Vector2(110,111),new Vector2(80,125),new Vector2(117,127),new Vector2(84,140),new Vector2(115,147)})
        {var t=Copy(shrubSource.transform,group);t.name="Understory_Shrub_"+(++n);t.localScale=Vector3.one*.9f;t.rotation=Quaternion.Euler(0,n*53,0);Place(t,p,true);}
        n=0;
        foreach(var p in new[]{new Vector2(85,105),new Vector2(109,108),new Vector2(79,129),new Vector2(118,136)})
        {
            Begin();
            for(int j=0;j<7;j++) {float a=j*2.4f;var b=new Vector3(Mathf.Sin(a)*1.1f,0,Mathf.Cos(a)*1.1f);float h=5.8f+(j%3)*.7f;for(int k=0;k<8;k++) Tube(b+new Vector3(k*.025f,k*h/8,0),b+new Vector3((k+1)*.025f,(k+1)*h/8,0),k%2==0?.073f:.08f);}
            var t=MeshObject("Bamboo_Cluster_"+(++n),stem);Place(t,p,true);
            Begin();for(int j=0;j<7;j++)for(int k=0;k<6;k++){float a=j*2.4f+k*1.8f;var b=new Vector3(Mathf.Sin(j*2.4f),3.7f+k*.38f,Mathf.Cos(j*2.4f));Leaf(b,b+new Vector3(Mathf.Sin(a)*1.1f,.22f,Mathf.Cos(a)*1.1f),.105f);}
            Place(MeshObject("Bamboo_Leaves_"+n,leaf),p,false);
        }
        n=0;
        foreach(var p in new[]{new Vector2(87,97),new Vector2(106,98),new Vector2(85,101),new Vector2(109,104),new Vector2(83,111),new Vector2(112,113),new Vector2(81,123),new Vector2(114,127),new Vector2(79,135),new Vector2(118,140)})
        {
            Begin();for(int j=0;j<9;j++){float a=j*Mathf.PI*2/9;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));for(int k=1;k<9;k++){float f=k/9f;var b=d*f*.85f+Vector3.up*(Mathf.Sin(f*2.4f)*.6f);var side=Vector3.Cross(d,Vector3.up)*(.20f*(1-f)+.025f);Leaf(b,b+side+d*.10f,.032f);Leaf(b,b-side+d*.10f,.032f);}}
            Place(MeshObject("Fern_Clump_"+(++n),leaf),p,false);
        }
        n=0;
        foreach(var p in new[]{new Vector2(83,101),new Vector2(110,102),new Vector2(78,128),new Vector2(119,135)})
        {
            Begin();for(int j=0;j<3;j++)for(int k=0;k<14;k++){var a=new Vector3(.8f+j*.36f+Mathf.Sin(k*.5f)*.13f,2.2f+k*.25f,.5f);Tube(a,a+new Vector3(.04f,.25f,0),.018f);Leaf(a,a+new Vector3((k%2==0?1:-1)*.20f,.07f,.06f),.07f);}
            Place(MeshObject("Hanging_Vines_"+(++n),leaf),p,false);
        }
        var rockPath=AssetDatabase.FindAssets("rock2 t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>x.EndsWith("/rock2.prefab"));
        n=0;foreach(var p in new[]{new Vector2(86,100),new Vector2(108,102),new Vector2(82,119),new Vector2(113,122)})
        {var t=Copy(AssetDatabase.LoadAssetAtPath<GameObject>(rockPath).transform,group);t.name="Moss_Rock_"+(++n);t.localScale=Vector3.one*.65f;foreach(var r in t.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials){if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",new Color(.54f,.61f,.38f));if(m.HasProperty("_Color"))m.color=new Color(.54f,.61f,.38f);}Place(t,p,true);}
        // Scene-specific material copies, including existing scenic trees only.
        // Clone TerrainData: retain heightmap, splats, trees and all detail distribution maps.
        var data=UnityEngine.Object.Instantiate(ground.terrainData);data.name="RungThachSanh_VisualTerrain";
        var prototypes=data.treePrototypes;
        for(int pi=0;pi<prototypes.Length;pi++)prototypes[pi].prefab=VisualTerrainPrefab(prototypes[pi].prefab,pi);
        data.treePrototypes=prototypes;
        var details=data.detailPrototypes;
        foreach(var d in details){d.healthyColor=new Color(.38f,.53f,.20f);d.dryColor=new Color(.48f,.46f,.23f);}
        data.detailPrototypes=details;data.wavingGrassTint=new Color(.52f,.65f,.31f);
        var dataPath=Folder+"/RungThachSanh_VisualTerrain.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);if(existing){UnityEngine.Object.DestroyImmediate(data);data=existing;}else AssetDatabase.CreateAsset(data,dataPath);
        ground.terrainData=data;ground.GetComponent<TerrainCollider>().terrainData=data;
        AddExtraDetails();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.59f,.67f,.69f);RenderSettings.ambientEquatorColor=new Color(.39f,.46f,.29f);RenderSettings.ambientGroundColor=new Color(.25f,.21f,.14f);RenderSettings.ambientIntensity=1;
        foreach(var light in roots.SelectMany(x=>x.GetComponentsInChildren<Light>()).Where(x=>x.type==LightType.Directional)){light.color=new Color(1,.94f,.79f);light.intensity=1.18f;light.shadows=LightShadows.Soft;light.shadowStrength=.72f;light.shadowBias=.035f;light.shadowNormalBias=.25f;}
        RefineMaterials();
        AssetDatabase.SaveAssets();
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=185;RenderSettings.fogColor=new Color(.61f,.70f,.66f);
        foreach(var pair in protectedBefore)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Protected component changed: "+pair.Key);
        File.WriteAllText(Output+"protected-check.txt","Original Camera, Transform, scripts, Animator, gameplay colliders and UI unchanged: "+protectedBefore.Count+" components");
        RestoreOriginalUIAnchors();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");PreserveSavedUI();Undo.CollapseUndoOperations(undo);
        Capture(cam,"after_edit");
        File.WriteAllLines(Output+"placement.txt",notes);
        Debug.Log("FOREST_BUILD_OK "+Application.dataPath+" decorations="+group.childCount);
        } catch {Undo.RevertAllDownToGroup(undo);throw;}
    }
    static Material Material(string name,Color c){var existing=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");if(existing)return existing;var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.14f);m.SetFloat("_Cull",0);m.enableInstancing=true;AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
    static Material CloneMaterial(Material source)
    {
        if(!source)return leaf;if(copies.TryGetValue(source,out var cached))return cached;
        var key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));var path=Folder+"/Visual_"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=source.name+"_Forest";Texture texture=null;foreach(var p in new[]{"_BaseMap","_AlbedoMap","_AlbedoA","_MainTex"})if(source.HasProperty(p)&&source.GetTexture(p)){texture=source.GetTexture(p);break;}
        m.SetTexture("_BaseMap",texture);var name=source.name.ToLower();bool wood=name.Contains("bark"),rock=name.Contains("rock"),flower=name.Contains("flower");m.SetColor("_BaseColor",wood?new Color(.65f,.49f,.34f):rock?new Color(.57f,.65f,.40f):flower?new Color(.94f,.96f,.86f):new Color(.60f,.85f,.43f));m.SetFloat("_Smoothness",.12f);m.SetFloat("_Cull",0);if(!wood&&!rock){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.4f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;m.SetOverrideTag("RenderType","TransparentCutout");}m.enableInstancing=true;AssetDatabase.CreateAsset(m,path);}
        copies[source]=m;return m;
    }
    static Transform Copy(Transform s,Transform parent)
    {
        var t=new GameObject(s.name).transform;Undo.RegisterCreatedObjectUndo(t.gameObject,"Forest geometry");t.SetParent(parent,false);t.localPosition=s.localPosition;t.localRotation=s.localRotation;t.localScale=s.localScale;t.gameObject.layer=2;
        var f=s.GetComponent<MeshFilter>();var r=s.GetComponent<MeshRenderer>();
        if(f&&r&&!s.name.Contains("LOD1")&&!s.name.Contains("LOD2")&&!s.name.Contains("LOD3")){t.gameObject.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;var nr=t.gameObject.AddComponent<MeshRenderer>();nr.sharedMaterials=r.sharedMaterials.Select(CloneMaterial).ToArray();}
        foreach(Transform child in s)Copy(child,t);return t;
    }
    static void Place(Transform t,Vector2 p,bool seat)
    {
        t.position=new Vector3(p.x,ground.SampleHeight(new Vector3(p.x,0,p.y))+ground.transform.position.y,p.y);
        var rs=t.GetComponentsInChildren<MeshRenderer>();if(rs.Length==0){UnityEngine.Object.DestroyImmediate(t.gameObject);return;}
        var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        if(seat)t.position+=Vector3.up*(t.position.y-b.min.y-.06f);
        b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        // Clearing and actor margins exclude both tall geometry and ground cover.
        bool reject=protectedPositions.Any(x=>Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(x.x,x.z))<(t.name.StartsWith("Ancient")?5f:2.8f));
        if(reject){notes.Add("REJECT "+t.name+" "+p);UnityEngine.Object.DestroyImmediate(t.gameObject);return;}
        string prefabPath=Folder+"/"+t.name+".prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if(!prefab){var pos=t.position;var rot=t.rotation;var scale=t.localScale;t.SetParent(null,true);t.position=Vector3.zero;t.rotation=Quaternion.identity;t.localScale=Vector3.one;prefab=PrefabUtility.SaveAsPrefabAsset(t.gameObject,prefabPath);t.SetParent(group,false);t.position=pos;t.rotation=rot;t.localScale=scale;}
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(instance,"Forest prefab");instance.name=t.name;instance.transform.SetParent(group,false);instance.transform.position=t.position;instance.transform.rotation=t.rotation;instance.transform.localScale=t.localScale;Undo.DestroyObjectImmediate(t.gameObject);
        notes.Add("ADD "+instance.name+" position="+instance.transform.position+" bounds="+b+" prefab="+prefabPath);
    }
    static void Begin(){vertices.Clear();triangles.Clear();}
    static void Tri(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
    static void Leaf(Vector3 a,Vector3 b,float width){var s=Vector3.Cross((b-a).normalized,Vector3.up);if(s.sqrMagnitude<.01f)s=Vector3.right;s*=width;int i=vertices.Count;vertices.Add(a);vertices.Add((a+b)*.5f+s);vertices.Add(b);vertices.Add((a+b)*.5f-s);Tri(i,i+1,i+2);Tri(i,i+2,i+3);}
    static void Tube(Vector3 a,Vector3 b,float radius){var d=(b-a).normalized;var u=Vector3.Cross(d,Vector3.right).normalized;if(u.sqrMagnitude<.01f)u=Vector3.up;var v=Vector3.Cross(d,u);int start=vertices.Count;for(int j=0;j<2;j++)for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;vertices.Add((j==0?a:b)+radius*(Mathf.Cos(angle)*u+Mathf.Sin(angle)*v));}for(int k=0;k<6;k++){int next=(k+1)%6;Tri(start+k,start+next,start+k+6);Tri(start+next,start+next+6,start+k+6);}}
    static Transform MeshObject(string name,Material m){var mesh=new Mesh();mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var meshPath=Folder+"/"+name+".asset";var oldMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(oldMesh){UnityEngine.Object.DestroyImmediate(mesh);mesh=oldMesh;}else AssetDatabase.CreateAsset(mesh,meshPath);var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Forest plant");g.layer=2;g.transform.SetParent(group,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;return g.transform;}
    static void Capture(Camera cam,string name){ShaderUtil.allowAsyncCompilation=false;var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);rt.Create();var previous=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;for(int frame=0;frame<4;frame++)cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Output+name+".png",tex.EncodeToPNG());cam.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    public static void Verify()
    {
        var scene=SceneManager.GetActiveScene();var root=scene.GetRootGameObjects().Single(x=>x.name=="Forest_Environment_Enhancement").transform.Find("GameView_Redesign").gameObject;
        if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative collider found");
        if(root.GetComponentsInChildren<Component>(true).Any(x=>!x || !(x is Transform || x is MeshRenderer || x is MeshFilter || x is NavMeshModifier)))throw new Exception("Unexpected component");
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){if(!r.GetComponent<MeshFilter>().sharedMesh||r.sharedMaterials.Any(x=>!x||!x.shader.isSupported))throw new Exception("Invalid renderer "+r.name);}
        int missing=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject));
        var terrain=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Terrain>()).Single();
        var originalData=AssetDatabase.LoadAssetAtPath<ForestEnvironmentState>(Folder+"/OriginalEnvironment.asset").terrain;
        var visual=terrain.terrainData;
        if(terrain.GetComponent<TerrainCollider>().terrainData!=visual)throw new Exception("Terrain/collider data mismatch");
        if(originalData.size!=visual.size||originalData.heightmapResolution!=visual.heightmapResolution)throw new Exception("Terrain dimensions changed");
        var a=originalData.GetHeights(0,0,originalData.heightmapResolution,originalData.heightmapResolution);
        var b=visual.GetHeights(0,0,visual.heightmapResolution,visual.heightmapResolution);
        for(int i=0;i<a.GetLength(0);i++)for(int j=0;j<a.GetLength(1);j++)if(a[i,j]!=b[i,j])throw new Exception("Heightmap changed");
        if(originalData.treeInstanceCount!=visual.treeInstanceCount)throw new Exception("Existing terrain trees changed");
        if(!EditorApplication.isPlaying)PreserveSavedUI();
        Capture(scene.GetRootGameObjects().Single(x=>x.name=="Main Camera").GetComponent<Camera>(),"after_verified");
        File.WriteAllText(Output+"unity-validation.txt","Project: "+Application.dataPath+"\nScene loaded; missing scripts="+missing+"\nDecorations="+root.transform.childCount+" renderers="+root.GetComponentsInChildren<MeshRenderer>().Length+"\nMesh/material/shader references valid; no new colliders or runtime scripts.\nTerrain dimensions and every height sample unchanged; TerrainCollider shares cloned visual data.\nPlay/Game View smoke check documented separately in REPORT.md; full gameplay and FPS not tested.");
        Debug.Log("FOREST_VERIFY_OK");
    }
    static Dictionary<UnityEngine.Object,string> ProtectedSnapshot(){return SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>x.name!="Forest_Environment_Enhancement").SelectMany(x=>x.GetComponentsInChildren<Component>(true)).Where(x=>x&&!(x is Terrain)&&!(x is TerrainCollider)&&!(x is Light)).ToDictionary(x=>(UnityEngine.Object)x,x=>EditorJsonUtility.ToJson(x));}
    public static void PreserveSavedUI()
    {
        var original=File.ReadAllText(Output+"RungThachSanh.before.unity.txt");var path=SceneManager.GetActiveScene().path;var saved=File.ReadAllText(path);
        foreach(System.Text.RegularExpressions.Match block in System.Text.RegularExpressions.Regex.Matches(original,@"(?m)^--- !u!224 &\d+\r?\n[\s\S]*?(?=^--- !u!|\z)"))
        {
            var header=block.Value.Substring(0,block.Value.IndexOf('\n')).TrimEnd('\r');var pattern=@"(?m)^"+System.Text.RegularExpressions.Regex.Escape(header)+@"\r?\n[\s\S]*?(?=^--- !u!|\z)";
            saved=System.Text.RegularExpressions.Regex.Replace(saved,pattern,_=>block.Value);
        }
        File.WriteAllText(path,saved);
    }
    public static void RestoreOriginalUIAnchors()
    {
        // Existing edit-mode UI initialisation normalises an anchor on load. Retain the saved source value.
        var original=File.ReadAllText(Output+"RungThachSanh.before.unity.txt");
        foreach(var rect in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RectTransform>(true)))
        {
            var id=GlobalObjectId.GetGlobalObjectIdSlow(rect).targetObjectId;
            var block=System.Text.RegularExpressions.Regex.Match(original,@"(?m)^--- !u!224 &"+id+@"\r?\n[\s\S]*?(?=^--- !u!|\z)").Value;
            var match=System.Text.RegularExpressions.Regex.Match(block,@"m_AnchorMax: \{x: ([\d.\-]+), y: ([\d.\-]+)\}");if(!match.Success)continue;
            var max=new Vector2(float.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture),float.Parse(match.Groups[2].Value,System.Globalization.CultureInfo.InvariantCulture));if(rect.anchorMax!=max){Undo.RecordObject(rect,"Preserve original UI anchors");rect.anchorMax=max;}
        }
    }
    static void SaveState(GameObject[] roots)
    {
        if(AssetDatabase.LoadAssetAtPath<ForestEnvironmentState>(Folder+"/OriginalEnvironment.asset"))return;
        var s=ScriptableObject.CreateInstance<ForestEnvironmentState>();var sun=roots.SelectMany(x=>x.GetComponentsInChildren<Light>()).First(x=>x.type==LightType.Directional);s.terrain=ground.terrainData;s.collider=ground.GetComponent<TerrainCollider>().terrainData;s.sunColor=sun.color;s.sunIntensity=sun.intensity;s.shadowStrength=sun.shadowStrength;s.shadowBias=sun.shadowBias;s.shadowNormalBias=sun.shadowNormalBias;s.shadows=sun.shadows;s.sky=RenderSettings.ambientSkyColor;s.equator=RenderSettings.ambientEquatorColor;s.ground=RenderSettings.ambientGroundColor;s.ambientMode=RenderSettings.ambientMode;s.fog=RenderSettings.fog;s.fogColor=RenderSettings.fogColor;s.fogMode=RenderSettings.fogMode;s.fogStart=RenderSettings.fogStartDistance;s.fogEnd=RenderSettings.fogEndDistance;AssetDatabase.CreateAsset(s,Folder+"/OriginalEnvironment.asset");
    }
    static GameObject VisualTerrainPrefab(GameObject source,int index)
    {
        var path=Folder+"/TerrainVisual_"+index+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var obj=UnityEngine.Object.Instantiate(source);obj.name="TerrainVisual_"+index;foreach(var r in obj.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=r.sharedMaterials.Select(CloneMaterial).ToArray();var prefab=PrefabUtility.SaveAsPrefabAsset(obj,path);UnityEngine.Object.DestroyImmediate(obj);return prefab;
    }
    public static void RefineMaterials()
    {
        foreach(var path in AssetDatabase.FindAssets("t:Material",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("Visual_")))
        {
            var key=Path.GetFileNameWithoutExtension(path).Substring(7);var source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(key));var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!source)continue;
            var shader=source.shader.name;var name=source.name.ToLower();bool woody=name.Contains("bark"),rock=name.Contains("rock"),flower=name.Contains("flower");
            if(shader.Contains("TTFE")||shader.Contains("Universal Render Pipeline"))
            {
                EditorUtility.CopySerialized(source,m);m.name=source.name+"_Forest";m.enableInstancing=true;
                if(shader.Contains("TTFE")&&!woody&&!rock&&!flower)
                {
                    foreach(var property in new[]{"_AlbedoColor","_AlebedoColor","_AlbedoCOLOZ"})if(m.HasProperty(property))m.SetColor(property,new Color(.70f,.84f,.53f,1));
                    foreach(var property in new[]{"_DryLeafColor","_Dryleafcolor","_Dryleafcolor1","_Dryleafcolor2","_Dryleafcolor3","_Leadydry"})if(m.HasProperty(property))m.SetColor(property,new Color(.40f,.56f,.19f,1));
                }
                if(shader.Contains("Universal Render Pipeline")&&m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",new Color(.94f,1,.87f));
            }
            else
            {
                m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetColor("_BaseColor",new Color(.64f,.70f,.49f));m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);
            }
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();Debug.Log("FOREST_MATERIALS_REFINED");
    }
    static void AddExtraDetails()
    {
        int n=0;foreach(var entry in new[]{new[]{"GrassShort_A","Soft_Grass"},new[]{"FlowersWTTFE-B","Wildflowers"},new[]{"ShrubTree_C","Layered_Tree"}})
        {
            var path=AssetDatabase.FindAssets(entry[0]+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/"+entry[0]+".prefab")&&!p.Contains("(SL)"));var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var p in new[]{new Vector2(85,112),new Vector2(111,115),new Vector2(81,132),new Vector2(117,144),new Vector2(88,154),new Vector2(108,160)}){var t=Copy(source.transform,group);t.name=entry[1]+"_"+(++n);t.localScale=Vector3.one*(entry[0]=="ShrubTree_C"?1.3f:.8f);t.rotation=Quaternion.Euler(0,n*37,0);Place(t,p,true);}
        }
        Begin();Tube(new Vector3(-1.2f,.22f,0),new Vector3(1.2f,.22f,0),.22f);Tube(new Vector3(.4f,.22f,0),new Vector3(.7f,.6f,.15f),.06f);var a=MeshObject("Fallen_Log_1",Material("Weathered_Bark",new Color(.25f,.16f,.085f)));Place(a,new Vector2(85,106),false);
        Begin();Tube(new Vector3(-1f,.2f,0),new Vector3(1f,.2f,0),.2f);Place(MeshObject("Fallen_Log_2",AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Weathered_Bark.mat")),new Vector2(111,117),false);
    }
    [MenuItem("Tools/Thach Sanh/Remove Forest Enhancement")]
    public static void Remove()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/RungThachSanh.unity")return;var root=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Forest_Environment_Enhancement");var child=root?root.transform.Find("GameView_Redesign"):null;if(!child)return;
        var s=AssetDatabase.LoadAssetAtPath<ForestEnvironmentState>(Folder+"/OriginalEnvironment.asset");Undo.IncrementCurrentGroup();int u=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Remove Forest Enhancement");Undo.DestroyObjectImmediate(child.gameObject);var t=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Terrain>()).Single();Undo.RecordObject(t,"Restore terrain");Undo.RecordObject(t.GetComponent<TerrainCollider>(),"Restore terrain data");t.terrainData=s.terrain;t.GetComponent<TerrainCollider>().terrainData=s.collider;var sun=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Light>()).First(x=>x.type==LightType.Directional);Undo.RecordObject(sun,"Restore sunlight");sun.color=s.sunColor;sun.intensity=s.sunIntensity;sun.shadowStrength=s.shadowStrength;sun.shadowBias=s.shadowBias;sun.shadowNormalBias=s.shadowNormalBias;sun.shadows=s.shadows;foreach(var o in Resources.FindObjectsOfTypeAll<RenderSettings>())Undo.RecordObject(o,"Restore ambience");RenderSettings.ambientMode=s.ambientMode;RenderSettings.ambientSkyColor=s.sky;RenderSettings.ambientEquatorColor=s.equator;RenderSettings.ambientGroundColor=s.ground;RenderSettings.fog=s.fog;RenderSettings.fogColor=s.fogColor;RenderSettings.fogMode=s.fogMode;RenderSettings.fogStartDistance=s.fogStart;RenderSettings.fogEndDistance=s.fogEnd;Undo.CollapseUndoOperations(u);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PreserveSavedUI();
    }
}
