using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Interfaces
{
    public interface IGameService
    {
        void Save();
        void Load();
        void HideMenus();
        void RefreshCanvas();
        void UpdateStatistics();
        IEnumerator WorkShop(ShopModel shop);
    }
}
