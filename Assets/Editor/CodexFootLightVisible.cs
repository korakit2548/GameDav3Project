using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
static class CodexFootLightVisible
{
    const string Key="CodexFootLightVisible.";
    static readonly string Work=Path.GetFullPath(Path.Combine(Application.dataPath,"../.superpowers/footlight-visible"));
    static double readyAfter=EditorApplication.timeSinceStartup+2;
    static Light lamp;static UniversalAdditionalLightData data;static float brightness;static int photo;static Vector2 center;
    [Serializable] sealed class Setup{public SceneSetup[] scenes;}
    static CodexFootLightVisible(){if(AssetDatabase.IsAssetImportWorkerProcess())return;Directory.CreateDirectory(Work);File.WriteAllText(Path.Combine(Work,"ready.txt"),DateTime.Now.ToString("O"));EditorApplication.playModeStateChanged+=s=>readyAfter=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;}
    static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    static void Note(string message)=>File.AppendAllText(Path.Combine(Work,"report.txt"),message+"\n");
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static void Touch(Object o){EditorUtility.SetDirty(o);if(o is Component c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<readyAfter)return;
        try{
            int stage=SessionState.GetInt(Key+"stage",0);if(stage>0){Run(stage);return;}
            var request=Path.Combine(Work,"diagnose-v1.request");if(!File.Exists(request)||EditorApplication.isPlayingOrWillChangePlaymode||PrefabStageUtility.GetCurrentPrefabStage()!=null)return;File.Delete(request);
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/Level2.unity");Need(scene.IsValid()&&scene.isLoaded,"Level2 loaded");File.Copy(scene.path,Path.Combine(Work,"Level2-before.unity"),true);EditorSceneManager.SaveScene(scene,Path.Combine(Work,"Level2-memory-before.unity"),true);
            int captions=0;foreach(var text in All<TMP_Text>(scene).Where(t=>t.text=="Place items on the yellow area")){text.text="Unpack the box and place it here";Touch(text);captions++;}Need(captions==1,"Exactly one placement caption updated");EditorSceneManager.MarkSceneDirty(scene);Need(EditorSceneManager.SaveScene(scene),"Save current scene including existing user edits");
            SessionState.SetString(Key+"setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup()}));EditorApplication.ExecuteMenuItem("Window/General/Game");SessionState.SetInt(Key+"stage",1);EditorApplication.EnterPlaymode();
        }catch(Exception e){Note("FAIL "+e);File.WriteAllText(Path.Combine(Work,"failed.txt"),e.ToString());SessionState.SetInt(Key+"stage",0);if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();}
    }
    static void Next(int stage,float delay){SessionState.SetInt(Key+"stage",stage);SessionState.SetFloat(Key+"after",(float)EditorApplication.timeSinceStartup+delay);}
    static string Png(int i)=>Path.Combine(Work,"variant-"+i+".png");
    static void Configure(int i)
    {
        lamp.enabled=i!=1;lamp.intensity=i==5?brightness*3:brightness;lamp.shadows=i<=1?LightShadows.Soft:LightShadows.None;data.customShadowLayers=true;data.renderingLayers=i==4?1u:4u;data.shadowRenderingLayers=i>=3?7u:4u;
        Note("VARIANT "+i+" enabled="+lamp.enabled+" shadows="+lamp.shadows+" native="+lamp.renderingLayerMask+" receive="+(uint)data.renderingLayers+" intensity="+lamp.intensity);
    }
    static void Run(int stage)
    {
        if(stage==8){if(EditorApplication.isPlayingOrWillChangePlaymode)return;Finish();return;}
        if(!EditorApplication.isPlaying)return;
        if(stage==1)
        {
            if(Time.frameCount<12)return;var pc=Object.FindAnyObjectByType<PlayerController>();if(pc==null)return;lamp=pc.GetComponentsInChildren<Light>(true).Single();data=lamp.GetUniversalAdditionalLightData();brightness=lamp.intensity;Time.timeScale=0f;
            foreach(var volume in Object.FindObjectsByType<Volume>()){if(volume.profile.TryGet<FilmGrain>(out var grain))grain.active=false;}
            var camera=Camera.main;Need(camera!=null,"Game camera available");var p=pc.transform.position;p.y=.04f;var screen=camera.WorldToScreenPoint(p);center=new Vector2(screen.x,screen.y);SessionState.SetFloat(Key+"cx",center.x);SessionState.SetFloat(Key+"cy",center.y);SessionState.SetFloat(Key+"brightness",brightness);
            Note("RUNTIME light="+PathOf(lamp.transform)+" active="+lamp.isActiveAndEnabled+" position="+lamp.transform.position+" direction="+lamp.transform.forward+" range="+lamp.range+" culling="+lamp.cullingMask+" spot="+lamp.spotAngle+" screen="+center);
            foreach(var renderer in All<Renderer>(pc.gameObject.scene).Where(r=>(r.renderingLayerMask&4)!=0)){Note("RECEIVER "+PathOf(renderer.transform)+" layer="+renderer.gameObject.layer+" mask="+renderer.renderingLayerMask+" culled="+((lamp.cullingMask&(1<<renderer.gameObject.layer))==0)+" shaders="+string.Join(",",renderer.sharedMaterials.Select(m=>m==null?"null":m.shader.name)));}
            photo=0;Configure(photo);Next(2,.5f);return;
        }
        if(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"after",0))return;
        if(stage==2){ScreenCapture.CaptureScreenshot(Png(photo));Next(3,.4f);return;}
        if(stage==3){if(!File.Exists(Png(photo)))return;photo++;if(photo<6){Configure(photo);Next(2,.5f);return;}Measure();SessionState.SetInt(Key+"stage",8);EditorApplication.ExitPlaymode();}
    }
    static Color32[] Pixels(string path,out int width,out int height){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);Need(texture.LoadImage(File.ReadAllBytes(path)),"Read screenshot");width=texture.width;height=texture.height;var pixels=texture.GetPixels32();Object.DestroyImmediate(texture);return pixels;}
    static double Luma(Color32 c)=>.2126*c.r+.7152*c.g+.0722*c.b;
    static void Measure()
    {
        var off=Pixels(Png(1),out int width,out int height);double best=0;int selected=-1;
        for(int i=0;i<6;i++){if(i==1)continue;var on=Pixels(Png(i),out int w,out int h);Need(w==width&&h==height,"Comparable screenshot sizes");double sum=0;int count=0,bright=0;for(int y=Math.Max(170,(int)center.y-160);y<Math.Min(height,(int)center.y+160);y++)for(int x=Math.Max(0,(int)center.x-160);x<Math.Min(width,(int)center.x+160);x++){double r=(x-center.x)*(x-center.x)+(y-center.y)*(y-center.y);if(r<45*45||r>160*160)continue;double delta=Luma(on[y*width+x])-Luma(off[y*width+x]);sum+=delta;count++;if(delta>5)bright++;}double mean=sum/Math.Max(1,count);Note("PIXELS variant="+i+" meanFloorLightDelta="+mean.ToString("F4")+" brighterPixels="+bright+" samples="+count);if((i==2||i==3)&&mean>best){best=mean;selected=i;}if(i==5&&best<3&&mean>best){best=mean;selected=i;}}
        Need(selected>=0&&best>.2,"At least one floor-only light configuration is visibly contributing");SessionState.SetInt(Key+"selected",selected);Note("SELECTED "+selected+" delta="+best.ToString("F4"));
    }
    static void Finish()
    {
        var setup=JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"setup",""));if(setup!=null)EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);var scene=SceneManager.GetSceneByPath("Assets/Scenes/Level2.unity");var pc=All<PlayerController>(scene).Single();var light=pc.GetComponentsInChildren<Light>(true).Single();var d=light.GetUniversalAdditionalLightData();int selected=SessionState.GetInt(Key+"selected",-1);Need(selected==2||selected==3||selected==5,"Keep floor-only receiver mode");var poses=All<Transform>(scene).Where(t=>!(t is RectTransform)).ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));var rooms=All<Room>(scene).ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
        light.enabled=true;light.shadows=LightShadows.None;light.intensity=SessionState.GetFloat(Key+"brightness",50)*(selected==5?3:1);d.customShadowLayers=true;d.renderingLayers=4u;d.shadowRenderingLayers=selected>=3?7u:4u;Touch(light);Touch(d);foreach(var p in poses)Need(EditorJsonUtility.ToJson(p.Key)==p.Value,"No existing world object moved");foreach(var p in rooms)Need(EditorJsonUtility.ToJson(p.Key)==p.Value,"Room values preserved");
        EditorSceneManager.MarkSceneDirty(scene);Need(EditorSceneManager.SaveScene(scene),"Save verified floor lighting");SessionState.SetInt(Key+"stage",0);Note("PASS saved floor-only visible variant="+selected+" intensity="+light.intensity+" shadowless player light; placement caption=Unpack the box and place it here; Room and world positions unchanged");File.WriteAllText(Path.Combine(Work,"complete.txt"),"PASS "+DateTime.Now.ToString("O")+"\n"+File.ReadAllText(Path.Combine(Work,"report.txt")));
    }
}
