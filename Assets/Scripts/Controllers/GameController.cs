using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Models;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace Assets.Scripts.Controllers
{
    public class GameController : MonoBehaviour
    {
        public PlayerModel player;
        public Text buyMultipleButtonText;
        public Text incomePerSecond;
        public Text playerMoney;
        public Text globalMultiplier;
        public Text[] shopAmountOwneds;
        public Text[] shopCosts;
        public Text[] shopMultipliers;
        public Text[] timeRemainings;
        public TextAsset shopModelData;

        #region Unity Methods

        void Awake()
        {
            GameObject[] menus = GameObject.FindGameObjectsWithTag("Menu Panel");
            foreach (GameObject menu in menus)
                menu.SetActive(false);
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
            StartCoroutine(WorkShop(shop));
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

        private PlayerModel GenerateNewPlayer()
        {
            PlayerModel player = new PlayerModel();
            player.Money = 0;
            player.GlobalMultiplier = 1;
            player.RevenuePerSecond = 0;
            player.Shops = GetShops();

            return player;
        }


        private List<ShopModel> GetShops()
        {
            List<ShopModel> shopModels = new List<ShopModel>();

            string[] lines = shopModelData.text.Split('\n');
            for (int i = 1; i < lines.Length - 1; i++)
            {
                string[] values = lines[i].Split(',');
                shopModels.Add(new ShopModel
                {
                    Name = values[0],
                    GrowthRate = Convert.ToSingle(values[1]),
                    InitialCost = Convert.ToSingle(values[2]),
                    NumberOwned = Convert.ToInt32(values[3]),
                    Multiplier = Convert.ToSingle(values[4]),
                    TimeToComplete = Convert.ToSingle(values[5]),
                    InitialProfit = Convert.ToSingle(values[6]),
                    Manager = Convert.ToBoolean(values[7]),
                    Working = Convert.ToBoolean(values[8]),
                    Id = Convert.ToInt32(values[9])
                });
            }

            return shopModels;
        }

        private void RefreshCanvas()
        {
            player.BuyMultiple = 1;
            UpdateAmountOwned();
            UpdatePlayerMoney();
            UpdateCostOfShops();
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
            incomePerSecond.text = "Income Per Second: " + player.RevenuePerSecond.ToString("e2");
            globalMultiplier.text = "Global Multiplier: " + player.GlobalMultiplier.ToString();
            for (int i = 0; i < shopMultipliers.Length; i++)
                shopMultipliers[i].text = player.Shops[i].Name + " Multiplier: " + player.Shops[i].Multiplier.ToString();
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
            PlayerPrefs.SetFloat("player.Money", player.Money);
            PlayerPrefs.SetString("logOffTime", DateTime.Now.ToString());
            for (int i = 0; i < player.Shops.Count; i++)
            {
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].NumberOwned", player.Shops[i].NumberOwned);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].Multiplier", player.Shops[i].Multiplier);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].TimeToComplete", player.Shops[i].TimeToComplete);
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Manager", player.Shops[i].Manager ? 1 : 0);
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Working", player.Shops[i].Working ? 1 : 0);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].TimeRemaining", player.Shops[i].TimeRemaining);
            }
        }

        private void Load()
        {
            player = GenerateNewPlayer();
            if (PlayerPrefs.GetFloat("player.Money", -1) != -1)
            {
                player.Money = PlayerPrefs.GetFloat("player.Money");
                for (int i = 0; i < player.Shops.Count; i++)
                {
                    player.Shops[i].NumberOwned = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].NumberOwned", 0);
                    player.Shops[i].Multiplier = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].Multiplier", 1);
                    player.Shops[i].TimeToComplete = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].TimeToComplete");
                    player.Shops[i].Manager = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Manager", 0) == 1;
                    player.Shops[i].Working = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Working", 0) == 1;
                    player.Shops[i].TimeRemaining = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].TimeRemaining", 0);
                }

                float offlineEarnings = CalculateOfflineEarnings();
                player.Money += offlineEarnings;

                foreach (ShopModel shop in player.Shops)
                {
                    if (shop.Working)
                        StartCoroutine(WorkPartialShop(shop));
                }
            }
        }

        private float CalculateOfflineEarnings()
        {
            DateTime logOffTime = Convert.ToDateTime(PlayerPrefs.GetString("logOffTime"));
            TimeSpan offlineTimeSpan = DateTime.Now - logOffTime;

            float offlineEarnings = 0;

            for (int i = 0; i < player.Shops.Count; i++)
            {
                if (player.Shops[i].Working)
                {
                    if (player.Shops[i].Manager)
                    {
                        int workCompletionsWhileOffline = CalculateWorkCompletionsWhileOffline(player.Shops[i], offlineTimeSpan);
                        offlineEarnings += CalculateCurrentProfitOfShop(player.Shops[i]) * workCompletionsWhileOffline;
                        player.Shops[i].TimeRemaining = ((workCompletionsWhileOffline + 1) * player.Shops[i].TimeToComplete) - ((float)offlineTimeSpan.TotalSeconds + player.Shops[i].TimeToComplete - player.Shops[i].TimeRemaining);
                    }
                    else
                    {
                        if (offlineTimeSpan.TotalSeconds > player.Shops[i].TimeRemaining)
                        {
                            offlineEarnings += CalculateCurrentProfitOfShop(player.Shops[i]);
                            player.Shops[i].Working = false;
                            player.Shops[i].TimeRemaining = 0;
                        }
                        else
                            player.Shops[i].TimeRemaining -= (float)offlineTimeSpan.TotalSeconds;
                    }
                }
            }

            return offlineEarnings;
        }

        private int CalculateWorkCompletionsWhileOffline(ShopModel shop, TimeSpan offlineTimeSpan)
        {
            return (int)Math.Floor((offlineTimeSpan.TotalSeconds + shop.TimeToComplete - shop.TimeRemaining) / shop.TimeToComplete);
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
