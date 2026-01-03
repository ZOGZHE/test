using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WoolyPath
{

    public class LoseMenu : MonoBehaviour
    {
        public Button RetryButton;
        public Button QuitButton;

        private void Start()
        {
            
            if (RetryButton != null) RetryButton.onClick.AddListener(OnRetryButtonClick);
            if (QuitButton != null) QuitButton.onClick.AddListener(OnQuitButtonClick);

        }

        private void OnRetryButtonClick()
        {
            GameManager.Instance.RetryGame();
        }
            private void OnQuitButtonClick()
            {
            GameManager.Instance.ReturnToMainMenu();
            }

    }
}