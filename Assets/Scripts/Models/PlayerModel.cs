using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class PlayerModel
    {
        public bool AudioOn { get; set; }
        public double Money { get; set; }
        public int BuyMultiple { get; set; }
        public double GlobalMultiplier { get; set; }
        public double RevenuePerSecond { get; set; }
        public List<int> Upgrades { get; set; }
        public List<int> Managers { get; set; }
        public List<ShopModel> Shops { get; set; }
    }
}