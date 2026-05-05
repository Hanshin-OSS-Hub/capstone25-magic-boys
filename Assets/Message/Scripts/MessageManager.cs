using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MessageManager : MonoBehaviour
{
    public static MessageManager instance;

    [Header("UI Elements")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI TextKeyGuide;

    [Header("Input Settings")]
    public KeyCode nextMessageKey = KeyCode.Backslash;

    private Queue<string> messageQueue = new Queue<string>();
    private bool isMessageActive = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        messagePanel.SetActive(false);
    }

    private void Update()
    {
        if (isMessageActive && Input.GetKeyDown(nextMessageKey))
        {
            DisplayNextMessage();
        }
    }

    public void StartMessage(string[] lines)
    {
        messageQueue.Clear();

        foreach (string line in lines)
        {
            messageQueue.Enqueue(line);
        }

        TextKeyGuide.text = $"Press: '{nextMessageKey.ToString()}'";

        messagePanel.SetActive(true);
        isMessageActive = true;
        DisplayNextMessage();
    }

    public void DisplayNextMessage()
    {
        if (messageQueue.Count == 0)
        {
            EndMessage();
            return;
        }

        string nextMessage = messageQueue.Dequeue();
        messageText.text = nextMessage;
    }

    private void EndMessage()
    {
        messagePanel.SetActive(false);
        isMessageActive = false;
    }
}