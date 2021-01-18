using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour{
    [SerializeField] private Text m_Feed_Ref;
    [SerializeField] private Text m_Stat_Ref;

    [SerializeField] private int m_maxLines = 12;
    private Queue<string> msgQueue = new Queue<string>();

    //Updates feed message box with the given string argument
    public void UpdateChat(string getMsg) {
        if (!m_Feed_Ref) {
            Debug.LogWarning("[Warning] Feed reference missing! Aborting operation...");
            return;
        }

        string msgBoxMessageFull = "";

        msgQueue.Enqueue(getMsg);

        if (msgQueue.Count > m_maxLines) {
            msgQueue.Dequeue();
        }

        foreach (string message in msgQueue){
            msgBoxMessageFull = msgBoxMessageFull + "\n" + message;
        }

        m_Feed_Ref.text = msgBoxMessageFull;
    }

    //Updates stat message displays
    public void UpdateStats(int resources, int scans, int extracts) {
        if (!m_Stat_Ref) {
            Debug.LogWarning("[Warning] Stat text reference missing! Aborting operation...");
            return;
        }

        string msgBoxMessageFull = "";
        msgBoxMessageFull = "Resources: " + resources + "\nScans Left: " + scans + "\nExtracts Left: " + extracts;
        m_Stat_Ref.text = msgBoxMessageFull;
    }
}
