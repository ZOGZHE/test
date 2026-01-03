using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WoolyPath
{

    public class SettingMenu : MonoBehaviour
    {
       
        public Button HomeButton;
        public Button RetryButton;
        public Button ExitButton;


        private void Start()
        {
             if (HomeButton != null) HomeButton.onClick.AddListener(OnHomeButtonClick);
            if (RetryButton != null) RetryButton.onClick.AddListener(OnRetryButtonClick);
            if (ExitButton != null) ExitButton.onClick.AddListener(OnExitButtonClick);
        }
       private void OnHomeButtonClick()
        {
            // Log mission abandoned when player returns to main menu from settings
            // 当玩家从设置返回主菜单时记录任务放弃
            if (GameManager.Instance != null && LionSDKManager.Instance != null)
            {
                int currentLevel = GameManager.Instance.CurrentLevel;
                LionSDKManager.Instance.LogMissionAbandoned(currentLevel);
            }

            GameManager.Instance.ReturnToMainMenu();
        }

        private void OnRetryButtonClick()
        {
            // Mission abandoned is already logged in GameManager.RetryGame()
            // 任务放弃已经在 GameManager.RetryGame() 中记录
            GameManager.Instance.RetryGame();
            //GameManager.Instance.ResumeGame();
        }

        private void OnExitButtonClick()
        {
            GameManager.Instance.ResumeGame();
        }
    }
}