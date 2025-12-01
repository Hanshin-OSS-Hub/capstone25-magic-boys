using UnityEngine;
using StarterAssets;

public class LockOnTarget : MonoBehaviour
{
    public string bossTag = "Boss";
    public KeyCode toggleKey = KeyCode.T;

    private ThirdPersonController controller;
    private Transform bossTarget;
    private bool isLockedOn = false;

    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        if (controller == null)
        {
            Debug.LogError("LockOnTarget: ThirdPersonController를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isLockedOn)
            {
                TryLockOn();
            }
            else
            {
                Unlock();
            }
        }
    }

    void TryLockOn()
    {
        GameObject bossObj = GameObject.FindGameObjectWithTag(bossTag);
        if (bossObj == null)
        {
            Debug.Log("LockOnTarget: Boss 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        bossTarget = bossObj.transform;
        isLockedOn = true;

        controller.IsLockOn = true;
        controller.LockOnTarget = bossTarget;

        // 카메라 입력을 잠가두고 싶으면 true, 마우스로 약간 보정하고 싶으면 false
        controller.LockCameraPosition = true;
    }

    void Unlock()
    {
        isLockedOn = false;
        bossTarget = null;

        controller.IsLockOn = false;
        controller.LockOnTarget = null;
        controller.LockCameraPosition = false;
    }
}
