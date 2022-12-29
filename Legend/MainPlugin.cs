using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using Z.Expressions;

namespace Legend
{
    [ApiVersion(2, 1)]
    public class MainPlugin : TerrariaPlugin
    {
        private bool English => Language.ActiveCulture.LegacyId == 1;
        public override string Name => "Legend";
        public override string Description => English ? "Legend-A role play game plugin for TShock" : "传奇-RPG插件";
        public override string Author => "Leader";
        public override Version Version => new Version(1, 0, 1, 0);
        #region GlobalValues
        public static Dictionary<string, User> Users = new Dictionary<string, User>();
        public static int Tick = 0;
        public static Random Random = new Random();
        #endregion
        public MainPlugin(Main game) : base(game)
        {
            Config.GetConfig();
            //ReleaseDLL();
        }
        void ReleaseDLL()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            BufferedStream input = new BufferedStream(assembly.GetManifestResourceStream("Legend.res.Z.Expressions.Eval.dll"));
            FileStream output = new FileStream("Z.Expressions.Eval.dll", FileMode.Create);
            byte[] data = new byte[1024];
            int lengthEachRead;
            while ((lengthEachRead = input.Read(data, 0, data.Length)) > 0)
            {
                output.Write(data, 0, lengthEachRead);
            }
            output.Flush();
            output.Close();
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("legend.use", cmd, "legend"));
            Commands.ChatCommands.Add(new Command("legend.admin", admin, "legadm"));
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostinit);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnNetGreetPlayer);
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.GamePostUpdate.Register(this, OnGamePostUpdate);
            GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            GetDataHandlers.PlayerDamage.Register(OnPlayerDamage);
            TShockAPI.Hooks.RegionHooks.RegionCreated += RegionHooks_RegionCreated;
            TShockAPI.Hooks.RegionHooks.RegionDeleted += RegionHooks_RegionDeleted;
        }

        private void OnGamePostUpdate(EventArgs args)
        {
            if (Tick % 3600 == 0)
            {
                if (Tick != 0)
                    Tick = 0;
                //Console.WriteLine("set buffer");
                foreach (var user in Users.Values)
                {
                    var list = TSPlayer.FindByNameOrID(user.Name);
                    if (list.Count > 0)
                    {
                        if (user.Buff != null)
                            foreach (var b in user.Buff)
                            {
                                list[0].SetBuff(b);
                            }
                    }
                }
            }
            Tick++;
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            var user = Users[args.Player.name];
            args.Damage = (int)(args.Damage * (1 + user.Damage));
        }

        private void OnPlayerDamage(object sender, GetDataHandlers.PlayerDamageEventArgs e)
        {
            if (TShock.Players[e.ID].Name == e.Player.Name)
                return;
            if (Clan.Get(Users[TShock.Players[e.ID].Name].Clan) != null && Users[TShock.Players[e.ID].Name].Clan == Users[e.Player.Name].Clan && e.PVP)
            {
                e.Handled = true;
                e.Player.SendErrorMessage(English ? "You can't hurt the member of your clan!" : "公会成员间禁止pvp!");
            }
        }

        private void admin(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                if (English)
                {
                    args.Player.SendInfoMessage("/legadm reset,Reset all of the data");
                    args.Player.SendInfoMessage("/legadm exp [Name] [Experience]，Edit the value of player's experience");
                    args.Player.SendInfoMessage("/legadm level [Name] [Level]，Edit the value of player's level.");
                }
                else
                {
                    args.Player.SendInfoMessage("/legadm reset,重置所有数据");
                    args.Player.SendInfoMessage("/legadm exp 玩家名 经验值，修改经验值");
                    args.Player.SendInfoMessage("/legadm level 玩家名 等级，修改等级");
                }
                return;
            }
            switch (args.Parameters[0])
            {
                case "level":
                    {
                        var user = Users.ContainsKey(args.Parameters[1]) ? Users[args.Parameters[1]] : User.Get(args.Parameters[1]);
                        if (user != null)
                        {
                            user.Level = int.Parse(args.Parameters[2]);
                            user.Update("Level");
                            args.Player.SendSuccessMessage(English ? "Edit successfully!" : "修改成功！");
                        }
                        else
                        {
                            args.Player.SendErrorMessage(English ? "Unable to find the player!" : "查无此人！");
                        }
                    }
                    break;
                case "exp":
                    {
                        var user = Users.ContainsKey(args.Parameters[1]) ? Users[args.Parameters[1]] : User.Get(args.Parameters[1]);
                        if (user != null)
                        {
                            user.Exp = int.Parse(args.Parameters[2]);
                            user.Update("Exp");
                            args.Player.SendSuccessMessage(English ? "Edit successfully!" : "修改成功！");
                        }
                        else
                        {
                            args.Player.SendErrorMessage(English ? "Unable to find the player!" : "查无此人！");
                        }
                    }
                    break;
                case "reset":
                    {
                        User.Delete();
                        Shop.Delete();
                        Clan.Delete();
                        args.Player.SendSuccessMessage(English ? "Reset data successfully!" : "重置数据成功");
                    }
                    break;
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            var plr = TShock.Players[args.Who];
            Users[plr.Name].Update("Exp");
            Users.Remove(plr.Name);
        }

        private void OnNPCKilled(NpcKilledEventArgs args)
        {
            try
            {
                var plr = TShock.Players[args.npc.lastInteraction];
                var user = Users[plr.Name];
                var config = Config.GetConfig();
                var rate = config.ExpRate;
                if (user.Level >= 0)
                {
                    rate = Eval.Execute<float>(config.ExpRateUp, new { init = config.ExpRate, level = (float)user.Level });
                }
                int exp = (int)(rate * args.npc.lifeMax);
                user.Exp += exp;
                Utils.SendCombatText(plr.Index, "Exp+" + exp, Color.Yellow);
                if (user.Level >= 0)
                {
                    if (user.Level < config.MaxLevel)
                    {
                        var job = config.Jobs.ToList().Find(x => x.Name == user.Job);
                        int levelup = Utils.GetNextLevelExp(user.Level + 1);
                        if (user.Exp >= levelup)
                        {
                            user.Exp -= levelup;
                            user.Level++;
                        }
                        Utils.SendCombatText(plr.Index, "Level " + user.Level, Color.Yellow);
                        user.Damage += Eval.Execute<float>(job.DamageUp, new { init = job.Damage, level = (float)user.Level });
                        var level = job.Levels.ToList().Find(x => x.num == levelup);
                        if (level != null)
                        {
                            user.Damage += level.Damage;
                            user.SkillNum += level.SkillNum;
                            user.MaxSkill += level.SkillMax;
                            var buff = user.Buff.ToList();
                            buff.AddRange(level.Buff);
                            user.Buff = buff.ToArray();
                        }
                        user.Update("Level", "Exp", "Damage", "SkillNum", "MaxSkill", "Buff");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is IndexOutOfRangeException))
                {
                    TShock.Log.ConsoleError(ex.StackTrace);
                }
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
        }

        private void OnServerChat(ServerChatEventArgs args)
        {
            if (args.Text.StartsWith("/"))
                return;
            args.Handled = true;
            var plr = TShock.Players[args.Who];
            var user = Users[plr.Name];
            var clan = user.GetClan();
            string text = (clan == null ? "" : $"[{clan.Prefix}]") + (user.Level == -1 ? (English ? "[No Job]" : "[无职业]").Color("00ff00") : $"[{user.Job} lv{user.Level} exp{user.Exp}]") + plr.Name + ":" + args.Text;
            TShock.Utils.Broadcast(text, Color.White);
        }

        private void RegionHooks_RegionDeleted(TShockAPI.Hooks.RegionHooks.RegionDeletedEventArgs args)
        {
            var who = TSPlayer.FindByNameOrID(args.Region.Owner)[0];
            var user = Users[who.Name];
            var clan = user.GetClan();
            clan.RegionID = -1;
            clan.Update("RegionID");
            clan.Say(English ? "Region has been destoried!" : "领地已销毁！");
        }

        private void OnNetGreetPlayer(GreetPlayerEventArgs args)
        {
            var plr = TShock.Players[args.Who];
            var list = User.Get(new Dictionary<string, object>() { { "Name", plr.Name } });
            var user = new User() { Name = plr.Name };
            if (list.Count == 0)
            {
                user.MaxSkill = Config.GetConfig().MaxSkill;
                user.Insert();
                Users.Add(plr.Name, user);
            }
            else
            {
                Users.Add(plr.Name, list[0]);
            }
            user = Users[plr.Name];
            var shops = Shop.Get(new Dictionary<string, object> { { "Seller", user.Name }, { "SoldOut", true }, { "Clan", user.Clan } });
            if (shops.Count > 0)
            {
                foreach (var shop in shops)
                {
                    plr.GiveItem(shop.Price.NetID, shop.Price.Stack);
                    if (English)
                        plr.SendSuccessMessage($"Your goods:{shop.SellItem} has sold out，please check out it！");
                    else
                        plr.SendSuccessMessage($"您的商品{shop.SellItem}已售出，请查收！");
                    shop.Delete("id", "Clan");
                }
            }
        }

        private void cmd(CommandArgs args)
        {
            if (args.Player == TSPlayer.Server)
            {
                args.Player.SendErrorMessage("请在游戏中操作！");
                return;
            }
            if (args.Parameters.Count == 0)
            {
                if (English)
                {
                    args.Player.SendInfoMessage("/legend clan,clan commands");
                    args.Player.SendInfoMessage("/legend shop,trade system commands");
                    args.Player.SendInfoMessage("/legend skill,ability commands");
                    args.Player.SendInfoMessage("/legend job,job commands");
                }
                else
                {
                    args.Player.SendInfoMessage("/legend clan,公会命令");
                    args.Player.SendInfoMessage("/legend shop,交易系统");
                    args.Player.SendInfoMessage("/legend skill,技能");
                    args.Player.SendInfoMessage("/legend job,职业");
                }
                return;
            }
            var user = Users[args.Player.Name];
            switch (args.Parameters[0])
            {
                case "skill":
                    {
                        if (user.Level != -1)
                        {
                            if (args.Parameters.Count == 1)
                            {
                                if (English)
                                {
                                    args.Player.SendInfoMessage("/legend skill list，list all of the abilities you can choose");
                                    args.Player.SendInfoMessage("/legend skill info [Index]，check out the detailed information of the skill");
                                    args.Player.SendInfoMessage("/legend skill learn [Index],learn selected ability");
                                    args.Player.SendInfoMessage("/legend skill state,show the current state of your skills");
                                    args.Player.SendInfoMessage("/legend skill upg [id],upgrade your skills");
                                    args.Player.SendInfoMessage("/legend skill forget [id]，forget the selected skill");
                                }
                                else
                                {
                                    args.Player.SendInfoMessage("/legend skill list，列出所有可选技能");
                                    args.Player.SendInfoMessage("/legend skill info 技能索引，查看技能详细信息");
                                    args.Player.SendInfoMessage("/legend skill learn 技能索引,学习指定技能");
                                    args.Player.SendInfoMessage("/legend skill state,当前技能状态");
                                    args.Player.SendInfoMessage("/legend skill upg 技能id,升级技能");
                                    args.Player.SendInfoMessage("/legend skill forget 技能id，遗忘技能");
                                }
                                return;
                            }
                            switch (args.Parameters[1])
                            {
                                case "forget":
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        user.Skill.RemoveAt(index);
                                        user.Update("Skill");
                                        args.Player.SendSuccessMessage("遗忘技能成功！");
                                    }
                                    break;
                                case "upg":
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var config = Config.GetConfig();
                                        var skill = config.Skills[user.Skill[index].Item1];
                                        var level = user.Skill[index].Item2;
                                        if (level >= config.MaxSkillLevel)
                                        {
                                            args.Player.SendErrorMessage("已升至最高等级！");
                                            return;
                                        }
                                        if (skill.GetPrice(level + 1) > user.SkillNum)
                                        {
                                            args.Player.SendErrorMessage("技能点数不足，还需要:" + (skill.GetPrice(level + 1) - user.SkillNum));
                                            return;
                                        }
                                        user.Skill[index] = (user.Skill[index].Item1, level + 1);
                                        user.SkillNum -= skill.GetPrice(level + 1);
                                        user.Update("Skill", "SkillNum");
                                        args.Player.SendSuccessMessage("升级成功！");
                                    }
                                    break;
                                case "state":
                                    {
                                        int i = 0;
                                        var config = Config.GetConfig();
                                        args.Player.SendInfoMessage($"已学习技能数：{user.Skill.Count}/{user.MaxSkill}");
                                        foreach (var s in user.Skill)
                                        {
                                            var skill = config.Skills[user.Skill[i].Item1];
                                            var level = user.Skill[i].Item2;
                                            args.Player.SendInfoMessage($"技能id:{i} 技能名称:{skill.Name} 当前等级:{level} {(level >= config.MaxSkillLevel ? "已升至最高等级" : $"升级至下级还需{skill.GetPrice(level + 1) - user.SkillNum}技能点")}\n" +
                                                $"可用武器:{(skill.Weapons.Contains(-1) ? "全部" : string.Join(",", skill.Weapons.Select(x => $"[i:{x}]{Lang.GetItemName(x)}")))}\n" +
                                                $"技能备注:{skill.Note}");
                                            i++;
                                        }
                                    }
                                    break;
                                case "learn":
                                    {
                                        if (user.Skill.Count >= user.MaxSkill)
                                        {
                                            args.Player.SendErrorMessage("您已达到技能学习上限！");
                                            return;
                                        }
                                        int index = int.Parse(args.Parameters[2]);
                                        var config = Config.GetConfig();
                                        if (user.Skill.Select(x => x.Item1).Contains(index))
                                        {
                                            args.Player.SendErrorMessage("您已学习过该技能！");
                                            return;
                                        }
                                        var skill = config.Skills[index];
                                        if (!skill.Jobs.Contains(user.Job))
                                        {
                                            args.Player.SendErrorMessage("所选职业不能学习该技能！");
                                            return;
                                        }
                                        if (skill.Price > user.SkillNum)
                                        {
                                            args.Player.SendErrorMessage("技能点不足！您还需要:" + (skill.Price - user.SkillNum));
                                            return;
                                        }
                                        user.SkillNum -= skill.Price;
                                        user.Skill.Add((index, 0));
                                        user.Update("SkillNum", "Skill");
                                        args.Player.SendSuccessMessage("技能学习成功！");
                                    }
                                    break;
                                case "info":
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var config = Config.GetConfig();
                                        var skill = config.Skills[index];
                                        args.Player.SendInfoMessage($"技能:{skill.Name} [{(skill.CanUse(user.Job) ? "可用".Color("00FF00") : "不可用".Color("FF0000"))}]\n" +
                                            $"允许使用职业:{(skill.Jobs.Contains("-1") ? "所有" : string.Join(",", skill.Jobs))}\n" +
                                            $"允许使用武器:{(skill.Weapons.Contains(-1) ? "所有" : string.Join(",", skill.Weapons.Select(x => $"[i:{x}]{Lang.GetItemName(x)}")))}\n" +
                                            $"{(user.Skill.Select(x => x.Item1).Contains(index) ? (user.Skill.Find(x => x.Item1 == index).Item2 >= config.MaxSkillLevel ? "已升至最高等级！" : "升级到下级所需技能点" + skill.GetPrice(user.Skill.Find(x => x.Item1 == index).Item2 + 1)) : "学习该技能所需技能点:" + skill.Price)}\n" +
                                            $"技能备注:{skill.Note}");
                                    }
                                    break;
                                case "list":
                                    {
                                        var config = Config.GetConfig();
                                        int i = 0;
                                        foreach (var s in config.Skills)
                                        {
                                            args.Player.SendInfoMessage($"索引:{i} 名称:{s.Name}");
                                            i++;
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("您尚未选择职业！");
                        }
                    }
                    break;
                case "job":
                    {
                        if (args.Parameters.Count == 1)
                        {
                            args.Player.SendInfoMessage("/legend job list,列出所有职业");
                            args.Player.SendInfoMessage("/legend job choose 职业名称,选择职业");
                            args.Player.SendInfoMessage("/legend job state,查看当前职业状态");
                            args.Player.SendInfoMessage("/legend job forget,遗忘职业");
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "forget":
                                {
                                    if (user.Level == -1)
                                    {
                                        args.Player.SendErrorMessage("请先选择职业！");
                                        return;
                                    }
                                    user.Job = "";
                                    user.Level = -1;
                                    user.Buff = new int[0];
                                    user.SkillNum = 0;
                                    user.Skill.Clear();
                                    user.Update("Job", "Level", "Skill", "Buff", "SkillNum");
                                    args.Player.SendSuccessMessage("已成功遗忘职业！");
                                }
                                break;
                            case "state":
                                {
                                    if (user.Level == -1)
                                    {
                                        args.Player.SendErrorMessage("请先选择职业！");
                                        return;
                                    }
                                    var config = Config.GetConfig();
                                    args.Player.SendInfoMessage($"{user.Job} lv{user.Level}/{config.MaxLevel}\n" +
                                        $"exp:{user.Exp}\n" +
                                        (user.Level == config.MaxLevel ? "已升至最高等级！" : $"升级到下一级还需:{Utils.GetNextLevelExp(user.Level + 1) - user.Exp}"));
                                }
                                break;
                            case "choose":
                                {
                                    if (user.Level != -1)
                                    {
                                        args.Player.SendErrorMessage("不可重复选择职业！");
                                        return;
                                    }
                                    string job = args.Parameters[2];
                                    var config = Config.GetConfig();
                                    if (!config.Jobs.Select(i => i.Name).Contains(job))
                                    {
                                        args.Player.SendErrorMessage("查无此项！");
                                        return;
                                    }
                                    if (user.Exp < config.Exp)
                                    {
                                        args.Player.SendErrorMessage("经验不足，还需要:" + (config.Exp - user.Exp));
                                        return;
                                    }
                                    user.Job = job;
                                    user.Level = 0;
                                    user.Exp -= config.Exp;
                                    user.Damage = config.Jobs.ToList().Find(i => i.Name == user.Job).Damage;
                                    user.Update("Job", "Level", "Exp", "Damage");
                                    args.Player.SendSuccessMessage("职业选择成功！");
                                }
                                break;
                            case "list":
                                {
                                    var config = Config.GetConfig();
                                    foreach (var j in config.Jobs)
                                    {
                                        args.Player.SendInfoMessage($"{j.Name}");
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case "shop":
                    {
                        if (args.Parameters.Count == 1)
                        {
                            if (user.GetClan() != null)
                            {
                                args.Player.SendInfoMessage("/legend shop list,列出所有商品");
                                args.Player.SendInfoMessage("/legend shop buy 商品id，购买指定商品");
                                args.Player.SendInfoMessage("/legend shop sell 背包格子id 交易物id 交易物数量,售出物品");
                                args.Player.SendInfoMessage("/legend shop ret 商品id，撤回商品");
                                args.Player.SendInfoMessage("/legend shop check,查看可售出物品");
                            }
                            else
                            {
                                args.Player.SendInfoMessage("请先加入公会！");
                            }
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "Sell":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        int netid = int.Parse(args.Parameters[3]);
                                        int stack = int.Parse(args.Parameters[4]);
                                        if (netid <= 0 || stack <= 0)
                                        {
                                            args.Player.SendErrorMessage("输入的数值不正确，必须大于0！");
                                            return;
                                        }
                                        var sellItem = new Item()
                                        {
                                            NetID = args.Player.TPlayer.inventory[index].netID,
                                            Stack = args.Player.TPlayer.inventory[index].stack,
                                            Prefix = args.Player.TPlayer.inventory[index].prefix
                                        };
                                        if (sellItem.NetID <= 0 || sellItem.Stack <= 0)
                                        {
                                            args.Player.SendErrorMessage("您不能出售空物品！");
                                            return;
                                        }
                                        var price = new Item() { NetID = netid, Stack = stack };
                                        var shop = new Shop() { Clan = user.Clan, Seller = args.Player.Name, SellItem = sellItem, Price = price };
                                        var list = Shop.Get(new Dictionary<string, object>() { { "Clan", user.Clan }, { "SoldOut", false } });
                                        list.Sort((x, y) => x.id.CompareTo(y.id));
                                        if (list[0].id == 0)
                                            for (int i = 1; i < list.Count; i++)
                                            {
                                                if (list[i].id - list[i - 1].id > 1)
                                                {
                                                    shop.id = list[i - 1].id + 1;
                                                    break;
                                                }
                                            }
                                        else
                                            shop.id = 0;
                                        shop.Insert();
                                        args.Player.SendSuccessMessage("出售成功！");
                                        user.GetClan().Say($"{args.Player.Name}正在出售:{sellItem}!");
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您尚未选择公会！");
                                    }
                                }
                                break;
                            case "buy":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var key = new Dictionary<string, object>() { { "Clan", user.Clan }, { "id", index } };
                                        var list = Shop.Get(key);
                                        if (list.Count == 0)
                                        {
                                            args.Player.SendErrorMessage("查无此项!");
                                        }
                                        else
                                        {
                                            var shop = list[0];
                                            if (!Utils.CanDel(args.Player.Index, shop.Price))
                                            {
                                                args.Player.SendErrorMessage("背包中没有足够的物品以交易！");
                                                return;
                                            }
                                            Utils.DelItem(args.Player.Index, shop.Price);
                                            args.Player.GiveItem(shop.SellItem.NetID, shop.SellItem.Stack, shop.SellItem.Prefix);
                                            var plrs = TSPlayer.FindByNameOrID(shop.Seller);
                                            if (plrs.Count == 0)
                                            {
                                                var plr = plrs[0];
                                                plr.GiveItem(shop.Price.NetID, shop.Price.Stack);
                                                shop.Delete("Clan", "id");
                                                plr.SendSuccessMessage($"您的商品:{shop.SellItem}已售出，请查收！");
                                            }
                                            else
                                            {
                                                shop.SoldOut = true;
                                                shop.Update(key.Select(t => t.Key).ToList(), "SoldOut");
                                            }
                                            args.Player.SendSuccessMessage("商品已到账，请查收！");
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您尚未选择公会！");
                                    }
                                }
                                break;
                            case "ret":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var key = new Dictionary<string, object>() { { "Clan", user.Clan }, { "id", index } };
                                        var list = Shop.Get(key);
                                        if (list.Count > 0)
                                        {
                                            var shop = list[0];
                                            if (shop.Seller != args.Player.Name)
                                            {
                                                args.Player.SendErrorMessage("您不能撤回他人的商品！");
                                                return;
                                            }
                                            args.Player.GiveItem(shop.SellItem.Stack, shop.SellItem.Stack, shop.SellItem.Prefix);
                                            shop.Delete("Clan", "id");
                                            args.Player.SendSuccessMessage("已退回！");
                                        }
                                        else
                                        {
                                            args.Player.SendErrorMessage("查无此项！");
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您尚未加入公会！");
                                    }
                                }
                                break;
                            case "check":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        int j = 0;
                                        foreach (var i in args.Player.TPlayer.inventory)
                                        {
                                            args.Player.SendInfoMessage($"背包格子id:{j} 物品:{new Item() { NetID = i.netID, Stack = i.stack, Prefix = i.prefix }}");
                                            j++;
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您尚未加入公会！");
                                    }
                                }
                                break;
                            case "list":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        var list = Shop.Get(new Dictionary<string, object>() { { "Clan", user.Clan }, { "SoldOut", false } });
                                        list.Sort((x, y) => x.id.CompareTo(y.id));
                                        foreach (var i in list)
                                        {
                                            args.Player.SendInfoMessage($"商品id:{i.id} 出售者:{i.Seller}\n" +
                                                $"出售物品:{i.SellItem}\n" +
                                                $"需要:{i.Price}以兑换此商品");
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您尚未加入公会！");
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case "clan":
                    {
                        if (args.Parameters.Count == 1)
                        {
                            if (user.Clan == String.Empty)
                            {
                                args.Player.SendInfoMessage("/legend clan add 公会名称,创建公会");
                                args.Player.SendInfoMessage("/legend clan join 公会名称, 加入公会");
                            }
                            if (user.GetClan() != null && !user.IsClanOwner())
                            {
                                args.Player.SendInfoMessage("/legend clan leave,离开公会");
                                args.Player.SendInfoMessage("/legend clan state,查看公会状态");
                            }
                            if (user.IsClanAdmin())
                            {
                                args.Player.SendInfoMessage("/legend clan kick 成员名，踢出成员");
                                args.Player.SendInfoMessage("/legend clan ban 成员名，踢出并禁止该成员加入公会");
                                args.Player.SendInfoMessage("/legend clan bandel 玩家名，解禁");
                                args.Player.SendInfoMessage("/legend clan prefix 前缀，设置公会聊天前缀");
                            }
                            if (user.IsClanOwner())
                            {
                                args.Player.SendInfoMessage("/legend clan remove,解散公会");
                                args.Player.SendInfoMessage("/legend clan admin 成员名,设置/取消一位成员为管理员");
                                args.Player.SendInfoMessage("/legend clan turn 成员名,将会长转让给指定成员");
                            }
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "state":
                                {
                                    if (user.GetClan() != null)
                                    {
                                        var clan = user.GetClan();
                                        args.Player.SendInfoMessage($"公会名称：{clan.Name}\n" +
                                            $"会长:{clan.Owner}\n" +
                                            $"成员:{string.Join(",", clan.Members)}\n" +
                                            $"管理员:{string.Join(",", clan.Admin)}");
                                    }
                                    else { args.Player.SendErrorMessage("您尚未加入公会！"); }
                                }
                                break;
                            case "join":
                                {
                                    if (user.GetClan() == null)
                                    {
                                        string name = args.Parameters[2];
                                        var clan = Clan.Get(name);
                                        if (clan == null)
                                        {
                                            args.Player.SendErrorMessage("不存在的公会！");
                                            return;
                                        }
                                        if (clan.Ban.Contains(args.Player.Name))
                                        {
                                            args.Player.SendErrorMessage("您已被该公会封禁，无法加入此公会！");
                                            return;
                                        }
                                        clan.Members.Add(args.Player.Name);
                                        user.Clan = name;
                                        user.Update("Clan");
                                        clan.Update("Members");
                                        if (clan.RegionID != -1)
                                        {
                                            var region = TShock.Regions.GetRegionByID(clan.RegionID);
                                            TShock.Regions.AddNewUser(region.Name, args.Player.Name);
                                        }
                                        clan.Say(args.Player.Name + "已加入公会！");
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您已加入公会，无法进行此操作！");
                                    }
                                }
                                break;
                            case "prefix":
                                {
                                    if (user.IsClanAdmin())
                                    {
                                        string prefix = args.Parameters[2];
                                        var clan = user.GetClan();
                                        clan.Prefix = prefix;
                                        clan.Update("Prefix");
                                        clan.Say(args.Player.Name + "已将公会聊天前缀更改为：" + prefix);
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您不是公会管理员，您无权限操作！");
                                    }
                                }
                                break;
                            case "admin":
                                {
                                    if (user.IsClanOwner())
                                    {
                                        string name = args.Parameters[2];
                                        if (name == args.Player.Name)
                                        {
                                            args.Player.SendErrorMessage("您无需对自己操作！");
                                            return;
                                        }
                                        var clan = user.GetClan();
                                        if (!clan.Members.Contains(name))
                                        {
                                            args.Player.SendErrorMessage("公会中无此成员！");
                                            return;
                                        }
                                        if (clan.Admin.Contains(name))
                                        {
                                            clan.Admin.Remove(name);
                                            clan.Say(args.Player.Name + "已撤销管理员:" + name);
                                            clan.Update("Admin");
                                        }
                                        else
                                        {
                                            clan.Admin.Add(name);
                                            clan.Update("Admin");
                                            clan.Say(args.Player.Name + "已将" + name + "设置为管理员！");
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您不是公会会长，您无权进行此操作！");
                                    }
                                }
                                break;
                            case "turn":
                                {
                                    if (user.IsClanOwner())
                                    {
                                        string name = args.Parameters[2];
                                        if (name == args.Player.Name)
                                        {
                                            args.Player.SendErrorMessage("您无需转让给自己！");
                                            return;
                                        }
                                        var clan = user.GetClan();
                                        if (!clan.Members.Contains(name))
                                        {
                                            args.Player.SendErrorMessage("公会中无此成员！");
                                            return;
                                        }
                                        if (!clan.Admin.Contains(name))
                                        {
                                            clan.Admin.Add(name);
                                        }
                                        clan.Owner = name;
                                        clan.Admin.Remove(name);
                                        if (clan.RegionID != -1)
                                        {
                                            var region = TShock.Regions.GetRegionByID(clan.RegionID);
                                            Data.Command($"update region set Owner={name} where RegionName={region.Name} and WorldID={region.WorldID}");
                                        }
                                        clan.Update("Admin", "Owner");
                                        clan.Say(args.Player.Name + "已将会长转让给:" + name);
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您不是公会会长，无权进行此操作！");
                                    }
                                }
                                break;
                            case "bandel":
                                {
                                    if (user.IsClanAdmin())
                                    {
                                        string name = args.Parameters[2];
                                        var clan = user.GetClan();
                                        if (!clan.Ban.Contains(name))
                                        {
                                            args.Player.SendInfoMessage("该用户并未被封禁！");
                                            return;
                                        }
                                        clan.Ban.Remove(name);
                                        clan.Update("Ban");
                                        args.Player.SendInfoMessage(name + "已解禁!");

                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您不是公会管理员，您无权限操作！");
                                    }
                                }
                                break;
                            case "ban":
                                {
                                    if (user.IsClanAdmin())
                                    {
                                        string name = args.Parameters[2];
                                        if (name == args.Player.Name)
                                        {
                                            args.Player.SendErrorMessage("您无法将自己移出公会！");
                                            return;
                                        }
                                        var clan = user.GetClan();
                                        if (!clan.Members.Contains(name))
                                        {
                                            args.Player.SendErrorMessage("公会中无此成员:" + name);
                                            return;
                                        }
                                        if (User.Get(name).IsClanAdmin() && !user.IsClanOwner())
                                        {
                                            args.Player.SendErrorMessage("您无法将管理员踢出公会！");
                                            return;
                                        }
                                        clan.Members.Remove(name);
                                        clan.Admin.Remove(name);
                                        clan.Ban.Add(name);
                                        clan.Update("Members", "Admin", "Ban");
                                        if (clan.RegionID != -1)
                                        {
                                            var region = TShock.Regions.GetRegionByID(clan.RegionID);
                                            TShock.Regions.RemoveUser(region.Name, args.Player.Name);
                                        }
                                        clan.Say(args.Player.Name + "已将" + name + "踢出公会并封禁！");

                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("您不是公会管理员，您无权限操作！");
                                    }
                                }
                                break;
                            case "kick":
                                {
                                    if (user.IsClanAdmin())
                                    {
                                        string name = args.Parameters[2];
                                        if (name == args.Player.Name)
                                        {
                                            args.Player.SendErrorMessage("您无法将自己移出公会！");
                                            return;
                                        }
                                        var clan = user.GetClan();
                                        if (!clan.Members.Contains(name))
                                        {
                                            args.Player.SendErrorMessage("公会中无此成员:" + name);
                                            return;
                                        }
                                        if (User.Get(name).IsClanAdmin() && !user.IsClanOwner())
                                        {
                                            args.Player.SendErrorMessage("您无法将管理员踢出公会！");
                                            return;
                                        }
                                        clan.Members.Remove(name);
                                        clan.Admin.Remove(name);
                                        clan.Update("Members", "Admin");
                                        if (clan.RegionID != -1)
                                        {
                                            var region = TShock.Regions.GetRegionByID(clan.RegionID);
                                            TShock.Regions.RemoveUser(region.Name, args.Player.Name);
                                        }
                                        clan.Say(args.Player.Name + "已将" + name + "踢出公会！");
                                    }
                                    else
                                    {
                                        args.Player.SendInfoMessage("您不是公会管理员，您无权进行此操作！");
                                    }
                                }
                                break;
                            case "leave":
                                {
                                    if (user.GetClan() == null)
                                    {
                                        args.Player.SendInfoMessage("您尚未加入任何公会！");
                                        return;
                                    }
                                    if (user.IsClanOwner())
                                    {
                                        args.Player.SendErrorMessage("您当前是公会会长，您无法离开公会，若您执意离开，请先将会长职位转交他人!");
                                        return;
                                    }
                                    var clan = user.GetClan();
                                    clan.Members.Remove(args.Player.Name);
                                    clan.Admin.Remove(args.Player.Name);
                                    clan.Update("Members", "Admin");
                                    if (clan.RegionID != -1)
                                    {
                                        var region = TShock.Regions.GetRegionByID(clan.RegionID);
                                        TShock.Regions.RemoveUser(region.Name, args.Player.Name);
                                    }
                                    clan.Say(args.Player.Name + "已离开公会！");
                                    args.Player.SendSuccessMessage("您已离开公会!");
                                }
                                break;
                            case "remove"://会长
                                {
                                    if (!user.IsClanOwner())
                                    {
                                        args.Player.SendErrorMessage("您不是公会会长或未加入公会，无法解散公会！");
                                        return;
                                    }
                                    var clan = user.GetClan();
                                    clan.Delete("Name");
                                    clan.Say("公会已解散！");
                                    if (clan.RegionID != -1)
                                        TShock.Regions.DeleteRegion(clan.RegionID);
                                    args.Player.SendSuccessMessage("公会已解散！");
                                }
                                break;
                            case "add"://会长
                                {
                                    if (user.GetClan() != null)
                                    {
                                        args.Player.SendErrorMessage("您已加入公会，无法创建！");
                                        return;
                                    }
                                    var name = args.Parameters[2];
                                    var list = Clan.Get(new Dictionary<string, object>() { { "Name", name } });
                                    if (list.Count == 0)
                                    {
                                        try
                                        {
                                            var clan = new Clan() { Name = name, Owner = args.Player.Name };
                                            clan.Members.Add(args.Player.Name);
                                            clan.Admin.Add(args.Player.Name);
                                            clan.Prefix = name;
                                            clan.Insert();
                                            user.Clan = name;
                                            user.Update("Clan");
                                            args.Player.SendInfoMessage("公会创建成功！");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex);
                                        }
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("公会已存在，不可重复创建");
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            if (e.Control.IsUsingItem)
            {
                var user = Users[e.Player.Name];
                if (user.Level != -1)
                {
                    var config = Config.GetConfig();
                    foreach (var s in user.Skill)
                    {
                        var skill = config.Skills[s.Item1];
                        if (skill.CanUse(user.Job, e.Player.TPlayer.inventory[e.SelectedItem].netID))
                            if (Utils.Rate(Eval.Execute<float>(skill.RateUp, new { init = skill.Rate, level = (float)s.Item2 })))
                                new Thread(() =>
                                {
                                    Utils.SendTextAboveHead(e.Player.Index, skill.Name, Color.Red);
                                    if (skill.HealRate > 0 && Utils.Rate(skill.HealRateUp, skill.HealRate, s.Item2))
                                        //治疗
                                        new Task(() =>
                                            {
                                                Utils.GetPlayersNearBy(e.Player, Eval.Execute<int>(skill.AreaUp, new { init = skill.HealArea, level = s.Item2 })).ForEach(p =>
                                             {
                                                 p.Heal(Eval.Execute<int>(skill.HealUp, new { init = skill.Heal, level = s.Item2 }));
                                             });
                                            }).Start();
                                    if (skill.UptakeRate > 0 && Utils.Rate(skill.UptakeRateUp, skill.UptakeRate, s.Item2))
                                        //吸血
                                        new Task(() =>
                                            {
                                                int heal = 0;
                                                Utils.GetNearByNPC(e.Player.Index, Eval.Execute<int>(skill.TakeAreaUp, skill.TakeArea, s.Item2)).ForEach(p =>
                                                {
                                                    int strike = Eval.Execute<int>(skill.UptakeUp, skill.Uptake, s.Item2);
                                                    if (strike > p.life)
                                                    {
                                                        strike = p.life;
                                                    }
                                                    p.StrikeNPC(strike, 0, e.Player.Index);
                                                    heal += (int)(strike * Eval.Execute<float>(skill.TakeRateUp, skill.TakeRate, s.Item2));
                                                });
                                                e.Player.Heal(heal);
                                            }).Start();
                                    //弹幕
                                    if (skill.Projs.Length > 0)
                                        new Thread(() =>
                                        {
                                            skill.Projs.ForEach(p =>
                                            {
                                                if (Utils.Rate(p.RateUp, p.Rate, s.Item2))
                                                {
                                                    for (int i = 0; i < Random.Next(p.Num.Item1, p.Num.Item2 + 1); i++)
                                                    {
                                                        if (p.Direct)
                                                        {
                                                            var vec = Utils.GetNearByNPC(e.Player.Index).position - e.Position;
                                                            vec /= vec.Length();
                                                            vec *= new Vector2(Utils.RanFloat(p.VX), Utils.RanFloat(p.VY));
                                                            Projectile.NewProjectile(null, e.Position.X + Utils.RanFloat(p.X), e.Position.Y + Utils.RanFloat(p.Y),
                                                                vec.X, vec.Y, p.ProjID, Eval.Execute<int>(p.DamageUp,
                                                                new { init = p.Damage, level = s.Item2 }), Eval.Execute<float>(p.KnockBackUp, new { init = p.KnockBack, level = s.Item2 }));
                                                        }
                                                        else
                                                            Projectile.NewProjectile(null, e.Position.X + Utils.RanFloat(p.X), e.Position.Y + Utils.RanFloat(p.Y),
                                                                Utils.RanFloat(p.VX), Utils.RanFloat(p.VY), p.ProjID, Eval.Execute<int>(p.DamageUp,
                                                                new { init = p.Damage, level = s.Item2 }), Eval.Execute<float>(p.KnockBackUp, new { init = p.KnockBack, level = s.Item2 }));
                                                        Thread.Sleep(Random.Next(p.Span.Item1, p.Span.Item2 + 1));
                                                    }
                                                }
                                            });
                                        })
                                        { IsBackground = true }.Start();
                                })
                                { IsBackground = true }.Start();
                    }
                }
            }
        }

        private void RegionHooks_RegionCreated(TShockAPI.Hooks.RegionHooks.RegionCreatedEventArgs args)
        {
            var who = TSPlayer.FindByNameOrID(args.Region.Owner)[0];
            var user = Users[who.Name];
            if (!user.IsClanOwner())
            {
                who.SendErrorMessage("您不是公会会长，无权创建领地！");
                TShock.Regions.DeleteRegion(args.Region.ID);
                return;
            }
            else
            {
                var clan = user.GetClan();
                if (clan.RegionID != -1)
                {
                    who.SendErrorMessage("您的公会已有领地，请勿重复创建！");
                    TShock.Regions.DeleteRegion(args.Region.ID);
                    return;
                }
                clan.RegionID = args.Region.ID;
                clan.Update("RegionID");
                foreach (var member in clan.Members)
                {
                    TShock.Regions.AddNewUser(args.Region.Name, member);
                }
                clan.Say("公会领地已创建！");
            }
        }

        private void OnGamePostinit(EventArgs args)
        {
            Data.Init();
        }
    }
}
