using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legend
{
    public class Config
    {
        private const string path = "tshock/Legend.json";
        //[JsonProperty("初始最多可学习技能数")]
        public int MaxSkill { get; set; } = 10;
        //[JsonProperty("最高技能等级")]
        public int MaxSkillLevel { get; set; } = 10;
        //[JsonProperty("初始经验值获取比例 获取的经验值=击杀怪物血量x此比例")]
        public float ExpRate { get; set; } = 0.2f;
        //[JsonProperty("经验值获取比例计算公式 init-初始比例 level-等级")]
        public string ExpRateUp { get; set; } = "init+level/100*2";
        /// <summary>
        /// 升级至1级所需经验值
        /// </summary>
        //[JsonProperty("初始经验值")]
        public int Exp { get; set; } = 300;
        //[JsonProperty("升级至下级所需经验计算公式(exp:初始经验值,level:玩家当前等级)")]
        public string LevelUp { get; set; } = "exp*2^level";
        //[JsonProperty("最高等级")]
        public int MaxLevel { get; set; } = 40;
        //[JsonProperty("职业")]
        public Job[] Jobs { get; set; }=new Job[] {new Job() };
        //[JsonProperty("技能")]
        public Skill[] Skills { get; set; } = new Skill[] { new Skill() };
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter(path))
            {
                wr.WriteLine(JsonConvert.SerializeObject(this,Formatting.Indented));
            }
        }
        public static Config GetConfig()
        {
            var config = new Config();
            if (!File.Exists(path))
            {
                config.Save();
                return config;
            }
            else
            {
                using (StreamReader sr=new StreamReader(path))
                {
                    config = JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
                }
                return config;
            }
        }
    }
}
