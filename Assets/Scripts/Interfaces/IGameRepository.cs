using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Interfaces
{
    public interface IGameRepository
    {
        void Save();
        void Load();
        PlayerModel GetPlayer();
        List<UpgradeModel> GetUpgrades();
        List<ManagerModel> GetManagers();
        List<UnlockModel> GetShopUnlocks(int index);
        List<UnlockModel> GetGlobalUnlocks();
        double CalculateCurrentProfitOfShop(ShopModel shop);
        double CalculateCostOfShop(int index);
    }
}
