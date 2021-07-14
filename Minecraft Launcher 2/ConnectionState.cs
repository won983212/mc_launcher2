namespace Minecraft_Launcher_2
{
    public enum RetrieveState
    {
        Processing, Loaded, Error
    }

    public class ConnectionState
    {
        public RetrieveState State { get; set; }
        public string ErrorMessage { get; set; } = null;
    }
}
