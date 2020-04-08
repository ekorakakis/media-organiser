using Autofac;
using Caliburn.Micro;
using MediaOrganiser.ViewModels;
using System.Windows;

namespace MediaOrganiser
{
    public class Bootstrapper :  BootstrapperBase
    {
        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            base.Configure();
            var builder = new ContainerBuilder();

            //// register ViewModels
            //builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
            //    .Where(type => type.Name == "ViewModel")
            //    .Where(type => !(string.IsNullOrWhiteSpace(type.Namespace)) && type.Namespace.EndsWith("ViewModels"))
            //    .Where(type => type.GetInterface(typeof(INotifyPropertyChanged).Name) != null)
            //    .AsSelf()
            //    .InstancePerDependency();
            
            //// register Views
            //builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
            //    .Where(type => type.Name == "ViewModel")
            //    .Where(type => !(string.IsNullOrWhiteSpace(type.Namespace)) && type.Namespace.EndsWith("ViewModels"))
            //    .Where(type => type.GetInterface(typeof(INotifyPropertyChanged).Name) != null)
            //    .AsSelf()
            //    .InstancePerDependency();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }
    }
}
