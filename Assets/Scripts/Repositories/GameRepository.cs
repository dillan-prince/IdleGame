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
        private static List<ManagerModel> _managers;

        public TextAsset shopModelData;
        public TextAsset upgradeModelData;
        public TextAsset managerModelData;

        void Awake()
        {
            _shops = PopulateShops();
            _upgrades = PopulateUpgrades();
            _managers = PopulateManagers();
        }

        #region Public Methods
        public void Save()
        {
            PlayerPrefs.SetString("logOffTime", DateTime.Now.ToString());
            PlayerPrefs.SetString("player.Money", player.Money.ToString());
            PlayerPrefs.SetString("player.RevenuePerSecond", player.RevenuePerSecond.ToString());

            string playerManagers = "";
            if (player.Managers != null)
            {
                for (int i = 0; i < player.Managers.Count; i++)
                    playerManagers += player.Managers[i].ToString() + ",";
            }
            PlayerPrefs.SetString("player.Managers", playerManagers);

            string playerUpgrades = "";
            if (player.Upgrades != null)
            {
                for (int i = 0; i < player.Upgrades.Count; i++)
                    playerUpgrades += player.Upgrades[i].ToString() + ",";
            }
            PlayerPrefs.SetString("player.Upgrades", playerUpgrades);

            for (int i = 0; i < player.Shops.Count; i++)
            {
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].NumberOwned", player.Shops[i].NumberOwned);
                PlayerPrefs.SetString("Shop[" + i.ToString() + "].InitialCost", player.Shops[i].InitialCost.ToString());
                PlayerPrefs.SetString("Shop[" + i.ToString() + "].Multiplier", player.Shops[i].Multiplier.ToString());
                PlayerPrefs.SetString("Shop[" + i.ToString() + "].TimeToComplete", player.Shops[i].TimeToComplete.ToString());
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Manager", player.Shops[i].Manager ? 1 : 0);
                PlayerPrefs.SetInt("Shop[" + i.ToString() + "].Working", player.Shops[i].Working ? 1 : 0);
                PlayerPrefs.SetString("Shop[" + i.ToString() + "].TimeRemaining", player.Shops[i].TimeRemaining.ToString());
            }
        }

        public PlayerModel Load()
        {
            player = GenerateNewPlayer();
            player.BuyMultiple = 1;
            if (Convert.ToDouble(PlayerPrefs.GetString("player.Money", "-1")) != -1)
            {
                player.Money = Convert.ToDouble(PlayerPrefs.GetString("player.Money"));
                player.Money += CalculateOfflineEarnings();

                player.RevenuePerSecond = Convert.ToDouble(PlayerPrefs.GetString("player.RevenuePerSecond"));

                string playerManagers = PlayerPrefs.GetString("player.Managers");
                if (playerManagers != "")
                {
                    playerManagers = playerManagers.Substring(0, playerManagers.Length - 1);
                    player.Managers = playerManagers.Split(',').Select<string, int>(int.Parse).ToList();
                }
                else
                    player.Managers = new List<int>();

                string playerUpgrades = PlayerPrefs.GetString("player.Upgrades");
                if (playerUpgrades != "")
                {
                    playerUpgrades = playerUpgrades.Substring(0, playerUpgrades.Length - 1);
                    player.Upgrades = playerUpgrades.Split(',').Select<string, int>(int.Parse).ToList();
                }
                else
                    player.Upgrades = new List<int>();

                for (int i = 0; i < player.Shops.Count; i++)
                {
                    player.Shops[i].NumberOwned = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].NumberOwned", i == 0 ? 1 : 0);
                    player.Shops[i].InitialCost = Convert.ToDouble(PlayerPrefs.GetString("Shop[" + i.ToString() + "].InitialCost"));
                    player.Shops[i].Multiplier = Convert.ToDouble(PlayerPrefs.GetString("Shop[" + i.ToString() + "].Multiplier"));
                    player.Shops[i].TimeToComplete = Convert.ToDouble(PlayerPrefs.GetString("Shop[" + i.ToString() + "].TimeToComplete"));
                    player.Shops[i].Manager = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Manager", 0) == 1;
                    player.Shops[i].Working = PlayerPrefs.GetInt("Shop[" + i.ToString() + "].Working", 0) == 1;
                    player.Shops[i].TimeRemaining = Convert.ToDouble(PlayerPrefs.GetString("Shop[" + i.ToString() + "].TimeRemaining"));
                }
                

                for (int i = 0; i < player.Upgrades.Count; i++)
                    _upgrades[player.Upgrades[i]].IsPurchased = true;

                for (int i = 0; i < player.Managers.Count; i++)
                    _managers[player.Managers[i]].IsPurchased = true;

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

        public List<ManagerModel> GetManagers()
        {
            return _managers;
        }

        public double CalculateCurrentProfitOfShop(ShopModel shop)
        {
            return shop.InitialProfit * shop.NumberOwned * shop.Multiplier * player.GlobalMultiplier;
        }

        public double CalculateCostOfShop(int index)
        {
            double cost = 0;
            for (int i = player.Shops[index].NumberOwned; i < player.Shops[index].NumberOwned + player.BuyMultiple; i++)
                cost += player.Shops[index].InitialCost * Math.Pow(player.Shops[index].GrowthRate, i);

            return cost;
        }
        #endregion

        #region Private Methods

        private double CalculateOfflineEarnings()
        {
            DateTime logOffTime = Convert.ToDateTime(PlayerPrefs.GetString("logOffTime"));
            TimeSpan offlineTimeSpan = DateTime.Now - logOffTime;

            double offlineEarnings = 0;

            for (int i = 0; i < player.Shops.Count; i++)
            {
                if (player.Shops[i].Working)
                {
                    if (player.Shops[i].Manager)
                    {
                        int workCompletionsWhileOffline = CalculateWorkCompletionsWhileOffline(player.Shops[i], offlineTimeSpan);
                        offlineEarnings += CalculateCurrentProfitOfShop(player.Shops[i]) * workCompletionsWhileOffline;
                        player.Shops[i].TimeRemaining = ((workCompletionsWhileOffline + 1) * player.Shops[i].TimeToComplete) - (offlineTimeSpan.TotalSeconds + player.Shops[i].TimeToComplete - player.Shops[i].TimeRemaining);
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
                            player.Shops[i].TimeRemaining -= offlineTimeSpan.TotalSeconds;
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
                    GrowthRate = Convert.ToDouble(values[1]),
                    InitialCost = Convert.ToDouble(values[2]),
                    NumberOwned = Convert.ToInt32(values[3]),
                    Multiplier = Convert.ToDouble(values[4]),
                    TimeToComplete = Convert.ToDouble(values[5]),
                    InitialProfit = Convert.ToDouble(values[6]),
                    Manager = Convert.ToBoolean(values[7]),
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
                    Multiplier = Convert.ToDouble(values[3]),
                    Cost = Convert.ToDouble(values[4])
                });
            }

            return upgradeModels;
        }

        private List<ManagerModel> PopulateManagers()
        {
            List<ManagerModel> managerModels = new List<ManagerModel>();

            string[] lines = managerModelData.text.Split('\n');
            for (int i = 1; i < lines.Length - 1; i++)
            {
                string[] values = lines[i].Split(',');
                managerModels.Add(new ManagerModel
                {
                    Name = values[0],
                    Id = Convert.ToInt32(values[1]),
                    ShopId = Convert.ToInt32(values[2]),
                    Multiplier = Convert.ToDouble(values[3]),
                    Cost = Convert.ToDouble(values[4])
                });
            }

            return managerModels;
        }
        #endregion
    }
}
