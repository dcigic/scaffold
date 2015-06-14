using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcScaf.UI
{
    public class MyViewModel
    {
        public MyParameters MyParameters { get; set; }
    }

    public class MyParameters : ObservableCollection<Parameter>
    {
        public MyParameters(IEnumerable<Parameter> parameters) : base(parameters)
        {

        }


    }

    public class Parameter : INotifyPropertyChanged
    {
        private string _myType;
        private string _myParameter;

        public Parameter(string myType, string myParameter)
        {
            _myType = myType;
            _myParameter = myParameter;
        }

        public string MyParameter
        {
            get { return _myParameter; }
            set
            {
                _myParameter = value;
                OnPropertyChanged("MyParameter");
            }
        }

        public string MyType
        {
            get { return _myType; }
            set
            {
                _myType = value;
                OnPropertyChanged("MyType");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
