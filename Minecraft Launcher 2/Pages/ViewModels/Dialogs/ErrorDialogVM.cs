using System;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.Pages.ViewModels.Dialogs
{
    public class ErrorDialogVM : ObservableObject
    {
        public ErrorDialogVM(string title, string message)
        {
            Title = title;
            Message = message;
            FullMessage = null;
        }

        public ErrorDialogVM(Exception e)
        {
            Title = "오류 발생";
            Message = e.Message;
            FullMessage = e.ToString();
        }

        public string Title { get; set; }

        public string Message { get; set; }

        public string FullMessage { get; set; }

        public bool HasFullMessage => FullMessage != null;

        public ICommand ShowDetailCommand => new RelayCommand(ShowDetail);

        private void ShowDetail()
        {
            if (FullMessage != null)
            {
                var res = MessageBox.Show("발생한 오류의 모든 내용을 복사하시겠습니까?", "복사", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                    Clipboard.SetText(FullMessage);
            }
        }
    }
}