#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class BabelGameplayVerification
{
    const string Key="MagicBoys.GameplayVerification";
    static int stage, seed, playerId;
    static double at;
    static Transform player;
    static DungeonGenerator generator;
    static int[] tiles;
    static Vector3 returnPosition;
    static readonly StringBuilder report=new StringBuilder();
    static BabelGameplayVerification()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
            DungeonGenerator.saveSeed=2056123686;
            stage=0;at=EditorApplication.timeSinceStartup;report.Clear();SessionState.SetBool(Key+"Ready",true);
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(BabelGameplayIntegration.ScenePath);
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Ready",false);
        SessionState.SetFloat(Key+"Started",(float)EditorApplication.timeSinceStartup);
        EditorApplication.EnterPlaymode();
    }
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Started",0)>180){Finish(false,"시간 초과 stage="+stage);return;}
        if(!EditorApplication.isPlaying||!SessionState.GetBool(Key+"Ready",false)||EditorApplication.timeSinceStartup-at<3)return;
        try
        {
            if(stage==0)
            {
                generator=Object.FindFirstObjectByType<DungeonGenerator>();
                if(generator==null||generator.dungeonState!=DungeonGenState.completed)return;
                player=GameObject.FindGameObjectWithTag("Player").transform;playerId=player.GetInstanceID();
                tiles=generator.genneratedTiles.Select(t=>t.tile.GetInstanceID()).ToArray();seed=DungeonGenerator.saveSeed;
                returnPosition=player.position;
                CheckPlayer();
                Assert(Object.FindFirstObjectByType<BabelLookPreview>()==null,"비교 UI가 게임에 남음");
                report.AppendLine($"생성 완료: seed={seed}, tile={tiles.Length}, player={playerId}, pos={returnPosition}");
                Capture("01_Start_FirstPerson");
                var camera=player.GetComponentInChildren<CameraSwitch>().cam;
                var nearby=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&Vector3.Distance(r.bounds.ClosestPoint(player.position),player.position)<15);
                File.WriteAllLines("WhisperServer/.runtime/babel-nearby-renderers.txt",nearby.Select(r=>$"{r.name} parent={r.transform.parent?.name} bounds={r.bounds} mats={string.Join(",",r.sharedMaterials.Select(m=>m?.name))}"));
                foreach(var hit in Physics.RaycastAll(camera.ViewportPointToRay(new Vector3(.85f,.1f,0)),50))
                    report.AppendLine($"시각 조사 광선: {hit.collider.name} pos={hit.point} distance={hit.distance}");
                player.GetComponentInChildren<CameraSwitch>().isFirstPerson=false;
            }
            else if(stage==1)
            {
                Capture("02_Start_ThirdPerson");Enter();
            }
            else if(stage==2||stage==4)
            {
                if(PuzzleTravelManager.Instance.IsLoading)return;
                Assert(SceneManager.GetActiveScene().name=="Puzzle","퍼즐 진입 실패");CheckPlayer();
                report.AppendLine($"퍼즐 진입 {stage/2}: player={player.GetInstanceID()}, pos={player.position}, grounded={player.GetComponent<CharacterController>().isGrounded}");
                PuzzleTravelManager.Instance.ReturnToDungeon();
            }
            else if(stage==3||stage==5)
            {
                if(PuzzleTravelManager.Instance.IsLoading)return;
                Assert(SceneManager.GetActiveScene().name=="0326_MapMerge","던전 복귀 실패");
                Assert(tiles.SequenceEqual(generator.genneratedTiles.Select(t=>t.tile.GetInstanceID())),"던전 객체 재생성");
                Assert(seed==DungeonGenerator.saveSeed,"시드 변경");
                Assert(player.GetInstanceID()==playerId,"플레이어 재생성");
                Assert(Vector3.Distance(player.position,returnPosition)<1f,"복귀 위치 이탈");CheckPlayer();
                var voice=Object.FindFirstObjectByType<VoiceCommandSystem>();
                Assert(voice!=null&&voice.whisper!=null&&voice.magicAttack==player.GetComponent<MagicAttack>(),"음성 명령 참조 이탈");
                report.AppendLine($"복귀 {(stage-1)/2}: 동일 타일 객체/시드/플레이어/음성 대상, pos={player.position}");
                if(stage==3)Enter();
                else {Capture("03_After_Two_Returns");Finish(true,"자동 씬 전환 2회 통과. E/Q 실제 입력·마이크·공격 판정은 별도 수동 확인.");return;}
            }
            stage++;at=EditorApplication.timeSinceStartup;
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void CheckPlayer()
    {
        Assert(player.gameObject.activeInHierarchy,"플레이어 비활성");
        Assert(player.GetComponent<CharacterController>().isGrounded,"바닥 접지 실패");
        var motion=player.GetComponent<BabelDwarfMotion>();
        Assert(motion!=null&&motion.visual.avatar.isValid&&!motion.visual.enabled,"드워프 리그 오류");
        Assert(!motion.appearance.originalBody.enabled&&motion.appearance.dressedBody.enabled,"외형 표시 오류");
        var hip=player.InverseTransformPoint(motion.visual.GetBoneTransform(HumanBodyBones.Hips).position);
        Assert(hip.y>0&&hip.y<1.6f&&Mathf.Abs(hip.x)<1,"드워프 월드 포즈 이탈");
    }
    static void Enter()
    {
        // E 입력 대신 같은 전환 API를 사용한다. 테스트 복귀 앵커는 안전한 시작 위치다.
        PuzzleTravelManager.Instance.EnterPuzzleRoom("Puzzle",player);
    }
    static void Capture(string name)=>BabelGameplayIntegration.Capture(player.GetComponentInChildren<CameraSwitch>().cam,"WhisperServer/.runtime/gameplay-previews/"+name+".png");
    static void Finish(bool success,string message)
    {
        SessionState.SetBool(Key,false);report.AppendLine(message);
        File.WriteAllText("WhisperServer/.runtime/babel-gameplay-verification.txt",report.ToString());
        if(success)Debug.Log("[Babel Gameplay Verify] "+report);else Debug.LogError("[Babel Gameplay Verify] "+report);
        EditorApplication.Exit(success?0:1);
    }
}
#endif
