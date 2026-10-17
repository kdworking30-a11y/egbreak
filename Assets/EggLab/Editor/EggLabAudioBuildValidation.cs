using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EggLab.Editor
{
    // Build-time only: runtime audio is serialized and has no dependency on MCP or this validator.
    public sealed class EggLabAudioBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report){ValidateAssets();}
        [MenuItem("Tools/Egg Lab/Validate ASMR Build Audio")]
        public static void ValidateAssets()
        {
            string scene=EggLabSceneBuilder.ScenePath;
            bool included=false;
            foreach(var entry in EditorBuildSettings.scenes)if(entry.enabled&&entry.path==scene)included=true;
            if(!included){Debug.Log("[Egg Lab ASMR] Prototype scene is not part of this build; ASMR validation skipped.");return;}
            var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(scene,true));
            var guids=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/EggLab/Audio"});
            if(guids.Length<17)throw new BuildFailedException("ASMR set is incomplete: expected 17 audio clips.");
            foreach(string guid in guids)
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(!dependencies.Contains(path))throw new BuildFailedException("ASMR clip is not serialized into the build scene: "+path);
                if(clip==null||clip.channels!=2||clip.samples==0||importer==null||importer.forceToMono)
                    throw new BuildFailedException("ASMR clip must be nonempty stereo: "+path);
                var settings=importer.defaultSampleSettings;
                if(settings.loadType!=AudioClipLoadType.DecompressOnLoad||!settings.preloadAudioData)
                    throw new BuildFailedException("Preload/decompress ASMR clip for responsive playback: "+path);
                foreach(string platform in new[]{"Android","iPhone"})
                {
                    var platformSettings=importer.GetOverrideSampleSettings(platform);
                    if(platformSettings.loadType!=AudioClipLoadType.DecompressOnLoad||!platformSettings.preloadAudioData)
                        throw new BuildFailedException("ASMR platform import is not ready for "+platform+": "+path);
                }
            }
            var audioAssets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            if(audioAssets.Length>0)
            {
                var settings=new SerializedObject(audioAssets[0]);var disabled=settings.FindProperty("m_DisableAudio");
                if(disabled!=null&&disabled.boolValue)throw new BuildFailedException("Unity Audio is disabled in Project Settings.");
            }
            Debug.Log("[Egg Lab ASMR] Build audio ready: "+guids.Length+" stereo clips are embedded in the enabled scene; Android/iOS preload validated.");
        }
    }
}
