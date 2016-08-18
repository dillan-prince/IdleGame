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
        void Buy(int index);
        void PurchaseManager(int index);
        void ChangeBuyMultiple();
        IEnumerator WorkShop(int index);
    }
}
