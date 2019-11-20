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
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Navigation;

namespace LoggerDeviceValues
{
    /// <summary>
    /// Логика взаимодействия для WindowDriver_Virtual.xaml
    /// </summary>
    public partial class WindowDriver_Virtual : Window
    {
        Driver_VirtualDevice TargerDriver;

        public WindowDriver_Virtual(Driver_VirtualDevice _driver)
        {
            InitializeComponent();
            TargerDriver = _driver;
            TextBlock_CoefA.Text = _driver.CoefA.ToString();
            TextBlock_CoefB.Text = _driver.CoefB.ToString();
            TextBlock_CoefC.Text = _driver.CoefC.ToString();
            CheckBox_EnableConvertOm2C.IsChecked = _driver.ModeConvertOm2C;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void CheckBox_EnableConvertOm2C_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CheckBox_EnableConvertOm2C.IsChecked)
            {
                double A = 0, B = 0, C = 0;
                double.TryParse(TextBlock_CoefA.Text, out A);
                double.TryParse(TextBlock_CoefA.Text, out B);
                double.TryParse(TextBlock_CoefA.Text, out C);

                TargerDriver.EnableConversionOm2C(A, B, C);
            }
            else
            {
                TargerDriver.DisableConversionOm2C();
            }
        }
    }
}
