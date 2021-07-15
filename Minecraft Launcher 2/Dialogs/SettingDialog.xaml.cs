using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Dialogs
{
    public partial class SettingDialog : UserControl
    {
        public SettingDialog()
        {
            InitializeComponent();
        }

        private void ForceUpdate_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageBox.Show("강제 업데이트가 적용되었습니다. 이제 설정창을 나가서 업데이트를 누르면 됩니다. 이 옵션은 저장되지않으며, 일회용으로 사용됩니다. 물론 서버와 연결되지 않을 경우 업데이트를 할 수 없습니다.", "안내");
        }
    }
}
