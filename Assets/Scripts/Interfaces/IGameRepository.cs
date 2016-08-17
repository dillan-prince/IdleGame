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
        PlayerModel Load();
        PlayerModel GetPlayer();
        float CalculateCurrentProfitOfShop(ShopModel shop);
        float CalculateCostOfShop(int index);
    }
}
