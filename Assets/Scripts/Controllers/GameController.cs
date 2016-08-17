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

        public PlayerModel player;
        public Text buyMultipleButtonText;
        public Text incomePerSecond;
        public Text playerMoney;
        public Text globalMultiplier;
        public Text[] shopAmountOwneds;
        public Text[] shopCosts;
        public Text[] shopMultipliers;
        public Text[] timeRemainings;

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
            if (PlayerCanAffordShop(index))
            {
                player.Money -= CalculateCostOfShop(index);
                player.Shops[index].NumberOwned += player.BuyMultiple;
                UpdateAmountOwned();
                UpdateCostOfShops();
                UpdatePlayerMoney();
                UpdateRevenuePerSecond();
            }
        }

        public void WorkShop(int index)
        {
            ShopModel shop = player.Shops[index];
            StartCoroutine(_gameService.WorkShop(shop));
        }

        public void PurchaseManager(int index)
        {
            ShopModel shop = player.Shops[index];
            shop.Manager = true;
            UpdateRevenuePerSecond();
        }

        public void PurchaseManager(GameObject button)
        {
            button.SetActive(false);
        }

        public void ChangeBuyMultiple()
        {
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
        private void HideMenus()
        {
            _gameService.HideMenus();
        }

        private void RefreshCanvas()
        {
            _gameService.RefreshCanvas();
        }

        private void UpdateAmountOwned()
        {
            for (int i = 0; i < shopAmountOwneds.Length; i++)
                shopAmountOwneds[i].text = player.Shops[i].NumberOwned.ToString();
        }

        private void UpdatePlayerMoney()
        {
            if (player.Money > 1e6)
                playerMoney.text = player.Money.ToString("e2");
            else
                playerMoney.text = player.Money.ToString("C");
        }

        private void UpdateCostOfShops()
        {
            for (int i = 0; i < shopCosts.Length; i++)
            {
                float cost = CalculateCostOfShop(i);
                if (cost > 1e6)
                    shopCosts[i].text = " $" + cost.ToString("e2");
                else
                    shopCosts[i].text = " " + cost.ToString("C");
            }
        }

        private void UpdateStatistics()
        {
            _gameService.UpdateStatistics();
        }

        private void UpdateRevenuePerSecond()
        {
            player.RevenuePerSecond = 0;
            for (int i = 0; i < player.Shops.Count; i++)
            {
                player.Shops[i].RevenuePerSecond = CalculateCurrentProfitOfShop(player.Shops[i]) / player.Shops[i].TimeToComplete;
                if (player.Shops[i].Manager)
                    player.RevenuePerSecond += player.Shops[i].RevenuePerSecond;
            }
        }

        private bool PlayerCanAffordShop(int index)
        {
            return player.Money > CalculateCostOfShop(index);
        }

        private float CalculateCostOfShop(int index)
        {
            float cost = 0;
            for (int i = player.Shops[index].NumberOwned; i < player.Shops[index].NumberOwned + player.BuyMultiple; i++)
                cost += player.Shops[index].InitialCost * (float)Math.Pow(player.Shops[index].GrowthRate, i);

            return cost;
        }

        private void Save()
        {
            _gameService.Save();
        }

        private void Load()
        {
            _gameService.Load();
        }

        private float CalculateCurrentProfitOfShop(ShopModel shop)
        {
            return shop.InitialProfit * shop.NumberOwned * shop.Multiplier * player.GlobalMultiplier;
        }

        private void UpdateTimeRemaining(ShopModel shop)
        {
            TimeSpan time = TimeSpan.FromSeconds(shop.TimeRemaining);
            timeRemainings[shop.Id].text = string.Format("{0}:{1}:{2}.{3}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
        }


        #region Coroutines

        private IEnumerator WorkShop(ShopModel shop)
        {
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

                player.Money += CalculateCurrentProfitOfShop(shop);
                shop.Working = false;
                shop.TimeRemaining = 0;

                UpdatePlayerMoney();
                UpdateTimeRemaining(shop);

                if (shop.Manager)
                    StartCoroutine(WorkShop(shop));
            }
        }

        private IEnumerator WorkPartialShop(ShopModel shop)
        {
            Stopwatch watch;
            do
            {
                UpdateTimeRemaining(shop);

                watch = Stopwatch.StartNew();
                yield return null;
                watch.Stop();

                shop.TimeRemaining -= (float)watch.Elapsed.TotalSeconds;
            } while (shop.TimeRemaining > 0);

            player.Money += CalculateCurrentProfitOfShop(shop);
            shop.Working = false;
            shop.TimeRemaining = 0;

            UpdatePlayerMoney();
            UpdateTimeRemaining(shop);

            if (shop.Manager)
                StartCoroutine(WorkShop(shop));
        }

        #endregion

        #endregion
    }
}
