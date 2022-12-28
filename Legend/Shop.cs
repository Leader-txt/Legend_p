using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace Legend
{
    public class Item
    {
        public int NetID { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public override string ToString()
        {
            return $"[i/{Stack}:{NetID}]{Lang.GetItemNameValue(NetID)}x{Stack}"+(Prefix==0?"":TShockAPI.TShock.Utils.GetPrefixById(Prefix));
        }
    }
    public class Shop : Table<Shop>
    {
        public int id { get; set; }
        public string Seller { get; set; }
        public string Clan { get; set; }
        public Item SellItem { get; set; }
        public Item Price { get; set; }
        public bool SoldOut { get; set; } = false;
    }
}
