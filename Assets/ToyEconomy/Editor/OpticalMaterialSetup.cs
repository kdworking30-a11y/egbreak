using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace ToyEconomy.Editor {
 public static class OpticalMaterialSetup {
  const string Root="Assets/ToyEconomy/";
  [MenuItem("Tools/Toy Economy/Upgrade Reflection and Refraction")]
  public static void Upgrade(){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before optical upgrade");
   var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save current scene first");
   if(scene.path!=ToyEconomySetup.ScenePath)scene=EditorSceneManager.OpenScene(ToyEconomySetup.ScenePath);
   var game=UnityEngine.Object.FindFirstObjectByType<GameLoopController>();
   if(game==null || game.eggs.Length<4 || game.eggs[0].materialProfile==null)throw new InvalidOperationException("Material gameplay must be set up first");
   var solid=Shader.Find("ToyEconomy/MaterialSurface");var glass=Shader.Find("ToyEconomy/OpticalGlass");
   if(solid==null || glass==null)throw new InvalidOperationException("Optical shaders missing");
   Directory.CreateDirectory(Root+"Materials");var cube=MakeEnvironment();
   foreach(var egg in game.eggs){var p=egg.materialProfile;bool optical=egg.eggId=="ice" || egg.eggId=="crystal";bool crystal=egg.eggId=="crystal";
    foreach(var mat in new[]{p.outside,p.inside}){
     bool interior=mat==p.inside;mat.shader=optical?glass:solid;mat.SetTexture("_EnvironmentCube",cube);
     mat.SetFloat("_ReflectionStrength",optical?2.4f:egg.eggId=="chocolate"?1.5f:.16f);
     mat.SetFloat("_Roughness",optical?(crystal?.025f:.045f):egg.eggId=="chocolate"?(interior?.82f:.19f):.88f);
     mat.SetFloat("_Interior",interior?1:0);
     if(optical){mat.renderQueue=3000;mat.SetFloat("_IOR",crystal?1.52f:1.31f);mat.SetFloat("_RefractionStrength",crystal?.105f:.115f);mat.SetFloat("_Thickness",crystal?1.8f:1.6f);mat.SetFloat("_Density",crystal?.65f:1.05f);mat.SetFloat("_Absorption",crystal?.8f:.42f);mat.SetFloat("_Dispersion",crystal?.018f:.004f);mat.SetFloat("_Facet",crystal?.94f:0);
      mat.SetColor("_BaseColor",crystal?new Color(.65f,.9f,.98f):new Color(.86f,.97f,1));
     }else{mat.renderQueue=2000;mat.SetColor("_BaseColor",egg.eggId=="chocolate"?(interior?new Color(.35f,.17f,.085f):new Color(.24f,.105f,.048f)):(interior?new Color(.95f,.92f,.8f):new Color(.9f,.84f,.68f)));}
     EditorUtility.SetDirty(mat);
    }
   }
   var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Main camera missing");camera.allowHDR=true;
   var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();if(cameraData==null)cameraData=camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
   cameraData.requiresColorTexture=true;cameraData.requiresDepthTexture=true;EditorUtility.SetDirty(cameraData);
   RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=cube;RenderSettings.reflectionIntensity=1;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.37f,.4f,.44f);
   var sun=UnityEngine.Object.FindFirstObjectByType<Light>();if(sun!=null){sun.color=new Color(1,.97f,.91f);sun.intensity=1.15f;EditorUtility.SetDirty(sun);}
   var backdrop=GameObject.Find("Optical Studio Backdrop");if(backdrop==null){backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Optical Studio Backdrop";UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());}
   backdrop.transform.position=new Vector3(0,.2f,2);backdrop.transform.localScale=new Vector3(12,12,1);
   string path=Root+"Materials/StudioBackdrop.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("ToyEconomy/StudioBackdrop"));AssetDatabase.CreateAsset(material,path);}backdrop.GetComponent<Renderer>().sharedMaterial=material;
   EditorUtility.SetDirty(backdrop);EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText("/tmp/optical-material-upgrade.txt","SUCCESS: HDR studio cubemap reflection; camera opaque texture refraction; Beer-Lambert tint; IOR; frost and crystal facets.");Debug.Log("[Optical Materials] Reflection environment and URP color-copy refraction configured.");
  }
  static Cubemap MakeEnvironment(){
   string path=Root+"Materials/StudioEnvironment.asset";var existing=AssetDatabase.LoadAssetAtPath<Cubemap>(path);if(existing!=null)return existing;
   const int size=128;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true);cube.name="Baked HDR softbox studio";cube.wrapMode=TextureWrapMode.Clamp;cube.filterMode=FilterMode.Trilinear;
   for(int f=0;f<6;f++){var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;Vector3 d;
    switch(f){case 0:d=new Vector3(1,-v,-u);break;case 1:d=new Vector3(-1,-v,u);break;case 2:d=new Vector3(u,1,v);break;case 3:d=new Vector3(u,-1,-v);break;case 4:d=new Vector3(u,-v,1);break;default:d=new Vector3(-u,-v,-1);break;}d.Normalize();
    Color color=Color.Lerp(new Color(.11f,.15f,.16f),new Color(.63f,.72f,.76f),Mathf.Clamp01(d.y*.5f+.5f));
    if(d.z<-.05f){float px=d.x/-d.z,py=d.y/-d.z;float a=Box(px,py,-.62f,.2f,.24f,.88f);float b=Box(px,py,.78f,.15f,.1f,1.12f);float c=Box(px,py,.05f,1.1f,.8f,.11f);color+=new Color(1,.95f,.84f)*a*6.5f+new Color(.7f,.88f,1)*b*5f+Color.white*c*2.8f;}
    pixels[y*size+x]=color;
   }cube.SetPixels(pixels,(CubemapFace)f);}cube.Apply(true,false);AssetDatabase.CreateAsset(cube,path);return cube;
  }
  static float Box(float x,float y,float cx,float cy,float sx,float sy){float dx=Mathf.Abs(x-cx)/sx,dy=Mathf.Abs(y-cy)/sy;return (1-Mathf.SmoothStep(.92f,1.08f,dx))*(1-Mathf.SmoothStep(.92f,1.08f,dy));}
 }
}
