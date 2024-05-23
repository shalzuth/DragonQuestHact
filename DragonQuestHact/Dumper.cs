using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.IO;
using uTinyRipper;
using uTinyRipper.Converters;
using System.Xml;

namespace DragonQuestHact
{
    public static class Extensions
    {
        public static AssetsFileInstance instance;
        public static AssetsManager am;
        public static AssetTypeValueField GetObj(this AssetTypeValueField atvf)
        {
            var FileID = atvf.Get("m_FileID").GetValue().AsInt64();
            var PathID = atvf.Get("m_PathID").GetValue().AsInt64();
            if (PathID == 0) return null;
            var info = instance.table.GetAssetInfo(PathID);
            var typeInstance = am.GetTypeInstance(instance, info);
            return typeInstance.GetBaseField();
        }
    }
    public class Drops
    {
        public String name { get; set; }
        public UInt32? quantity { get; set; }
        public Single rate { get; set; }
    }
    public class Stage
    {
        public String name { get; set; }
        public UInt32 code { get; set; }
        public Int32 stamina { get; set; }
        public List<Int32> missions { get; set; }
        public List<Int32> treasures { get; set; }
        //public UInt32 gold { get; set; }
        //public UInt32 exp { get; set; }
        public List<Drops> drops { get; set; }
    }
    public class Item
    {
        public String name { get; set; }
        public UInt32 code { get; set; }
    }
    class Program
    {
        static String DumpObj(AssetTypeValueField obj)
        {
            var sb = new StringBuilder();
            foreach (var c in obj.children)
            {
                sb.AppendLine(c.GetName() + " : " + c.GetValue()?.ToString());
            }
            return sb.ToString();
        }
        static Dictionary<String, String> translation = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            var assetUrl = "https://prd-cdn-gdt-game.akamaized.net/339330752ee9099ddaf5eaf0c29a29ed44ca4e7b";
            //var assetUrl = "https://prd-cdn-gdt-game.akamaized.net/77ee048d4a8ace48ab40ca1af94619abae59cca7";
            new System.Net.WebClient().DownloadFile(assetUrl + "/AssetBundles/Android/data/variant/masterfile.assetbundle.en", "masterfile.assetbundle.en");
            new System.Net.WebClient().DownloadFile(assetUrl + "/AssetBundles/Android/data/master.assetbundle", "master.assetbundle");
            //var masterFile = @"C:\Users\andy\Downloads\dqt\android\assets\Android\data\variant\masterfile.assetbundle.en.unpack";

