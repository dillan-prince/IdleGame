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
        public Text buyMultipleButtonText;
        public Text incomePerSecond;
        public Text globalMultiplier;

        public GameObject[] managers;

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
                    StartCoroutine(WorkPartialShop(shop.Id));
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
            UpdateManagers();
            UpdateRevenuePerSecond();
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

        public void Buy(int index)
        {
            if (PlayerCanAffordShop(index))
            {
                PlayerModel player = _gameRepository.GetPlayer();
                player.Money -= _gameRepository.CalculateCostOfShop(index);
                player.Shops[index].NumberOwned += player.BuyMultiple;
                RefreshCanvas();
            }
        }

        public void PurchaseManager(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            player.Shops[index % 10].Managers = index / 10 + 1;

            if (player.Shops[index % 10].Managers == 2)
                player.Shops[index % 10].InitialCost *= .9f;
            else if (player.Shops[index % 10].Managers == 3)
                player.Shops[index % 10].InitialCost *= .00001f;

            RefreshCanvas();
        }

        public void ChangeBuyMultiple()
        {
            PlayerModel player = _gameRepository.GetPlayer();

            switch (buyMultipleButtonText.text)
            {
                case "x1":
                    player.BuyMultiple = 10;
                    buyMultipleButtonText.text = "x10";
                    break;
                case "x10":
                    player.BuyMultiple = 100;
                    buyMultipleButtonText.text = "x100";
                    break;
                case "x100":
                    player.BuyMultiple = 250;
                    buyMultipleButtonText.text = "x250";
                    break;
                case "x250":
                    player.BuyMultiple = 1;
                    buyMultipleButtonText.text = "x1";
                    break;
            }

            UpdateCostOfShops();
        }

        public IEnumerator WorkShop(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            ShopModel shop = player.Shops[index];
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

                if (shop.Managers >= 1)
                    StartCoroutine(WorkShop(index));
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

        private void UpdateRevenuePerSecond()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            player.RevenuePerSecond = 0;
            for (int i = 0; i < player.Shops.Count; i++)
            {
                player.Shops[i].RevenuePerSecond = _gameRepository.CalculateCurrentProfitOfShop(player.Shops[i]) / player.Shops[i].TimeToComplete;
                if (player.Shops[i].Managers >= 1)
                    player.RevenuePerSecond += player.Shops[i].RevenuePerSecond;
            }
        }

        private void UpdateManagers()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            List<int> managersToShow = Enumerable.Range(0, managers.Length).ToList();
            for (int i = 0; i < player.Shops.Count; i++)
            {
                for (int j = 0; j < player.Shops[i].Managers; j++)
                    managersToShow.Remove(i + 10 * j);
            }

            foreach (GameObject manager in managers)
                manager.SetActive(false);

            for (int i = 0; i < Math.Min(6, managersToShow.Count); i++)
            {
                managers[managersToShow[i]].SetActive(true);
                managers[managersToShow[i]].transform.localPosition = new Vector2(0, 210 - 70 * i);
            }
        }

        private bool PlayerCanAffordShop(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            return player.Money > _gameRepository.CalculateCostOfShop(index);
        }

        private IEnumerator WorkPartialShop(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            ShopModel shop = player.Shops[index];
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

            if (shop.Managers >= 1)
                StartCoroutine(WorkShop(index));
        }
        #endregion
    }
}
