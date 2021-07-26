namespace Minecraft_Launcher_2.Pages.ViewModels.Dialogs
{
    public class PromptMessageBoxVM : ObservableObject
    {
        public PromptMessageBoxVM(string message)
            : this(message, null)
        { }

        public PromptMessageBoxVM(string message, string defaultText)
        {
            Message = message;
            InputText = defaultText ?? "";
        }

        public string Message { get; }

        public string InputText { get; set; }
    }
}