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
        Driver_VirtualDevice TargetDriver;
        bool NTCDataValidate = true;

        public WindowDriver_Virtual(Driver_VirtualDevice _driver)
        {
            TargetDriver = _driver;
            InitializeComponent();
            foreach (LabDevice.DataTypes type in Enum.GetValues(typeof(LabDevice.DataTypes))) ComboBox_DataType.Items.Add(type.ToString());

            ComboBox_Waveform.SelectedIndex = (int)TargetDriver.TypeSignal;

            IntegerUpDown_MeasPerMin.Value = TargetDriver.MeasPerMin;
            IntegerUpDown_CoefMinY.Value = TargetDriver.CoefMinY;
            IntegerUpDown_CoefMaxY.Value = TargetDriver.CoefMaxY;
            IntegerUpDown_CoefPulseWith.Value = TargetDriver.CoefPulseWith;
            IntegerUpDown_CoefSinOfsPulse.Value = TargetDriver.CoefSinOfsPulse;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ComboBox_Waveform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            TargetDriver.type = (LabDevice.DataTypes)ComboBox_DataType.SelectedIndex;
            TargetDriver.TypeSignal = (Driver_VirtualDevice.TypesSignal)ComboBox_Waveform.SelectedIndex;
            TargetDriver.MeasPerMin = (double)IntegerUpDown_MeasPerMin.Value;
            TargetDriver.CoefMinY = (double)IntegerUpDown_CoefMinY.Value;
            TargetDriver.CoefMaxY = (double)IntegerUpDown_CoefMaxY.Value;
            TargetDriver.CoefPulseWith = (double)IntegerUpDown_CoefPulseWith.Value;
            TargetDriver.CoefSinOfsPulse = (double)IntegerUpDown_CoefSinOfsPulse.Value;
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

    }
}
