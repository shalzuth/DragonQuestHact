using System;
using Grpc.Core;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Security.Cryptography;
using Grpc.Core.Interceptors;
using System.Text.Json;

namespace DragonQuestHact
{
    public class Login
    {
        public Headers Header;
        public static Channel BaseGameChannel;
        public DQTRPC.UsersYouReply Profile;
        public Login(String uid, String loginUrl = "prd-login-green-gbl.gdt-game.net.:443", String sqexUrl = "https://psg.sqex-bridge.jp")
        {
            var entrypointsChannel = new Channel("prd-entrypoint-gbl.gdt-game.net:443", new SslCredentials());
            // var entrypointsChannel = new Channel("localhost:8082", ChannelCredentials.Insecure);
            var entryClient = new DQTRPC.Entrypoints.EntrypointsClient(entrypointsChannel);
            var entrypoints = entryClient.Current(new DQTRPC.EntrypointsCurrentRequest { ClientVersion = "1.0.3", DeviceType = 2 });
            loginUrl = entrypoints.Entrypoint.LoginUrl;
            Header = new Headers();
            if (BaseGameChannel == null) BaseGameChannel = new Channel(loginUrl, new SslCredentials());
            var loginChannelWithHeaders = BaseGameChannel.Intercept(Header.Add);
            var loginClient = new DQTRPC.Auths.AuthsClient(loginChannelWithHeaders);
            var sesh = loginClient.PrepareSession(new DQTRPC.AuthsPrepareSessionRequest());
            //var uid = Guid.NewGuid().ToString().ToLower();
            var sqex = SqExSignin(sqexUrl, uid, sesh.OnetimeToken);
            Header.SharedSecurityKey = sqex.sharedSecurityKey;
            //var verify = loginClient.Verify(new MQRPC.AuthsVerifyRequest { SessionId = sqex.nativeSessionId });
            //var login = loginClient.Signup(new MQRPC.AuthsSignupRequest
            var login = loginClient.Login(new DQTRPC.AuthsLoginRequest
            {
                SessionId = sqex.nativeSessionId,
                DeviceType = 2,
                HasDeviceToken = false,
                DeviceToken = "",
                AdvertisingTrackingEnabled = false,
                AdvertisingId = "",
                //TerminalId = sqex.sharedSecurityKey,
                Platform = "Android",
                Store = "GooglePlay",
                DeviceModel = "OnePlus ONEPLUS A5000",
                OperatingSystem = "Android OS 7.1.1 / API-25 (NMF26X/327)"
            });
            Header.AccessToken = login.AccessToken;
            var usersClient = new DQTRPC.Users.UsersClient(loginChannelWithHeaders);
            var exists = usersClient.Existing(new DQTRPC.Empty());
            if (!exists.Existing)
                if (!usersClient.Create(new DQTRPC.Empty()).Success)
                    throw new Exception("failed to create new");
            Profile = usersClient.You(new DQTRPC.Empty());
        }

        class SqExResponse
        {
            public String sharedSecurityKey { get; set; }
            public String nativeSessionId { get; set; }
        }
        static SqExResponse SqExSignin(String url, String uuid, String token)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url + "/native/session");
            webRequest.Headers.Clear();
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.UserAgent = "dqtact/0 CFNetwork/1220.1 Darwin/20.3.0";
            webRequest.Timeout = 150000;
            webRequest.Accept = "application/json";
            webRequest.Headers[HttpRequestHeader.AcceptLanguage] = "en-us";
            webRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate, br";
            webRequest.Headers["X-Unity-Version"] = "2018.4.21f1";
            webRequest.KeepAlive = true;
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                UUID = uuid,
                deviceType = 2,
                nativeToken = token
            }));
            using (var stream = webRequest.GetRequestStream()) stream.Write(data, 0, data.Length);
            using (var stream = webRequest.GetResponse().GetResponseStream())
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[4096];
                var count = 0;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                var rawResponse = ms.ToArray();
                //var qq = Encoding.UTF8.GetString(rawResponse);
                var keyid = JsonSerializer.Deserialize<SqExResponse>(Encoding.UTF8.GetString(rawResponse));
                return keyid;
                //return new Tuple<String, String>(keyid["sharedSecurityKey"].ToString(), keyid["nativeSessionId"].ToString());
            }
        }
    }
}
