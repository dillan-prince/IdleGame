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

        public Text[] _timeRemainings;
        public Text[] _shopAmountOwneds;
        public Text[] _shopCosts;

        public Text _playerMoney;
        public Text _buyMultipleButtonText;

        public GameObject[] _menus;

        public GameObject _menuButton;
        public GameObject _statisticsExpandableList;

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
            UpdateUpgrades();
        }

        public void Buy(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            if (player.Money > _gameRepository.CalculateCostOfShop(index))
            {
                player.Money -= _gameRepository.CalculateCostOfShop(index);
                player.Shops[index].NumberOwned += player.BuyMultiple;
                CheckForUnlocks(index, player.Shops[index].NumberOwned - player.BuyMultiple);

                UpdateAmountOwned();
                UpdateCostOfShops();
                UpdatePlayerMoney();
                UpdateRevenuePerSecond();
                UpdateStatistics();
            }
        }

        public void PurchaseManager(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            List<ManagerModel> managers = _gameRepository.GetManagers();
            ManagerModel manager = managers.FirstOrDefault(m => m.Id == index);

            if (player.Money > manager.Cost)
            {
                player.Shops[manager.ShopId].InitialCost *= manager.Multiplier;

                if (index < 10)
                {
                    player.Shops[index].Manager = true;
                    UpdateStatistics();
                }

                manager.IsPurchased = true;
                player.Money -= manager.Cost;
                player.Managers.Add(index);
                UpdatePlayerMoney();
                UpdateManagers();
            }
        }

        public void PurchaseUpgrade(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            List<UpgradeModel> upgrades = _gameRepository.GetUpgrades();
            UpgradeModel upgrade = upgrades.FirstOrDefault(u => u.Id == index);

            if (player.Money >= upgrade.Cost)
            {
                if (upgrade.ShopId == 10)
                    player.GlobalMultiplier *= upgrade.Multiplier;
                else
                    player.Shops[upgrade.ShopId].Multiplier *= upgrade.Multiplier;

                upgrade.IsPurchased = true;
                player.Money -= upgrade.Cost;
                player.Upgrades.Add(index);
                UpdatePlayerMoney();
                UpdateUpgrades();
                UpdateStatistics();
            }
        }

        public void ChangeBuyMultiple()
        {
            PlayerModel player = _gameRepository.GetPlayer();

            switch (_buyMultipleButtonText.text)
            {
                case "x1":
                    player.BuyMultiple = 10;
                    _buyMultipleButtonText.text = "x10";
                    break;
                case "x10":
                    player.BuyMultiple = 100;
                    _buyMultipleButtonText.text = "x100";
                    break;
                case "x100":
                    player.BuyMultiple = 250;
                    _buyMultipleButtonText.text = "x250";
                    break;
                case "x250":
                    player.BuyMultiple = 1;
                    _buyMultipleButtonText.text = "x1";
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

                    shop.TimeRemaining -= watch.Elapsed.TotalSeconds;
                } while (shop.TimeRemaining > 0);

                player.Money += _gameRepository.CalculateCurrentProfitOfShop(shop);

                shop.Working = false;
                shop.TimeRemaining = 0;

                UpdatePlayerMoney();
                UpdateTimeRemaining(shop);

                if (shop.Manager)
                    StartCoroutine(WorkShop(index));
            }
        }
        #endregion

        #region Private Methods
        private void CheckForUnlocks(int index, int oldLevel)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            List<UnlockModel> shopUnlocks = _gameRepository.GetShopUnlocks(index);
            List<UnlockModel> shopUnlocksReached = shopUnlocks.Where(su => su.Level > oldLevel && su.Level <= player.Shops[index].NumberOwned).ToList();
            for (int i = 0; i < shopUnlocksReached.Count; i++)
            {
                UnlockModel shopUnlockReached = shopUnlocksReached[i];
                player.Shops[shopUnlockReached.AffectsShopId].Multiplier *= shopUnlockReached.ProfitMultiplier;
                player.Shops[shopUnlockReached.AffectsShopId].TimeToComplete *= shopUnlockReached.SpeedMultiplier;

                if (shopUnlockReached.SpeedMultiplier != 1)
                {
                    player.Shops[shopUnlockReached.AffectsShopId].TimeRemaining -= player.Shops[shopUnlockReached.AffectsShopId].TimeToComplete;
                    if (player.Shops[shopUnlockReached.AffectsShopId].TimeRemaining < 0)
                    {
                        player.Money += _gameRepository.CalculateCurrentProfitOfShop(player.Shops[shopUnlockReached.AffectsShopId]);
                        player.Shops[shopUnlockReached.AffectsShopId].TimeRemaining = player.Shops[shopUnlockReached.AffectsShopId].TimeToComplete - Math.Abs(player.Shops[shopUnlockReached.AffectsShopId].TimeRemaining);
                    }
                }
            }

            int lowestLevel = player.Shops.Min(shop => shop.NumberOwned);
            List<ShopModel> shopsAtLowestLevel = player.Shops.Where(shop => shop.NumberOwned == lowestLevel).ToList();
            if (shopsAtLowestLevel.Any(shop => shop.Id == index))
            {
                List<UnlockModel> globalUnlocks = _gameRepository.GetGlobalUnlocks();
                List<UnlockModel> globalUnlocksReached = globalUnlocks.Where(gu => gu.Level > oldLevel && gu.Level <= player.Shops[index].NumberOwned).ToList();
                for (int i = 0; i < globalUnlocksReached.Count; i++)
                {
                    UnlockModel globalUnlockReached = globalUnlocksReached[i];
                    foreach (ShopModel shop in player.Shops)
                    {
                        shop.Multiplier *= globalUnlockReached.ProfitMultiplier;
                        shop.TimeToComplete *= globalUnlockReached.SpeedMultiplier;

                        if (globalUnlockReached.SpeedMultiplier != 1)
                        {
                            shop.TimeRemaining -= shop.TimeToComplete;
                            if (shop.TimeRemaining < 0)
                            {
                                player.Money += _gameRepository.CalculateCurrentProfitOfShop(shop);
                                shop.TimeRemaining = shop.TimeToComplete - Math.Abs(shop.TimeRemaining);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateCostOfShops()
        {
            for (int i = 0; i < _shopCosts.Length; i++)
            {
                double cost = _gameRepository.CalculateCostOfShop(i);
                _shopCosts[i].text = " $" + cost.ToString("e2");
            }
        }

        private void UpdateAmountOwned()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            for (int i = 0; i < _shopAmountOwneds.Length; i++)
                _shopAmountOwneds[i].text = player.Shops[i].NumberOwned.ToString();
        }

        private void UpdateTimeRemaining(ShopModel shop)
        {
            TimeSpan time = TimeSpan.FromSeconds(shop.TimeRemaining);
            _timeRemainings[shop.Id].text = shop.TimeToComplete > .01 ? string.Format("{0}:{1}:{2}.{3}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds) : "Really fast!";
        }

        private void UpdatePlayerMoney()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            _playerMoney.text = string.Format("${0:e2}", player.Money);
        }

        private void UpdateStatistics()
        {
            UpdateRevenuePerSecond();
            PlayerModel player = _gameRepository.GetPlayer();
            bool isStatsMenuOpen = _menus[2].activeSelf;

            if (!isStatsMenuOpen)
                _menus[2].SetActive(true);

            GameObject[] oldStats = GameObject.FindGameObjectsWithTag("Statistics Expandable List");
            foreach (GameObject oldStat in oldStats)
            {
                oldStat.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                Destroy(oldStat);
            }

            if (!isStatsMenuOpen)
                _menus[2].SetActive(false);

            GameObject playerStatistics = Instantiate(_statisticsExpandableList);
            playerStatistics.transform.SetParent(_menus[2].transform, false);
            playerStatistics.GetComponent<Text>().text = "Player";

            Button playerStatisticsButton = playerStatistics.GetComponentInChildren<Button>();
            playerStatisticsButton.onClick.AddListener(() =>
            {
                Vector3 rotationVector = playerStatisticsButton.transform.rotation.eulerAngles;
                CloseAllStatisticsLists();
                if (rotationVector.z == 90)
                {
                    MoveStatsListsDown(0);
                    rotationVector.z = 0;
                    playerStatisticsButton.transform.rotation = Quaternion.Euler(rotationVector);
                    playerStatistics.GetComponentsInChildren<Text>()[1].text = string.Format("Global Multiplier: {0:e2}\nIncome Per Second: ${1:e2}", player.GlobalMultiplier, player.RevenuePerSecond);
                }
            });

            for (int i = 1; i <= player.Shops.Count; i++)
            {
                GameObject shopStatistics = Instantiate(_statisticsExpandableList);
                shopStatistics.transform.SetParent(_menus[2].transform, false);
                shopStatistics.transform.localPosition = new Vector2(25, 300 - 40 * i);
                shopStatistics.GetComponent<Text>().text = player.Shops[i - 1].Name;
                AddStatisticsListener(shopStatistics, i);
            }

        }

        private void UpdateRevenuePerSecond()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            player.RevenuePerSecond = 0;
            for (int i = 0; i < player.Shops.Count; i++)
            {
                player.Shops[i].RevenuePerSecond = _gameRepository.CalculateCurrentProfitOfShop(player.Shops[i]) / player.Shops[i].TimeToComplete;
                if (player.Shops[i].Manager)
                    player.RevenuePerSecond += player.Shops[i].RevenuePerSecond;
            }
        }

        private void UpdateManagers()
        {
            GameObject[] oldManagers = GameObject.FindGameObjectsWithTag("Manager Button");
            foreach (GameObject oldManager in oldManagers)
            {
                oldManager.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(oldManager);
            }

            List<ManagerModel> managers = _gameRepository.GetManagers();
            List<ManagerModel> managersToShow = managers.Where(m => !m.IsPurchased).OrderBy(m => m.Cost).Take(6).ToList();

            for (int i = 0; i < managersToShow.Count; i++)
            {
                GameObject manager = Instantiate(_menuButton);

                manager.transform.SetParent(_menus[0].transform, false);
                manager.transform.localPosition = new Vector2(0, 210 - 70 * i);
                manager.GetComponentInChildren<Text>().text = string.Format("{0}\n${1:e2}", managersToShow[i].Name, managersToShow[i].Cost);
                manager.tag = "Manager Button";

                AddManagerListener(manager, managersToShow[i].Id);
            }
        }

        private void UpdateUpgrades()
        {
            GameObject[] oldUpgrades = GameObject.FindGameObjectsWithTag("Upgrade Button");
            foreach (GameObject oldUpgrade in oldUpgrades)
            {
                oldUpgrade.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(oldUpgrade);
            }

            List<UpgradeModel> upgrades = _gameRepository.GetUpgrades();
            List<UpgradeModel> upgradesToShow = upgrades.Where(u => !u.IsPurchased).OrderBy(u => u.Cost).Take(6).ToList();

            for (int i = 0; i < upgradesToShow.Count; i++)
            {
                GameObject upgrade = Instantiate(_menuButton);

                upgrade.transform.SetParent(_menus[1].transform, false);
                upgrade.transform.localPosition = new Vector2(0, 210 - 70 * i);
                upgrade.GetComponentInChildren<Text>().text = string.Format("{0}\n${1:e2}", upgradesToShow[i].Name, upgradesToShow[i].Cost);
                upgrade.tag = "Upgrade Button";

                AddUpgradeListener(upgrade, upgradesToShow[i].Id);
            }
        }

        private void AddUpgradeListener(GameObject upgrade, int index)
        {
            upgrade.GetComponent<Button>().onClick.AddListener(() => { PurchaseUpgrade(index); });
        }

        private void AddManagerListener(GameObject manager, int index)
        {
            manager.GetComponent<Button>().onClick.AddListener(() => { PurchaseManager(index); });
        }

        private void AddStatisticsListener(GameObject stat, int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            ShopModel shop = player.Shops[index - 1];
            Button statButton = stat.GetComponentInChildren<Button>();
            statButton.onClick.AddListener(() =>
            {
                Vector3 rotationVector = statButton.transform.rotation.eulerAngles;
                CloseAllStatisticsLists();
                if (rotationVector.z == 90)
                {
                    MoveStatsListsDown(index);
                    rotationVector.z = 0;
                    statButton.transform.rotation = Quaternion.Euler(rotationVector);
                    stat.GetComponentsInChildren<Text>()[1].text = string.Format("Multiplier: {0:e2}\nProfit: ${1:e2} \nSpeed: {2:e2} (sec)\nIncome Per Second: ${3:e2}", shop.Multiplier, _gameRepository.CalculateCurrentProfitOfShop(shop), shop.TimeToComplete, shop.RevenuePerSecond);
                }
            });
        }

        private void CloseAllStatisticsLists()
        {
            GameObject[] stats = GameObject.FindGameObjectsWithTag("Statistics Expandable List");
            for (int i = 0; i < stats.Length; i++)
            {
                GameObject stat = stats[i];
                stat.transform.localPosition = new Vector2(25, 300 - 40 * i);
                Button statButton = stat.GetComponentInChildren<Button>();
                Vector3 statButtonRotation = statButton.transform.rotation.eulerAngles;
                if (statButtonRotation.z == 0)
                {
                    statButtonRotation.z = 90;
                    statButton.transform.rotation = Quaternion.Euler(statButtonRotation);
                    stat.GetComponentsInChildren<Text>()[1].text = string.Empty;
                }
            }
        }

        private void MoveStatsListsDown(int index)
        {
            GameObject[] stats = GameObject.FindGameObjectsWithTag("Statistics Expandable List");
            for (int i = index + 1; i < stats.Length; i++)
            {
                GameObject stat = stats[i];
                Vector3 statPosition = stat.transform.localPosition;
                statPosition.y -= 100;
                stat.transform.localPosition = statPosition;
            }
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

                shop.TimeRemaining -= watch.Elapsed.TotalSeconds;
            } while (shop.TimeRemaining > 0);

            player.Money += _gameRepository.CalculateCurrentProfitOfShop(shop);
            shop.Working = false;
            shop.TimeRemaining = 0;

            UpdatePlayerMoney();
            UpdateTimeRemaining(shop);

            if (shop.Manager)
                StartCoroutine(WorkShop(index));
        }
        #endregion
    }
}
