using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Models;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Diagnostics;
using Assets.Scripts.Services;

namespace Assets.Scripts.Controllers
{
    public class GameController : MonoBehaviour
    {
        private static GameService _gameService;

        #region Unity Methods

        void Awake()
        {
            _gameService = GetComponent<GameService>();
            HideMenus();
        }

        void Start()
        {
            //PlayerPrefs.DeleteAll();
            Load();
            RefreshCanvas();
            InvokeRepeating("Save", 0, 1);
            InvokeRepeating("UpdateStatistics", 0, 1);
        }

        #endregion

        #region Public Methods

        public void Buy(int index)
        {
            _gameService.Buy(index);
        }

        public void WorkShop(int index)
        {
            StartCoroutine(_gameService.WorkShop(index));
        }

        public void PurchaseManager(int index)
        {
            _gameService.PurchaseManager(index);
        }

        public void PurchaseManager(GameObject button)
        {
            button.SetActive(false);
        }

        public void ChangeBuyMultiple()
        {
            _gameService.ChangeBuyMultiple();
        }

        public void OpenMenu(GameObject menu)
        {
            menu.transform.parent.transform.SetAsLastSibling();
            menu.SetActive(true);
        }

        public void CloseMenu(GameObject menu)
        {
            menu.SetActive(false);
        }

        #endregion

        #region Private Methods
        private void Save()
        {
            _gameService.Save();
        }

        private void Load()
        {
            _gameService.Load();
        }

        private void HideMenus()
        {
            _gameService.HideMenus();
        }

        private void RefreshCanvas()
        {
            _gameService.RefreshCanvas();
        }
        #endregion
    }
}
