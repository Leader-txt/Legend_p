using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z.Expressions;

namespace Legend
{
    public class Skill
    {
        public bool CanUse(string job)
        {
            return Jobs.Contains(job) || job.Contains("-1");
        }
        public bool CanUse(int Weapon)
        {
            return Weapons.Contains(Weapon) || Weapons.Contains(-1);
        }
        public bool CanUse(string job,int weapon)
        {
            return CanUse(job) && CanUse(weapon);
        }
        public int GetPrice(int level)
        {
            return Eval.Execute<int>(PriceUp, new { init = Price, level = level });
        }
        //[JsonProperty("技能名称")]
        public string Name { get; set; } = "";
        //[JsonProperty("技能备注")]
        public string Note { get; set; } = "";
        //[JsonProperty("初始技能触发概率")]
        public float Rate { get; set; } = 0.2f;
        //[JsonProperty("技能触发概率计算公式 init-初始技能触发概率 level-等级")]
        public string RateUp { get; set; } = "init +level/100*2";
        //[JsonProperty("初始技能学习点数")]
        public int Price { get; set; } = 2;
        //[JsonProperty("升级所需点数计算公式 init-初始技能学习点数 level-等级")]
        public string PriceUp { get; set; } = "init+level*2";
        //[JsonProperty("允许使用的职业名称 若该数组中包含‘-1’则所有职业均可使用")]
        public string[] Jobs { get; set; } = new string[] { "" };
        //[JsonProperty("允许使用的武器id 若该数组中包含-1，则所有职业均可使用")]
        public int[] Weapons { get; set; }=new int[] { 0 };
        //[JsonProperty("弹幕")]
        public Proj[] Projs = new Proj[] { new Proj() };
        #region 治疗
        //[JsonProperty("初始治疗触发概率 0-1的值")]
        public float HealRate { get; set; } = 0.2f;
        //[JsonProperty("治疗概率计算公式 init-初始治疗触发概率 level-等级")]
        public string HealRateUp { get; set; } = "init +level/100*2";
        //[JsonProperty("初始治疗半径")]
        public int HealArea { get; set; } = 300;
        //[JsonProperty("治疗半径计算公式 init-初始治疗半径 level-等级")]
        public string AreaUp { get; set; } = "init*level";
        //[JsonProperty("初始治疗点数")]
        public int Heal { get; set; } = 10;
        //[JsonProperty("治疗点数计算公式 init-初始治疗点数 level-等级")]
        public string HealUp { get; set; } = "init+level*10";
        #endregion
        #region 吸血
        //[JsonProperty("初始吸血概率 0-1的值")]
        public float UptakeRate { get; set; } = 0.2f;
        //[JsonProperty("吸血概率计算公式 init-初始治疗概率 level-等级")]
        public string UptakeRateUp { get; set; } = "init +level/100*2";
        //[JsonProperty("初始吸血点数")]
        public int Uptake { get; set; } = 10;
        //[JsonProperty("吸血点数计算公式 init-初始吸血点数 level-等级")]
        public string UptakeUp { get; set; } = "init+level*2";
        //[JsonProperty("初始吸血转化率")]
        public float TakeRate { get; set; } = 0.5f;
        //[JsonProperty("吸血转化率计算公式 init-初始吸血转化率 level-等级")]
        public string TakeRateUp { get; set; } = "init+level/100*2";
        //[JsonProperty("初始吸血半径")]
        public int TakeArea { get; set; } = 300;
        //[JsonProperty("吸血半径计算公式 init-初始吸血半径 level-等级")]
        public string TakeAreaUp { get; set; } = "init*level";
        #endregion
    }
    public class Proj
    {
        //[JsonProperty("弹幕id")]
        public int ProjID { get; set; }
        //[JsonProperty("初始生成概率(0-1的值)")]
        public float Rate { get; set; } = 0.8f;
        //[JsonProperty("生成概率计算公式 init-初始生成概率 level-等级")]
        public string RateUp { get; set; } = "init+level/100*2";
        //[JsonProperty("生成数量范围(最小值，最大值)")]
        public (int, int) Num { get; set; } = (5, 10);
        //[JsonProperty("多个弹幕生成时间间隔范围(最小值，最大值)单位：ms")]
        public (int ,int ) Span { get; set; }
        //[JsonProperty("弹幕生成位置范围X 以下均以玩家为原点")]
        public (float, float) X { get; set; } = (-10, 10);
        //[JsonProperty("弹幕生成位置范围Y")]
        public (float, float) Y { get; set; } = (-10, 10);
        //[JsonProperty("是否以最近敌对npc为攻击目标")]
        public bool Direct { get; set; } = false;
        //[JsonProperty("弹幕生成速度X分矢量范围")]
        public (float, float) VX { get; set; } = (-10, 10);
        //[JsonProperty("弹幕生成速度y分矢量范围")]
        public (float, float) VY { get; set; } = (-10, 10);
        //[JsonProperty("初始弹幕伤害")]
        public int Damage { get; set; } = 10;
        //[JsonProperty("弹幕伤害计算公式 init-初始伤害 level-弹幕等级")]
        public string DamageUp { get; set; } = "init+level*2";
        //[JsonProperty("初始弹幕击退")]
        public float KnockBack { get; set; } = 5;
        //[JsonProperty("弹幕击退计算公式 init-初始击退 level-弹幕等级")]
        public string KnockBackUp { get; set; } = "init+level*2";
    }
}
