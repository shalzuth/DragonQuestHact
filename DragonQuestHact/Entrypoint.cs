using Grpc.Core;

namespace DragonQuestHact
{
    public static class Entrypoint
    {
        public static DQTRPC.EntrypointsCurrentReply Entrypoints()
        {
            var entrypointsChannel = new Channel("prd-entrypoint-gbl.gdt-game.net:443", new SslCredentials());
            var entryClient = new DQTRPC.Entrypoints.EntrypointsClient(entrypointsChannel);
            var entrypoints = entryClient.Current(new DQTRPC.EntrypointsCurrentRequest { ClientVersion = "1.1.0", DeviceType = 2 });
            return entrypoints;
        }
    }
}
