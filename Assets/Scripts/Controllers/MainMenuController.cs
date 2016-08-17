using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Scripts.Models;
using System;

namespace Assets.Scripts.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        #region Public Methods

        public void Start()
        {
            
        }

        public void LoadIntroduction(bool playWithAudio)
        {
            SceneManager.LoadScene("Introduction");
        }
        #endregion

        #region Private Methods


        #endregion
    }
}