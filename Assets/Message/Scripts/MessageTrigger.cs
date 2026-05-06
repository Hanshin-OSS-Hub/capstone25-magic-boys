using UnityEngine;

public class MessageTrigger : MonoBehaviour
{
    [Header("메세지 목록")]
    [TextArea(3, 10)]
    public string[] lines;

    [Header("설정")]
    public bool triggerOnce = true;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;

            MessageManager.instance.StartMessage(lines);
            hasTriggered = true;
        }
    }
}