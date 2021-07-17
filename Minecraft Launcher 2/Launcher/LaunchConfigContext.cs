using Minecraft_Launcher_2.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Minecraft_Launcher_2.Launcher
{
    public class LaunchConfigContext
    {
        private static readonly Dictionary<string, object> RuleData = new Dictionary<string, object>();

        public LaunchConfigContext() { }

        public LaunchConfigContext(JObject json)
        {
            DeserializeFromLaunchConfigJson(json);
        }

        static LaunchConfigContext()
        {
            RuleData.Add("features#is_demo_user", false);
            RuleData.Add("features#has_custom_resolution", false);
            RuleData.Add("os#name", "windows");
        }

        public JObject Serialize()
        {
            JObject json = new JObject();
            json.Add("mainClass", MainClass);
            json.Add("arguments", new JObject(
                new JProperty("game", MinecraftGameArguments),
                new JProperty("jvm", MinecraftJVMArguments)
            ));
            json.Add("assets", AssetsVersion);
            json.Add("assetsUrl", AssetsURL);

            List<JObject> libraryObjs = new List<JObject>();
            foreach (Library lib in Libraries)
            {
                libraryObjs.Add(new JObject(new JProperty("name", lib.Name),
                    new JProperty("version", lib.Version)));
            }

            json.Add("libraries", new JArray(libraryObjs));
            return json;
        }

        public void DeserializeMinecraftJsonData(JObject json)
        {
            if (json.TryGetValue("mainClass", out JToken value))
                MainClass = value.ToString();

            if (json.TryGetValue("assets", out value))
                AssetsVersion = value.ToString();

            if (json.TryGetValue("assetIndex", out value))
                AssetsURL = value["url"].ToString();

            if (json.TryGetValue("arguments", out value))
            {
                MinecraftGameArguments = (MinecraftGameArguments + " " + ParseMinecraftArgument((JArray)value["game"])).Trim();
                MinecraftJVMArguments = (MinecraftJVMArguments + " " + ParseMinecraftArgument((JArray)value["jvm"])).Trim();
            }

            if (json.TryGetValue("libraries", out value))
            {
                foreach (JObject obj in (JArray)value)
                {
                    if (obj.ContainsKey("rules") && !CheckRule((JArray)obj["rules"]))
                        continue;

                    string[] names = obj.Value<string>("name").Split(':');
                    Library lib = new Library() { Name = names[0] + ":" + names[1], Version = names[2] };

                    if (Libraries.Contains(lib))
                        Libraries.Remove(lib);

                    Libraries.Add(lib);
                }
            }
        }

        public void DeserializeFromLaunchConfigJson(JObject json)
        {
            MainClass = json.Value<string>("mainClass");
            MinecraftGameArguments = json["arguments"].Value<string>("game");
            MinecraftJVMArguments = json["arguments"].Value<string>("jvm");
            AssetsVersion = json.Value<string>("assets");
            AssetsURL = json.Value<string>("assetsUrl");

            Libraries.Clear();
            JArray libs = json["libraries"] as JArray;
            foreach (JObject lib in libs)
                Libraries.Add(new Library(lib));
        }

        private int CheckRuleDataMatch(JObject rule, string category)
        {
            if (!rule.ContainsKey(category))
                return 0;

            foreach (JProperty p in rule[category])
            {
                string key = category + "#" + p.Name;
                if (!RuleData.ContainsKey(key))
                    return 0; // ignore
                if (!((JValue)p.Value).Value.Equals(RuleData[key]))
                    return -1; // not match
            }
            return 1; // match
        }

        private bool CheckRule(JArray rules)
        {
            int matchCount = 0;
            foreach (JObject rule in rules)
            {
                int match1 = CheckRuleDataMatch(rule, "features");
                int match2 = CheckRuleDataMatch(rule, "os");

                if (match1 == 0 && match2 == 0)
                    continue;

                if ((match1 == -1 || match2 == -1) == (rule.Value<string>("action") == "allow"))
                    return false;
                matchCount++;
            }

            return matchCount > 0;
        }

        private string ParseMinecraftArgument(JArray partialArgument)
        {
            if (partialArgument == null)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (JToken token in partialArgument)
            {
                if (token is JValue)
                {
                    sb.Append(token);
                }
                else if (token is JObject)
                {
                    if (!CheckRule((JArray)token["rules"]))
                        continue;
                    JToken value = token["value"];
                    if (value is JValue)
                        sb.Append(value);
                    else if (value is JArray)
                        sb.Append(string.Join(" ", value.ToObject<string[]>()));
                    else
                        throw new ArgumentException("Argument를 읽는 도중 알 수 없는 타입의 value를 발견했습니다: " + value);
                }
                else
                    throw new ArgumentException("Argument를 읽는 도중 알 수 없는 타입의 token을 발견했습니다: " + token);
                sb.Append(' ');
            }
            return sb.ToString().Substring(0, sb.Length - 1);
        }


        public string MainClass { get; set; } = "";

        public string MinecraftGameArguments { get; set; } = "";

        public string MinecraftJVMArguments { get; set; } = "";

        public string AssetsVersion { get; set; } = "";

        public string AssetsURL { get; set; } = "";

        public HashSet<Library> Libraries { get; set; } = new HashSet<Library>();
    }

    public class Library
    {
        public string Name { get; set; }

        public string Version { get; set; }


        public Library() { }

        public Library(JObject libraryJson)
        {
            Name = libraryJson.Value<string>("name");
            Version = libraryJson.Value<string>("version");
        }


        public override bool Equals(object obj)
        {
            return obj is Library library && library.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name + ":" + Version;
        }

        public string GetPath()
        {
            string[] nameToken = Name.Split(':');
            string path = string.Format("{0}\\{1}\\{2}\\{1}-{2}.jar", nameToken[0].Replace('.', '\\'), nameToken[1], Version);
            return Path.Combine(Settings.Default.MinecraftDir, "libraries", path);
        }
    }
}
