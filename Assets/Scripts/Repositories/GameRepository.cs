using Assets.Scripts.Interfaces;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Repositories
{
    public class GameRepository : MonoBehaviour, IGameRepository
    {
        private static PlayerModel player;

        private static List<ShopModel> _shops;
        private static List<UpgradeModel> _upgrades;

        public TextAsset shopModelData;
        public TextAsset upgradeModelData;

        void Awake()
        {
            _shops = PopulateShops();
            _upgrades = PopulateUpgrades();
        }

        #region Public Methods
        public void Save()
        {
            PlayerPrefs.SetString("logOffTime", DateTime.Now.ToString());
            PlayerPrefs.SetFloat("player.Money", player.Money);
            PlayerPrefs.SetFloat("player.RevenuePerSecond", player.RevenuePerSecond);
            for (int i = 0; i < player.Shops.Count; i++)
            {
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].NumberOwned", player.Shops[i].NumberOwned);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].InitialCost", player.Shops[i].InitialCost);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].Multiplier", player.Shops[i].Multiplier);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].TimeToComplete", player.Shops[i].TimeToComplete);
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Manager", player.Shops[i].Managers);
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Working", player.Shops[i].Working ? 1 : 0);
                PlayerPrefs.SetFloat("Shop[" + i.ToString() + "].TimeRemaining", player.Shops[i].TimeRemaining);
            }
        }

        public PlayerModel Load()
        {
            player = GenerateNewPlayer();
            player.BuyMultiple = 1;
            if (PlayerPrefs.GetFloat("player.Money", -1) != -1)
            {
                player.Money = PlayerPrefs.GetFloat("player.Money");
                player.RevenuePerSecond = PlayerPrefs.GetFloat("player.RevenuePerSecond");
                for (int i = 0; i < player.Shops.Count; i++)
                {
                    player.Shops[i].NumberOwned = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].NumberOwned", 0);
                    player.Shops[i].InitialCost = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].InitialCost", player.Shops[i].InitialCost);
                    player.Shops[i].Multiplier = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].Multiplier", 1);
                    player.Shops[i].TimeToComplete = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].TimeToComplete");
                    player.Shops[i].Managers = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Manager", 0);
                    player.Shops[i].Working = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Working", 0) == 1;
                    player.Shops[i].TimeRemaining = PlayerPrefs.GetFloat("Shop[" + i.ToString() + "].TimeRemaining", 0);
                }

                float offlineEarnings = CalculateOfflineEarnings();
                player.Money += offlineEarnings;

            }
            return player;
        }

        public PlayerModel GetPlayer()
        {
            return player;
        }

        public List<UpgradeModel> GetUpgrades()
        {
            return _upgrades;
        }

        public float CalculateCurrentProfitOfShop(ShopModel shop)
        {
            return shop.InitialProfit * shop.NumberOwned * shop.Multiplier * player.GlobalMultiplier;
        }

        public float CalculateCostOfShop(int index)
        {
            float cost = 0;
            for (int i = player.Shops[index].NumberOwned; i < player.Shops[index].NumberOwned + player.BuyMultiple; i++)
                cost += player.Shops[index].InitialCost * (float)Math.Pow(player.Shops[index].GrowthRate, i);

            return cost;
        }
        #endregion

        #region Private Methods

        private float CalculateOfflineEarnings()
        {
            DateTime logOffTime = Convert.ToDateTime(PlayerPrefs.GetString("logOffTime"));
            TimeSpan offlineTimeSpan = DateTime.Now - logOffTime;

            float offlineEarnings = 0;

            for (int i = 0; i < player.Shops.Count; i++)
            {
                if (player.Shops[i].Working)
                {
                    if (player.Shops[i].Managers >= 1)
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

        private PlayerModel GenerateNewPlayer()
        {
            PlayerModel player = new PlayerModel();
            player.Money = 0;
            player.GlobalMultiplier = 1;
            player.RevenuePerSecond = 0;
            player.Shops = _shops;

            return player;
        }

        private List<ShopModel> PopulateShops()
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
                    Managers = Convert.ToInt32(values[7]),
                    Working = Convert.ToBoolean(values[8]),
                    Id = Convert.ToInt32(values[9])
                });
            }

            return shopModels;
        }

        private List<UpgradeModel> PopulateUpgrades()
        {
            List<UpgradeModel> upgradeModels = new List<UpgradeModel>();

            string[] lines = upgradeModelData.text.Split('\n');
            for (int i = 1; i < lines.Length - 1; i++)
            {
                string[] values = lines[i].Split(',');
                upgradeModels.Add(new UpgradeModel
                {
                    Name = values[0],
                    Id = Convert.ToInt32(values[1]),
                    ShopId = Convert.ToInt32(values[2]),
                    Multiplier = Convert.ToSingle(values[3]),
                    Cost = Convert.ToSingle(values[4])
                });
            }

            return upgradeModels;
        }
        #endregion
    }
}
