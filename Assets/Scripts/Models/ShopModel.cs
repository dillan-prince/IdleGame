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
        public double GrowthRate { get; set; }
        public double InitialCost { get; set; }
        public double Multiplier { get; set; }
        public double TimeToComplete { get; set; }
        public double InitialProfit { get; set; }
        public int NumberOwned { get; set; }
        public bool Manager { get; set; }
        public bool Working { get; set; }
        public double TimeRemaining { get; set; }
        public double RevenuePerSecond { get; set; }
    }
}
