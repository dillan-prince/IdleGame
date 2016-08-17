using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Models
{
    public class ShopModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public float GrowthRate { get; set; }
        public float InitialCost { get; set; }
        public float Multiplier { get; set; }
        public float TimeToComplete { get; set; }
        public float InitialProfit { get; set; }
        public int NumberOwned { get; set; }
        public bool Manager { get; set; }
        public bool Working { get; set; }
        public float TimeRemaining { get; set; }
        public float RevenuePerSecond { get; set; }
    }
}
