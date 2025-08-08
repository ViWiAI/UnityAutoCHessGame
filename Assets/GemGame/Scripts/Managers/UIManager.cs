using Game.Core;
using Game.Managers;
using Game.Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject LoginUI;
        [SerializeField] private Button buttonLogin;
        [SerializeField] private Button buttonLoginNow;
        [SerializeField] private Button buttonSignup;
        [SerializeField] private Button closeLogin;
        [SerializeField] private Button buttonPwd;
        [SerializeField] private Button buttonCreateCharacter;
        [SerializeField] private Button buttonStartGame;
        [SerializeField] private TMP_InputField username;
        [SerializeField] private TMP_InputField password;
        [SerializeField] private GameObject errorMessage; // �� GameObject������ TextMeshProUGUI
        [SerializeField] private GameObject tipsMessage;
        [SerializeField] private GameObject UIButton;
        [SerializeField] private GameObject CharacterSelectPanel;


        private TextMeshProUGUI errorText; // TextMeshProUGUI ���
        private TextMeshProUGUI tipsText; // TextMeshProUGUI ���
        private string signupUrl = "https://www.baidu.com";
        private string forgotPwdUrl = "https://www.baidu.com";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // ��������
            if (buttonLogin == null || buttonLoginNow == null || buttonSignup == null ||
                closeLogin == null || buttonPwd == null || username == null || password == null ||
                errorMessage == null)
            {
                Debug.LogError("UIManager: ĳЩ UI ���δ�� Inspector �а󶨣�");
            }

            // ��ȡ errorMessage �� TextMeshProUGUI �����λ���Ӷ����������
            if (errorMessage != null)
            {
                errorText = errorMessage.GetComponentInChildren<TextMeshProUGUI>();
                if (errorText == null)
                {
                    Debug.LogError("UIManager: errorMessage ��δ�ҵ� TextMeshProUGUI �����");
                }
                else
                {
                    errorMessage.SetActive(false); // ��ʼ������
                }
            }

            // ��ȡ tipsMessage �� TextMeshProUGUI �����λ���Ӷ����������
            if (tipsMessage != null)
            {
                tipsText = tipsMessage.GetComponentInChildren<TextMeshProUGUI>();
                if (tipsText == null)
                {
                    Debug.LogError("UIManager: tipsMessage ��δ�ҵ� TextMeshProUGUI �����");
                }
                else
                {
                    tipsMessage.SetActive(false); // ��ʼ������
                }
            }

            // �󶨰�ť����¼�
            buttonLogin.onClick.AddListener(Click_Login);
            buttonLoginNow.onClick.AddListener(Click_LoginNow);
            buttonSignup.onClick.AddListener(Click_Signup);
            closeLogin.onClick.AddListener(Close_Login);
            buttonPwd.onClick.AddListener(Click_Pwd);
            buttonCreateCharacter.onClick.AddListener(Click_CreateCharacter);
            buttonStartGame.onClick.AddListener(Click_StartGame);
        }

        private void Click_CreateCharacter()
        { 
            
        }

        private void Click_StartGame()
        {

        }

        private void Click_Login()
        {
            // ��ȡ�������ı�
            string usernameText = username.text;
            string passwordText = password.text;

            // ��֤�����Ƿ���Ч
            if (string.IsNullOrEmpty(usernameText) || string.IsNullOrEmpty(passwordText))
            {
                ShowErrorMessage("�û��������벻��Ϊ�գ�");
                return;
            }

            // ֪ͨ�������û���¼
            if (WebSocketManager.Instance.IsConnected())
            {
                WebSocketManager.Instance.Send(new Dictionary<string, object>
                {
                    { "type", "player_login" },
                    { "username", usernameText },
                    { "password", passwordText }
                });
                Debug.Log($"������ҵ�¼��Ϣ: username: {usernameText}, password: {passwordText}");
                // ������������첽���ؽ����������ʾ�� WebSocket �ص��д���
            }
            else
            {
                ShowErrorMessage("����δ���ӣ��������磡");
            }
        }

        // ��������ʾ������Ϣ��5�������
        public void ShowErrorMessage(string message)
        {
            if (errorText == null || errorMessage == null)
            {
                Debug.LogError("UIManager: errorMessage �� errorText δ��ȷ��ʼ����");
                return;
            }

            // ȷ���ı�����Ϊ UTF-8��ͨ�������ֶ�ת����Unity Ĭ��֧�֣�
            errorText.text = message;
            errorMessage.SetActive(true);
            StartCoroutine(HideErrorMessageAfterDelay(5f));

            // ���ԣ�����Ƿ������֧�ֵ��ַ�
            foreach (char c in message)
            {
                if (!errorText.font.HasCharacter(c))
                {
                    Debug.LogWarning($"���� {errorText.font.name} ��֧���ַ�: {c} (Unicode: \\u{(int)c:X4})");
                }
            }
        }

        // ��������ʾ������Ϣ��5�������
        public void ShowTipsMessage(string message)
        {
            if (tipsText == null || tipsMessage == null)
            {
                Debug.LogError("UIManager: tipsMessage �� tipsText δ��ȷ��ʼ����");
                return;
            }

            // ȷ���ı�����Ϊ UTF-8��ͨ�������ֶ�ת����Unity Ĭ��֧�֣�
            tipsText.text = message;
            tipsMessage.SetActive(true);
            StartCoroutine(HideTipsMessageAfterDelay(5f));

            // ���ԣ�����Ƿ������֧�ֵ��ַ�
            foreach (char c in message)
            {
                if (!tipsText.font.HasCharacter(c))
                {
                    Debug.LogWarning($"���� {tipsText.font.name} ��֧���ַ�: {c} (Unicode: \\u{(int)c:X4})");
                }
            }
        }

        private IEnumerator HideErrorMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (errorMessage != null)
            {
                errorMessage.SetActive(false);
            }
        }

        private IEnumerator HideTipsMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (tipsMessage != null)
            {
                tipsMessage.SetActive(false);
            }
        }

        private void Click_Pwd()
        {
            Application.OpenURL(forgotPwdUrl);
        }

        private void Click_LoginNow()
        {
            LoginUI.SetActive(true);
            if (username != null)
            {
                username.Select();
                username.ActivateInputField();
            }
        }

        private void Click_Signup()
        {
            Application.OpenURL(signupUrl);
        }

        public void Close_Login()
        {
            LoginUI.SetActive(false);
            if (errorMessage != null)
            {
                errorMessage.SetActive(false); // �رյ�¼����ʱ���ش�����Ϣ
            }
        }

        public void ShowUIButton(bool show)
        {
            UIButton.SetActive(show);
        }

        public void ShowCharacterSelectPanel(bool show)
        {
            CharacterSelectPanel.SetActive(show);
        }

        private void Update()
        {
            if (LoginUI.activeSelf && Input.GetKeyDown(KeyCode.Return))
            {
                Click_Login();
            }
        }

        private void OnDestroy()
        {
            if (buttonLogin != null) buttonLogin.onClick.RemoveListener(Click_Login);
            if (buttonLoginNow != null) buttonLoginNow.onClick.RemoveListener(Click_LoginNow);
            if (buttonSignup != null) buttonSignup.onClick.RemoveListener(Click_Signup);
            if (closeLogin != null) closeLogin.onClick.RemoveListener(Close_Login);
            if (buttonPwd != null) buttonPwd.onClick.RemoveListener(Click_Pwd);
        }
    }
}