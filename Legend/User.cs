using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace Legend
{
    public class User : Table<User>
    {
        public string Name { get; set; }
        public string Clan { get; set; } = "";
        public int Level { get; set; } = -1;
        public int Exp { get; set; }
        public float Damage { get; set; }
        public string Job { get; set; } = "null";
        public List<(int,int)> Skill { get; set; }=new List<(int,int)> { };
        /// <summary>
        /// 最多技能数
        /// </summary>
        public int MaxSkill { get; set; } = 10;
        /// <summary>
        /// 技能点数
        /// </summary>
        public int SkillNum { get; set; }
        public int[] Buff { get; set; }
        public TSPlayer GetTSPlayer()
        {
            var list = TSPlayer.FindByNameOrID(Name);
            if (list.Count == 0)
                return null;
            else
                return list[0];
        }
        public void Update(params string[] args)
        {
            Update(new List<string> { "Name" }, args);
        }
        public static User Get(string name)
        {
            var list=User.Get(new Dictionary<string, object>() { { "Name",name} });
            if (list.Count == 0)
            {
                return null;
            }
            else
            {
                return list[0];
            }
        }
        public Clan GetClan()
        {
            if (Clan == String.Empty)
                return null;
            return Legend.Clan.Get(Clan);
        }
        public bool IsClanOwner()
        {
            var clan = GetClan();
            if(clan == null)
                return false;
            return clan.Owner == Name;
        }
        public bool IsClanAdmin()
        {
            var clan = GetClan();
            if (clan == null)
                return false;
            return clan.Admin.Contains(Name);
        }
    }
}
