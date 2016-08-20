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
        public Text[] _shopMultipliers;

        public Text _playerMoney;
        public Text _buyMultipleButtonText;
        public Text _incomePerSecond;
        public Text _globalMultiplier;

        public GameObject[] _menus;

        public GameObject _menuButton;

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

        public void UpdateStatistics()
        {
            PlayerModel player = _gameRepository.GetPlayer();
            _incomePerSecond.text = "Income Per Second: " + player.RevenuePerSecond.ToString("e2");
            _globalMultiplier.text = "Global Multiplier: " + player.GlobalMultiplier.ToString();
            for (int i = 0; i < _shopMultipliers.Length; i++)
                _shopMultipliers[i].text = player.Shops[i].Name + " Multiplier: " + player.Shops[i].Multiplier.ToString();
        }

        public void Buy(int index)
        {
            PlayerModel player = _gameRepository.GetPlayer();
            if (player.Money > _gameRepository.CalculateCostOfShop(index))
            {
                player.Money -= _gameRepository.CalculateCostOfShop(index);
                player.Shops[index].NumberOwned += player.BuyMultiple;
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
                    player.Shops[index].Manager = true;
                

                manager.IsPurchased = true;
                player.Managers.Add(index);
                UpdateManagers();
                UpdateStatistics();
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
                {
                    foreach (ShopModel shop in player.Shops)
                        shop.Multiplier *= upgrade.Multiplier;
                }
                else
                    player.Shops[upgrade.ShopId].Multiplier *= upgrade.Multiplier;

                upgrade.IsPurchased = true;
                player.Upgrades.Add(index);
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
            _timeRemainings[shop.Id].text = string.Format("{0}:{1}:{2}.{3}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
        }

        private void UpdatePlayerMoney()
        {
            PlayerModel player = _gameRepository.GetPlayer();

            if (player.Money > 1e6)
                _playerMoney.text = player.Money.ToString("e2");
            else
                _playerMoney.text = player.Money.ToString("C");
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
