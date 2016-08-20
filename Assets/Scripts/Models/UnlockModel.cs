using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Models
{
    public class UnlockModel
    {
        public int ShopId { get; set; }
        public int AffectsShopId { get; set; }
        public int Level { get; set; }
        public double ProfitMultiplier { get; set; }
        public double SpeedMultiplier { get; set; }
    }
}
