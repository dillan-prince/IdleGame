using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Interfaces
{
    public interface IGameService
    {
        void Save();
        void Load();
        void DisplayOfflineEarnings();
        void HideMenus();
        void RefreshCanvas();
        void Buy(int index);
        void PurchaseManager(int index);
        void PurchaseUpgrade(int index);
        void ChangeBuyMultiple();
        void WorkShop(int index);
    }
}
