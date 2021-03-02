public static class ZNetExtensions
{
    public static bool IsLocalInstance(this ZNet znet)
    {
        return !znet.IsDedicated() && znet.IsServer() && (znet.GetNrOfPlayers() > znet.GetPeers().Count);
    }

    public static bool IsClientInstance(this ZNet znet)
    {
        return !znet.IsDedicated() && !znet.IsServer();
    }

    public static bool IsServerInstance(this ZNet znet)
    {
        return !znet.IsLocalInstance() && !znet.IsClientInstance();
    }
}
