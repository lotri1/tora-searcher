using GalaSoft.MvvmLight;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToraSearcher.UI.ViewModels
{
    public class ViewModelLocator : ViewModelBase
    {
        public MainVM MainVM { get; } = new MainVM();

        public ViewModelLocator()
        {
        }
    }
}
