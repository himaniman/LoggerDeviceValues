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
            TargerDriver = _driver;
            InitializeComponent();
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.None) ComboBox_NTCMethod.SelectedIndex = 0;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.StHr) ComboBox_NTCMethod.SelectedIndex = 1;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.B25) ComboBox_NTCMethod.SelectedIndex = 2;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.RT8016) ComboBox_NTCMethod.SelectedIndex = 3;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.B57861S0103F040) ComboBox_NTCMethod.SelectedIndex = 4;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void CheckBox_EnableConvertOm2C_Click(object sender, RoutedEventArgs e)
        {
            //if ((bool)CheckBox_EnableConvertOm2C.IsChecked)
            //{
            //    double A = 0, B = 0, C = 0;
            //    double.TryParse(TextBlock_CoefA.Text, out A);
            //    double.TryParse(TextBlock_CoefA.Text, out B);
            //    double.TryParse(TextBlock_CoefA.Text, out C);

            //    TargerDriver.EnableConversionOm2C(A, B, C);
            //}
            //else
            //{
            //    TargerDriver.DisableConversionOm2C();
            //}
        }

        private void ComboBox_NTCMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TextBlock_R25.Text = TargerDriver.ConventerNTC_obj.R25.ToString();

            //TextBlock_CoefR25.Text = TargerDriver.ConventerNTC_obj.CoefR25.ToString();
            //TextBlock_CoefB25.Text = TargerDriver.ConventerNTC_obj.CoefB25.ToString();

            //TextBlock_CoefA.Text = TargerDriver.ConventerNTC_obj.CoefA.ToString();
            //TextBlock_CoefB.Text = TargerDriver.ConventerNTC_obj.CoefB.ToString();
            //TextBlock_CoefC.Text = TargerDriver.ConventerNTC_obj.CoefC.ToString();
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            switch (ComboBox_NTCMethod.SelectedIndex)
            {
                case 0:
                    TargerDriver.DisableConversionTemperature();
                    break;
                case 1:
                    double A, B, C;
                    if (double.TryParse(TextBlock_CoefA.Text, out A) && double.TryParse(TextBlock_CoefB.Text, out B) && double.TryParse(TextBlock_CoefC.Text, out C))
                    {
                        TargerDriver.EnableConversionStHr(A, B, C);
                    }
                    else
                    {
                        if (double.TryParse(TextBlock_CoefA.Text, out A)) TextBlock_CoefA.Background = new SolidColorBrush(Colors.Red);
                        if (double.TryParse(TextBlock_CoefB.Text, out B)) TextBlock_CoefB.Background = new SolidColorBrush(Colors.Red);
                        if (double.TryParse(TextBlock_CoefC.Text, out C)) TextBlock_CoefC.Background = new SolidColorBrush(Colors.Red);
                    }
                    break;
                case 4:
                    TargerDriver.EnableConversionNTCB57861S0103F040();
                    break;
            }
        }
    }
}
