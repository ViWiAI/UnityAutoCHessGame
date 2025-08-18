using Best.HTTP.Shared.PlatformSupport.Memory;
using Game.Core;
using Game.Data;
using Game.Managers;
using Game.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        //��¼UI
        [SerializeField] private GameObject LoginUI;

        //��¼UI ��ť
        [SerializeField] private Button buttonLogin;
        [SerializeField] private Button buttonLoginNow;
        [SerializeField] private Button buttonSignup;
        [SerializeField] private Button closeLogin;
        [SerializeField] private Button buttonPwd;
        [SerializeField] private Button buttonStartGame;

        [SerializeField] private Button buttonCreateCharacter;
        [SerializeField] private Button buttonCreateCharacterOK;

        //��ɫUI��ť
        [SerializeField] private Button buttonCharacterWarrior;
        [SerializeField] private Button buttonCharacterMage;
        [SerializeField] private Button buttonCharacterHunter;
        [SerializeField] private Button buttonCharacterRogue;
        [SerializeField] private Button buttonCharacterPriest;
        //��ɫUI��ť ����
        [SerializeField] private GameObject focusCharacterWarrior;
        [SerializeField] private GameObject focusCharacterMage;
        [SerializeField] private GameObject focusCharacterHunter;
        [SerializeField] private GameObject focusCharacterRogue;
        [SerializeField] private GameObject focusCharacterPriest;

        // ���UI
        [SerializeField] private TextMeshProUGUI descriptionText;

        [SerializeField] private TMP_InputField username;
        [SerializeField] private TMP_InputField password;

        [SerializeField] private TMP_InputField characterName;

        [SerializeField] private GameObject errorMessage; // �� GameObject������ TextMeshProUGUI
        [SerializeField] private GameObject tipsMessage;
        [SerializeField] private GameObject UIButton;
        [SerializeField] private GameObject GameUI;
        [SerializeField] private GameObject CharacterUI;
        [SerializeField] private GameObject StartGameUI;

        private TextMeshProUGUI errorText; // TextMeshProUGUI ���
        private TextMeshProUGUI tipsText; // TextMeshProUGUI ���
        private string signupUrl = "https://www.baidu.com";
        private string forgotPwdUrl = "https://www.baidu.com";

        public bool isCreateCharacter = false;
        


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

            buttonStartGame.interactable = false;

            // �󶨰�ť����¼�
            buttonLogin.onClick.AddListener(Click_Login);
            buttonLoginNow.onClick.AddListener(Click_LoginNow);
            buttonSignup.onClick.AddListener(Click_Signup);
            closeLogin.onClick.AddListener(Close_Login);
            buttonPwd.onClick.AddListener(Click_Pwd);
            buttonStartGame.onClick.AddListener(Click_StartGame);

            buttonCharacterWarrior.onClick.AddListener(Click_Warrior);
            buttonCharacterMage.onClick.AddListener(Click_Mage);
            buttonCharacterHunter.onClick.AddListener(Click_Hunter);
            buttonCharacterRogue.onClick.AddListener(Click_Rogue);
            buttonCharacterPriest.onClick.AddListener(Click_Priest);

            buttonCreateCharacterOK.onClick.AddListener(Click_CreateCharacterOK);
            buttonCreateCharacter.onClick.AddListener(Click_CreateCharacter);

            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Warrior);
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }

        private void Click_CreateCharacter()
        {
            if(isCreateCharacter == false)
            {
                isCreateCharacter = true;
                CharacterUI.SetActive(true);
                TextMeshProUGUI buttonText = buttonCreateCharacter.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = "MyCharacter";
                CharacterManager.Instance.InitRoleCharacter(HeroRole.Warrior);
            }
            else
            {
                isCreateCharacter = false;
                CharacterUI.SetActive(false);
                TextMeshProUGUI buttonText = buttonCreateCharacter.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = "CreateCharacter";
                CharacterManager.Instance.InitPlayerCharacterList();
            }
            UIManager.Instance.ShowStartGameButton(false);
        }

        private void Click_CreateCharacterOK()
        {
            if (GameManager.Instance.GetLoginStatus() == true)
            {
                // ��ȡ�������ı�
                string name = characterName.text;

                // ��֤�����Ƿ���Ч
                if (string.IsNullOrEmpty(name))
                {
                    ShowErrorMessage("��ɫ���ֲ���Ϊ�գ�");
                    return;
                }

                // ֪ͨ�������û���¼
                if (WebSocketManager.Instance.IsConnected)
                {
                    NetworkMessageHandler.Instance.SendCreateCharacter(name, CharacterManager.Instance.selectRole,GameManager.Instance.GetLoginAccount());
                    Debug.Log($"���ͽ�ɫ������Ϣ: username: {characterName}, Role: {CharacterManager.Instance.selectRole}");
                }
                else
                {
                    ShowErrorMessage("����δ���ӣ��������磡");
                }
            }
        }

        private void Click_Warrior()
        { 
            CharacterManager.Instance.InitRoleCharacter(HeroRole.Warrior);
            focusCharacterWarrior.SetActive(true);
            focusCharacterMage.SetActive(false);
            focusCharacterHunter.SetActive(false);
            focusCharacterRogue.SetActive(false);
            focusCharacterPriest.SetActive(false);
            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Warrior);
            descriptionText.text = "";
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }
        private void Click_Mage()
        {
            CharacterManager.Instance.InitRoleCharacter(HeroRole.Mage);
            focusCharacterWarrior.SetActive(false);
            focusCharacterMage.SetActive(true);
            focusCharacterHunter.SetActive(false);
            focusCharacterRogue.SetActive(false);
            focusCharacterPriest.SetActive(false);
            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Mage);
            descriptionText.text = "";
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }
        private void Click_Hunter()
        {
            CharacterManager.Instance.InitRoleCharacter(HeroRole.Hunter);
            focusCharacterWarrior.SetActive(false);
            focusCharacterMage.SetActive(false);
            focusCharacterHunter.SetActive(true);
            focusCharacterRogue.SetActive(false);
            focusCharacterPriest.SetActive(false);
            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Hunter);
            descriptionText.text = "";
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }
        private void Click_Rogue()
        {
            CharacterManager.Instance.InitRoleCharacter(HeroRole.Rogue);
            focusCharacterWarrior.SetActive(false);
            focusCharacterMage.SetActive(false);
            focusCharacterHunter.SetActive(false);
            focusCharacterRogue.SetActive(true);
            focusCharacterPriest.SetActive(false);
            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Rogue);
            descriptionText.text = "";
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }
        private void Click_Priest()
        {
            CharacterManager.Instance.InitRoleCharacter(HeroRole.Priest);
            focusCharacterWarrior.SetActive(false);
            focusCharacterMage.SetActive(false);
            focusCharacterHunter.SetActive(false);
            focusCharacterRogue.SetActive(false);
            focusCharacterPriest.SetActive(true);
            string description = LocalizationManager.Instance.GetJobDescription(HeroRole.Priest);
            descriptionText.text = "";
            descriptionText.richText = true;
            descriptionText.text = description;
            StartCoroutine(ShowDescriptionEffect());
        }

        private System.Collections.IEnumerator ShowDescriptionEffect()
        {
            descriptionText.alpha = 0f;
            float fadeDuration = 1f;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                descriptionText.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            Debug.Log($"ְҵ �ļ����ʾ��ɣ�");
        }

        private void Click_StartGame()
        {
            GameManager.Instance.setMapId(2);
            GameManager.Instance.SetOnlineGame(true);
            MapManager.Instance.SwitchMap(2);
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
            if (WebSocketManager.Instance.IsConnected)
            {
                NetworkMessageHandler.Instance.SendLoginRequest(usernameText, passwordText);
                GameManager.Instance.SetLoginAccount(usernameText);
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
            UIButton.SetActive(true);
            if (errorMessage != null)
            {
                errorMessage.SetActive(false); // �رյ�¼����ʱ���ش�����Ϣ
            }
        }

        public void ShowUIButton(bool show)
        {
            UIButton.SetActive(show);
        }

        public void ShowGameUI(bool show)
        {
            GameUI.SetActive(show);
        }

        public void ShowCharacterUI(bool show)
        {
            CharacterUI.SetActive(show);
            isCreateCharacter = false;
        }

        public void ShowStartGameUI(bool show)
        {
            StartGameUI.SetActive(show);
        }

        public void ShowStartGameButton(bool show)
        {
            buttonStartGame.interactable = show;
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