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
    /// Логика взаимодействия для WindowDriver_UT71D.xaml
    /// </summary>
    public partial class WindowDriver_UT71D : Window
    {
        Driver_UT71D TargerDriver;
        bool NTCDataValidate = true;

        public WindowDriver_UT71D(Driver_UT71D _driver)
        {
            TargerDriver = _driver;
            InitializeComponent();
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.None) ComboBox_NTCMethod.SelectedIndex = 0;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.StHr) ComboBox_NTCMethod.SelectedIndex = 1;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.B25) ComboBox_NTCMethod.SelectedIndex = 2;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.RT8016) ComboBox_NTCMethod.SelectedIndex = 3;
            if (TargerDriver.ConventerNTC_obj.mode == ConventerNTC.ConversionModes.B57861S0103F040) ComboBox_NTCMethod.SelectedIndex = 4;
            TextBlock_R25.Text = TargerDriver.ConventerNTC_obj.R25.ToString();
            TextBlock_CoefR25.Text = TargerDriver.ConventerNTC_obj.CoefR25.ToString();
            TextBlock_CoefB25.Text = TargerDriver.ConventerNTC_obj.CoefB25.ToString();
            TextBlock_CoefA.Text = TargerDriver.ConventerNTC_obj.CoefA.ToString();
            TextBlock_CoefB.Text = TargerDriver.ConventerNTC_obj.CoefB.ToString();
            TextBlock_CoefC.Text = TargerDriver.ConventerNTC_obj.CoefC.ToString();
            ComboBox_NTCMethod_SelectionChanged(null, null);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ComboBox_NTCMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TextBlock_R25 != null)
            {
                StackPanel_CalcStHt.Visibility = Visibility.Collapsed;
                StackPanel_CalcB25.Visibility = Visibility.Collapsed;
                StackPanel_Calc8016.Visibility = Visibility.Collapsed;
                StackPanel_CalcB57861S0103F040.Visibility = Visibility.Collapsed;
                switch (ComboBox_NTCMethod.SelectedIndex)
                {
                    case (int)ConventerNTC.ConversionModes.StHr:
                        StackPanel_CalcStHt.Visibility = Visibility.Visible;
                        break;
                    case (int)ConventerNTC.ConversionModes.B25:
                        StackPanel_CalcB25.Visibility = Visibility.Visible;
                        break;
                    case (int)ConventerNTC.ConversionModes.RT8016:
                        StackPanel_Calc8016.Visibility = Visibility.Visible;
                        break;
                    case (int)ConventerNTC.ConversionModes.B57861S0103F040:
                        StackPanel_CalcB57861S0103F040.Visibility = Visibility.Visible;
                        break;
                }
                TextBox_NTCDataChange(null, null);
            }
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            switch (ComboBox_NTCMethod.SelectedIndex)
            {
                case (int)ConventerNTC.ConversionModes.None:
                    TargerDriver.DisableConversionTemperature();
                    break;
                case (int)ConventerNTC.ConversionModes.StHr:
                    double A, B, C;
                    if (double.TryParse(TextBlock_CoefA.Text, out A) && double.TryParse(TextBlock_CoefB.Text, out B) && double.TryParse(TextBlock_CoefC.Text, out C))
                    {
                        TargerDriver.EnableConversionStHr(A, B, C);
                    }
                    else
                    {
                        TextBox_NTCDataChange(null, null);
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.B25:
                    double CoefR25, CoefB25;
                    if (double.TryParse(TextBlock_CoefR25.Text, out CoefR25) && double.TryParse(TextBlock_CoefB25.Text, out CoefB25))
                    {
                        TargerDriver.EnableConversionB25(CoefR25, CoefB25);
                    }
                    else
                    {
                        TextBox_NTCDataChange(null, null);
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.RT8016:
                    double R25;
                    if (double.TryParse(TextBlock_R25.Text, out R25))
                    {
                        TargerDriver.EnableConversionRT8016(R25);
                    }
                    else
                    {
                        TextBox_NTCDataChange(null, null);
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.B57861S0103F040:
                    TargerDriver.EnableConversionNTCB57861S0103F040();
                    break;
            }
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            Button_Apply_Click(null, null);
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_NTCDataChange(object sender, TextChangedEventArgs e)
        {
            switch (ComboBox_NTCMethod.SelectedIndex)
            {
                case (int)ConventerNTC.ConversionModes.None:
                    NTCDataValidate = true;
                    break;
                case (int)ConventerNTC.ConversionModes.StHr:
                    double A, B, C;
                    if (double.TryParse(TextBlock_CoefA.Text, out A) && double.TryParse(TextBlock_CoefB.Text, out B) && double.TryParse(TextBlock_CoefC.Text, out C))
                    {
                        TextBlock_CoefA.Background = null;
                        TextBlock_CoefB.Background = null;
                        TextBlock_CoefC.Background = null;
                        NTCDataValidate = true;
                    }
                    else
                    {
                        if (!double.TryParse(TextBlock_CoefA.Text, out A)) TextBlock_CoefA.Background = new SolidColorBrush(Colors.Red);
                        if (!double.TryParse(TextBlock_CoefB.Text, out B)) TextBlock_CoefB.Background = new SolidColorBrush(Colors.Red);
                        if (!double.TryParse(TextBlock_CoefC.Text, out C)) TextBlock_CoefC.Background = new SolidColorBrush(Colors.Red);
                        NTCDataValidate = false;
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.B25:
                    double CoefR25, CoefB25;
                    if (double.TryParse(TextBlock_CoefR25.Text, out CoefR25) && double.TryParse(TextBlock_CoefB25.Text, out CoefB25))
                    {
                        TextBlock_CoefR25.Background = null;
                        TextBlock_CoefB25.Background = null;
                        NTCDataValidate = true;
                    }
                    else
                    {
                        if (!double.TryParse(TextBlock_CoefR25.Text, out CoefR25)) TextBlock_CoefR25.Background = new SolidColorBrush(Colors.Red);
                        if (!double.TryParse(TextBlock_CoefB25.Text, out CoefB25)) TextBlock_CoefB25.Background = new SolidColorBrush(Colors.Red);
                        NTCDataValidate = false;
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.RT8016:
                    double R25;
                    if (double.TryParse(TextBlock_R25.Text, out R25))
                    {
                        TextBlock_R25.Background = null;
                        NTCDataValidate = true;
                    }
                    else
                    {
                        TextBlock_R25.Background = new SolidColorBrush(Colors.Red);
                        NTCDataValidate = false;
                    }
                    break;
                case (int)ConventerNTC.ConversionModes.B57861S0103F040:
                    NTCDataValidate = true;
                    break;
            }
            if (NTCDataValidate)
            {
                Button_Apply.IsEnabled = true;
                Button_Ok.IsEnabled = true;
            }
            else
            {
                Button_Apply.IsEnabled = false;
                Button_Ok.IsEnabled = false;
            }
        }
    }
}
