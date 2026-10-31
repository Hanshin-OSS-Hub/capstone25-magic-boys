#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BabelDwarfVerification
{
    private const string Key = "MagicBoys.DwarfVerification";
    private static int stage;
    private static double at;
    private static readonly StringBuilder report = new StringBuilder();
    private static Vector3[] resting;
    private static Vector3 initialHip;

    static BabelDwarfVerification()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key,false) || state != PlayModeStateChange.EnteredPlayMode) return;
            stage=0; at=EditorApplication.timeSinceStartup; report.Clear(); SessionState.SetBool(Key+".Ready",true);
        };
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(BabelGameplayIntegration.DwarfRoot + "/Babel_Dwarf_Compare.unity");
        SessionState.SetBool(Key,true); SessionState.SetBool(Key+".Ready",false);
        SessionState.SetFloat(Key+".Started",(float)EditorApplication.timeSinceStartup);
        EditorApplication.EnterPlaymode();
    }

    private static Vector3[] Bake(SkinnedMeshRenderer renderer)
    {
        var mesh = new Mesh(); renderer.BakeMesh(mesh); var values=mesh.vertices; Object.DestroyImmediate(mesh); return values;
    }

    private static void Capture(BabelLookPreview look,string name,Vector3 offset)
    {
        Transform p=look.appearance.transform;
        Camera camera=look.portraitCamera;
        camera.transform.position=p.TransformPoint(offset);
        camera.transform.LookAt(p.position+Vector3.up*1.0f);
        BabelGameplayIntegration.Capture(camera,BabelGameplayIntegration.DwarfRoot+"/Previews/"+name+".png");
    }

    private static void Assert(bool value,string message) { if(!value)throw new Exception(message); }

    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Started",0)>150){Finish(false,"시간 초과");return;}
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||!SessionState.GetBool(Key+".Ready",false))return;
        if(EditorApplication.timeSinceStartup-at<(stage==0?3:.7))return;
        try
        {
            var look=Object.FindFirstObjectByType<BabelLookPreview>();
            var app=look.appearance;
            var sync=app.GetComponent<BabelDwarfMotion>();
            var pants=app.clothing.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Pants_low");
            var root=app.transform;
            Vector3 hip=root.InverseTransformPoint(sync.visual.GetBoneTransform(HumanBodyBones.Hips).position);
            report.AppendLine($"단계 {stage}: root={root.position}, nativeHipLocal={hip}, nativeScale={sync.visual.humanScale}, gameplayScale={sync.gameplay.humanScale}");
            Assert(hip.y>-.1f&&hip.y<1.6f&&Mathf.Abs(hip.x)<.8f&&Mathf.Abs(hip.z)<1f,"드워프 포즈 위치 범위 오류");
            switch(stage)
            {
                case 0:
                    Validate(app);
                    Assert(app.GetComponent<CharacterController>().isGrounded,"바닥 접지 실패");
                    look.Inspect(true);
                    Capture(look,"01_Front",new Vector3(.7f,1.3f,2.8f));
                    initialHip=hip;resting=Bake(pants);
                    break;
                case 1:
                    Capture(look,"02_Side",new Vector3(2.8f,1.25f,.4f));
                    break;
                case 2:
                    Capture(look,"03_Back",new Vector3(-.7f,1.3f,-2.8f));
                    // 월드 좌표/회전이 포즈에 중복 적용되지 않는지 검사한다.
                    app.GetComponent<CharacterController>().enabled=false;
                    root.SetPositionAndRotation(new Vector3(3,.12f,1),Quaternion.Euler(0,90,0));
                    break;
                case 3:
                    Assert(Vector3.Distance(hip,initialHip)<.2f,"이동/90도 회전 후 인물 루트 좌표가 어긋남");
                    Capture(look,"04_Rotated",new Vector3(.7f,1.3f,2.8f));
                    root.SetPositionAndRotation(new Vector3(0,.12f,-5.5f),Quaternion.identity);
                    sync.gameplay.SetFloat("Speed",2);sync.gameplay.SetFloat("MotionSpeed",1);
                    break;
                case 4:
                    float delta=Bake(pants).Select((v,i)=>Vector3.Distance(v,resting[i])).Max();
                    Assert(delta>.01f,"걷기 포즈 전달 실패");
                    report.AppendLine($"걷기 포즈 바지 최대 정점 변화 {delta:F4}m");
                    Capture(look,"05_Walk",new Vector3(2.8f,1.3f,.7f));
                    sync.gameplay.SetFloat("Speed",0);sync.gameplay.SetTrigger("Attack");
                    break;
                case 5:
                    Capture(look,"06_Attack",new Vector3(.7f,1.3f,2.8f));
                    report.AppendLine("공격 트리거 후 렌더. 실제 타격 판정/음성은 별도 수동 검증 대상.");
                    look.Inspect(false);app.GetComponent<StarterAssets.ThirdPersonController>().enabled=false;
                    look.cameraSwitch.isFirstPerson=true;
                    break;
                case 6:
                    Assert(app.headRenderers.All(r=>r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly),"1인칭 머리 숨김 실패");
                    BabelGameplayIntegration.Capture(look.cameraSwitch.cam,BabelGameplayIntegration.DwarfRoot+"/Previews/07_FirstPerson.png");
                    app.Show(false);
                    break;
                case 7:
                    Assert(app.originalBody.enabled&&!app.clothing.activeSelf,"F8 원본 전환 실패");
                    app.Show(true);look.cameraSwitch.isFirstPerson=false;
                    break;
                case 8:
                    BabelGameplayIntegration.Capture(look.cameraSwitch.cam,BabelGameplayIntegration.DwarfRoot+"/Previews/08_ThirdPerson.png");
                    look.cameraSwitch.isFirstPerson=true;
                    break;
                case 9:
                    BabelGameplayIntegration.Capture(look.cameraSwitch.cam,BabelGameplayIntegration.DwarfRoot+"/Previews/09_FirstPerson_Idle.png");
                    look.Inspect(true);sync.gameplay.Play("Roll",0,.2f);sync.gameplay.Update(0);sync.gameplay.speed=0;
                    break;
                case 10:
                    Capture(look,"09_Roll_Start",new Vector3(2.8f,1.3f,.7f));
                    sync.gameplay.Play("Roll",0,.5f);sync.gameplay.Update(0);
                    break;
                case 11:
                    Capture(look,"10_Roll_Middle",new Vector3(2.8f,1.3f,.7f));
                    Assert(sync.gameplay.GetCurrentAnimatorStateInfo(0).IsName("Roll"),"구르기 포즈 샘플 실패");
                    report.AppendLine("검증: 리깅 FBX/시안 메시 일치, 접지, 이동/회전 포즈, 걷기 변형, 공격 후 렌더, 구르기 20%/50% 포즈, 1인칭 상체·장비 숨김, 외형 전환.");
                    report.AppendLine("미검증: 실제 키보드 이동/점프/구르기 이동, 공격 판정, 숫자1/Q 마이크, 포털 왕복, 발표 PC 성능.");
                    Finish(true,report.ToString());return;
            }
            stage++;at=EditorApplication.timeSinceStartup;
        }
        catch(Exception e){Finish(false,report+"\n"+e);}
    }

    private static void Validate(BabelMageAppearance app)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(BabelGameplayIntegration.DwarfRoot+"/Rigged/Dwarf_Humanoid.fbx");
        var originals=source.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(var renderer in app.clothing.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            string originalName=renderer.name=="Dwarf_Head"?"Body_low":renderer.name=="Dwarf_Cuffs"?"Gloves_low":renderer.name;
            var original=originals.First(r=>r.name==originalName);
            var a=renderer.sharedMesh;var b=original.sharedMesh;
            Assert(a.vertices.SequenceEqual(b.vertices),"정점 좌표가 변경됨: "+renderer.name);
            Assert(a.boneWeights.SequenceEqual(b.boneWeights)&&a.bindposes.SequenceEqual(b.bindposes),"스킨/바인드 변경: "+renderer.name);
            Assert(a.normals.SequenceEqual(b.normals),"원본 노멀 변경: "+renderer.name);
        }
        var sync=app.GetComponent<BabelDwarfMotion>();
        Assert(!sync.visual.enabled&&sync.visual.runtimeAnimatorController==null,"시안에 중복 애니메이션 실행기 존재");
        var expected=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/StarterAssets/ThirdPersonController/Character/Models/Armature.fbx");
        Assert(sync.gameplay.avatar==expected.GetComponent<Animator>().avatar,"게임플레이 Avatar 변경");
        Assert(app.clothing.GetComponentsInChildren<Collider>(true).Length==0,"인물 장식 충돌체 존재");
        var room=Object.FindFirstObjectByType<BabelRoomPreview>();
        Assert(room.babelRoom.GetComponent<MeshCollider>().sharedMesh==room.originalRoom.GetComponent<MeshCollider>().sharedMesh,"바닥 충돌 메시 변경");
        report.AppendLine("Unity 시안 메시와 별도 리깅 FBX 정점·노멀·스킨 웨이트·바인드 포즈 동일. 원본 게임플레이 Avatar/방 충돌 유지. 선택한 드워프 원본 장비 사용.");
    }

    private static void Finish(bool success,string message)
    {
        SessionState.SetBool(Key,false);
        File.WriteAllText("WhisperServer/.runtime/babel-dwarf-play-verification.txt",message);
        if(success)Debug.Log("[Babel Dwarf Verify] "+message);else Debug.LogError("[Babel Dwarf Verify] "+message);
        EditorApplication.Exit(success?0:1);
    }
}
#endif