            var engineExporter = new EngineAssetExporter();
            var gs = uTinyRipper.GameStructure.Load(new List<String> { "masterfile.assetbundle.en" });
            gs.FileCollection.Exporter.OverrideExporter(ClassIDType.MonoBehaviour, engineExporter);
            gs.Export("masterfile.assetbundle.en.unpack", (a) =>
            {
                return true;
            });
            //var master = @"C:\Users\andy\Downloads\dqt\android\assets\Android\data\master.assetbundle.unpack";
            var am = new AssetsManager();
            Extensions.am = am;
            var bun1 = am.LoadBundleFile("masterfile.assetbundle.en", true);
            var files = BundleHelper.LoadAllAssetsDataFromBundle(bun1.file);
            for (var i = 0; i < files.Count; i++)
            {
                var name = bun1.file.bundleInf6.dirInf[i].name;

                var stream = new MemoryStream(files[i]);
                var inst = am.LoadAssetsFile(stream, name, true);
                var inf = inst.table.GetAssetInfo("Translation");
                var typeInst = am.GetTypeInstance(inst, inf);
                var rawData = typeInst.GetBaseField().Get("rawData").Get("Array").GetChildrenList();
                foreach (var kv in rawData)
                {
                    var key = kv.Get("key").GetValue().AsString();
                    var value = kv.Get("value").GetValue().AsString();
                    translation.Add(key, value);
                    //Console.WriteLine(key + " : " + value);
                }
            }
            var bun2 = am.LoadBundleFile("master.assetbundle", true);
            files = BundleHelper.LoadAllAssetsDataFromBundle(bun2.file);
            for (var i = 0; i < files.Count; i++)
            {
                var name = bun2.file.bundleInf6.dirInf[i].name;
                var stream = new MemoryStream(files[i]);
                var inst = am.LoadAssetsFile(stream, name, true);
                Extensions.instance = inst;

                if (true)
                {
                    var info = inst.table.GetAssetInfo("StageMasterDataStoreSource");
                    var typeInst = am.GetTypeInstance(inst, info);

                    var stages = new List<Stage> { };
                    var stageCodeSeeds = typeInst.GetBaseField().Get("indexedByStageCodeSeed").Get("seeds").Get("Array").GetChildrenList();
                    foreach (var kv in stageCodeSeeds)
                    {
                        var code = kv.Get("code").GetValue().AsInt();
                        var stageObj = kv.Get("data").GetObj();
                        var displayName = stageObj.Get("displayName").GetValue().AsString();
                        var stageCode = stageObj.Get("code").GetValue().AsUInt();
                        var nrg = stageObj.Get("consumptionStamina").GetValue().AsInt();
                        //var stage = new Stage { name = translation[displayName], code = stageCode, nrg = nrg, drops = new List<Drops> { } };
                        var stage = new Stage { name = translation[displayName], code = stageCode, stamina = nrg, drops = new List<Drops> { } };
                        //if (!displayName.Contains("Festival.Slime.Story.") || !(displayName.Contains("05") || displayName.Contains("04"))) continue;
                        //if (!displayName.Contains("Festival.Slime.Story.")) continue;
                        var rewards = stageObj.Get("randomRewards").Get("Array").GetChildrenList();
                        foreach (var reward in rewards)
                        {
                            var rewardObj = reward.GetObj();
                            var dropCandidates = rewardObj.Get("dropCandidates").Get("Array").GetChildrenList();
                            foreach (var drop in dropCandidates)
                            {
                                var weight = drop.Get("weight").GetValue().AsUInt();
                                var dropObj = drop.Get("lootGroup").GetObj();
                                //var item = dropObj.Get("consumableItemLoots").Get("Array").children[0];

                                if (dropObj.Get("consumableItemLoots").Get("Array").GetChildrenCount() > 0)
                                {
                                    var item = dropObj.Get("consumableItemLoots").Get("Array").children[0];
                                    var quantity = item.Get("quantity").GetValue().AsUInt();
                                    var itemName = item.Get("drop").GetObj();
                                    var itemDisplayName = itemName.Get("displayName").GetValue().AsString();
                                    var dropName = translation[itemDisplayName];
                                    stage.drops.Add(new Drops { name = dropName, quantity = quantity, rate = weight });
                                }

                                if (dropObj.Get("equipmentLoots").Get("Array").GetChildrenCount() > 0)
                                {
                                    var item = dropObj.Get("equipmentLoots").Get("Array").children[0];
                                    var quantity = item.Get("quantity").GetValue().AsUInt();
                                    var eqObj = item.Get("drop").GetObj().Get("profile").GetObj();
                                    var itemDisplayName = eqObj.Get("displayName").GetValue().AsString();
                                    var dropName = translation[itemDisplayName];
                                    stage.drops.Add(new Drops { name = dropName, quantity = quantity, rate = weight });
                                }
                            }
                        }
                        var fixedRewards = stageObj.Get("fixedReward").GetObj();
                        if (fixedRewards != null)
                        {
                            var fixedDropCandidates = fixedRewards.Get("dropCandidates").Get("Array").GetChildrenList();
                            foreach (var drop in fixedDropCandidates)
                            {
                                var dropObj = drop.Get("lootGroup").GetObj();

                                if (dropObj.Get("consumableItemLoots").Get("Array").GetChildrenCount() > 0)
                                {
                                    var item = dropObj.Get("consumableItemLoots").Get("Array").children[0];
                                    //stage.gold = item.Get("quantity").GetValue().AsUInt();
                                }
                                if (dropObj.Get("monsterExperiences").Get("Array").GetChildrenCount() > 0)
                                {
                                    var xp = dropObj.Get("monsterExperiences").Get("Array").children[0];
                                    //stage.exp = xp.Get("quantity").GetValue().AsUInt();
                                }
                            }
                        }
                        var enemies = stageObj.Get("enemies").Get("Array").GetChildrenList();
                        var weighted = new Dictionary<String, Single>();
                        var rareWeighted = new Dictionary<String, Single>();
                        var enemyCount = 0;
                        var rareEnemyCount = 0;
                        foreach (var enemy in enemies)
                        {
                            var monster = enemy.Get("monster").GetObj();
                            var monsterName = translation[monster.Get("profile").GetObj().Get("displayName").GetValue().AsString()];
                            var chance = (Single)monster.Get("scoutProbabilityPermyriad").GetValue().AsInt();
                            var q = DumpObj(monster);
                            var prize = monster.Get("isRareScout").GetValue().AsInt() > 0;
                            if (prize) rareEnemyCount++;
                            else enemyCount++;
                            if (chance == 0) continue;
                            if (monsterName.Contains("Great Sabrecat"))
                                Console.WriteLine("");
                            if (prize)
                            {
                                if (rareWeighted.ContainsKey(monsterName))
                                    rareWeighted[monsterName] += chance;
                                else rareWeighted.Add(monsterName, chance);
                            }
                            else
                            {
                                if (weighted.ContainsKey(monsterName))
                                    weighted[monsterName] += chance;
                                else weighted.Add(monsterName, chance);
                            }
                            stage.drops.Add(new Drops { name = monsterName + " : " + ((monster.Get("isRareScout").GetValue().AsInt() > 0) ? "_prize" : ""), rate = chance });
                        }
                        foreach (var w in weighted)
                        {
                            stage.drops.Add(new Drops { name = w.Key, rate = w.Value / (enemyCount * 100) });
                        }
                        foreach (var w in rareWeighted)
                        {
                            stage.drops.Add(new Drops { name = w.Key, rate = w.Value / (rareEnemyCount * 100) });
                        }
                        var missions = stageObj.Get("stageMissionList").GetObj().Get("stageMissions").Get("Array").GetChildrenList();
                        if (missions.Length > 0) stage.missions = new List<Int32>();
                        foreach (var mission in missions)
                            stage.missions.Add(mission.GetObj().Get("code").GetValue().AsInt());
                        var treasures = stageObj.Get("treasureChests").Get("Array").GetChildrenList();
                        if (treasures.Length > 0) stage.treasures = new List<Int32>();
                        foreach (var treasure in treasures)
                            stage.treasures.Add(treasure.Get("treasureChest").GetObj().Get("code").GetValue().AsInt());
                        if (stage.drops.Count == 0) stage.drops = null;
                        stages.Add(stage);
                    }

                    File.WriteAllText("stages.json", JsonConvert.SerializeObject(stages, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                }
                if (true)
                {

                    var info = inst.table.GetAssetInfo("ConsumableItemMasterDataStoreSource");
                    var typeInst = am.GetTypeInstance(inst, info);
                    var items = new List<Item>();
                    var stageCodeSeeds = typeInst.GetBaseField().Get("indexedByConsumableItemCodeSeed").Get("seeds").Get("Array").GetChildrenList();
                    foreach (var kv in stageCodeSeeds)
                    {
                        var code = kv.Get("code").GetValue().AsInt();
                        var itemObj = kv.Get("data").GetObj();
                        var displayName = itemObj.Get("displayName").GetValue().AsString();
                        var itemCode = itemObj.Get("code").GetValue().AsUInt();
                        items.Add(new Item { code = itemCode, name = translation[displayName] });
                    }

                    File.WriteAllText("items.json", JsonConvert.SerializeObject(items, Formatting.Indented));
                }
                {
                    var info = inst.table.GetAssetInfo("MonsterProfileMasterDataStoreSource", 114);
                    var typeInst = am.GetTypeInstance(inst, info);
                    var monsters = new List<Item>();
                    var qq = typeInst.GetBaseField();
                    var codeSeeds = typeInst.GetBaseField().Get("indexedByMonsterProfileCodeSeed").Get("seeds").Get("Array").GetChildrenList();
                    foreach (var kv in codeSeeds)
                    {
                        var code = kv.Get("code").GetValue().AsInt();
                        var itemObj = kv.Get("data").GetObj();
                        var displayName = itemObj.Get("displayName").GetValue().AsString();
                        var itemCode = itemObj.Get("code").GetValue().AsUInt();
                        monsters.Add(new Item { code = itemCode, name = translation[displayName] });
                    }

                    File.WriteAllText("monsters.json", JsonConvert.SerializeObject(monsters, Formatting.Indented));
                }
                {
                    var info = inst.table.GetAssetInfo("EquipmentMasterDataStoreSource", 114);
                    var typeInst = am.GetTypeInstance(inst, info);
                    var monsters = new List<Item>();
                    var qq = typeInst.GetBaseField();
                    var codeSeeds = typeInst.GetBaseField().Get("indexedByEquipmentCodeSeed").Get("seeds").Get("Array").GetChildrenList();
                    foreach (var kv in codeSeeds)
                    {
                        var code = kv.Get("code").GetValue().AsInt();
                        var itemObj = kv.Get("data").GetObj();
                        var displayName = itemObj.Get("profile").GetObj().Get("displayName").GetValue().AsString();
                        var itemCode = itemObj.Get("code").GetValue().AsUInt();
                        monsters.Add(new Item { code = itemCode, name = translation[displayName] });
                    }

                    File.WriteAllText("equipments.json", JsonConvert.SerializeObject(monsters, Formatting.Indented));
                }
                if (false)
                {
                    var t = "StageMission";
                    var info = inst.table.GetAssetInfo(t + "MasterDataStoreSource", 114);
                    var typeInst = am.GetTypeInstance(inst, info);
                    var missions = new List<Item>();
                    var qq = typeInst.GetBaseField();
                    var codeSeeds = typeInst.GetBaseField().Get("indexedBy" + t + "CodeSeed").Get("seeds").Get("Array").GetChildrenList();
                    foreach (var kv in codeSeeds)
                    {
                        var code = kv.Get("code").GetValue().AsInt();
                        var itemObj = kv.Get("data").GetObj();
                        var displayName = itemObj.Get("displayName").GetValue().AsString();
                        var itemCode = itemObj.Get("code").GetValue().AsUInt();
                        missions.Add(new Item { code = itemCode, name = translation[displayName] });
                    }

                    File.WriteAllText("missions.json", JsonConvert.SerializeObject(missions, Formatting.Indented));
                }
            }
            //var qq = inst.table.GetAssetInfo("StageMasterDataStoreSource");
        }
        /*using (var file = new FileStream(fullPath, FileMode.Open))
        {
            var bun = new AssetsTools.NET.AssetBundleFile();// BundleFileInstance(file, "", true);
            var reader = new AssetsTools.NET.AssetsFileReader(file);
            bun.Read(reader, true);
            Console.WriteLine("");

        }*/
        /*if (bun.bundleHeader6.GetCompressionType() != 0 && unpackIfPacked)
        {
            bun = BundleHelper.UnpackBundle(bun);
        }
        var firstAssetsFile = BundleHelper.LoadAssetFromBundle(bun, 0);
        var qq = inst.table.GetAssetInfo("StageMasterDataStoreSource");*/
    }
}

