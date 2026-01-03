using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WoolyPath
{
 
    public class Victory : MonoBehaviour
    {

        public Button NextLevelButton;
        public Image _image;

        private void Awake()
        {
           
        }

        private void Start()
        {
            if (NextLevelButton != null) NextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        }
          
        private void OnNextLevelButtonClick()
        {
            LevelManager.Instance.OnPassTheLevel();
            GameManager.Instance.LoadNextLevel();
           // GameManager.Instance.ResumeGame();
        }

        
    }
}