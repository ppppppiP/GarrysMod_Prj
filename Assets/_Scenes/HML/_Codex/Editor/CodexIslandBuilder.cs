using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public static class CodexIslandBuilder
{
    const string RootPath = "Assets/_Scenes/HML/_Codex";
    const string MatPath = RootPath + "/Generated/Materials";
    const string MeshPath = RootPath + "/Generated/Meshes";
    const string AllowedPrefabs = "Assets/_Scenes/HML/PREFAB/";
    static Transform root;
    static Material grass, grass2, sand, cliff, path, wood, darkWood, teal, cream, pink, leaf, trunk, stone, red, yellow;
    static readonly System.Random rng = new System.Random(240903);

    [MenuItem("Codex/Build Low Poly Island")]
    public static void Build()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != RootPath + "/test.unity")
        {
            Debug.LogError("CODEX: Open Assets/_Scenes/HML/_Codex/test.unity before building.");
            return;
        }

        var old = GameObject.Find("CODEX_ISLAND");
        if (old) Object.DestroyImmediate(old);
        var r = new GameObject("CODEX_ISLAND");
        root = r.transform;
        Undo.RegisterCreatedObjectUndo(r, "Build Codex Island");

        MakeMaterials();
        BuildLand();
        BuildArchitecture();
        BuildVegetation();
        BuildWaterAnchors();
        ConfigureScene();

        SetStaticRecursive(r);
        LogStats(r);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = r;
        Debug.Log("CODEX_ISLAND_BUILT: environment-only island created. User prefab assets were not modified.");
    }

    static void MakeMaterials()
    {
        grass = Mat("Grass", new Color(0.48f,0.72f,0.22f));
        grass2 = Mat("GrassLight", new Color(0.61f,0.80f,0.28f));
        sand = Mat("Sand", new Color(0.94f,0.80f,0.48f));
        cliff = Mat("Cliff", new Color(0.34f,0.32f,0.30f));
        path = Mat("Path", new Color(0.86f,0.72f,0.40f));
        wood = Mat("Wood", new Color(0.47f,0.29f,0.15f));
        darkWood = Mat("DarkWood", new Color(0.25f,0.14f,0.09f));
        teal = Mat("TealRoof", new Color(0.10f,0.53f,0.48f));
        cream = Mat("Cream", new Color(0.91f,0.82f,0.61f));
        pink = Mat("CherryPink", new Color(1.00f,0.48f,0.62f));
        leaf = Mat("TropicalLeaf", new Color(0.33f,0.65f,0.18f));
        trunk = Mat("Trunk", new Color(0.48f,0.29f,0.15f));
        stone = Mat("Stone", new Color(0.43f,0.43f,0.41f));
        red = Mat("Red", new Color(0.78f,0.18f,0.13f));
        yellow = Mat("Yellow", new Color(0.98f,0.76f,0.18f));
    }

    static Material Mat(string name, Color color)
    {
        string p = MatPath + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (!m)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (!sh) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, p);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void BuildLand()
    {
        var land = Group("01_LAND", root);
        Vector2[] main = P((-58,-42),(-35,-57),(-5,-62),(27,-58),(51,-45),(61,-20),(60,8),(53,31),(35,47),(8,52),(-16,48),(-38,56),(-56,38),(-64,12),(-62,-18));
        Extrude("Main_Island", Vector3.zero, main, 2f, -6f, grass, cliff, land, true);

        Vector2[] beach = P((-45,-43),(-25,-55),(5,-61),(31,-56),(50,-44),(53,-34),(38,-37),(17,-42),(-5,-44),(-28,-40));
        Extrude("South_Beach", Vector3.zero, beach, 2.18f, 1.9f, sand, sand, land, false);

        Vector2[] nw = P((-21,-14),(0,-18),(16,-8),(18,9),(9,18),(-8,21),(-20,12));
        Extrude("Waterfall_Mountain", new Vector3(-32,0,27), nw, 18f, 2f, grass2, cliff, land, true);

        Vector2[] east = P((-18,-10),(0,-16),(18,-9),(21,7),(12,16),(-5,18),(-20,8));
        Extrude("East_Plateau", new Vector3(37,0,25), east, 8f, 2f, grass2, cliff, land, true);

        Vector2[] west = P((-16,-12),(4,-15),(17,-4),(14,11),(-4,15),(-18,5));
        Extrude("West_Garden", new Vector3(-42,0,-8), west, 6f, 2f, grass, cliff, land, true);

        Ribbon("Main_Path", new[]{V(-37,2.22,-35),V(-20,2.22,-22),V(0,2.22,-15),V(18,2.22,-4),V(31,2.22,8)}, 5.2f, path, land);
        Ribbon("Beach_Path", new[]{V(-28,2.24,-40),V(-5,2.24,-42),V(18,2.24,-45),V(37,2.24,-39)}, 4.0f, path, land);
        Ribbon("North_Path", new[]{V(-8,2.23,15),V(7,2.23,24),V(20,2.23,33)}, 4.2f, path, land);
        Ribbon("Mountain_Path", new[]{V(-46,18.05,23),V(-35,18.05,31),V(-24,18.05,34)}, 3.6f, path, land);
        Ribbon("East_Path", new[]{V(26,8.05,21),V(38,8.05,27),V(48,8.05,32)}, 3.5f, path, land);

        var rim = Group("Cliff_Rocks", land);
        Vector3[] rimPts = {V(-55,2,-39),V(-38,2,-52),V(-17,2,-58),V(8,2,-60),V(32,2,-54),V(51,2,-41),V(59,2,-20),V(60,2,4),V(52,2,28),V(35,2,45),V(10,2,50),V(-14,2,47),V(-38,2,54),V(-56,2,36),V(-62,2,12),V(-61,2,-16)};
        string[] rocks = {"new Statick/Rocha_Um.prefab","new Statick/Rocha_Dois.prefab","new Statick/Rocha_Tres.prefab","new Statick/Rocha_Quatro.prefab"};
        for(int i=0;i<rimPts.Length;i++) Instance(rocks[i%rocks.Length], "RimRock_"+i, rimPts[i], 6.5f+(float)rng.NextDouble()*2f, rim, RandomYaw());
    }

    static void BuildArchitecture()
    {
        var a = Group("02_ARCHITECTURE", root);
        CreateCabin("Main_House",V(-8,2.25,8),new Vector3(12,8,10),a);
        CreateMarket("Market_Stall",V(31,2.25,-3),-35,a);
        CreateMarket("Beach_Hut",V(12,2.25,-45),-12,a);
        Instance("new Statick/wood most.prefab","Stream_Bridge",V(15,2.4,18),9f,a,50);
        CreateDock("Beach_Pier",V(40,1.6,-49),18f,-20,a);

        CreateCabin("Hill_Cabin",V(39,8.1,29),new Vector3(8,6,7),a);
        CreateCabin("Waterfall_Lodge",V(-22,2.2,22),new Vector3(11,8,9),a);
        CreateMarket("North_Stall",V(18,2.25,34),20,a);
        CreateCabin("West_Cottage",V(-39,2.2,5),new Vector3(8,6,7),a);
        CreateMarket("East_Fruit_Stall",V(46,2.25,5),65,a);
        CreateCabin("West_Cottage",V(-39,2.2,5),new Vector3(8,6,7),a);
        CreateMarket("East_Fruit_Stall",V(46,2.25,5),65,a);
        Cube("Waterfall_Cliff_L",V(-38,10,11),new Vector3(7,16,7),stone,a,new Vector3(0,12,4));
        Cube("Waterfall_Cliff_R",V(-26,10,11),new Vector3(7,16,7),stone,a,new Vector3(0,-8,-3));
        CreateDock("West_Dock",V(-51,2.0,-29),10f,35,a);

        var circle = Group("Central_Stone_Circle",a);
        for(int i=0;i<18;i++)
        {
            float ang=i*Mathf.PI*2/18f;
            Vector3 p=V(17+Mathf.Cos(ang)*13f,2.45f,-17+Mathf.Sin(ang)*10f);
            Sphere("CircleStone_"+i,p,new Vector3(2.2f,.55f,1.5f),stone,circle);
        }

        CreateUmbrella(V(-10,2.25,-49), red, a);
        CreateUmbrella(V(2,2.25,-52), teal, a);
        CreateUmbrella(V(16,2.25,-50), yellow, a);
        CreatePicnicMat(V(-11,2.21,-46),red,a);
        CreatePicnicMat(V(2,2.21,-49),teal,a);
        CreatePicnicMat(V(16,2.21,-47),yellow,a);
    }

    static void BuildVegetation()
    {
        var v = Group("03_VEGETATION", root);
        string[] trees={"new Statick/Arvore_Um_Clara.prefab","new Statick/Arvore_Dois_Clara.prefab","new Statick/Arvore_Tres_Escura.prefab","new Statick/Mangue_Arvore.prefab"};
        Vector3[] treePts={V(-48,2.2,-25),V(-38,2.2,-33),V(44,2.2,-20),V(51,2.2,4),V(18,2.2,40),V(28,2.2,37),V(-3,2.2,39),V(-51,2.2,12)};
        for(int i=0;i<treePts.Length;i++) Instance(trees[i%trees.Length],"Tree_"+i,treePts[i],9f+(i%3)*2f,v,RandomYaw());

        CreatePalm(V(-31,2.2,-43),9,v); CreatePalm(V(-22,2.2,-48),10,v);
        CreatePalm(V(29,2.2,-42),9,v); CreatePalm(V(44,2.2,-32),11,v);
        CreatePalm(V(48,8.1,23),11,v); CreatePalm(V(30,8.1,35),11,v);
        CreatePalm(V(-5,2.2,-37),12,v); CreatePalm(V(24,2.2,-36),13,v);
        CreatePalm(V(-5,2.2,-37),12,v); CreatePalm(V(24,2.2,-36),13,v);

        CreateCherry(V(-43,18.1,28),9,v); CreateCherry(V(-32,18.1,37),11,v);
        CreateCherry(V(-24,18.1,25),8,v); CreateCherry(V(-49,6.1,-6),7,v);

        string[] flora={"new Statick/Grama_Baixa.prefab","new Statick/Grama_Media.prefab","new Statick/Flor_Branca.prefab","new Statick/Flor_Amarela.prefab","new Statick/Flor_Roxa.prefab","new Statick/Arbusto_Pequeno.prefab"};
        for(int i=0;i<55;i++)
        {
            float x=-50+(float)rng.NextDouble()*100f, z=-38+(float)rng.NextDouble()*80f;
            if(z<-32 && x>-38) continue;
            Instance(flora[i%flora.Length],"Flora_"+i,V(x,2.22,z),0.8f+(float)rng.NextDouble()*1.1f,v,RandomYaw());
        }

        string[] bushes={"new Statick/Arbusto_Grande.prefab","new Statick/Arbusto_Grande_Escuro.prefab","new Statick/Arbusto_Pequeno_Escuro.prefab"};
        Vector3[] bp={V(-26,2.2,-15),V(-19,2.2,-11),V(24,2.2,8),V(31,2.2,13),V(47,2.2,10),V(-52,2.2,25)};
        for(int i=0;i<bp.Length;i++) Instance(bushes[i%3],"Bush_"+i,bp[i],3.2f,v,RandomYaw());
    }

    static void BuildWaterAnchors()
    {
        var w=Group("04_WATER_ANCHORS_FOR_USER",root);
        Anchor("Waterfall_Main",V(-32,10.0,11),new Vector3(12,16,1),w);
        Anchor("Upper_Pool",V(-32,18.2,29),new Vector3(18,.2f,15),w);
        Anchor("Stream_Center",V(13,2.15,18),new Vector3(5,.2f,28),w);
        Anchor("West_Pond",V(-42,6.1,-7),new Vector3(19,.2f,14),w);
        Anchor("Shoreline_Water_Level",V(0,0,-48),new Vector3(110,.2f,110),w);
    }

    static void ConfigureScene()
    {
        var cam=Camera.main;
        if(cam)
        {
            cam.transform.position=V(92,118,-110);
            cam.transform.LookAt(V(0,4,0));
            cam.orthographic=true; cam.orthographicSize=50; cam.nearClipPlane=.3f; cam.farClipPlane=400; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.18f,.53f,.68f);
        }
        var light=Object.FindFirstObjectByType<Light>();
        if(light){light.transform.rotation=Quaternion.Euler(52,-35,0);light.intensity=1.2f;light.color=new Color(1f,.94f,.82f);}
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.58f,.61f,.58f);
    }

    static void CreateCabin(string n, Vector3 p, Vector3 s, Transform parent)
    {
        var g=Group(n,parent); g.position=p;
        Cube("Body",V(0,s.y*.38f,0),new Vector3(s.x,s.y*.76f,s.z),cream,g);
        Cube("RoofA",V(-s.x*.22f,s.y*.86f,0),new Vector3(s.x*.58f,.45f,s.z*1.18f),teal,g,new Vector3(0,0,28));
        Cube("RoofB",V(s.x*.22f,s.y*.86f,0),new Vector3(s.x*.58f,.45f,s.z*1.18f),teal,g,new Vector3(0,0,-28));
        Cube("Door",V(0,s.y*.3f,-s.z*.505f),new Vector3(s.x*.22f,s.y*.55f,.18f),wood,g);
        Cube("Deck",V(0,.15f,-s.z*.7f),new Vector3(s.x*1.15f,.3f,s.z*.45f),wood,g);
    }

    static void CreateDock(string n,Vector3 p,float length,float yaw,Transform parent)
    {
        var g=Group(n,parent);g.position=p;g.rotation=Quaternion.Euler(0,yaw,0);
        for(int i=0;i<8;i++) Cube("Plank_"+i,V(0,.15f,(i-3.5f)*length/8f),new Vector3(4,.3f,length/8f-.08f),wood,g);
        for(int i=0;i<4;i++){Cube("PostL_"+i,V(-1.8f,-1.2f,(i-1.5f)*length/4f),new Vector3(.28f,3f,.28f),darkWood,g);Cube("PostR_"+i,V(1.8f,-1.2f,(i-1.5f)*length/4f),new Vector3(.28f,3f,.28f),darkWood,g);}
    }

    static void CreateUmbrella(Vector3 p,Material m,Transform parent)
    {
        var g=Group("Beach_Umbrella",parent);g.position=p;
        Cyl("Pole",V(0,1.7f,0),new Vector3(.12f,1.7f,.12f),cream,g);
        var top=Sphere("Canopy",V(0,3.35f,0),new Vector3(2.5f,.45f,2.5f),m,g); top.transform.rotation=Quaternion.Euler(0,RandomYaw(),0);
    }

    static void CreatePicnicMat(Vector3 p,Material m,Transform parent){Cube("Beach_Mat",p,new Vector3(3.2f,.08f,5f),m,parent,new Vector3(0,RandomYaw(),0));}

    static void CreatePalm(Vector3 p,float h,Transform parent)
    {
        var g=Group("Palm",parent);g.position=p;
        Cyl("Trunk",V(0,h*.42f,0),new Vector3(.48f,h*.42f,.48f),trunk,g,new Vector3(-6,0,4));
        var crown=Group("Crown",g);crown.localPosition=V(-.4f,h*.84f,.25f);
        for(int i=0;i<8;i++)
        {
            var pivot=Group("Leaf_"+i,crown);pivot.localRotation=Quaternion.Euler(18,i*45,0);
            Cube("Blade",V(0,-.18f,1.8f),new Vector3(.65f,.12f,3.6f),leaf,pivot,new Vector3(12,0,0));
        }
    }

    static void CreateCherry(Vector3 p,float h,Transform parent)
    {
        var g=Group("Cherry_Tree",parent);g.position=p;
        Cyl("Trunk",V(0,h*.32f,0),new Vector3(.45f,h*.32f,.45f),trunk,g);
        Sphere("CrownA",V(0,h*.72f,0),new Vector3(h*.52f,h*.34f,h*.48f),pink,g);
        Sphere("CrownB",V(-h*.25f,h*.63f,0),new Vector3(h*.34f,h*.27f,h*.34f),pink,g);
        Sphere("CrownC",V(h*.25f,h*.66f,.1f),new Vector3(h*.36f,h*.28f,h*.36f),pink,g);
    }

    static GameObject Instance(string rel,string n,Vector3 p,float targetSize,Transform parent,float yaw)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AllowedPrefabs+rel);
        if(!prefab){Debug.LogWarning("Missing prefab: "+rel);return null;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=n;go.transform.SetParent(parent);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var rr=go.GetComponentsInChildren<Renderer>();
        if(rr.Length>0)
        {
            Bounds b=rr[0].bounds;for(int i=1;i<rr.Length;i++)b.Encapsulate(rr[i].bounds);
            float max=Mathf.Max(b.size.x,b.size.y,b.size.z);
            if(max>.001f)go.transform.localScale*=targetSize/max;
            rr=go.GetComponentsInChildren<Renderer>();b=rr[0].bounds;for(int i=1;i<rr.Length;i++)b.Encapsulate(rr[i].bounds);
            go.transform.position+=Vector3.up*(p.y-b.min.y);
        }
        return go;
    }

    static GameObject Extrude(string n,Vector3 c,Vector2[] poly,float top,float bottom,Material topM,Material sideM,Transform parent,bool collider)
    {
        int count=poly.Length;var verts=new List<Vector3>();verts.Add(V(c.x,top,c.z));
        for(int i=0;i<count;i++)verts.Add(V(c.x+poly[i].x,top,c.z+poly[i].y));
        for(int i=0;i<count;i++)verts.Add(V(c.x+poly[i].x,bottom,c.z+poly[i].y));
        var topTris=new List<int>();var sideTris=new List<int>();
        for(int i=0;i<count;i++){int next=(i+1)%count;topTris.Add(0);topTris.Add(next+1);topTris.Add(i+1);int a=i+1,b=next+1,bb=count+1+next,aa=count+1+i;sideTris.Add(a);sideTris.Add(aa);sideTris.Add(bb);sideTris.Add(a);sideTris.Add(bb);sideTris.Add(b);}
        var mesh=new Mesh{name=n+"_Mesh"};mesh.SetVertices(verts);mesh.subMeshCount=2;mesh.SetTriangles(topTris,0);mesh.SetTriangles(sideTris,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        SaveMesh(mesh,n);
        var go=new GameObject(n);go.transform.SetParent(parent);var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=new[]{topM,sideM};
        if(collider){var mc=go.AddComponent<MeshCollider>();mc.sharedMesh=mesh;}
        return go;
    }

    static void Ribbon(string n,Vector3[] pts,float width,Material mat,Transform parent)
    {
        var verts=new List<Vector3>();for(int i=0;i<pts.Length;i++){Vector3 dir=(i==pts.Length-1?pts[i]-pts[i-1]:pts[i+1]-pts[i]).normalized;Vector3 side=Vector3.Cross(Vector3.up,dir)*width*.5f;verts.Add(pts[i]-side);verts.Add(pts[i]+side);}
        var tris=new List<int>();for(int i=0;i<pts.Length-1;i++){int a=i*2;tris.Add(a);tris.Add(a+1);tris.Add(a+2);tris.Add(a+1);tris.Add(a+3);tris.Add(a+2);}
        var mesh=new Mesh{name=n+"_Mesh"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(mesh,n);
        var go=new GameObject(n);go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }

    static void SaveMesh(Mesh m,string n){string p=MeshPath+"/"+n+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(old)AssetDatabase.DeleteAsset(p);AssetDatabase.CreateAsset(m,p);}

    static Transform Group(string n,Transform p){var g=new GameObject(n);g.transform.SetParent(p);return g.transform;}
    static void Anchor(string n,Vector3 p,Vector3 scale,Transform par){var g=Group(n,par);g.position=p;g.localScale=scale;}
    static GameObject Cube(string n,Vector3 p,Vector3 s,Material m,Transform par,Vector3? rot=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(par);g.transform.localPosition=p;g.transform.localScale=s;g.transform.localRotation=Quaternion.Euler(rot??Vector3.zero);g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);return g;}
    static GameObject Sphere(string n,Vector3 p,Vector3 s,Material m,Transform par){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(par);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);return g;}
    static GameObject Cyl(string n,Vector3 p,Vector3 s,Material m,Transform par,Vector3? rot=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(par);g.transform.localPosition=p;g.transform.localScale=s;g.transform.localRotation=Quaternion.Euler(rot??Vector3.zero);g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);return g;}
    static Vector2[] P(params (float,float)[] a){var r=new Vector2[a.Length];for(int i=0;i<a.Length;i++)r[i]=new Vector2(a[i].Item1,a[i].Item2);return r;}
    static Vector3 V(double x,double y,double z)=>new Vector3((float)x,(float)y,(float)z);
    static float RandomYaw()=>(float)rng.NextDouble()*360f;
    static void SetStaticRecursive(GameObject g){g.isStatic=true;foreach(Transform c in g.transform)SetStaticRecursive(c.gameObject);}


