using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace DragonQuestHact
{
    public class Game
    {
        Headers Header;
        public static Channel BaseGameChannel;
        CallInvoker GameChannel;
        public DQTRPC.UserProfilesYouReply Profile;
        public DQTRPC.ConsumableItemsAllReply Consumables;
        public Game(String accessToken, String key, String url = "prd-api-blue-gbl.gdt-game.net.:443")
        {
            Header = new Headers { AccessToken = accessToken, SharedSecurityKey = key };
            if (BaseGameChannel == null) BaseGameChannel = new Channel(url, new SslCredentials());
            GameChannel = BaseGameChannel.Intercept(Header.Add);
            var userProfilesClient = new DQTRPC.UserProfiles.UserProfilesClient(GameChannel);
            Profile = userProfilesClient.You(new DQTRPC.Empty());
            var consumablesClient = new DQTRPC.ConsumableItems.ConsumableItemsClient(GameChannel);
            //Consumables = consumablesClient.All(new MQRPC.ConsumableItemsAllRequest { MasterConsumableItemCodes = 0 });
            //var stagesClient = new MQRPC.Stages.StagesClient(GameChannel);
            //var allStages = stagesClient.All(new MQRPC.Empty());
        }
        Byte[] RandomBattle()
        {
            var json = "{\"actions\": []}";
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
        public List<UInt32> GetMissions(UInt32 stageCode)
        {
            var missions = new List<UInt32>();
            foreach(var stage in DqtFiles.Caches["stages"])
                if (stage["code"].GetValue<UInt32>() == stageCode && stage["missions"] != null)
                    foreach(var mission in stage["missions"].AsArray())
                        missions.Add(mission.GetValue<UInt32>());
            return missions;
        }
        public List<UInt32> GetTreasures(UInt32 stageCode)
        {
            var treasures = new List<UInt32>();
            foreach(var stage in DqtFiles.Caches["stages"])
                if (stage["code"].GetValue<UInt32>() == stageCode && stage["treasures"] != null)
                    foreach(var mission in stage["treasures"].AsArray())
                        treasures.Add(mission.GetValue<UInt32>());
            return treasures;
        }
        public DQTRPC.AdventuresEndReply DoBattle(UInt32 stageCode, out DQTRPC.AdventuresStartReply startReply)
        {
            var qq = GetTreasures(stageCode);
            var adventuresClient = new DQTRPC.Adventures.AdventuresClient(GameChannel);
            startReply = adventuresClient.Start(new DQTRPC.AdventuresStartRequest
            {
                StageCode = stageCode,
                PartyId = 1
            });
            var deadEnemies = new List<UInt32>();
            foreach (var r in startReply.DefeatingEnemyRewards) deadEnemies.Add(r.EnemyCode);
            var endRequest = new DQTRPC.AdventuresEndRequest
            {
                StageCode = stageCode,
                Win = true,
                TurnCount = 2,
                Reason = DQTRPC.DefeatReasonType.UnsetDefeatReasonType,
                BattleRecord = Google.Protobuf.ByteString.CopyFrom(RandomBattle())
            };
            endRequest.DefeatedEnemyCodes.Add(deadEnemies);
            endRequest.AcquiredTreasureChestCodes.Add(GetTreasures(stageCode));
            endRequest.CompletedStageMissionCodes.Add(GetMissions(stageCode));
            //endRequest.MasterRanks.Add(new List<MasterRank> { MasterRank });
            var end = adventuresClient.End(endRequest);
            return end;
        }
    }
}
