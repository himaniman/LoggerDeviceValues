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

using System.IO.Ports;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Geared;
using LiveCharts.Defaults;
using System.Collections.Concurrent;
using LiveCharts.Configurations;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Threading;

using HidSharp.Utility;
using HidSharp;
using System.Diagnostics;
using System.Threading;

namespace LoggerDeviceValues
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// среднее между измерениями времени, сколько бегает время
    /// проблема с перезапуском таймера..
    /// добавить ошибку если формат данных не тот в трей
    /// </summary>
    public partial class MainWindow : Window
    {
        public ConcurrentQueue<string> System_serialDataQueue = new ConcurrentQueue<string>();
        public ConcurrentQueue<byte[]> System_HIDDataQueue = new ConcurrentQueue<byte[]>();

        public SerialPort MainParam_SerialPort;
        public HidDevice MainParam_HIDDevice;
        public HidStream MainParam_HIDStream;

        public GearedValues<MeasureModel> MainChartValues { get; set; }
        public GearedValues<MeasureModel> AllChartValues { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> AllYFormatter { get; set; }
        public int MainParam_CounterMeasure;
        public int MainParam_CounterMeasure_Start;
        public int MainParam_CounterMeasure_End;
        StreamWriter MainParam_StreamWriter;
        DispatcherTimer MainParam_Timer;
        DateTime MainParam_TimeEndMeasure;
        DateTime MainParam_TimeStartMeasure;

        public string MainParam_InputBuffer;
        public byte[] MainParam_HIDBuffer;

        //public double MainParam_SummTimeBetweenMeasure;
        public DateTime MainParam_PastMeasure;

        //public LabDevice.DeviceTypes MainParam_DeviceType;
        public LabDevice.SupportedDevices MainParam_DeviceName;
        public LabDevice.DataTypes MainParam_DataType;
        //public List<KeyValuePair<List<double>, LabDevice.DataTypes>> MainParam_ValueBase;
        public List<decimal> MainParam_Values;

        public List<int> MainParam_MillsBetweenMeasure;

        public bool AcceptProccessData;

        //var MainParam_CurrentDevice = new LabDevice();

        public void System_ConnectToDeviceFromAppSettings()
        {
            try
            {
                if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
                {
                    return;
                }
                if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.HP53132A.ToString())
                {
                    if (Properties.Settings.Default.LastConfig == "null" || Properties.Settings.Default.LastConfig == "" || Properties.Settings.Default.LastConfig.Count(x => x==' ') !=2)
                    {
                        return;
                    }
                    else {
                        String COMName = Properties.Settings.Default.LastConfig.Split(' ')[0];
                        int baudrate = int.Parse(Properties.Settings.Default.LastConfig.Split(' ')[1]);
                        MainParam_SerialPort = new SerialPort(COMName, baudrate, Parity.None, 8, StopBits.One);
                        MainParam_SerialPort.DataReceived += new SerialDataReceivedEventHandler(System_SerialDataReceived);
                        if (!MainParam_SerialPort.IsOpen) MainParam_SerialPort.Open();
                        if (MainParam_SerialPort.IsOpen)
                        {
                            TextBlock_Status.Text = "COM port открыт (" + COMName + ")";
                            ComboBox_Interfaces.Items.Clear();
                            ComboBox_Interfaces.Items.Add(COMName);
                            ComboBox_Interfaces.SelectedIndex = 0;
                            //ComboBox_Interfaces.Background = new SolidColorBrush(Colors.LightGreen);
                            MainParam_DeviceName = LabDevice.SupportedDevices.HP53132A;
                        }
                    }
                }
                if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.UT71D.ToString())
                {
                    DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352);
                    if (MainParam_HIDDevice == null) { TextBlock_Status.Text = "Ошибка доступа к HID устройству"; return; }
                    if (!MainParam_HIDDevice.TryOpen(out MainParam_HIDStream)) { TextBlock_Status.Text = "Ошибка получения потока чтения для HID устройства"; return; }
                    //byte[] InitialConfigStructure = new byte[] { 0x00, 0x09, 0x60, 0x00, 0x00, 0x03 };
                    byte[] InitialConfigStructure = new byte[] { 0x00, 0x4B, 0x00, 0x00, 0x00, 0x03 };
                    MainParam_HIDStream.SetFeature(InitialConfigStructure);
                    MainParam_HIDBuffer = new byte[11];
                    //MainParam_HIDStream.ReadAsync()
                    //MainParam_HIDStream.BeginRead(MainParam_HIDBuffer, 0, 11, new AsyncCallback(System_HIDStreamDataReceived), null);
                    Thread thread1 = new Thread(System_HIDStreamDataReceived);
                    thread1.Start();
                    TextBlock_Status.Text = "Успешное получения потока чтения для HID устройства";

                    if (ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName()) == -1) ComboBox_Interfaces.Items.Add(MainParam_HIDDevice.GetProductName());
                    ComboBox_Interfaces.SelectedIndex = ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName());
                    ComboBox_Devices.SelectedIndex = ComboBox_Devices.Items.IndexOf(LabDevice.SupportedDevices.UT71D.ToString());

                    //ComboBox_Devices.Text = "asdsad";
                    //((ComboBoxItem)ComboBox_Devices.Items[ComboBox_Devices.SelectedIndex]).Content = new ComboBoxItem().Content="23123";
                    ComboBox_Devices.Background = new SolidColorBrush(Colors.LightGreen);
                    MainParam_DeviceName = LabDevice.SupportedDevices.UT71D;
                }
            }
            catch (Exception ex)
            {
                Properties.Settings.Default.LastConfig = "null";
                Properties.Settings.Default.LastConnectedDevice = "null";
                Properties.Settings.Default.Save();
                TextBlock_Status.Text = "Ошибка подключения к COM порту "+ex.ToString().Take(50);
                return;
            }
            try
            {
                //TextBlock_Status.Text = "Try connect to port " + Properties.Settings.Default.COMName;
                //if (MainParam_SerialPort.IsOpen) MainParam_SerialPort.Close();

                //MainParam_SerialPort.PortName = Properties.Settings.Default.COMName;
                //MainParam_SerialPort.Handshake = Handshake.None;
                //MainParam_SerialPort.Open();

                //if (MainParam_SerialPort.IsOpen)
                //{
                //    TextBlock_Status.Text = "COM port is open (" + Properties.Settings.Default.COMName + ")";
                //    Label_StatusCOM.Content = "Подключено " + Properties.Settings.Default.COMName + " " + Properties.Settings.Default.COMBaudrate.ToString() + "bps";
                //    Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGreen);
                //    ComboBox_COMPorts.SelectedValue = Properties.Settings.Default.COMName;
                //}
            }
            catch
            {
                //TextBlock_Status.Text = "COM port connect error (" + Properties.Settings.Default.COMName + ")";
                //Label_StatusCOM.Content = "Не подключено";
                //Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGray);
            }
        }

        void System_SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;

            try
            {
                string Buffer = sp.ReadExisting();
                System_serialDataQueue.Enqueue(Buffer);
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show(ex.ToString());
            }
            this.Dispatcher.Invoke(() => System_SerialDataProcessing());
        }

        void System_HIDStreamDataReceived()
        {
            try
            {
                while (true)
                {
                    MainParam_HIDBuffer = MainParam_HIDStream.Read();
                    System_HIDDataQueue.Enqueue(MainParam_HIDBuffer);
                    this.Dispatcher.Invoke(() => System_HIDStreamDataProcessing());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return;
            }
        }

        public void System_HIDStreamDataProcessing()
        {
            if (MainParam_DeviceName == LabDevice.SupportedDevices.UT71D)
            {
                byte[] LocalBuffer;
                while (System_HIDDataQueue.TryDequeue(out LocalBuffer))
                {
                    decimal value;
                    string valueRAW;
                    LabDevice.DataTypes type;
                    if (Driver_UT71D.TryParceData(LocalBuffer, out value, out type, out valueRAW))
                    {
                        //Debug.WriteLine(valueRAW, type.ToString());
                        System_NewValue(value, type, valueRAW);
                    }
                }
            }
        }

        public void System_SerialDataProcessing()
        {
            if (MainParam_DeviceName == LabDevice.SupportedDevices.HP53132A)
            {
                string LocalBuffer;
                while (System_serialDataQueue.TryDequeue(out LocalBuffer))
                {
                    MainParam_InputBuffer += LocalBuffer;
                    if (MainParam_InputBuffer.Contains("\r\n"))
                    {
                        decimal value;
                        Driver_HP53132A.TryParceData(new string(MainParam_InputBuffer.Take(MainParam_InputBuffer.IndexOf('\n')).ToArray()), out value);
                        System_NewValue(value, LabDevice.DataTypes.Voltage, new string(MainParam_InputBuffer.Replace("\r", "").Take(MainParam_InputBuffer.IndexOf('\n')).ToArray()));
                        MainParam_InputBuffer = new string(MainParam_InputBuffer.Skip(MainParam_InputBuffer.IndexOf('\n')+1).ToArray());                        
                    }
                }
            }
        }

        public void System_NewValue(decimal value, LabDevice.DataTypes type, string valueRAW = "")
        {
            if (!AcceptProccessData) return;
            if (MainParam_DataType == LabDevice.DataTypes.Abstract)
            {
                MainParam_DataType = type;
                RadioButton_FixedPoint_Click(null, null);
            }
            if (MainParam_DataType != type)
            {
                AcceptProccessData = false;
                if (MessageBox.Show("Обнаружены данные другого типа, удалить предыдущие измерения?", "Изменился тип данных", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    AcceptProccessData = true;
                    return;
                }
                else
                {
                    AcceptProccessData = true;
                    MainParam_DataType = type;
                    System_ClearAllData();
                    return;
                }
            }

            MainChartValues.Add(new MeasureModel
            {
                Label = MainParam_CounterMeasure++,
                Value = (double)value
            });

            MainParam_Values.Add(value);

            if ((bool)RadioButton_FixedPoint.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertFixedPoint(value, type);
            if ((bool)RadioButton_Scientific.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertScientific(value, type);
            TextBlock_CounterMeasure.Text = MainParam_CounterMeasure.ToString();

            while (MainChartValues.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()) && int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString())!=1)
            {
                MainChartValues.RemoveAt(0);
            }

            //if (MainChartValues.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()))
            //    MainChartValues.RemoveAt(0) = (ChartValues<MeasureModel>)MainChartValues.Skip(MainChartValues.Count - int.Parse(((ComboBoxItem)ComboBox_SizeGraph.SelectedItem).Tag.ToString()));

            MainParam_MillsBetweenMeasure.Add((int)DateTime.Now.Subtract(MainParam_PastMeasure).TotalMilliseconds);
            MainParam_PastMeasure = DateTime.Now;
            if (MainParam_MillsBetweenMeasure.Count > 10) MainParam_MillsBetweenMeasure.RemoveAt(0);

            if (MainParam_CounterMeasure>0 && MainParam_MillsBetweenMeasure.Sum()>0) TextBlock_FreqMeasure.Text = (60 / (MainParam_MillsBetweenMeasure.Average() / 1000)).ToString("F1") + " выб/мин";

            if (Button_FileBurnStart.Content.ToString() == "ЗАПИСЬ...")
            {
                System_AddValueToFile(value, type, valueRAW);

                if (MainParam_CounterMeasure_End == 0)
                {
                    ProgressBar_Status.Value = ((double)(DateTime.Now.Ticks - MainParam_TimeStartMeasure.Ticks) / (double)(MainParam_TimeEndMeasure.Ticks - MainParam_TimeStartMeasure.Ticks)) * 100;
                    TextBlock_ETA.Text = "До окончания таймера: " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
                }
                if (MainParam_CounterMeasure_End > 0)
                {
                    ProgressBar_Status.Value = ((double)(MainParam_CounterMeasure - MainParam_CounterMeasure_Start) / (double)(MainParam_CounterMeasure_End - MainParam_CounterMeasure_Start)) * 100;
                    TextBlock_ETA.Text = "Измерений осталось: " + (MainParam_CounterMeasure_End - MainParam_CounterMeasure).ToString() +
                        //" (ETA: "+ DateTime.Now.AddMilliseconds((MainParam_CounterMeasure_End - (MainParam_CounterMeasure - MainParam_CounterMeasure_Start)) *(MainParam_SummTimeBetweenMeasure / (MainParam_CounterMeasure - MainParam_CounterMeasure_Start))).ToString("dd'.'hh':'mm':'ss");
                        " (ETA: " + TimeSpan.FromMilliseconds(
                            (MainParam_CounterMeasure_End - MainParam_CounterMeasure) *
                            MainParam_MillsBetweenMeasure.Average()
                            ).ToString(@"d\.hh\:mm\:ss")+")";
                    if (MainParam_CounterMeasure_End == MainParam_CounterMeasure)
                    {
                        Button_FileBurnStart_Click(null, null);
                    }
                }
            }
        }

        public void System_AddValueToFile(decimal value, LabDevice.DataTypes type, string valueRAW)
        {
            string StringForBurnToFile = "";
            //if (CheckBox_ValueOnly.IsChecked.Value) StringForBurnToFile = LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            //else
            //{
            //    StringForBurnToFile += LabDevice.ConvertFixedPoint(value, type) + "\t" + LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            //    StringForBurnToFile += "\t" + string.Format("{0:u}", DateTime.Now).Replace("Z", "") + ":" + string.Format("{0:d}", DateTime.Now.Millisecond);
            //    if (valueRAW != "") StringForBurnToFile += "\t" + valueRAW;
            //}
            MainParam_StreamWriter.WriteLine(StringForBurnToFile);
            try
            {
                if (MainParam_CounterMeasure % Slider_FragmentSize.Value == 0)
                {
                    MainParam_StreamWriter.Flush();
                    if (MainParam_CounterMeasure_End == 0)
                    {
                        TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
                        //TextBlock_StatusBurn.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "\nОкончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
                    }
                    else
                    {
                        TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание в ~" + DateTime.Now.AddMilliseconds((MainParam_CounterMeasure_End - MainParam_CounterMeasure) * MainParam_MillsBetweenMeasure.Average()).ToString("dd/MM/yyyy HH:mm:ss");
                    }
                }
            }
            catch (Exception ex)
            {
                TextBlock_Status.Text = "Ошибка записи "+ ex.ToString();
            }
            
        }

        public void System_RefreshAllChart()
        {
            List<MeasureModel> ListForAdding = new List<MeasureModel>();
            for (int i=0;i< MainParam_Values.Count; i+=2)
            {
                MeasureModel a = new MeasureModel {
                    Label = i,
                    Value = (double)MainParam_Values[i]
                };
                ListForAdding.Add(a);
            }
            AllChartValues.Clear();
            AllChartValues.AddRange(ListForAdding);
        }

        public void System_ClearAllData()
        {
            MainChartValues.Clear();
            MainChart.AxisX[0].Sections.Clear();
            MainChart.Update();
            MainParam_Values.Clear();
            if (Button_FileBurnStart.Content.ToString() == "ЗАПИСЬ...") Button_FileBurnStart_Click(null, null);
            MainParam_PastMeasure = DateTime.Now;
            MainParam_CounterMeasure = 0;
            MainParam_CounterMeasure_End = 0;
            MainParam_CounterMeasure_Start = 0;
            MainParam_MillsBetweenMeasure.Clear();
        }

        public MainWindow()
        {
            InitializeComponent();
            //Properties.Settings.Default.COMBaudrate = 19200;
            //Properties.Settings.Default.Save();
            //MainParam_SerialPort = new SerialPort(Properties.Settings.Default.COMName, Properties.Settings.Default.COMBaudrate, Parity.None, 8, StopBits.One);
            //MainParam_SerialPort.DataReceived += new SerialDataReceivedEventHandler(System_SerialDataReceived);
            var mapper = Mappers.Xy<MeasureModel>()
               .X(model => model.Label)
               .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            DataContext = this;
            MainChartValues = new GearedValues<MeasureModel>();
            MainChartValues.WithQuality(Quality.Low);
            AllChartValues = new GearedValues<MeasureModel>();
            AllChartValues.WithQuality(Quality.Low);

            if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
                TextBox_FilePos.Text = "Logger_Measure " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            else TextBox_FilePos.Text = "Logger_Measure_"+ Properties.Settings.Default.LastConnectedDevice+ " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";

            YFormatter = value => value.ToString("e");

            ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.HP53132A.ToString());
            ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.UT71D.ToString());

            //MainParam_DeviceType = LabDevice.DeviceTypes.Serial;
            //MainParam_DeviceName = LabDevice.SupportedDevices.HP53132A;
            //MainParam_DataType = LabDevice.DataTypes.Freq;

            //MainParam_ValueBase.Add(new KeyValuePair<List<double>, LabDevice.DataTypes>(new List<double>(), MainParam_DataType));
            MainParam_CounterMeasure = 0;
            //MainParam_SummTimeBetweenMeasure = 0;
            MainParam_PastMeasure = DateTime.Now;
            MainParam_Values = new List<decimal>();
            MainParam_CounterMeasure_Start = 0;
            MainParam_DataType = LabDevice.DataTypes.Abstract;
            AcceptProccessData = true;
            MainParam_MillsBetweenMeasure = new List<int>();

            System_ConnectToDeviceFromAppSettings();
        }

        private void ComboBox_Interfaces_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox_Devices.Items.Clear();
            try
            {
                string[] AviliblePorts;
                AviliblePorts = System.IO.Ports.SerialPort.GetPortNames();

                ComboBox_Interfaces.Items.Clear();
                foreach (string currentPort in AviliblePorts) ComboBox_Interfaces.Items.Add(currentPort);
                //var deviceList = DeviceList.Local.GetDevices(DeviceTypes.Hid).ToArray();
                DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352); //UT71D
                if (MainParam_HIDDevice != null) ComboBox_Interfaces.Items.Add(MainParam_HIDDevice.GetProductName());
                TextBlock_Status.Text = "Сканирование интерфейсов успешно завершено";
            }
            catch
            {
                TextBlock_Status.Text = "Ошибка сканирования интерфейсов";
            }
        }

        private void ComboBox_Interfaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Interfaces.SelectedIndex == -1) return;
            if (ComboBox_Interfaces.SelectedItem.ToString().Contains("COM"))
            {
                //ComboBox_Devices.Items.Clear();

                ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.HP53132A.ToString());
                //ComboBox_Devices.SelectedIndex = 0;
            }
            if (ComboBox_Interfaces.SelectedItem.ToString().Contains("USB to Serial"))
            {
                //ComboBox_Devices.Items.Clear();
                //ComboBoxItem CBI = new ComboBoxItem(); 
                ComboBox_Devices.Items.Add(new ComboBoxItem().Content=LabDevice.SupportedDevices.UT71D.ToString());
                //ComboBox_Devices.SelectedIndex = 0;
            }
        }

        private void ComboBox_Devices_DropDownOpened(object sender, EventArgs e)
        {
            
        }

        private void ComboBox_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Devices.SelectedIndex == -1) return;
            if (ComboBox_Devices.SelectedItem.ToString().Contains(LabDevice.SupportedDevices.UT71D.ToString()))
            {
                Properties.Settings.Default.LastConnectedDevice = LabDevice.SupportedDevices.UT71D.ToString();
                Properties.Settings.Default.Save();
                System_ConnectToDeviceFromAppSettings();
                TextBox_FilePos.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            }
            if (ComboBox_Devices.SelectedItem.ToString().Contains(LabDevice.SupportedDevices.HP53132A.ToString()))
            {
                Properties.Settings.Default.LastConnectedDevice = LabDevice.SupportedDevices.HP53132A.ToString();
                Properties.Settings.Default.LastConfig = ComboBox_Interfaces.SelectedItem.ToString() + " 19200 ";
                Properties.Settings.Default.Save();
                System_ConnectToDeviceFromAppSettings();
                TextBox_FilePos.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            }
        }

        private void Button_GenerateNewNameFile_Click(object sender, RoutedEventArgs e)
        {
            TextBox_FilePos.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
        }

        private void Button_SetPathFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog.FileName = TextBox_FilePos.Text;
            if (saveFileDialog.ShowDialog() == true)
                TextBox_FilePos.Text = saveFileDialog.FileName;
        }

        private void RadioButton_FixedPoint_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)RadioButton_Scientific.IsChecked) YFormatter = value => LabDevice.ConvertScientific((decimal)value, MainParam_DataType);
            if ((bool)RadioButton_FixedPoint.IsChecked) YFormatter = value => LabDevice.ConvertFixedPoint((decimal)value, MainParam_DataType);
            if (MainChart.AxisY.Count>0) MainChart.AxisY[0].LabelFormatter = YFormatter;
        }

        private void TextBox_FragmentSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RadioButton_QtyMeas_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)RadioButton_QtyMeas.IsChecked)
            {
                TextBox_QtyMeas.IsEnabled = true;
                TimeSpanUpDown_Timer.IsEnabled = false;
            }
            if ((bool)RadioButton_Timer.IsChecked)
            {
                TextBox_QtyMeas.IsEnabled = false;
                TimeSpanUpDown_Timer.IsEnabled = true;
            }
        }

        private void RadioButton_Timer_Checked(object sender, RoutedEventArgs e)
        {
            if (TextBox_QtyMeas != null)
            {
                if ((bool)RadioButton_QtyMeas.IsChecked)
                {
                    TextBox_QtyMeas.IsEnabled = true;
                    TimeSpanUpDown_Timer.IsEnabled = false;
                }
                if ((bool)RadioButton_Timer.IsChecked)
                {
                    TextBox_QtyMeas.IsEnabled = false;
                    TimeSpanUpDown_Timer.IsEnabled = true;
                }
            }
        }

        private void TextBox_QtyMeas_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void MainParam_Timer_Tick(object s, EventArgs e)
        {
            Button_FileBurnStart_Click(null, null);
        }

        private void Button_FileBurnStart_Click(object sender, RoutedEventArgs e)
        {
            if (Button_FileBurnStart.Content.ToString() == "ЗАПИСЬ...")
            {
                Button_FileBurnStart.Content = "Начать запись";
                Button_FileBurnStart.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));

                RadioButton_FixedPoint.IsEnabled = true;
                RadioButton_Scientific.IsEnabled = true;
                RadioButton_QtyMeas.IsEnabled = true;
                RadioButton_Timer.IsEnabled = true;
                TextBox_FilePos.IsEnabled = true;
                Button_GenerateNewNameFile.IsEnabled = true;
                Button_SetPathFile.IsEnabled = true;
                //CheckBox_ValueOnly.IsEnabled = true;
                //TextBox_FragmentSize.IsEnabled = true;

                MainParam_CounterMeasure_End = MainParam_CounterMeasure;
                MainChart.AxisX[0].Sections.Add(new AxisSection
                {
                    Value = MainParam_CounterMeasure_End,
                    StrokeThickness = 3,
                    Stroke = new SolidColorBrush(Color.FromRgb(220, 30, 30)),
                    DataLabel = true,
                });

                TextBlock_Status.Text = "Запись окончена в " + DateTime.Now.ToString("HH:mm") +
                    "    Записано " + (MainParam_CounterMeasure_End - MainParam_CounterMeasure_Start).ToString() + " Значений";

                MainParam_StreamWriter.WriteLine("Окончание записи " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
                    ". Записано " + (MainParam_CounterMeasure_End - MainParam_CounterMeasure_Start).ToString() + " значений.");
                MainParam_StreamWriter.WriteLine("");
                MainParam_StreamWriter.Flush();

                ProgressBar_Status.Value = 0;
                TextBlock_ETA.Text = "";
                return;
            }
            if (Button_FileBurnStart.Content.ToString() == "Начать запись")
            {
                try
                {
                    string filepath;
                    if (TextBox_FilePos.Text.Contains("\\")) filepath = TextBox_FilePos.Text;
                    else filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + TextBox_FilePos.Text;
                    if (MainParam_StreamWriter != null) MainParam_StreamWriter.Close();
                    if (File.Exists(filepath))
                    {
                        if (MessageBox.Show("Файл " + filepath + " уже существует, дописать?", "Запись в файл", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            MainParam_StreamWriter = File.AppendText(filepath);
                        }
                        else return;
                    }
                    else
                    {
                        MainParam_StreamWriter = File.CreateText(filepath);
                    }
                    MainParam_StreamWriter.WriteLine("Старт записи измерений " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    MainParam_StreamWriter.WriteLine("Название устройства " + MainParam_DeviceName.ToString());
                    MainParam_StreamWriter.WriteLine("Тип данных " + MainParam_DataType.ToString());
                    MainParam_StreamWriter.Flush();
                    MainParam_StreamWriter.AutoFlush = false;

                    Button_FileBurnStart.Content = "ЗАПИСЬ...";
                    Button_FileBurnStart.Background = new SolidColorBrush(Colors.OrangeRed);
                    RadioButton_FixedPoint.IsEnabled = false;
                    RadioButton_Scientific.IsEnabled = false;
                    RadioButton_QtyMeas.IsEnabled = false;
                    RadioButton_Timer.IsEnabled = false;
                    TextBox_FilePos.IsEnabled = false;
                    Button_GenerateNewNameFile.IsEnabled = false;
                    Button_SetPathFile.IsEnabled = false;
                    //CheckBox_ValueOnly.IsEnabled = false;
                    //TextBox_FragmentSize.IsEnabled = false;

                    MainParam_CounterMeasure_Start = MainParam_CounterMeasure;
                    MainParam_TimeStartMeasure = DateTime.Now;
                    MainChart.AxisX[0].Sections.Add(new AxisSection
                    {
                        Value = MainParam_CounterMeasure_Start,
                        StrokeThickness = 3,
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 213, 72)),
                        DataLabel = true,
                    });

                    TextBlock_Status.Text = "Запись начата в " + DateTime.Now.ToString("HH:mm");

                    if ((bool)RadioButton_Timer.IsChecked)
                    {
                        MainParam_CounterMeasure_End = 0;
                        if (MainParam_Timer != null) MainParam_Timer.Stop();
                        MainParam_Timer = new DispatcherTimer();

                        MainParam_Timer.Interval = TimeSpanUpDown_Timer.Value.Value;
                        MainParam_Timer.Start();
                        MainParam_Timer.Tick += MainParam_Timer_Tick;
                        MainParam_TimeEndMeasure = DateTime.Now.AddSeconds(TimeSpanUpDown_Timer.Value.Value.TotalSeconds);
                    }
                    if ((bool)RadioButton_QtyMeas.IsChecked)
                    {
                        MainParam_CounterMeasure_End = MainParam_CounterMeasure + int.Parse(TextBox_QtyMeas.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            //MainParam_SummTimeBetweenMeasure = 0;
            MainParam_PastMeasure = DateTime.Now;
        }

        private void MainChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainChart.AxisX[0].MinValue = double.NaN;
            MainChart.AxisX[0].MaxValue = double.NaN;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System_RefreshAllChart();
        }

        private void CheckBox_BurnStringChange(object sender, RoutedEventArgs e)
        {
            string StringForBurnToFile = "";
            Random rnd = new Random();
            decimal value = (decimal)rnd.NextDouble()*3000;
            LabDevice.DataTypes type = (LabDevice.DataTypes)rnd.Next(Enum.GetValues(typeof(LabDevice.DataTypes)).Length);
            StringForBurnToFile = LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            if (CheckBox_BurnFixedPoint.IsChecked.Value) StringForBurnToFile += " " + LabDevice.ConvertFixedPoint(value, type);
            if (CheckBox_BurnCounter.IsChecked.Value) StringForBurnToFile += " " + rnd.Next(10000);//(MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString();
            if (CheckBox_BurnDate.IsChecked.Value) StringForBurnToFile += " " + DateTime.Now.ToString("dd.MM.yyyy");
            if (CheckBox_BurnTime.IsChecked.Value) StringForBurnToFile += " " + DateTime.Now.ToString("HH:mm:ss:") + string.Format("{0:d}", DateTime.Now.Millisecond);
            if (CheckBox_BurnRAW.IsChecked.Value) StringForBurnToFile += " " + "[RAW]";
            TextBlock_BurnString.Text = StringForBurnToFile;
        }

        //private void CheckBox_AnimatedGraph_Click(object sender, RoutedEventArgs e)
        //{
        //    if (CheckBox_AnimatedGraph.IsChecked.Value) MainChart.DisableAnimations = false;
        //    else MainChart.DisableAnimations = true;
        //}
    }
}
