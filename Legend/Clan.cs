using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace Legend
{
    public class Clan : Table<Clan>
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Owner { get; set; }
        public List<string> Admin { get; set; }=new List<string>(){ };
        public List<string> Members { get; set; }= new List<string>(){ };
        public List<string> Ban { get; set; }=new List<string>();
        public int RegionID { get; set; } = -1;
        public void Update(params string[] args)
        {
            Update(new List<string>() { "Name"}, args);
        }
        public static Clan Get(string name)
        {
            var list = Get(new Dictionary<string, object>() { { "Name", name } });
            if (list.Count == 0)
            {
                return null;
            }
            else
            {
                return list[0];
            }
        }
        public void Say(string text)
        {
            foreach (var s in Members)
            {
                MainPlugin.Users[s].Clan = String.Empty;
                MainPlugin.Users[s].Update(new List<string>() { "Name" }, "Clan");
                MainPlugin.Users[s].GetTSPlayer()?.SendInfoMessage($"[{Name}]公告:{text}");
            }
        }
    }
}
