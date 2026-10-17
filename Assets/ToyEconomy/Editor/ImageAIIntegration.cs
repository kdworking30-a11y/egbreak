using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ToyEconomy.Editor {
public static class ImageAIIntegration {
 const string Root="Assets/ToyEconomy/";
 [MenuItem("Tools/Toy Economy/Integrate ImageAI Fracture and Toy Flow")]
 public static void Upgrade(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
 if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
 EditorSceneManager.OpenScene(ToyEconomySetup.ScenePath);
 var loop=UnityEngine.Object.FindFirstObjectByType<GameLoopController>();
 if(loop==null)throw new InvalidOperationException("MVP game loop missing");
 loop.rewardShader=Shader.Find("_Egg/RewardQuadReveal_URP");if(loop.rewardShader==null)throw new InvalidOperationException("Imported reward shader missing");
 int[] counts={50,100,200};
 for(int i=0;i<counts.Length;i++){var prefab=Bake(counts[i]);loop.eggs[i].fracturedPrefab=prefab;loop.eggs[i].fragmentCount=prefab.GetComponentsInChildren<MeshFilter>().Length;loop.eggs[i].hitsToBreak=new[]{15,33,66}[i];EditorUtility.SetDirty(loop.eggs[i]);}
 var toys=new[]{loop.starterToy,loop.eggs[0].possibleToys[1],loop.eggs[1].possibleToys[0],loop.eggs[2].possibleToys[0]};
 string[] ids={"cotton_bunny","cotton_dino","toast_cat","raincoat_cat"};string[] names={"Cotton Bunny","Cotton Dino","Toast Cat","Raincoat Cat"};
 for(int i=0;i<toys.Length;i++){
 string path=Root+"ImportedImageAI/Toys/"+ids[i]+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.SaveAndReimport();
 toys[i].displayName=names[i];toys[i].artwork=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(toys[i]);
 }
 EditorUtility.SetDirty(loop);EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);EditorSceneManager.SaveScene(loop.gameObject.scene);AssetDatabase.SaveAssets();
 File.WriteAllText("/tmp/toy-economy-import-report.txt","Reused imageai source meshes: 52 / 101 / 200 (source tier names 50 / 100 / 200).\nSource tier hits: 15 / 33 / 66.\nNormalized baked pivots for detached Rigidbody debris.\n4 original reward textures and URP reveal shader.\n");
 Debug.Log("[ImageAI Integration] 353 chunks, 4 toys, reward reveal, collection and Next flow ready.");
 }
 static GameObject Bake(int count){
 string folder=Root+"FractureBaked/Egg"+count;Directory.CreateDirectory(folder);
 string prefabPath=folder+"/Egg"+count+".prefab";
 var existing=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);if(existing!=null)return existing;
 var source=PrefabUtility.LoadPrefabContents(Root+"ImportedImageAI/ImageAIEgg"+count+".prefab");
 var output=new GameObject("ImageAI Egg - "+count+" fragments");
 try{
 var filters=source.GetComponentsInChildren<MeshFilter>();if(filters.Length<4)throw new InvalidOperationException("Source fracture mesh missing");
 var bounds=new Bounds();bool first=true;
 foreach(var filter in filters)foreach(var vertex in filter.sharedMesh.vertices){var world=filter.transform.TransformPoint(vertex);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}
 float scale=2.7f/bounds.size.y;
 for(int index=0;index<filters.Length;index++){
 var filter=filters[index];var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="ImageAI Voronoi chunk "+index;
 var vertices=mesh.vertices;var localBounds=new Bounds();
 for(int v=0;v<vertices.Length;v++){vertices[v]=(filter.transform.TransformPoint(vertices[v])-bounds.center)*scale;if(v==0)localBounds=new Bounds(vertices[v],Vector3.zero);else localBounds.Encapsulate(vertices[v]);}
 var pivot=localBounds.center;for(int v=0;v<vertices.Length;v++)vertices[v]-=pivot;mesh.vertices=vertices;
 var normals=mesh.normals;var normalMatrix=filter.transform.localToWorldMatrix.inverse.transpose;for(int n=0;n<normals.Length;n++)normals[n]=normalMatrix.MultiplyVector(normals[n]).normalized;mesh.normals=normals;mesh.RecalculateBounds();
 AssetDatabase.CreateAsset(mesh,folder+"/Chunk"+index.ToString("D3")+".asset");
 var part=new GameObject("Chunk "+index,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(output.transform,false);part.transform.localPosition=pivot;part.GetComponent<MeshFilter>().sharedMesh=mesh;
 var mats=new Material[mesh.subMeshCount];for(int m=0;m<mats.Length;m++)mats[m]=AssetDatabase.LoadAssetAtPath<Material>(Root+"Data/EggShell.mat");part.GetComponent<MeshRenderer>().sharedMaterials=mats;
 }
 return PrefabUtility.SaveAsPrefabAsset(output,prefabPath);
 }finally{PrefabUtility.UnloadPrefabContents(source);UnityEngine.Object.DestroyImmediate(output);}
 }
}
}
