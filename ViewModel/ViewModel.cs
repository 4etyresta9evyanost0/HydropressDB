using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HydropressDB
{
    internal abstract class ViewModel : INotifyPropertyChanged
    {
        #region Data Members
        ///
        #endregion
        public ViewModel() { }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
        
    }
}
