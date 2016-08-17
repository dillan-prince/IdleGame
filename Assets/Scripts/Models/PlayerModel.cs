using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class PlayerModel
    {
        public bool AudioOn { get; set; }
        public float Money { get; set; }
        public int BuyMultiple { get; set; }
        public float GlobalMultiplier { get; set; }
        public float RevenuePerSecond { get; set; }
        public List<ShopModel> Shops { get; set; }
    }
}