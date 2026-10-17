using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace EggLab
{
    // A bounded, reusable fracture set plus spring-based surface deformation.
    // This is a visual soft-body approximation, not a fluid solver.
    public sealed class EggLabPrototype : MonoBehaviour
    {
        public Shader shellShader;
        public Shader slimeShader;
        public Shader propShader;
        public RenderPipelineAsset mobilePipeline;
        RenderPipelineAsset previousPipeline;
        public int targetFrameRate = 60;
        public enum ShellFragmentSize { Large24, Small48, Tiny96 }
        [Header("Shell fragments")]
        [Tooltip("Smaller pieces create more rigidbodies. Can also be changed in the Game view.")]
        public ShellFragmentSize fragmentSize = ShellFragmentSize.Small48;
        [HideInInspector] public int longitudePieces = 8;
        [HideInInspector] public int latitudePieces = 6;
        [Tooltip("Deterministic Voronoi layout. Change before Play for a new cut pattern.")]
        public int fractureSeed = 137;
        [Range(0,1)] public float fractureIrregularity = .7f;
        public int hitsToBreak = 5;
        [Header("Slime filling and spill")]
        [Range(.55f,.82f)] public float slimeFill = .76f;
        [Range(.7f,2.5f)] public float spillDuration = 1.4f;
        [Header("Liquid squish")]
        [Range(25,90)] public float springStiffness=48;
        [Range(5,16)] public float viscosity=9.5f;
        [Range(0,60)] public float surfaceTension=24;
        [Range(.8f,2)] public float stretchLimit=1.65f;
        public float SlimeVolumeRatio { get; private set; }=1;
        SlimeSurfaceDynamics surfaceDynamics;
        Vector3[] flowGoals,goals,pullMemory;
        float restVolume,motionSpeed,instantMotion,rippleAt=-100,rippleStrength;
        Vector3 rippleOrigin;
        EggLabAudio audioFeedback;
        Material slimeMaterial;
        public float SlimeFlowProgress { get; private set; }
        public bool IsSlimeSettled => Broken && SlimeFlowProgress >= 1;
        public Vector3 SlimeCenter => slimeObject == null ? eggCenter : slimeObject.transform.position;
        public int Hits { get; private set; }
        public bool Broken { get; private set; }
        public int FragmentCount => pieces.Count;
        public int SlimeVertexCount => rest == null ? 0 : rest.Length;
        public float MaxDeformation { get; private set; }
        public bool BenchmarkRunning => benchmarking;
        public string LastReport { get; private set; } = "";

        readonly List<Rigidbody> pieces = new List<Rigidbody>();
        readonly List<Collider> shellColliders = new List<Collider>();
        readonly List<Vector3> pieceOrigins = new List<Vector3>();
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly List<float> frameTimes = new List<float>(8192);
        readonly List<int> framePhases = new List<int>(8192);
        readonly Vector3 eggCenter = new Vector3(0, 1.52f, 0);
        GameObject generated, shellRoot, slimeObject;
        Transform eyeL, eyeR;
        Camera view;
        Material shellMaterial;
        Mesh slimeMesh;
        Vector3[] rest, vertices, velocity, packed;
        Vector3 spillDirection=Vector3.back;
        const float PuddleCenterY=.30f, SlimeFloorY=.016f;
        int[] slimeTriangles;
        readonly List<Vector3> slimeNormals=new List<Vector3>(425);
        public bool IsDragging => dragging;
        public Vector2 LastPointerPosition { get; private set; }
        public bool PointerPressed { get; private set; }
        Vector3 grabPoint, grabOffset;
        sealed class GrabHandle
        {
            public int id;
            public bool active, seen;
            public Vector3 point, offset;
            public Vector2 screen;
        }
        readonly GrabHandle[] handles = { new GrabHandle(), new GrabHandle() };
        public int ActiveGrabCount { get; private set; }
        public int DecorationIndex { get; private set; }
        static readonly string[] DecorationNames = { "PETAL", "LAGOON", "PEACH", "PLAIN" };
        bool mouseWasPressed, touchWasActive;
        Vector2 mirrorBias;
        bool dragging, wasPressed, benchmarking;
        int touchId = -1, oldFrameRate, oldVsync, oldQuality;
        bool oldBackground;
        float shake, brokenAt, benchClock, hudClock, averageMs, benchmarkStarted;
        int hudFrames, benchPhase = -1, benchmarkCycles, benchmarkSkipFrames;
        string notice = "";
        float noticeUntil;
        GUIStyle title, kicker, body, small, button, metric;
        readonly Color ink = new Color(.17f,.23f,.23f);
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        void Awake()
        {
            oldFrameRate = Application.targetFrameRate;
            oldVsync = QualitySettings.vSyncCount;
            oldQuality = QualitySettings.GetQualityLevel();
            oldBackground = Application.runInBackground;
            previousPipeline = QualitySettings.renderPipeline;
            if (mobilePipeline != null) QualitySettings.renderPipeline = mobilePipeline;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = true;
            Build();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged += OnEditorPause;
#endif
        }

        Material OwnMaterial(Shader shader, Color color)
        {
            if (shader == null) throw new InvalidOperationException("Egg Lab shader reference missing. Rebuild scene via Tools/Egg Lab.");
            var m = new Material(shader);
            m.SetColor("_BaseColor", color);
            if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .35f);
            ownedMaterials.Add(m); return m;
        }
        GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material, bool collider)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name;
            g.transform.SetParent(generated.transform, false);
            g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Destroy(g.GetComponent<Collider>());
            return g;
        }
        void Build()
        {
            audioFeedback=GetComponent<EggLabAudio>();
            generated = new GameObject("Generated Egg Lab");
            generated.transform.SetParent(transform, false);
            var floorMat = OwnMaterial(propShader, new Color(.84f,.88f,.82f));
            var plinthMat = OwnMaterial(propShader, new Color(.96f,.94f,.86f));
            var darkMat = OwnMaterial(propShader, ink);
            shellMaterial = OwnMaterial(shellShader, new Color(1,.88f,.7f));
            var jelly = OwnMaterial(slimeShader, new Color(.18f,.78f,.53f));slimeMaterial=jelly;
            Primitive("Backdrop", PrimitiveType.Cube, new Vector3(0,-.36f,0), new Vector3(100,.2f,100),floorMat,true);
            Primitive("Ceramic dish", PrimitiveType.Cylinder,new Vector3(0,-.16f,0),new Vector3(4,.16f,4),plinthMat,true);
            // Cheap grounding disks, avoiding a realtime shadow map on the jelly.
            var shadow = OwnMaterial(propShader,new Color(.60f,.69f,.60f));
            Primitive("Contact shadow",PrimitiveType.Cylinder,new Vector3(0,.006f,0),new Vector3(1.95f,.003f,1.65f),shadow,false);
            shellRoot = new GameObject("Fractured egg"); shellRoot.transform.SetParent(generated.transform,false);
            shellRoot.transform.position = eggCenter;
            ConfigureFragmentGrid();
            CreateVoronoiShell();
            slimeObject = new GameObject("Spring surface slime"); slimeObject.transform.SetParent(generated.transform,false);
            slimeObject.transform.position = eggCenter;
            slimeMesh = SphereMesh(24,16); slimeMesh.name="Dynamic jelly surface"; slimeMesh.MarkDynamic();
            slimeTriangles=slimeMesh.triangles;
            rest=slimeMesh.vertices; packed=new Vector3[rest.Length];
            for(int i=0;i<rest.Length;i++)
            {
                Vector3 direction=new Vector3(rest[i].x/1.05f,rest[i].y/.67f,rest[i].z/1.05f).normalized;
                Vector2 uv=SphereUV(direction);
                packed[i]=EggSurface(uv.x,uv.y)*Mathf.Clamp(slimeFill,.55f,.82f);
                float angle=Mathf.Atan2(direction.z,direction.x);
                float edge=1+.055f*Mathf.Sin(angle*3+.8f)+.035f*Mathf.Sin(angle*5-1);
                rest[i]=new Vector3(direction.x*1.24f*edge,direction.y*.30f,direction.z*1.24f*edge);
                rest[i].y=Mathf.Max(rest[i].y,SlimeFloorY-PuddleCenterY);
            }
            vertices=(Vector3[])packed.Clone(); velocity=new Vector3[rest.Length];
            flowGoals=new Vector3[rest.Length];goals=new Vector3[rest.Length];pullMemory=new Vector3[rest.Length];
            surfaceDynamics=new SlimeSurfaceDynamics(packed,slimeTriangles);restVolume=surfaceDynamics.Volume(rest);
            slimeObject.AddComponent<MeshFilter>().sharedMesh=slimeMesh;
            slimeObject.AddComponent<MeshRenderer>().sharedMaterial=jelly;
            var sc=slimeObject.AddComponent<SphereCollider>(); sc.radius=1.1f;sc.isTrigger=true; // Visual body; debris must not be launched by a coarse slime collider.
            eyeL=Primitive("Left eye",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.095f,darkMat,false).transform;
            eyeR=Primitive("Right eye",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.095f,darkMat,false).transform;
            eyeL.SetParent(slimeObject.transform,false); eyeR.SetParent(slimeObject.transform,false);
            view=Camera.main;
            ApplyDecoration();
            ResetEgg();
        }

        Vector3 EggSurface(float u,float v)
        {
            // Periodic shared warp keeps neighboring curved patches watertight.
            float theta=u*Mathf.PI*2;
            float phi=v*Mathf.PI + .045f*Mathf.Sin(theta*3+v*9)*Mathf.Sin(v*Mathf.PI);
            float angle=theta+.06f*Mathf.Sin(v*13+theta*2);
            float y=Mathf.Cos(phi), r=Mathf.Sin(phi)*(.88f-.13f*y);
            return new Vector3(Mathf.Cos(angle)*r,y*1.22f,Mathf.Sin(angle)*r);
        }
        Vector3 EggNormal(float u,float v)
        {
            const float e=.0001f;
            Vector3 du=EggSurface(u+e,v)-EggSurface(u-e,v);
            Vector3 dv=EggSurface(u,v+e)-EggSurface(u,v-e);
            Vector3 normal=Vector3.Cross(du,dv);
            if(normal.sqrMagnitude<1e-18f)return v<.5f?Vector3.up:Vector3.down;
            return normal.normalized;
        }
        void ConfigureFragmentGrid()
        {
            switch(fragmentSize)
            {
                case ShellFragmentSize.Large24: longitudePieces=6;latitudePieces=4;break;
                case ShellFragmentSize.Tiny96: longitudePieces=12;latitudePieces=8;break;
                default: fragmentSize=ShellFragmentSize.Small48;longitudePieces=8;latitudePieces=6;break;
            }
        }
        public void SetFragmentSize(ShellFragmentSize size)
        {
            if(benchmarking)return;
            fragmentSize=size;
            if(shellRoot==null){ConfigureFragmentGrid();return;}
            // Only the shell is rebuilt; the slime, decorations and materials are reused.
            foreach(var rb in pieces)
            {
                rb.gameObject.SetActive(false);
                var mesh=rb.GetComponent<MeshFilter>().sharedMesh;
                ownedMeshes.Remove(mesh);Destroy(mesh);Destroy(rb.gameObject);
            }
            pieces.Clear();shellColliders.Clear();pieceOrigins.Clear();
            ConfigureFragmentGrid();
            shellRoot.transform.position=eggCenter;shellRoot.transform.rotation=Quaternion.identity;
            CreateVoronoiShell();
            ResetEgg();
        }
        public void CycleFragmentSize()
        { SetFragmentSize((ShellFragmentSize)(((int)fragmentSize+1)%3)); }
        void CreateVoronoiShell()
        {
            var cells=EggVoronoiFracture.Generate(longitudePieces*latitudePieces,fractureSeed,fractureIrregularity);
            for(int i=0;i<cells.Count;i++)CreatePiece(cells[i],i);
        }
        static Vector2 SphereUV(Vector3 direction)
        {
            float u=Mathf.Atan2(direction.z,direction.x)/(Mathf.PI*2);
            if(u<0)u+=1;
            return new Vector2(u,Mathf.Acos(Mathf.Clamp(direction.y,-1,1))/Mathf.PI);
        }
        void CreatePiece(EggVoronoiFracture.Cell cell,int index)
        {
            const int edgeSteps=4,radialRings=2;
            int perimeter=cell.corners.Length*edgeSteps;
            var dirs=new List<Vector3>{cell.seed};
            for(int ring=1;ring<=radialRings;ring++)
            for(int edge=0;edge<cell.corners.Length;edge++)
            for(int step=0;step<edgeSteps;step++)
            {
                Vector3 a=cell.corners[edge],b=cell.corners[(edge+1)%cell.corners.Length];
                // Symmetric chord samples give both neighboring cells identical edges.
                Vector3 boundary=(a*(edgeSteps-step)+b*step).normalized;
                dirs.Add(Vector3.Slerp(cell.seed,boundary,ring/(float)radialRings).normalized);
            }
            var verts=new List<Vector3>();var uv=new List<Vector2>();var seams=new List<Vector3>();var surfaceNormals=new List<Vector3>();var tri=new List<int>();
            Vector2 centerUV=SphereUV(cell.seed);
            for(int k=0;k<dirs.Count;k++)
            {
                Vector2 tex=SphereUV(dirs[k]);
                Vector3 pos=EggSurface(tex.x,tex.y);surfaceNormals.Add(EggNormal(tex.x,tex.y));
                // Unwrap only this cell so Easter paint stays continuous across longitude 0.
                if(tex.x-centerUV.x>.5f)tex.x-=1;else if(tex.x-centerUV.x<-.5f)tex.x+=1;
                verts.Add(pos);uv.Add(tex);
                bool boundary=k>=1+(radialRings-1)*perimeter;
                float distance=boundary?0:EggVoronoiFracture.BoundaryDistance(cell,dirs[k]);
                seams.Add(new Vector3(distance,0,1));
            }
            for(int k=0;k<perimeter;k++){tri.Add(0);tri.Add(1+k);tri.Add(1+(k+1)%perimeter);}
            for(int ring=1;ring<radialRings;ring++)for(int k=0;k<perimeter;k++)
            {
                int a=1+(ring-1)*perimeter+k,b=1+(ring-1)*perimeter+(k+1)%perimeter;
                int c=1+ring*perimeter+k,d=1+ring*perimeter+(k+1)%perimeter;
                tri.Add(a);tri.Add(c);tri.Add(b);tri.Add(b);tri.Add(c);tri.Add(d);
            }
            int n=verts.Count,outerCount=tri.Count;
            for(int k=0;k<n;k++){verts.Add(verts[k]*.96f);uv.Add(uv[k]);seams.Add(Vector3.zero);}
            for(int k=0;k<outerCount;k+=3){tri.Add(tri[k]+n);tri.Add(tri[k+2]+n);tri.Add(tri[k+1]+n);}
            int edgeStart=1+(radialRings-1)*perimeter;
            for(int k=0;k<perimeter;k++)Rim(verts,uv,seams,tri,edgeStart+k,edgeStart+(k+1)%perimeter,n);
            Vector3 center=Vector3.zero;for(int k=0;k<n;k++)center+=verts[k];center/=n;
            for(int k=0;k<verts.Count;k++)verts[k]-=center;
            var mesh=new Mesh{name="Voronoi curved shell "+index};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetUVs(1,seams);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
            var normals=mesh.normals;for(int k=0;k<n;k++){normals[k]=surfaceNormals[k];normals[k+n]=-surfaceNormals[k];}
            mesh.normals=normals;mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var g=new GameObject("Shell Voronoi "+index);g.transform.SetParent(shellRoot.transform,false);g.transform.localPosition=center;
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=shellMaterial;
            var collider=g.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;
            var rb=g.AddComponent<Rigidbody>();rb.mass=.6f/(longitudePieces*latitudePieces);rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
            pieces.Add(rb);shellColliders.Add(collider);pieceOrigins.Add(center);
        }
        static void Rim(List<Vector3> vertices,List<Vector2> uv,List<Vector3> seams,List<int> triangles,int a,int b,int n)
        {
            // Separate rim vertices keep cut-face lighting and crack masks independent.
            int start=vertices.Count;
            int[] source={a,b,a+n,b+n};
            foreach(int index in source){vertices.Add(vertices[index]);uv.Add(uv[index]);seams.Add(new Vector3(.5f,.5f,0));}
            triangles.Add(start);triangles.Add(start+2);triangles.Add(start+1);
            triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start+3);
        }
        Mesh SphereMesh(int around,int rings)
        {
            var v=new Vector3[(around+1)*(rings+1)];var t=new List<int>();
            for(int y=0;y<=rings;y++)for(int x=0;x<=around;x++)
            {
                float p=y/(float)rings*Mathf.PI,a=x/(float)around*Mathf.PI*2;
                v[y*(around+1)+x]=new Vector3(Mathf.Sin(p)*Mathf.Cos(a)*1.05f,Mathf.Cos(p)*.67f,Mathf.Sin(p)*Mathf.Sin(a)*1.05f);
            }
            for(int y=0;y<rings;y++)for(int x=0;x<around;x++)
            {
                int a=y*(around+1)+x,b=a+1,c=a+around+1,d=c+1;
                t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);
            }
            var m=new Mesh();m.vertices=v;m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();ownedMeshes.Add(m);return m;
        }

        public void ResetEgg()
        {
            if(shellRoot==null)return;
            Hits=0;Broken=false;dragging=false;shake=0;MaxDeformation=0;ActiveGrabCount=0;
            foreach(var handle in handles)handle.active=false;
            shellRoot.transform.position=eggCenter;shellRoot.transform.rotation=Quaternion.identity;
            for(int i=0;i<pieces.Count;i++)
            {
                var rb=pieces[i];rb.gameObject.SetActive(true);rb.isKinematic=true;
                rb.transform.localPosition=pieceOrigins[i];rb.transform.localRotation=Quaternion.identity;
                rb.detectCollisions=true;
            }
            shellMaterial.SetFloat("_Damage",0);
            SlimeFlowProgress=0;spillDirection=Vector3.back;SlimeVolumeRatio=1;
            Array.Clear(pullMemory,0,pullMemory.Length);motionSpeed=0;rippleStrength=0;rippleAt=-100;
            if(audioFeedback!=null)audioFeedback.StopAudio();
            slimeObject.transform.position=eggCenter;slimeObject.transform.rotation=Quaternion.identity;
            Array.Copy(packed,vertices,packed.Length);Array.Clear(velocity,0,velocity.Length);
            slimeMesh.vertices=vertices;slimeMesh.RecalculateNormals();slimeMesh.RecalculateBounds();
            // It exists inside the opaque shell from the beginning, rather than spawning on the dish.
            slimeObject.SetActive(true);eyeL.gameObject.SetActive(true);eyeR.gameObject.SetActive(true);
            UpdateSlimeFace();
        }
        public void CycleDecoration()
        {
            DecorationIndex=(DecorationIndex+1)%DecorationNames.Length;
            ApplyDecoration();
        }
        void ApplyDecoration()
        {
            if(shellMaterial==null)return;
            Color cream=new Color(1,.94f,.81f);
            Color pink=new Color(.94f,.49f,.65f), mint=new Color(.40f,.78f,.69f), lilac=new Color(.68f,.56f,.88f);
            if(DecorationIndex==1){pink=new Color(.40f,.69f,.88f);mint=new Color(.98f,.79f,.34f);lilac=new Color(.38f,.78f,.73f);}
            if(DecorationIndex==2){cream=new Color(.95f,.87f,1);pink=new Color(1,.64f,.43f);mint=new Color(.96f,.49f,.64f);lilac=new Color(1,.88f,.60f);}
            shellMaterial.SetColor("_BaseColor",cream);
            shellMaterial.SetColor("_AccentA",pink);shellMaterial.SetColor("_AccentB",mint);shellMaterial.SetColor("_AccentC",lilac);
            shellMaterial.SetFloat("_Decoration",DecorationIndex==3?0:1);
        }
        public void Hit(Vector3 worldPoint)
        {
            if(Broken)return;
            Hits++;shake=.12f;shellMaterial.SetFloat("_Damage",Hits/(float)hitsToBreak);
            if(audioFeedback!=null)audioFeedback.Tap(Hits);
            if(Hits<hitsToBreak)return;
            Broken=true;brokenAt=Time.time;SlimeFlowProgress=0;
            if(audioFeedback!=null)audioFeedback.Break();
            spillDirection=worldPoint-eggCenter;spillDirection.y=0;
            spillDirection=spillDirection.sqrMagnitude>.001f?spillDirection.normalized:Vector3.back;
            slimeObject.transform.position=eggCenter;slimeObject.transform.rotation=Quaternion.identity;
            shellRoot.transform.rotation=Quaternion.identity;shellRoot.transform.position=eggCenter;
            for(int i=0;i<pieces.Count;i++)
            {
                var rb=pieces[i];rb.detectCollisions=true;rb.isKinematic=false;
                // Stable, deterministic launch; a slight outward separation limits overlap impulses.
                Vector3 dir=pieceOrigins[i].normalized;
                rb.transform.position+=dir*.05f;
                rb.linearVelocity=dir*(1.7f+(i%3)*.22f)+Vector3.up*1.4f;
                rb.angularVelocity=new Vector3((i%3)-1,1,(i%5)-2)*3;
            }
            for(int i=0;i<velocity.Length;i++)velocity[i]=Vector3.down*.15f;
        }
        public void SetGrab(Vector3 localPoint,Vector3 offset,bool active)
        {grabPoint=localPoint;grabOffset=Vector3.ClampMagnitude(offset,stretchLimit);dragging=active&&Broken;}

        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.033f);instantMotion=0;
            if(benchmarking) UpdateBenchmark(Time.unscaledDeltaTime); else ReadPointer();
            if(!Broken)
            {
                shake=Mathf.MoveTowards(shake,0,Time.deltaTime);
                float wobble=Mathf.Sin(Time.time*60)*shake;
                shellRoot.transform.rotation=Quaternion.Euler(0,0,wobble*45);
                shellRoot.transform.position=eggCenter+new Vector3(wobble*.18f,0,0);
                slimeObject.transform.SetPositionAndRotation(shellRoot.transform.position,shellRoot.transform.rotation);
            }
            else
            {
                Deform(dt);
                if(Time.time-brokenAt>7) foreach(var rb in pieces) if(rb.gameObject.activeSelf)rb.gameObject.SetActive(false);
            }
            motionSpeed=Mathf.Lerp(motionSpeed,instantMotion,1-Mathf.Exp(-dt*9));
            float audioPan=0;
            if(ActiveGrabCount>0)
            {
                float x=0;foreach(var handle in handles)if(handle.active)x+=handle.screen.x;
                audioPan=(x/ActiveGrabCount-view.pixelRect.center.x)/Mathf.Max(1,view.pixelWidth*.5f);
            }
            if(audioFeedback!=null)audioFeedback.Motion(dragging,motionSpeed,MaxDeformation,Broken&&!IsSlimeSettled,audioPan);
            hudClock+=Time.unscaledDeltaTime;hudFrames++;
            if(hudClock>=.5f){averageMs=hudClock*1000/Mathf.Max(1,hudFrames);hudClock=0;hudFrames=0;}
        }
        void Deform(float dt)
        {
            MaxDeformation=0;
            float age=Mathf.Max(0,Time.time-brokenAt);
            SlimeFlowProgress=Mathf.Clamp01(age/Mathf.Max(.7f,spillDuration));
            float spread=SlimeFlowProgress*SlimeFlowProgress*(3-2*SlimeFlowProgress);
            float fall=Mathf.Clamp01(.5f*9.81f*age*age/(eggCenter.y-PuddleCenterY));
            slimeObject.transform.position=new Vector3(spillDirection.x*.12f*spread,
                Mathf.Lerp(eggCenter.y,PuddleCenterY,fall),spillDirection.z*.12f*spread);
            float floor=SlimeFloorY-slimeObject.transform.position.y;
            float elapsedRipple=Mathf.Max(0,Time.time-rippleAt);
            float rippleEnvelope=Mathf.Exp(-elapsedRipple*3.5f)*rippleStrength;
            float follow=1-Mathf.Exp(-dt*(dragging?14:3.2f));
            for(int i=0;i<vertices.Length;i++)
            {
                float crown=Mathf.InverseLerp(-1,1,packed[i].y),delay=.16f*crown;
                float flow=Mathf.Clamp01((age-delay)/Mathf.Max(.4f,spillDuration-delay));flow=flow*flow*(3-2*flow);
                Vector3 baseline=Vector3.Lerp(packed[i],rest[i],flow);baseline.y=Mathf.Max(baseline.y,floor);flowGoals[i]=baseline;
                Vector3 displacement=Vector3.zero;float totalWeight=0;
                if(dragging)
                {
                    if(ActiveGrabCount>0)
                    {
                        foreach(var handle in handles)if(handle.active)
                        {float w=Mathf.Exp(-(rest[i]-handle.point).sqrMagnitude*2f);displacement+=handle.offset*w;totalWeight+=w;}
                    }
                    else{float w=Mathf.Exp(-(rest[i]-grabPoint).sqrMagnitude*2f);displacement=grabOffset*w;totalWeight=w;}
                    displacement/=Mathf.Max(1,totalWeight);
                }
                pullMemory[i]=Vector3.Lerp(pullMemory[i],displacement,follow);
                float distance=new Vector2(rest[i].x-rippleOrigin.x,rest[i].z-rippleOrigin.z).magnitude;
                float wave=Mathf.Sin(distance*11-elapsedRipple*16)*Mathf.Exp(-distance*1.6f)*rippleEnvelope;
                float top=Mathf.Clamp01((rest[i].y+.12f)/.4f);
                goals[i]=baseline+pullMemory[i]+Vector3.up*(wave*top);
                goals[i].y=Mathf.Max(goals[i].y,floor);
            }
            SlimeVolumeRatio=restVolume>.0001f?surfaceDynamics.Volume(vertices)/restVolume:1;
            float pressure=IsSlimeSettled?Mathf.Clamp(1-SlimeVolumeRatio,-.3f,.3f)*16:0;
            float step=dt/3;
            for(int sub=0;sub<3;sub++)
            {
                surfaceDynamics.Snapshot(vertices,flowGoals,velocity);
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 normal=new Vector3(rest[i].x/(1.24f*1.24f),rest[i].y/.09f,rest[i].z/(1.24f*1.24f)).normalized;
                    Vector3 force=(goals[i]-vertices[i])*springStiffness-velocity[i]*viscosity;
                    force+=surfaceDynamics.Coupling(i,surfaceTension,2.4f)+normal*pressure;
                    velocity[i]=Vector3.ClampMagnitude(velocity[i]+force*step,8);
                    vertices[i]+=velocity[i]*step;
                    if(vertices[i].y<floor){vertices[i].y=floor;velocity[i].y=Mathf.Max(0,velocity[i].y)*.12f;}
                    if(sub==2)MaxDeformation=Mathf.Max(MaxDeformation,(vertices[i]-rest[i]).magnitude);
                }
                surfaceDynamics.Weld(vertices,velocity);
            }
            slimeMesh.vertices=vertices;slimeMesh.RecalculateNormals();slimeMesh.GetNormals(slimeNormals);
            surfaceDynamics.SmoothNormals(slimeNormals);slimeMesh.SetNormals(slimeNormals);slimeMesh.RecalculateBounds();
            slimeMaterial.SetFloat("_Agitation",Mathf.Clamp01(MaxDeformation*.6f+motionSpeed*.06f));
            UpdateSlimeFace();
        }
        void UpdateSlimeFace()
        {
            // Face is attached to the actual surface, in both the packed and spread states.
            slimeMesh.GetNormals(slimeNormals);
            const int left=7*25+17,right=7*25+19;
            eyeL.localPosition=vertices[left]+slimeNormals[left]*.026f;
            eyeR.localPosition=vertices[right]+slimeNormals[right]*.026f;
        }
        bool IsHud(Vector2 pos)
        {
            Rect safe=Application.isEditor ? view.pixelRect : Screen.safeArea;
            float scale=safe.width/390f;
            return pos.y<safe.y+137*scale||pos.y>safe.yMax-210*scale||pos.x<safe.x||pos.x>safe.xMax;
        }
        void ReadPointer()
        {
            int previousCount=ActiveGrabCount;
            foreach(var handle in handles)handle.seen=false;
            bool hasTouch=false;
            var screen=Touchscreen.current;
            if(screen!=null)
            {
                bool screenPressed=false;
                foreach(var touch in screen.touches)if(touch.press.isPressed){screenPressed=true;break;}
                // Free ended IDs before admitting a replacement finger in the same frame.
                foreach(var handle in handles)if(handle.active)
                {
                    if(handle.id<0){if(screenPressed)handle.active=false;continue;}
                    bool stillDown=false;
                    foreach(var touch in screen.touches)
                        if(touch.press.isPressed&&touch.touchId.ReadValue()==handle.id){stillDown=true;break;}
                    if(!stillDown)handle.active=false;
                }
                // Read individual touches, not primaryTouch; IDs survive touch-slot reordering.
                foreach(var touch in screen.touches)
                {
                    if(!touch.press.isPressed)continue;
                    hasTouch=true;
                    TrackPointer(touch.touchId.ReadValue(),touch.position.ReadValue(),touch.press.wasPressedThisFrame);
                }
            }
            if(!hasTouch)
            {
                // End touch grabs before handing control back to a mouse.
                if(touchWasActive)foreach(var handle in handles)handle.active=false;
                var mouse=Mouse.current;
                bool pressed=mouse!=null&&mouse.leftButton.isPressed;
                if(pressed)
                {
                    Vector2 pos=mouse.position.ReadValue();
                    TrackPointer(-1,pos,!mouseWasPressed);
                    bool mirror=Keyboard.current!=null&&Keyboard.current.shiftKey.isPressed;
                    if(mirror&&Broken)
                    {
                        Vector3 center=view.WorldToScreenPoint(slimeObject.transform.position+Vector3.up*.15f);
                        Vector2 mirrored=new Vector2(center.x*2-pos.x,center.y*2-pos.y);
                        bool starting=!handles[1].active||handles[1].id!=-2;
                        if(starting){mirrorBias=Vector2.zero;if((mirrored-pos).sqrMagnitude<900)mirrorBias=Vector2.left*view.pixelWidth*.12f;}
                        TrackPointer(-2,mirrored+mirrorBias,starting);
                    }
                }
                mouseWasPressed=pressed;
                if(mouse!=null)LastPointerPosition=mouse.position.ReadValue();
            }
            else mouseWasPressed=false;
            touchWasActive=hasTouch;
            ActiveGrabCount=0;
            foreach(var handle in handles)
            {
                if(!handle.seen)handle.active=false;
                if(handle.active)ActiveGrabCount++;
            }
            dragging=Broken&&ActiveGrabCount>0;
            PointerPressed=hasTouch||mouseWasPressed;
            if(previousCount>0&&ActiveGrabCount==0)
            {
                rippleAt=Time.time;rippleStrength=Mathf.Lerp(.02f,.055f,Mathf.Clamp01(MaxDeformation));
                if(audioFeedback!=null)audioFeedback.Release(MaxDeformation);
            }
        }
        void TrackPointer(int id,Vector2 pos,bool began)
        {
            if(Broken&&!IsSlimeSettled)return;
            LastPointerPosition=pos;
            GrabHandle selected=null;
            foreach(var handle in handles)if(handle.active&&handle.id==id){selected=handle;break;}
            Ray ray=view.ScreenPointToRay(pos);
            if(selected==null&&began&&!IsHud(pos))
            {
                if(!Broken)
                {
                    if(TryPickEgg(ray,out Vector3 eggHit))Hit(eggHit);
                    return;
                }
                if(TryPickSlime(ray,out Vector3 picked))
                {
                    foreach(var handle in handles)if(!handle.active){selected=handle;break;}
                    if(selected!=null)
                    {
                        selected.id=id;selected.point=picked;selected.offset=Vector3.zero;selected.active=true;
                        rippleOrigin=picked;rippleAt=Time.time;rippleStrength=.018f;
                        if(audioFeedback!=null)audioFeedback.Grab();
                    }
                }
            }
            if(selected==null)return;
            selected.seen=true;selected.screen=pos;
            var plane=new Plane(-view.transform.forward,slimeObject.transform.TransformPoint(selected.point));
            if(plane.Raycast(ray,out float distance))
            {
                Vector3 pull=slimeObject.transform.InverseTransformPoint(ray.GetPoint(distance))-selected.point;
                Vector3 pressure=slimeObject.transform.InverseTransformDirection(ray.direction).normalized*.38f;
                Vector3 offset=Vector3.ClampMagnitude(pull*1.25f+pressure,stretchLimit);
                instantMotion=Mathf.Max(instantMotion,(offset-selected.offset).magnitude/Mathf.Max(.008f,Time.deltaTime));
                selected.offset=offset;
            }
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused)return;
            foreach(var handle in handles)handle.active=false;
            ActiveGrabCount=0;dragging=false;mouseWasPressed=false;touchWasActive=false;
            if(audioFeedback!=null)audioFeedback.StopAudio();
        }

