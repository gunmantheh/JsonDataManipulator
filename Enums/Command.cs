using System.ComponentModel;

namespace JsonDataManipulator.Enums
{
    public enum Command
    {
        [Description("resume")]
        Resume,
        [Description("pause")]
        Pause,
        [Description("queue")]
        Queue,
        [Description("config")]
        Config
    }
}