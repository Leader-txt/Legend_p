using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;

namespace Legend
{
    public class Data
    {
        public static void Init()
        {
            Clan.Create();
            User.Create();
            Shop.Create();
        }
        public static QueryResult Command(string text)
        {
            //Console.WriteLine(text);
            return TShock.DB.QueryReader(text);
        }
    }
    public abstract class Table<T> where T : new()
    {
        private static string CSType2SqlTYpe(Type type)
        {
            if (type == typeof(string))
            {
                return "text";
            }
            else if (type == typeof(int) || type.BaseType == typeof(Enum) || type == typeof(bool))
            {
                return "int(32)";
            }
            else if (type == typeof(long))
            {
                return "int(64)";
            }
            else if (type == typeof(float))
            {
                return "real";
            }
            else if (type == typeof(double))
            {
                return "float";
            }
            else if (type == typeof(byte))
            {
                return "tinyint";
            }
            else if (type == typeof(short))
            {
                return "smallint";
            }
            else
            {
                return "text";
            }
        }
        public static void Create()
        {
            string cmd = $"create table if not exists {typeof(T).Name}(";
            bool first = true;
            foreach (var yield in typeof(T).GetProperties())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cmd += ",";
                }
                cmd += $"{yield.Name} {CSType2SqlTYpe(yield.PropertyType)}";
            }
            cmd += ")";
            //Console.WriteLine(cmd);
            Data.Command(cmd).Dispose();
        }
        public static void Delete(Dictionary<string, object> key = null)
        {
            Data.Command($"delete from {typeof(T).Name} " + GetKey(key)).Dispose();
        }
        public void Delete(params string[] key)
        {
            Delete(key.ToList());
        }
        public void Delete(List<string> key = null)
        {
            string _key = GetKey(key);
            Data.Command($"delete from {GetType().Name} " + _key).Dispose();
        }
        public void Insert()
        {
            string con = $"insert into {GetType().Name} (";
            string value = ")values(";
            bool first = true;
            foreach (var proty in GetType().GetProperties())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    con += ",";
                    value += ",";
                }
                con += proty.Name;
                value += CSVar2SQLVar(proty.PropertyType, proty.GetValue(this));
            }
            string cmd = con + value + ")";
            //Console.WriteLine(cmd);
            Data.Command(cmd).Dispose();
        }
        public static string CSVar2SQLVar(string type, object value)
        {
            return CSVar2SQLVar(typeof(T).GetProperty(type).PropertyType, value);
        }
        private static string CSVar2SQLVar(Type type, object value)
        {
            if (type == typeof(string))
            {
                return $"'{value}'";
            }
            else if (CSType2SqlTYpe(type) == "text")
            {
                return $"'{JsonConvert.SerializeObject(value)}'";
            }
            else if (type.BaseType == typeof(Enum))
            {
                return ((int)value).ToString();
            }
            else if (type == typeof(bool))
            {
                return (bool)value ? "1" : "0";
            }
            return value.ToString();
        }
        public string CSVar2SQLVar(string val)
        {
            var proty = GetType().GetProperty(val);
            return CSVar2SQLVar(proty.PropertyType, proty.GetValue(this));
        }
        private static string GetKey(Dictionary<string, object> key = null)
        {
            if (key == null)
            {
                return "";
            }
            else
            {
                string _key = "";
                bool first = true;
                if (key != null)
                {
                    _key = "where ";
                    foreach (var pair in key)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            _key += "and ";
                        }
                        _key += pair.Key + "=" + CSVar2SQLVar(pair.Key, pair.Value) + " ";
                    }
                }
                return _key;
            }
        }
        private string GetKey(List<string> key = null)
        {
            if (key == null)
            {
                return "";
            }
            else
            {
                string _key = "";
                bool first = true;
                if (key != null)
                {
                    _key = "where ";
                    foreach (var pair in key)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            _key += "and ";
                        }
                        _key += pair + "=" + CSVar2SQLVar(pair) + " ";
                    }
                }
                return _key;
            }
        }
        public void Update(List<string> key = null, params string[] source)
        {
            Update(source.ToList(), key);
        }
        public void Update(List<string> source, List<string> key = null)
        {
            var _key = GetKey(key);
            string cmd = $"update {GetType().Name} set ";
            bool first = true;
            foreach (var pair in source)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cmd += ",";
                }
                var proty = GetType().GetProperty(pair);
                cmd += pair + "=" + CSVar2SQLVar(proty.PropertyType, proty.GetValue(this)) + " ";
            }
            cmd += _key;
            Data.Command(cmd).Dispose();
        }
        public static List<T> Get(Dictionary<string, object> key = null)
        {
            List<T> list = new List<T>();
            string _key = GetKey(key);
            using (var reader = Data.Command($"select * from {typeof(T).Name} " + _key))
            {
                int j = 0;
                while (reader.Read())
                {
                    list.Add(new T());
                    int i = 0;
                    foreach (var proty in list[j].GetType().GetProperties())
                    {
                        var type = proty.PropertyType;
                        if (type == typeof(string))
                        {
                            proty.SetValue(list[j], reader.Reader.GetString(i), null);
                        }
                        else if (type == typeof(int) || type.BaseType == typeof(Enum))
                        {
                            proty.SetValue(list[j], reader.Reader.GetInt32(i), null);
                        }
                        else if (type == typeof(long))
                        {
                            proty.SetValue(list[j], reader.Reader.GetInt64(i), null);
                        }
                        else if (type == typeof(float))
                        {
                            proty.SetValue(list[j], reader.Reader.GetFloat(i), null);
                        }
                        else if (type == typeof(double))
                        {
                            proty.SetValue(list[j], reader.Reader.GetDouble(i), null);
                        }
                        else if (type == typeof(byte))
                        {
                            proty.SetValue(list[j], reader.Reader.GetByte(i), null);
                        }
                        else if (type == typeof(short))
                        {
                            proty.SetValue(list[j], reader.Reader.GetInt16(i), null);
                        }
                        else if (type == typeof(bool))
                        {
                            proty.SetValue(list[j], reader.Reader.GetInt32(i) != 0);
                        }
                        else
                        {
                            proty.SetValue(list[j], JsonConvert.DeserializeObject(reader.Reader.GetString(i), proty.PropertyType), null);
                        }
                        i++;
                    }
                    j++;
                }
            }
            return list;
        }
    }
}
