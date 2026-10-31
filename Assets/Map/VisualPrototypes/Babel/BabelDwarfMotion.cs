using UnityEngine;

// 원본 게임플레이 이벤트는 유지하고 드워프 시각 리그에 자세만 전달한다.
[DefaultExecutionOrder(100)]
public sealed class BabelDwarfMotion : MonoBehaviour
{
    public Animator gameplay, visual;
    public BabelMageAppearance appearance;
    public Transform weapon;
    private HumanPoseHandler from, to;
    private HumanPose pose;
    private Transform sourceHand, dwarfHand;
    private Transform sourceLeft, leftUpper, leftLower, leftHand;
    private float gripBlend;
    private Vector3 weaponLocal;
    private Quaternion weaponRotation;

    private void Start()
    {
        from = new HumanPoseHandler(gameplay.avatar, gameplay.transform);
        to = new HumanPoseHandler(visual.avatar, visual.transform);
        sourceHand = gameplay.GetBoneTransform(HumanBodyBones.RightHand);
        dwarfHand = visual.GetBoneTransform(HumanBodyBones.RightHand);
        sourceLeft = gameplay.GetBoneTransform(HumanBodyBones.LeftHand);
        leftUpper = visual.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLower = visual.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = visual.GetBoneTransform(HumanBodyBones.LeftHand);
        weaponLocal = weapon.localPosition; weaponRotation = weapon.localRotation;
    }

    private void LateUpdate()
    {
        if (from == null) return;
        if (!appearance.dressed)
        {
            weapon.SetLocalPositionAndRotation(weaponLocal, weaponRotation);
            return;
        }
        from.GetHumanPose(ref pose);
        // 종족 고유의 다리/몸통 길이를 보존한다. 사람의 월드 골반 높이를 강제하지 않는다.
        pose.bodyPosition = gameplay.transform.InverseTransformPoint(pose.bodyPosition * gameplay.humanScale) / gameplay.humanScale;
        pose.bodyRotation = Quaternion.Inverse(visual.transform.rotation) * pose.bodyRotation;
        to.SetHumanPose(ref pose);
        // 기존 망치 객체/판정 참조를 유지하며 손의 체형 차이만 보정한다. 누적 이동하지 않는다.
        weapon.SetPositionAndRotation(weapon.parent.TransformPoint(weaponLocal) + dwarfHand.position - sourceHand.position,
            weapon.parent.rotation * weaponRotation);
        // 대기 자세의 양손 잡기만 보정한다. 관절 회전으로 풀고 뼈 길이/메시를 늘이지 않는다.
        bool holding = gameplay.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend") && gameplay.GetFloat("Speed") < .1f && !gameplay.IsInTransition(0);
        gripBlend = Mathf.MoveTowards(gripBlend, holding ? 1 : 0, Time.deltaTime * 8);
        if (gripBlend > 0)
            MatchLeftGrip(Vector3.Lerp(leftHand.position, sourceLeft.position + dwarfHand.position - sourceHand.position, gripBlend));
    }

    private void MatchLeftGrip(Vector3 target)
    {
        Vector3 a=leftUpper.position, b=leftLower.position, c=leftHand.position;
        float upper=Vector3.Distance(a,b), lower=Vector3.Distance(b,c);
        Vector3 direction=(target-a).normalized;
        float distance=Mathf.Clamp(Vector3.Distance(a,target), Mathf.Abs(upper-lower)+.001f, upper+lower-.001f);
        target=a+direction*distance;
        Vector3 pole=Vector3.ProjectOnPlane(b-a,direction).normalized;
        if(pole.sqrMagnitude<.1f)pole=Vector3.ProjectOnPlane(-transform.forward,direction).normalized;
        float along=(upper*upper+distance*distance-lower*lower)/(2*distance);
        Vector3 elbow=a+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        Quaternion handRotation=leftHand.rotation;
        leftUpper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*leftUpper.rotation;
        leftLower.rotation=Quaternion.FromToRotation(leftHand.position-leftLower.position,target-leftLower.position)*leftLower.rotation;
        leftHand.rotation=handRotation;
    }

    private void OnDestroy() { from?.Dispose(); to?.Dispose(); }
}
