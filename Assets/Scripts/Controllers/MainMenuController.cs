using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Scripts.Models;
using System;

namespace Assets.Scripts.Controllers
{
    public class MainMenuController : MonoBehaviour
    {
        #region Public Properties

        public PlayerModel player;

        #endregion

        #region Public Methods

        public void Start()
        {
            
        }

        #endregion

        #region Private Methods

        private void LoadIntroduction(bool playWithAudio)
        {
            player.AudioOn = playWithAudio;
            SceneManager.LoadScene("Introduction");
        }

        #endregion
    }
}