using SharedToolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SamNet
{
    /// <summary>
    /// Interaction logic for Page4View.xaml
    /// </summary>
    public partial class Page4View : UserControl
    {
        private EdwardsMessageBus eventBus;
        public Page4View()
        {
            InitializeComponent();

            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];
            eventBus.Subscribe<EventMessage>(handleEventMessage);

        }

        private void handleEventMessage(EventMessage em)
        {

        }

    }
}