static void CreateMarket(string n, Vector3 p, float yaw, Transform parent)
    {
        var g=Group(n,parent); g.position=p; g.rotation=Quaternion.Euler(0,yaw,0);
        Cube("Deck",V(0,.18f,0),new Vector3(7,.35f,6),wood,g);
        Cube("Back",V(0,2.2f,2.4f),new Vector3(6.6f,4.2f,.35f),cream,g);
        Cube("Counter",V(0,1.25f,-2.0f),new Vector3(6.2f,1.1f,.8f),darkWood,g);
        Cube("Roof",V(0,4.6f,0),new Vector3(7.5f,.35f,6.8f),teal,g,new Vector3(0,0,3));
        Cube("PostL",V(-3.1f,2.3f,-2.4f),new Vector3(.3f,4.5f,.3f),wood,g);
        Cube("PostR",V(3.1f,2.3f,-2.4f),new Vector3(.3f,4.5f,.3f),wood,g);
        Cube("CrateA",V(-2.1f,.75f,-1.3f),new Vector3(1.2f,1.2f,1.2f),yellow,g);
        Cube("CrateB",V(2.0f,.75f,-1.3f),new Vector3(1.2f,1.2f,1.2f),red,g);
    }


static void LogStats(GameObject r)
    {
        int renderers=r.GetComponentsInChildren<Renderer>(true).Length, tris=0;
        foreach(var f in r.GetComponentsInChildren<MeshFilter>(true)) if(f.sharedMesh) tris+=f.sharedMesh.triangles.Length/3;
        foreach(var s in r.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if(s.sharedMesh) tris+=s.sharedMesh.triangles.Length/3;
        Debug.Log("CODEX_ISLAND_STATS: renderers="+renderers+", instance triangles="+tris);
    }
}