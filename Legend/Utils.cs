using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using Z.Expressions;

namespace Legend
{
    public static class Utils
    {
        public static float RanFloat((float,float) src)
        {
            return RanFloat(src.Item1, src.Item2);
        }
        public static float RanFloat(float min,float max)
        {
            return min + (max - min) * (float)MainPlugin.Random.NextDouble();
        }
        public static TSPlayer[] GetPlayersNearBy(TSPlayer who,int area)
        {
            return TShock.Players.ToList().FindAll(p => p != null || p.Active || (Vector2.Distance(p.TPlayer.position, who.TPlayer.position) <= area)).ToArray();
        }
        public static bool Rate(string exp,float init,int level)
        {
            return Rate(Eval.Execute<float>(exp, new { init = init, level = level }));
        }
        public static bool Rate(float src)
        {
            return MainPlugin.Random.NextDouble() <= src;
        }
        public static void SendTextAboveHead(int who,string text,Color color)
        {
            var plr = TShock.Players[who];
            var data = new MemoryStream();
            using (var wr = new BinaryWriter(data))
            {
                wr.Write((short)0);
                wr.Write((byte)82);
                wr.Write((short)1);
                wr.Write((byte)who);
                wr.Write((byte)0);
                wr.Write(text);
                wr.Write((byte)color.R);
                wr.Write((byte)color.G);
                wr.Write((byte)color.B);
                var length = wr.BaseStream.Length;
                wr.BaseStream.Position = 0;
                wr.Write((short)length);
            }
            plr.SendRawData(data.ToArray());
        }
        public static void SendCombatText(int who,string text,Color color)
        {
            var plr = TShock.Players[who];
            plr.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, plr.X, plr.Y);
        }
        public static int GetNextLevelExp(int level)
        {
            var config = Config.GetConfig();
            return Eval.Execute<int>(config.LevelUp,new {exp=config.Exp,level=level});
        }
        public static bool CanDel(int who,Item item)
        {
            var plr = TShock.Players[who];
            foreach (var i in plr.Inventory)
            {
                if (i.netID == item.NetID)
                {
                    item.Stack-=i.stack;
                    if (item.Stack <= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void DelItem(int who ,Item item)
        {
            var plr = TShock.Players[who];
            int j = 0;
            foreach (var i in plr.Inventory)
            {
                if (i.netID == item.NetID)
                {
                    item.Stack -= i.stack;
                    plr.SendData(PacketTypes.PlayerSlot, "", who, j, i.prefix);
                    if (item.Stack <= 0)
                    {
                        return;
                    }
                }
                j++;
            }
        }
        public static IEnumerable<NPC> GetNearByNPC(int who, int dis , bool friendly = false)
        {
            var plr = TShock.Players[who];
            foreach (var npc in Main.npc)
            {
                if (Vector2.Distance(npc.position, plr.TPlayer.position) / 16 <= dis&&npc.friendly==friendly)
                {
                    yield return npc;
                }
            }
        }
        public static NPC GetNearByNPC(int who,bool friendly=false)
        {
            var plr = TShock.Players[who];
            float min = float.MaxValue;
            NPC result = null;
            foreach (var npc in Main.npc)
            {
                if (npc.friendly == friendly)
                {
                    var dis = Vector2.Distance(npc.position, plr.TPlayer.position);
                    if(dis < min)
                    {
                        min = dis;
                        result = npc;
                    }
                }
            }
            return result;
        }
    }
}
