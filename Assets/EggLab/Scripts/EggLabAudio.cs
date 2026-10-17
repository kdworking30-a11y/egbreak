using UnityEngine;

namespace EggLab
{
    [DisallowMultipleComponent]
    public sealed class EggLabAudio : MonoBehaviour
    {
        public AudioClip tap, crack, shatter, squish, release, pour, liquidLoop, bubble;
        public AudioClip[] tapVariations, squishVariations, releaseVariations, bubbleVariations;
        [Header("ASMR headphone mix")]
        [Range(0,1)] public float masterVolume=.75f;
        [Range(0,.35f)] public float stereoWidth=.18f;
        public bool muted;
        const string VolumeKey="EggLab.Asmr.Volume.v1", MuteKey="EggLab.Asmr.Muted.v1";
        AudioSource[] voices;
        float[] cueLevels;
        AudioSource wet;
        int cursor,eventNumber,lastTap=-1,lastSquish=-1,lastRelease=-1,lastBubble=-1;
        float targetWet,targetPitch=1,targetPan,nextGrab,nextBubble,saveAt;
        bool pendingSave;
        public int PlayedEvents { get; private set; }
        public float WetLevel => wet==null?0:wet.volume;
        public bool WetPlaying => wet!=null&&wet.isPlaying;
        public int VoiceCount => voices==null?0:voices.Length+1;

        void Awake()
        {
            masterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,masterVolume));
            muted=PlayerPrefs.GetInt(MuteKey,muted?1:0)!=0;
            EnsureSources();
        }
        void EnsureSources()
        {
            if(voices!=null)return;
            // Six non-stealing foley voices; two reserved for shell break and pour.
            voices=new AudioSource[8];cueLevels=new float[8];
            for(int i=0;i<voices.Length;i++)voices[i]=NewSource();
            wet=NewSource();wet.loop=true;wet.volume=0;wet.clip=liquidLoop;
        }
        AudioSource NewSource()
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
            source.spatialBlend=0;source.dopplerLevel=0;source.priority=100;
            return source;
        }
        AudioClip Pick(AudioClip[] pool,AudioClip fallback,ref int previous)
        {
            if(pool==null||pool.Length==0)return fallback;
            int index=pool.Length==1?0:(previous+1+eventNumber%(pool.Length-1))%pool.Length;
            previous=index;return pool[index]!=null?pool[index]:fallback;
        }
        public void Tap(int hit)
        {
            Play(Pick(tapVariations,tap,ref lastTap),.65f,.98f+hit*.006f);
            if(hit>=2)Play(crack,.19f+hit*.018f,1);
        }
        public void Break()
        { Play(shatter,.52f,.98f,6);Play(pour,.72f,1,7); }
        public void Grab()
        {
            if(Time.unscaledTime<nextGrab)return;
            nextGrab=Time.unscaledTime+.18f;
            Play(Pick(squishVariations,squish,ref lastSquish),.88f,.98f+(eventNumber%3)*.009f);
        }
        public void Release(float amount)
        { Play(Pick(releaseVariations,release,ref lastRelease),Mathf.Lerp(.42f,.82f,Mathf.Clamp01(amount)),.99f); }
        public void Motion(bool active,float speed,float strain,bool flowing,float horizontal=0)
        {
            EnsureSources();
            targetWet=muted?0:masterVolume*(flowing?.18f:active?Mathf.Clamp(.065f+speed*.06f+strain*.04f,.065f,.26f):0);
            targetPitch=Mathf.Clamp(.97f+speed*.012f+strain*.015f,.94f,1.06f);
            targetPan=Mathf.Clamp(horizontal,-1,1)*stereoWidth;
            if(active&&speed>.45f&&Time.unscaledTime>nextBubble)
            {
                nextBubble=Time.unscaledTime+.62f+(eventNumber%4)*.19f;
                Play(Pick(bubbleVariations,bubble,ref lastBubble),.33f,1);
            }
        }
        void Update()
        {
            EnsureSources();
            if(pendingSave&&Time.unscaledTime>saveAt){PlayerPrefs.Save();pendingSave=false;}
            if(muted){StopAudio();return;}
            for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying)
                voices[i].volume=Mathf.MoveTowards(voices[i].volume,Mathf.Min(.20f,cueLevels[i]*masterVolume),Time.unscaledDeltaTime*.8f);
            if(wet.clip!=liquidLoop)wet.clip=liquidLoop;
            wet.volume=Mathf.MoveTowards(wet.volume,targetWet,Time.unscaledDeltaTime*(targetWet>wet.volume?.35f:.23f));
            wet.pitch=Mathf.Lerp(wet.pitch,targetPitch,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
            wet.panStereo=Mathf.Lerp(wet.panStereo,targetPan,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
            if(wet.clip!=null&&wet.volume>.002f&&!wet.isPlaying)wet.Play();
            if(wet.volume<.001f&&targetWet==0&&wet.isPlaying)wet.Stop();
        }
        void Play(AudioClip clip,float volume,float pitch,int reserved=-1)
        {
            EnsureSources();if(muted||clip==null||masterVolume<=0)return;
            AudioSource source=null;int voiceIndex=-1;
            if(reserved>=0){source=voices[reserved];voiceIndex=reserved;if(source.isPlaying)return;}
            else for(int i=0;i<6;i++)
            {
                int index=(cursor+i)%6;
                if(!voices[index].isPlaying){source=voices[index];voiceIndex=index;cursor=(index+1)%6;break;}
            }
            // Skipping a low-priority grain is quieter than abruptly cutting an existing voice.
            if(source==null)return;
            cueLevels[voiceIndex]=volume;source.clip=clip;source.volume=Mathf.Min(.20f,volume*masterVolume);
            source.pitch=Mathf.Clamp(pitch,.94f,1.06f);source.panStereo=targetPan*.65f;
            source.Play();eventNumber++;PlayedEvents++;
        }
        public void SetMuted(bool value)
        {
            if(muted==value)return;
            muted=value;if(value)StopAudio();
            PlayerPrefs.SetInt(MuteKey,value?1:0);ScheduleSave();
        }
        public void SetMasterVolume(float value)
        {
            value=Mathf.Clamp01(value);if(Mathf.Abs(value-masterVolume)<.001f)return;
            masterVolume=value;PlayerPrefs.SetFloat(VolumeKey,value);ScheduleSave();
            if(value<=.001f)StopAudio();
        }
        void ScheduleSave(){pendingSave=true;saveAt=Time.unscaledTime+.35f;}
        public void StopAudio()
        {
            targetWet=0;if(voices==null)return;
            foreach(var source in voices)source.Stop();wet.Stop();wet.volume=0;
        }
        void OnDisable(){StopAudio();if(pendingSave){PlayerPrefs.Save();pendingSave=false;}}
    }
}
