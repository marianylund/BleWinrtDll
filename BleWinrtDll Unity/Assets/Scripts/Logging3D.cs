using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Logging3D : MonoBehaviour
{
    public TMP_Text consoleText;
    public int maxMsg = 5;

    private Queue<string> msg = new Queue<string>();

    void OnEnable ()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable ()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog (string message, string stackTrace, LogType type)
    {
        if (msg.Count > maxMsg)
        {
            msg.Dequeue();
        }
        msg.Enqueue(message);
        consoleText.text = FromQueueToString();
    }

    string FromQueueToString()
    {
        return String.Join("\n", msg.ToArray());
    }
}