#if UNITY_EDITOR
        void OnEditorPause(UnityEditor.PauseState state)
        {
            if (benchmarking) benchmarkSkipFrames = 2;
        }
#endif
        // The contained slime trigger can extend beyond the shell. Pick shell pieces
        // directly so other colliders cannot consume an egg tap.
        bool TryPickEgg(Ray ray,out Vector3 point)
        {
            float closest=50f;bool found=false;point=Vector3.zero;
            foreach(var collider in shellColliders)
            {
                if(collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy)continue;
                if(collider.Raycast(ray,out RaycastHit hit,closest))
                { closest=hit.distance;point=hit.point;found=true; }
            }
            return found;
        }

        // Pick the displayed deformed surface on pointer-down, independent of debris
        // and the coarse physics collider. Triangle tests run only when a grab begins.
        bool TryPickSlime(Ray worldRay,out Vector3 point)
        {
            Vector3 o=slimeObject.transform.InverseTransformPoint(worldRay.origin);
            Vector3 d=slimeObject.transform.InverseTransformDirection(worldRay.direction).normalized;
            float closest=float.MaxValue;bool found=false;point=Vector3.zero;
            for(int k=0;k<slimeTriangles.Length;k+=3)
            {
                Vector3 a=vertices[slimeTriangles[k]],b=vertices[slimeTriangles[k+1]],c=vertices[slimeTriangles[k+2]];
                Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,p);
                if(Mathf.Abs(det)<.000001f)continue;
                float inv=1/det;Vector3 t=o-a;float u=Vector3.Dot(t,p)*inv;
                if(u<0||u>1)continue;
                Vector3 q=Vector3.Cross(t,e1);float v=Vector3.Dot(d,q)*inv;
                if(v<0||u+v>1)continue;
                float distance=Vector3.Dot(e2,q)*inv;
                if(distance>0&&distance<closest){closest=distance;found=true;}
            }
            if(found)point=o+d*closest;
            return found;
        }
        public void StartBenchmark()
        {
            if(benchmarking)return;
            frameTimes.Clear();framePhases.Clear();benchClock=0;benchPhase=-1;benchmarkCycles=0;
            benchmarking=true;benchmarkStarted=Time.realtimeSinceStartup;LastReport="";benchmarkSkipFrames=2;
        }
        void UpdateBenchmark(float dt)
        {
            if(benchmarkSkipFrames>0){benchmarkSkipFrames--;return;}
            // Each 5-second phase: 2s warm-up, 3s measured. 20 seconds in total.
            benchClock+=dt;int phase=Mathf.Min(3,(int)(benchClock/5));
            if(phase!=benchPhase)
            {
                benchPhase=phase;ResetEgg();
                if(phase==1){Hit(eggCenter);Hit(eggCenter);Hit(eggCenter);}
                if(phase>=2)for(int i=0;i<hitsToBreak;i++)Hit(eggCenter);
            }
            if(phase==2&&((int)((benchClock-10)/1.7f))>benchmarkCycles)
            {
                benchmarkCycles++;ResetEgg();for(int i=0;i<hitsToBreak;i++)Hit(eggCenter);
            }
            if(phase==3)
            {
                ActiveGrabCount=2;dragging=true;
                for(int i=0;i<2;i++)
                {
                    float sign=i==0?-1:1;
                    handles[i].active=true;
                    handles[i].point=new Vector3(sign*.42f,.2f,-.8f);
                    handles[i].offset=new Vector3(sign*(.3f+Mathf.Sin(benchClock*3)*.35f),sign*Mathf.Cos(benchClock*2)*.25f,0);
                }
            }
            if(benchClock%5>=2&&frameTimes.Count<8192){frameTimes.Add(dt*1000);framePhases.Add(phase);}
            if(benchClock>=20){benchmarking=false;dragging=false;WriteReport();}
        }
        void WriteReport()
        {
            try
            {
                string folder=Path.Combine(Application.persistentDataPath,"EggLabBenchmarks");Directory.CreateDirectory(folder);
                string stem=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss",Inv);
                string[] phases={"intact","cracked","fracture_cycles","two_finger_slime"};
                var raw=new StringBuilder("phase,frame_ms\n");
                for(int i=0;i<frameTimes.Count;i++)raw.Append(phases[framePhases[i]]).Append(',').Append(frameTimes[i].ToString("F4",Inv)).Append('\n');
                File.WriteAllText(Path.Combine(folder,stem+"-frames.csv"),raw.ToString());
                var summary=new StringBuilder();
                summary.AppendLine("Unity: "+Application.unityVersion+" | Platform: "+Application.platform+" | Editor: "+Application.isEditor);
                summary.AppendLine("Device: "+SystemInfo.deviceModel+" | GPU: "+SystemInfo.graphicsDeviceName);
                summary.AppendLine("Resolution: "+Screen.width+"x"+Screen.height+" | cap: "+Application.targetFrameRate+" | quality: "+QualitySettings.names[QualitySettings.GetQualityLevel()]);
                summary.AppendLine("Pipeline: "+GraphicsSettings.currentRenderPipeline.name+" | fragments: "+FragmentCount+" | slime vertices: "+SlimeVertexCount);
                summary.AppendLine("Frame pacing only (unscaledDeltaTime); not CPU/GPU timings. Two-second warm-up per phase. First two frames after start/Editor pause are excluded. P95 nearest-rank.");
                summary.AppendLine("phase,samples,mean_ms,p95_ms,max_ms,fps,over_16.67_percent");
                for(int p=0;p<4;p++)
                {
                    var a=new List<float>();float total=0;int over=0;
                    for(int i=0;i<frameTimes.Count;i++)if(framePhases[i]==p){a.Add(frameTimes[i]);total+=frameTimes[i];if(frameTimes[i]>16.667f)over++;}
                    a.Sort();if(a.Count==0)continue;float mean=total/a.Count;
                    summary.AppendLine(string.Format(Inv,"{0},{1},{2:F3},{3:F3},{4:F3},{5:F1},{6:F1}",phases[p],a.Count,mean,a[Mathf.Clamp(Mathf.CeilToInt(a.Count*.95f)-1,0,a.Count-1)],a[a.Count-1],1000/mean,100f*over/a.Count));
                }
                string path=Path.Combine(folder,stem+"-summary.txt");File.WriteAllText(path,summary.ToString());LastReport=path;
                notice="Report saved";noticeUntil=Time.unscaledTime+5;
                Debug.Log("[Egg Lab] Benchmark saved: "+path+"\n"+summary);
            }
            catch(Exception e){notice="Export failed: "+e.Message;noticeUntil=Time.unscaledTime+10;Debug.LogException(e);}
        }
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold};title.normal.textColor=ink;
            kicker=new GUIStyle(GUI.skin.label){fontSize=11,fontStyle=FontStyle.Bold};kicker.normal.textColor=new Color(.32f,.43f,.38f);
            body=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleCenter};body.normal.textColor=ink;
            small=new GUIStyle(body){fontSize=11};
            metric=new GUIStyle(body){fontSize=12,alignment=TextAnchor.MiddleRight};
            button=new GUIStyle(GUI.skin.button){fontSize=14,fontStyle=FontStyle.Bold};button.normal.textColor=ink;
        }
        void OnGUI()
        {
            Styles();GUI.enabled=true;GUI.color=Color.white;GUI.contentColor=Color.white;
            Rect safe=Screen.safeArea;float scale=safe.width/390f;float h=safe.height/scale;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax,0),Quaternion.identity,new Vector3(scale,scale,1));
            GUI.Label(new Rect(26,23,300,20),"TACTILE PLAYGROUND  /  01",kicker);
            if(audioFeedback!=null&&GUI.Button(new Rect(272,22,92,25),audioFeedback.muted?"ASMR OFF":"ASMR ON",small))audioFeedback.SetMuted(!audioFeedback.muted);
            if(audioFeedback!=null)
            {
                float volume=GUI.HorizontalSlider(new Rect(272,57,92,16),audioFeedback.masterVolume,0,1);
                if(Mathf.Abs(volume-audioFeedback.masterVolume)>.002f)audioFeedback.SetMasterVolume(volume);
            }
            GUI.Label(new Rect(23,47,330,58),"Egg & goo.",title);
            GUI.Label(new Rect(26,109,330,26),Broken?"A little creature. A lot of stretch.":"Something soft is waiting inside.",new GUIStyle(body){alignment=TextAnchor.MiddleLeft,fontSize=14});
            GUI.Label(new Rect(26,145,190,24),Broken?"02  /  STRETCH & SQUISH":"01  /  CRACK THE SHELL",kicker);
            GUI.enabled=!benchmarking;
            if(!Broken&&GUI.Button(new Rect(26,176,160,27),"EASTER / "+DecorationNames[DecorationIndex],button))CycleDecoration();
            if(!Broken&&GUI.Button(new Rect(197,176,167,27),"PIECES / "+FragmentCount,button))CycleFragmentSize();
            GUI.enabled=true;
            GUI.Label(new Rect(210,145,155,24),averageMs>0?(1000/averageMs).ToString("F0")+" FPS  ·  "+averageMs.ToString("F1")+" ms":"Warming up",metric);
            float y=h-137;
            GUI.Label(new Rect(20,y,350,30),benchmarking?"Benchmark  "+Mathf.Min(20,benchClock).ToString("F0")+" / 20 s":Broken?(!IsSlimeSettled?"Let it flow...":ActiveGrabCount==2?"Two fingers! Pinch, stretch, and twist.":dragging?"Holding! Move to stretch, release to bounce.":"Hold to squish. Drag to stretch."):"Tap the egg. Make a little mess.",body);
            GUI.Label(new Rect(20,y+30,350,20),benchmarking?"Intact → cracks → fracture → stretch":Broken?(Application.isMobilePlatform?"Use two fingers to pull two points.":"SHIFT + drag = two fingers"):"SHELL STRENGTH   "+Mathf.Max(0,hitsToBreak-Hits)+" / "+hitsToBreak,small);
            if(Broken&&ActiveGrabCount==2)
            {
                foreach(var handle in handles)if(handle.active)
                {
                    float px=(handle.screen.x-safe.x)/scale,py=(Screen.height-handle.screen.y-(Screen.height-safe.yMax))/scale;
                    GUI.Label(new Rect(px-9,py-12,24,24),"+",body);
                }
            }
            GUI.enabled=!benchmarking;
            if(GUI.Button(new Rect(26,h-70,159,40),"NEW EGG",button))ResetEgg();
            if(GUI.Button(new Rect(197,h-70,167,40),"BENCHMARK  ↗",button))StartBenchmark();
            GUI.enabled=true;
            if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(20,h-26,350,22),notice,small);
        }
        void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged -= OnEditorPause;
#endif
            QualitySettings.renderPipeline=previousPipeline;
            Application.targetFrameRate=oldFrameRate;Application.runInBackground=oldBackground;
            QualitySettings.SetQualityLevel(oldQuality,true);QualitySettings.vSyncCount=oldVsync;
            foreach(var m in ownedMeshes)if(m!=null)Destroy(m);
            foreach(var m in ownedMaterials)if(m!=null)Destroy(m);
        }
    }
}
