using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z.Expressions;

namespace Legend
{
    public class Job
    {
        [JsonProperty("职业名称")]
        public string Name { get; set; } = "";
        [JsonProperty("特殊等级升级需求")]
        public Level[] Levels { get; set; }=new Level[] {new Level()};
        [JsonProperty("初始伤害点数 (0-1的数值)")]
        public float Damage { get; set; } = 0.05f;
        [JsonProperty("升级时奖励伤害点数计算公式 init-初始伤害点数 level-等级")]
        public string DamageUp { get; set; } = "init+level/100*2";
        [JsonProperty("初始奖励技能点数")]
        public int SkillNum { get; set; } = 2;
        [JsonProperty("升级时奖励技能点数计算公式 init-初始奖励技能点数 level-等级")]
        public string SkillNumUp { get; set; } = "init+level*2";
        public static Job GetJob(string name)
        {
            var config = Config.GetConfig();
            var list=config.Jobs.ToList().FindAll(x => x.Name == name);
            if (list.Count == 0)
                return null;
            else
                return list[0];
        }

    }
    public class Level
    {
        [JsonProperty("等级")]
        public int num { get; set; }
        [JsonProperty("额外增加的伤害点数 (0-1的数值)")]
        public float Damage { get; set; }
        [JsonProperty("额外给予的技能点数")]
        public int SkillNum { get; set; }
        [JsonProperty("额外给予的技能学习位数量")]
        public int SkillMax { get; set; }
        [JsonProperty("额外给予的buff")]
        public int[] Buff { get; set; } = new int[] { 0 };
    }
}
