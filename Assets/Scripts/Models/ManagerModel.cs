using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Models
{
    public class ManagerModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int ShopId { get; set; }
        public double Multiplier { get; set; }
        public double Cost { get; set; }
        public bool IsPurchased { get; set; }
    }
}
