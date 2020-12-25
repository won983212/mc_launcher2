using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [Conditional("DEBUG")]
        private void VerifyPropertyName(string name)
        {
            if(TypeDescriptor.GetProperties(this)[name] == null)
            {
                Debug.Fail("Invaild property name: " + name);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            VerifyPropertyName(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
