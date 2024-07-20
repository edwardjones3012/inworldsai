using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using Eldersoft.Movement;

public class ChatbotController : MonoBehaviour
{
    public static ChatbotController Instance;

    public TMP_InputField userInputField;
    public Button sendButton;
    public GameObject messageBoxPrefab;
    public Transform contentTransform;
    public ScrollRect chatScrollRect;
    public CanvasGroup canvasGroup;
    private const string apiUrl = "https://api.inworld.ai/v1/dialog";
    private const string apiKey = "dont_have_api_key_as_inworld_is_not_allowing_new_accounts_presently";

    public SurfCharacter SurfCharacter;
    public PlayerAiming PlayerAiming;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClicked);
        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnSendButtonClicked();
        }
    }

    void OnSendButtonClicked()
    {
        string userMessage = userInputField.text;
        if (!string.IsNullOrEmpty(userMessage))
        {
            StartCoroutine(SendMessageToAPI(userMessage));
            CreateUIMessageBox(SenderType.User, userMessage);
            userInputField.text = "";
            userInputField.ActivateInputField();
        }
    }

    IEnumerator SendMessageToAPI(string message)
    {
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes("{\"message\":\"" + message + "\"}");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " +
        apiKey);
        
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            OnMessageSentSuccessfully(request);
        }
        else
        {
            yield return new WaitForSeconds(1);
            OnMessageFail(request);
        }
    }

    private void OnMessageSentSuccessfully(UnityWebRequest request)
    {
        string responseText = request.downloadHandler.text;
        CreateUIMessageBox(SenderType.Bot, responseText);

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0;
    }

    private void OnMessageFail(UnityWebRequest request)
    {
        CreateUIMessageBox(SenderType.Bot, "huh?");
    }

    private void CreateUIMessageBox(SenderType senderType, string message)
    {
        GameObject go = Instantiate(messageBoxPrefab, contentTransform);
        var box = go.GetComponent<UI_MessageBox>();
        box.Set(senderType, message);
    }

    public void Open()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // I should use an event / game event system here, but due to limited scope of project I have used references.
        SurfCharacter.AllowMove = false;
        PlayerAiming.AllowAim = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void Close()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // I should use an event / game event system here, but due to limited scope I have used references.
        SurfCharacter.AllowMove = true;
        PlayerAiming.AllowAim = true;
        Cursor.lockState = CursorLockMode.Locked;
    }
}