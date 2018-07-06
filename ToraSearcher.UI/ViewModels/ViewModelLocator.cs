using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
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
        public MainVM MainVM => ServiceLocator.Current.GetInstance<MainVM>();

        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainVM>();
        }

        public ViewModelLocator()
        {
        }
    }
}
