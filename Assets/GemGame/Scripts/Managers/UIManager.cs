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

        //登录UI
        [SerializeField] private GameObject LoginUI;

        //登录UI 按钮
        [SerializeField] private Button buttonLogin;
        [SerializeField] private Button buttonLoginNow;
        [SerializeField] private Button buttonSignup;
        [SerializeField] private Button closeLogin;
        [SerializeField] private Button buttonPwd;
        [SerializeField] private Button buttonStartGame;

        [SerializeField] private Button buttonCreateCharacter;
        [SerializeField] private Button buttonCreateCharacterOK;

        //角色UI按钮
        [SerializeField] private Button buttonCharacterWarrior;
        [SerializeField] private Button buttonCharacterMage;
        [SerializeField] private Button buttonCharacterHunter;
        [SerializeField] private Button buttonCharacterRogue;
        [SerializeField] private Button buttonCharacterPriest;
        //角色UI按钮 焦点
        [SerializeField] private GameObject focusCharacterWarrior;
        [SerializeField] private GameObject focusCharacterMage;
        [SerializeField] private GameObject focusCharacterHunter;
        [SerializeField] private GameObject focusCharacterRogue;
        [SerializeField] private GameObject focusCharacterPriest;

        // 简介UI
        [SerializeField] private TextMeshProUGUI descriptionText;

        [SerializeField] private TMP_InputField username;
        [SerializeField] private TMP_InputField password;

        [SerializeField] private TMP_InputField characterName;

        [SerializeField] private GameObject errorMessage; // 父 GameObject，包含 TextMeshProUGUI
        [SerializeField] private GameObject tipsMessage;
        [SerializeField] private GameObject UIButton;
        [SerializeField] private GameObject GameUI;
        [SerializeField] private GameObject CharacterUI;
        [SerializeField] private GameObject StartGameUI;

        private TextMeshProUGUI errorText; // TextMeshProUGUI 组件
        private TextMeshProUGUI tipsText; // TextMeshProUGUI 组件
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
            // 检查组件绑定
            if (buttonLogin == null || buttonLoginNow == null || buttonSignup == null ||
                closeLogin == null || buttonPwd == null || username == null || password == null ||
                errorMessage == null)
            {
                Debug.LogError("UIManager: 某些 UI 组件未在 Inspector 中绑定！");
            }

            // 获取 errorMessage 的 TextMeshProUGUI 组件（位于子对象第三级）
            if (errorMessage != null)
            {
                errorText = errorMessage.GetComponentInChildren<TextMeshProUGUI>();
                if (errorText == null)
                {
                    Debug.LogError("UIManager: errorMessage 中未找到 TextMeshProUGUI 组件！");
                }
                else
                {
                    errorMessage.SetActive(false); // 初始化隐藏
                }
            }

            // 获取 tipsMessage 的 TextMeshProUGUI 组件（位于子对象第三级）
            if (tipsMessage != null)
            {
                tipsText = tipsMessage.GetComponentInChildren<TextMeshProUGUI>();
                if (tipsText == null)
                {
                    Debug.LogError("UIManager: tipsMessage 中未找到 TextMeshProUGUI 组件！");
                }
                else
                {
                    tipsMessage.SetActive(false); // 初始化隐藏
                }
            }

            buttonStartGame.interactable = false;

            // 绑定按钮点击事件
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
                // 获取输入框的文本
                string name = characterName.text;

                // 验证输入是否有效
                if (string.IsNullOrEmpty(name))
                {
                    ShowErrorMessage("角色名字不能为空！");
                    return;
                }

                // 通知服务器用户登录
                if (WebSocketManager.Instance.IsConnected)
                {
                    NetworkMessageHandler.Instance.SendCreateCharacter(name, CharacterManager.Instance.selectRole,GameManager.Instance.GetLoginAccount());
                    Debug.Log($"发送角色创建消息: username: {characterName}, Role: {CharacterManager.Instance.selectRole}");
                }
                else
                {
                    ShowErrorMessage("网络未连接，请检查网络！");
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
            Debug.Log($"职业 的简介显示完成！");
        }

        private void Click_StartGame()
        {
            GameManager.Instance.setMapId(2);
            GameManager.Instance.SetOnlineGame(true);
            MapManager.Instance.SwitchMap(2);
        }

        private void Click_Login()
        {
            // 获取输入框的文本
            string usernameText = username.text;
            string passwordText = password.text;

            // 验证输入是否有效
            if (string.IsNullOrEmpty(usernameText) || string.IsNullOrEmpty(passwordText))
            {
                ShowErrorMessage("用户名或密码不能为空！");
                return;
            }

            // 通知服务器用户登录
            if (WebSocketManager.Instance.IsConnected)
            {
                NetworkMessageHandler.Instance.SendLoginRequest(usernameText, passwordText);
                GameManager.Instance.SetLoginAccount(usernameText);
                Debug.Log($"发送玩家登录消息: username: {usernameText}, password: {passwordText}");
                // 假设服务器会异步返回结果，错误提示在 WebSocket 回调中处理
            }
            else
            {
                ShowErrorMessage("网络未连接，请检查网络！");
            }
        }

        // 新增：显示错误消息，5秒后隐藏
        public void ShowErrorMessage(string message)
        {
            if (errorText == null || errorMessage == null)
            {
                Debug.LogError("UIManager: errorMessage 或 errorText 未正确初始化！");
                return;
            }

            // 确保文本编码为 UTF-8（通常无需手动转换，Unity 默认支持）
            errorText.text = message;
            errorMessage.SetActive(true);
            StartCoroutine(HideErrorMessageAfterDelay(5f));

            // 调试：检查是否包含不支持的字符
            foreach (char c in message)
            {
                if (!errorText.font.HasCharacter(c))
                {
                    Debug.LogWarning($"字体 {errorText.font.name} 不支持字符: {c} (Unicode: \\u{(int)c:X4})");
                }
            }
        }

        // 新增：显示错误消息，5秒后隐藏
        public void ShowTipsMessage(string message)
        {
            if (tipsText == null || tipsMessage == null)
            {
                Debug.LogError("UIManager: tipsMessage 或 tipsText 未正确初始化！");
                return;
            }

            // 确保文本编码为 UTF-8（通常无需手动转换，Unity 默认支持）
            tipsText.text = message;
            tipsMessage.SetActive(true);
            StartCoroutine(HideTipsMessageAfterDelay(5f));

            // 调试：检查是否包含不支持的字符
            foreach (char c in message)
            {
                if (!tipsText.font.HasCharacter(c))
                {
                    Debug.LogWarning($"字体 {tipsText.font.name} 不支持字符: {c} (Unicode: \\u{(int)c:X4})");
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
                errorMessage.SetActive(false); // 关闭登录界面时隐藏错误消息
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