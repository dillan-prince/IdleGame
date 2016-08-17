using Assets.Scripts.Interfaces;
using Assets.Scripts.Models;
using Assets.Scripts.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Services
{
    public class GameService : MonoBehaviour, IGameService
    {
        private GameRepository _gameRepository;

        public Text[] timeRemainings;
        public Text[] shopAmountOwneds;
        public Text[] shopCosts;
        public Text[] shopMultipliers;

        public Text playerMoney;
        public Text incomePerSecond;
        public Text globalMultiplier;

        void Awake()
        {
            _gameRepository = GetComponent<GameRepository>();
        }

        #region Public Methods
        public void Save()
        {
            _gameRepository.Save();
        }
        
        public void Load()
        {
            PlayerModel player = _gameRepository.Load();

            foreach (ShopModel shop in player.Shops)
            {
                if (shop.Working)
                    StartCoroutine(WorkPartialShop(shop));
            }
        }

        public void HideMenus()
        {
            GameObject[] menus = GameObject.FindGameObjectsWithTag("Menu Panel");
            foreach (GameObject menu in menus)
                menu.SetActive(false);
        }

        public void RefreshCanvas()
        {
            UpdateAmountOwned();
            UpdatePlayerMoney();
            UpdateCostOfShops();
            UpdateStatistics();
        }
        public void UpdateStatistics()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            incomePerSecond.text = "Income Per Second: " + player.RevenuePerSecond.ToString("e2");
            globalMultiplier.text = "Global Multiplier: " + player.GlobalMultiplier.ToString();
            for (int i = 0; i < shopMultipliers.Length; i++)
                shopMultipliers[i].text = player.Shops[i].Name + " Multiplier: " + player.Shops[i].Multiplier.ToString();
        }

        public IEnumerator WorkShop(ShopModel shop)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            Stopwatch watch;
            if (!shop.Working && shop.NumberOwned > 0)
            {
                shop.Working = true;
                shop.TimeRemaining = shop.TimeToComplete;

                do
                {
                    UpdateTimeRemaining(shop);

                    watch = Stopwatch.StartNew();
                    yield return null;
                    watch.Stop();

                    shop.TimeRemaining -= (float)watch.Elapsed.TotalSeconds;
                } while (shop.TimeRemaining > 0);

                player.Money += _gameRepository.CalculateCurrentProfitOfShop(shop);

                shop.Working = false;
                shop.TimeRemaining = 0;

                UpdatePlayerMoney();
                UpdateTimeRemaining(shop);

                if (shop.Manager)
                    StartCoroutine(WorkShop(shop));
            }
        }
        #endregion

        #region Private Methods

        private void UpdateCostOfShops()
        {
            for (int i = 0; i < shopCosts.Length; i++)
            {
                float cost = _gameRepository.CalculateCostOfShop(i);
                if (cost > 1e6)
                    shopCosts[i].text = " $" + cost.ToString("e2");
                else
                    shopCosts[i].text = " " + cost.ToString("C");
            }
        }

        private void UpdateAmountOwned()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            for (int i = 0; i < shopAmountOwneds.Length; i++)
                shopAmountOwneds[i].text = player.Shops[i].NumberOwned.ToString();
        }

        private void UpdateTimeRemaining(ShopModel shop)
        {
            TimeSpan time = TimeSpan.FromSeconds(shop.TimeRemaining);
            timeRemainings[shop.Id].text = string.Format("{0}:{1}:{2}.{3}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
        }

        private void UpdatePlayerMoney()
        {
            PlayerModel player = _gameRepository.GetPlayer();

            if (player.Money > 1e6)
                playerMoney.text = player.Money.ToString("e2");
            else
                playerMoney.text = player.Money.ToString("C");
        }

        private IEnumerator WorkPartialShop(ShopModel shop)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            Stopwatch watch;
            do
            {
                UpdateTimeRemaining(shop);

                watch = Stopwatch.StartNew();
                yield return null;
                watch.Stop();

                shop.TimeRemaining -= (float)watch.Elapsed.TotalSeconds;
            } while (shop.TimeRemaining > 0);

            player.Money += _gameRepository.CalculateCurrentProfitOfShop(shop);
            shop.Working = false;
            shop.TimeRemaining = 0;

            UpdatePlayerMoney();
            UpdateTimeRemaining(shop);

            if (shop.Manager)
                StartCoroutine(WorkShop(shop));
        }
        #endregion
    }
}
