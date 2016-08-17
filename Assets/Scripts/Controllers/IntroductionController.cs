using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Scripts.Models;
using System;

namespace Assets.Scripts.Controllers
{
    public class IntroductionController : MonoBehaviour
    {
        #region Public Methods

        public void Start()
        {

        }

        public void PlayIntroduction()
        {
            // Play Introduction

            SceneManager.LoadScene("Game");
        }

        public void SkipIntroduction()
        {
            SceneManager.LoadScene("Game");
        }

        #endregion

        #region Private Methods


        #endregion
    }
}
