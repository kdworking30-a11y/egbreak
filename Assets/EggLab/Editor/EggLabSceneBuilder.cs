using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EggLab.Editor
{
    public static class EggLabSceneBuilder
    {
        public const string ScenePath="Assets/Scenes/EggLabPrototype.unity";
        public static void ConfigureAudio(EggLabPrototype lab)
        {
            if(lab==null)throw new InvalidOperationException("Egg Lab is missing.");
            var sound=lab.GetComponent<EggLabAudio>();
            if(sound==null)sound=lab.gameObject.AddComponent<EggLabAudio>();
            const string folder="Assets/EggLab/Audio/";
            sound.tap=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"tap.wav");
            sound.crack=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"crack.wav");
            sound.shatter=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"break.wav");
            sound.squish=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"squish.wav");
            sound.release=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"release.wav");
            sound.pour=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"pour.wav");
            sound.liquidLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"liquid-loop.wav");
            sound.bubble=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"bubble.wav");
            sound.tapVariations=new[]{sound.tap,AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"tap-02.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"tap-03.wav")};
            sound.squishVariations=new[]{sound.squish,AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"squish-02.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"squish-03.wav")};
            sound.releaseVariations=new[]{sound.release,AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"release-02.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"release-03.wav")};
            sound.bubbleVariations=new[]{sound.bubble,AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"bubble-02.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"bubble-03.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"bubble-04.wav")};
            EditorUtility.SetDirty(sound);
            EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        }

        [MenuItem("Tools/Egg Lab/Create or Open Prototype")]
        public static void CreateOrOpen()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before opening Egg Lab.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null){EditorSceneManager.OpenScene(ScenePath);ConfigureAudio(UnityEngine.Object.FindFirstObjectByType<EggLabPrototype>());return;}
            var shell=Shader.Find("EggLab/CrackedShell");var slime=Shader.Find("EggLab/Jelly");var lit=Shader.Find("Universal Render Pipeline/Lit");
            if(shell==null||slime==null||lit==null)throw new InvalidOperationException("Required URP shaders are missing.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Main Camera");cam.tag="MainCamera";
            var camera=cam.AddComponent<Camera>();cam.AddComponent<AudioListener>();
            camera.orthographic=true;camera.orthographicSize=3.55f;camera.nearClipPlane=.1f;camera.farClipPlane=60;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.84f,.88f,.82f);
            cam.transform.position=new Vector3(2.4f,3.1f,-7);cam.transform.LookAt(new Vector3(0,1.05f,0));
            var light=new GameObject("Softbox sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;
            light.transform.rotation=Quaternion.Euler(38,-35,0);light.shadows=LightShadows.None;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.71f,.69f);
            var lab=new GameObject("Egg Lab").AddComponent<EggLabPrototype>();lab.shellShader=shell;lab.slimeShader=slime;lab.propShader=lit;lab.mobilePipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            ConfigureAudio(lab);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
            foreach(var s in EditorBuildSettings.scenes)if(s.path!=ScenePath)scenes.Add(s);
            EditorBuildSettings.scenes=scenes.ToArray();
            Selection.activeGameObject=lab.gameObject;
            Debug.Log("[Egg Lab] Prototype scene ready. Press Play. Runtime selects the existing Mobile URP quality tier.");
        }
    }
}
