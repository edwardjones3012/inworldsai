using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MessageBox : MonoBehaviour
{
    private Color userColor = new Color(0.4470588f, 0.4470588f, 0.4470588f);
    private Color botColor = new Color(0, 0.5f, 0.5f);

    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text messageText;

    [SerializeField] private Image image;

    public void Set(SenderType senderType, string message)
    {
        if (senderType == SenderType.User)
        {
            image.color = userColor;
            senderText.text = "User";
        }
        else
        {
            image.color = botColor;
            senderText.text = "Bot";
        }
        messageText.text = message;
    }
}

public enum SenderType
{
    User,
    Bot
}