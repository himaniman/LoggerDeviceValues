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
//using LiveCharts;
//using LiveCharts.Wpf;
//using LiveCharts.Geared;
//using LiveCharts.Defaults;
using System.Collections.Concurrent;
//using LiveCharts.Configurations;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Threading;

using HidSharp.Utility;
using HidSharp;
using System.Diagnostics;
using System.Threading;

using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

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

        public static PlotModel MainChartModel { get; set; }
        //public GearedValues<MeasureModel> MainChartValues { get; set; }
        //public GearedValues<MeasureModel> AllChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> AllYFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
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

        public Thread ThreadAwaitData_Discriptor;
        DeviceManager DeviceManager_Obj;

        public List<RadioButton> VisibleDevices { get; set; }

        int Global_SelectedDevice;

        //var MainParam_CurrentDevice = new LabDevice();

        public void System_ConnectToDeviceFromAppSettings()
        {
            //try
            //{
            //    if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
            //    {
            //        return;
            //    }
            //    if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.HP53132A.ToString())
            //    {
            //        if (Properties.Settings.Default.LastConfig == "null" || Properties.Settings.Default.LastConfig == "" || Properties.Settings.Default.LastConfig.Count(x => x==' ') !=2)
            //        {
            //            return;
            //        }
            //        else {
            //            String COMName = Properties.Settings.Default.LastConfig.Split(' ')[0];
            //            int baudrate = int.Parse(Properties.Settings.Default.LastConfig.Split(' ')[1]);
            //            MainParam_SerialPort = new SerialPort(COMName, baudrate, Parity.None, 8, StopBits.One);
            //            MainParam_SerialPort.DataReceived += new SerialDataReceivedEventHandler(System_SerialDataReceived);
            //            if (!MainParam_SerialPort.IsOpen) MainParam_SerialPort.Open();
            //            if (MainParam_SerialPort.IsOpen)
            //            {
            //                TextBlock_Status.Text = "COM port открыт (" + COMName + ")";
            //                ComboBox_Interfaces.Items.Clear();
            //                ComboBox_Interfaces.Items.Add(COMName);
            //                ComboBox_Interfaces.SelectedIndex = 0;
            //                //ComboBox_Interfaces.Background = new SolidColorBrush(Colors.LightGreen);
            //                MainParam_DeviceName = LabDevice.SupportedDevices.HP53132A;
            //            }
            //        }
            //    }
            //    if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.UT71D.ToString())
            //    {
            //        DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352);
            //        if (MainParam_HIDDevice == null) { TextBlock_Status.Text = "Ошибка доступа к HID устройству"; return; }
            //        if (!MainParam_HIDDevice.TryOpen(out MainParam_HIDStream)) { TextBlock_Status.Text = "Ошибка получения потока чтения для HID устройства"; return; }
            //        //byte[] InitialConfigStructure = new byte[] { 0x00, 0x09, 0x60, 0x00, 0x00, 0x03 };
            //        byte[] InitialConfigStructure = new byte[] { 0x00, 0x4B, 0x00, 0x00, 0x00, 0x03 };
            //        MainParam_HIDStream.SetFeature(InitialConfigStructure);
            //        MainParam_HIDBuffer = new byte[11];

            //        string z = MainParam_HIDDevice.DevicePath;
            //        //MainParam_HIDStream.ReadAsync()
            //        //MainParam_HIDStream.BeginRead(MainParam_HIDBuffer, 0, 11, new AsyncCallback(System_HIDStreamDataReceived), null);
            //        Thread thread1 = new Thread(System_HIDStreamDataReceived);
            //        thread1.Start();
            //        TextBlock_Status.Text = "Успешное получения потока чтения для HID устройства";

            //        if (ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName()) == -1) ComboBox_Interfaces.Items.Add(MainParam_HIDDevice.GetProductName());
            //        ComboBox_Interfaces.SelectedIndex = ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName());
            //        ComboBox_Devices.SelectedIndex = ComboBox_Devices.Items.IndexOf(LabDevice.SupportedDevices.UT71D.ToString());

            //        //ComboBox_Devices.Text = "asdsad";
            //        //((ComboBoxItem)ComboBox_Devices.Items[ComboBox_Devices.SelectedIndex]).Content = new ComboBoxItem().Content="23123";
            //        ComboBox_Devices.Background = new SolidColorBrush(Colors.LightGreen);
            //        MainParam_DeviceName = LabDevice.SupportedDevices.UT71D;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Properties.Settings.Default.LastConfig = "null";
            //    Properties.Settings.Default.LastConnectedDevice = "null";
            //    Properties.Settings.Default.Save();
            //    TextBlock_Status.Text = "Ошибка подключения к COM порту "+ex.ToString().Take(50);
            //    return;
            //}
            ////try
            ////{
            ////    //TextBlock_Status.Text = "Try connect to port " + Properties.Settings.Default.COMName;
            ////    //if (MainParam_SerialPort.IsOpen) MainParam_SerialPort.Close();

            ////    //MainParam_SerialPort.PortName = Properties.Settings.Default.COMName;
            ////    //MainParam_SerialPort.Handshake = Handshake.None;
            ////    //MainParam_SerialPort.Open();

            ////    //if (MainParam_SerialPort.IsOpen)
            ////    //{
            ////    //    TextBlock_Status.Text = "COM port is open (" + Properties.Settings.Default.COMName + ")";
            ////    //    Label_StatusCOM.Content = "Подключено " + Properties.Settings.Default.COMName + " " + Properties.Settings.Default.COMBaudrate.ToString() + "bps";
            ////    //    Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGreen);
            ////    //    ComboBox_COMPorts.SelectedValue = Properties.Settings.Default.COMName;
            ////    //}
            ////}
            ////catch
            ////{
            ////    //TextBlock_Status.Text = "COM port connect error (" + Properties.Settings.Default.COMName + ")";
            ////    //Label_StatusCOM.Content = "Не подключено";
            ////    //Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGray);
            ////}
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
                    //if (Driver_UT71D.TryParceData(LocalBuffer, out value, out type, out valueRAW))
                    //{
                    //    //Debug.WriteLine(valueRAW, type.ToString());
                    //    System_NewValue(value, type, valueRAW);
                    //}
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
                        MainParam_InputBuffer = new string(MainParam_InputBuffer.Skip(MainParam_InputBuffer.IndexOf('\n') + 1).ToArray());
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

            //System_AddValueToGraph(value, type);

            if ((bool)RadioButton_FixedPoint.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertFixedPoint(value, type);
            if ((bool)RadioButton_Scientific.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertScientific(value, type);
            TextBlock_CounterMeasure.Text = MainParam_CounterMeasure.ToString();

            //if (MainChartValues.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()))
            //    MainChartValues.RemoveAt(0) = (ChartValues<MeasureModel>)MainChartValues.Skip(MainChartValues.Count - int.Parse(((ComboBoxItem)ComboBox_SizeGraph.SelectedItem).Tag.ToString()));

            MainParam_MillsBetweenMeasure.Add((int)DateTime.Now.Subtract(MainParam_PastMeasure).TotalMilliseconds);
            MainParam_PastMeasure = DateTime.Now;
            if (MainParam_MillsBetweenMeasure.Count > 10) MainParam_MillsBetweenMeasure.RemoveAt(0);

            if (MainParam_CounterMeasure > 0 && MainParam_MillsBetweenMeasure.Sum() > 0) TextBlock_FreqMeasure.Text = (60 / (MainParam_MillsBetweenMeasure.Average() / 1000)).ToString("F1") + " выб/мин";

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
                            ).ToString(@"d\.hh\:mm\:ss") + ")";
                    if (MainParam_CounterMeasure_End == MainParam_CounterMeasure)
                    {
                        Button_FileBurnStart_Click(null, null);
                    }
                }
            }
        }

        public void System_AddValueToGraph(decimal value, LabDevice.DataTypes type, DateTime time)
        {
            (MainChartModel.Series[0] as LineSeries).Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), (double)value));
            if ((MainChartModel.Series[0] as LineSeries).Points.Count > 150) (MainChartModel.Series[0] as LineSeries).Points.RemoveAt(0);
            MainChart.InvalidatePlot();
            //if (MainChartValues.Count == 0)
            //{
            //    SolidColorBrush NewColor = new SolidColorBrush(Colors.Blue);
            //    switch (type)
            //    {
            //        case LabDevice.DataTypes.Current:
            //            NewColor = new SolidColorBrush(Colors.Red); break;
            //        case LabDevice.DataTypes.Voltage:
            //            NewColor = new SolidColorBrush(Colors.Blue); break;
            //        case LabDevice.DataTypes.Capacity:
            //            NewColor = new SolidColorBrush(Colors.Orange); break;
            //        case LabDevice.DataTypes.Temperature:
            //            NewColor = new SolidColorBrush(Colors.Gold); break;
            //        case LabDevice.DataTypes.Freq:
            //            NewColor = new SolidColorBrush(Colors.Green); break;
            //    }
            //    MainChart.Series.Clear();
            //    MainChart.Series.Add(new GLineSeries
            //    {
            //        Values = MainChartValues,
            //        Title = type.ToString(),
            //        StrokeThickness = 2,
            //        LineSmoothness = 0,
            //        Stroke = NewColor
            //    });
            //    MainChartValues.Quality = Quality.Highest;
            //}


            //MainChartValues.Add(new MeasureModel
            //{
            //    DateTime = DateTime.Now,
            //    Value = (double)value
            //});

            ////MainChart.AxisX[0].MaxValue = 100;

            //MainParam_Values.Add(value);

            //while (MainChartValues.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()) && int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()) != 1)
            //{
            //    MainChartValues.RemoveAt(0);
            //}

            //AxisStep = TimeSpan.FromSeconds(100).Ticks;
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
                TextBlock_Status.Text = "Ошибка записи " + ex.ToString();
            }

        }

        public void System_LogMessage(String _text)
        {
            ListBox_Log.Items.Add(new ListBoxItem()
            {
                Content = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + _text,
            });
        }

        public void System_RefreshAllChart()
        {
            //List<MeasureModel> ListForAdding = new List<MeasureModel>();
            //for (int i=0;i< MainParam_Values.Count; i+=2)
            //{
            //    MeasureModel a = new MeasureModel {
            //        Label = i,
            //        Value = (double)MainParam_Values[i]
            //    };
            //    ListForAdding.Add(a);
            //}
            //AllChartValues.Clear();
            //AllChartValues.AddRange(ListForAdding);
        }

        public void System_ClearAllData()
        {
            //MainChartValues.Clear();
            //MainChart.AxisX[0].Sections.Clear();
            //MainChart.Update();
            //MainParam_Values.Clear();
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
            DataContext = this;

            DateTimeAxis xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",

                Title = "Время",
                MinorIntervalType = DateTimeIntervalType.Minutes,
                IntervalType = DateTimeIntervalType.Minutes,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.None,
            };

            FunctionSeries fs = new FunctionSeries();

            MainChartModel = new PlotModel();

            MainChartModel.Series.Add(fs);

            MainChartModel.Axes.Add(xAxis);
            MainChartModel.Axes.Add(new LinearAxis());


            //var s1 = new LineSeries
            //{
            //    StrokeThickness = 1,
            //    MarkerSize = 3,
            //    MarkerStroke = OxyColors.ForestGreen,
            //    MarkerType = MarkerType.Plus
            //};


            ////s1.Points.Add(new DataPoint(1, 1));
            ////s1.Points.Add(new DataPoint(2, 2));
            ////s1.Points.Add(new DataPoint(3, 1));


            //MainChartModel.Series.Add(s1);


            //MainChartModel.Axes[0].Title = "asdas";

            //this.MainChartModel = tmp;

            //Properties.Settings.Default.COMBaudrate = 19200;
            //Properties.Settings.Default.Save();
            //MainParam_SerialPort = new SerialPort(Properties.Settings.Default.COMName, Properties.Settings.Default.COMBaudrate, Parity.None, 8, StopBits.One);
            //MainParam_SerialPort.DataReceived += new SerialDataReceivedEventHandler(System_SerialDataReceived);
            ////var mapper = Mappers.Xy<MeasureModel>()
            ////   .X(model => model.DateTime.Ticks)
            ////   .Y(model => model.Value);
            ////Charting.For<MeasureModel>(mapper);


            ////DataContext = this;
            ////MainChartValues = new GearedValues<MeasureModel>();
            ////MainChartValues.WithQuality(Quality.Medium);
            ////AllChartValues = new GearedValues<MeasureModel>();
            ////AllChartValues.WithQuality(Quality.Low);

            ////DateTimeFormatter = value => new DateTime(Math.Abs((long)value)).ToString("mm:ss");
            ////AxisStep = TimeSpan.FromSeconds(10).Ticks;
            ////AxisUnit = TimeSpan.TicksPerSecond;

            if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
                TextBox_FileName.Text = "Logger_Measure " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            else TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";

            //YFormatter = value => value.ToString("e");

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

            System_LogMessage("asdas sdfsdfdsfdsf dsfsdfsd sdfsdf sdfsdf sd sd");
            System_LogMessage("asdas2 sdfsdfdsfdsf dsfsdfsd sdfsdf sdfsdf sd sd");
            System_LogMessage("asdas3 sdfsdfdsfdsf ds1");
            System_LogMessage("asdas3 sdfsdfdsfdsf ds2");
            System_LogMessage("asdas3 sdfsdfdsfdsf ds3");

            //ListView_CurrentDev.Items.Add(new ListBoxItem()
            //{
            //    Content = " #1 UT70D V ",
            //    Background = new SolidColorBrush(Color.FromArgb(0x7f, 0xff, 0x00, 0x00)),
            //    IsSelected = true,
            //    Tag = "2",
            //    //IsChecked = true,
            //});

            //ListView_CurrentDev.Items.Add(new ListBoxItem()
            //{
            //    Content = " #1 UT61C A ",
            //    Background = new SolidColorBrush(Colors.Transparent)
            //});



            //(ListView_CurrentDev.Items[1] as ListBoxItem).IsSelected = true;
            //(ListView_CurrentDev.Items[0] as ListBoxItem).IsSelected = false;

            //(ListView_CurrentDev.Items[0] as ListBoxItem).DataContext = new SolidColorBrush(Colors.Red);

            //VisibleDevices = new List<RadioButton>();

            //VisibleDevices.Add(new RadioButton() { Content = "asd" });



            //System.Drawing.SystemIcons.Information
            DeviceManager_Obj = new DeviceManager(EventNewValue);
            //ThreadAwaitData_Discriptor = new Thread(DeviceManager_Obj.AwaitData);
            //ThreadAwaitData_Discriptor.Start();
            //DeviceManager_Obj.ConnectToDeviceThroughInterface("Virtual-", "");
        }

        public delegate void EventNewValueDelegate(LabDevice.MeasureStruct value, LabDevice device);

        public void EventNewValue(LabDevice.MeasureStruct measure, LabDevice device)
        {
            System_AddValueToGraph(measure.Val, measure.Typ, measure.TS);
            this.Dispatcher.Invoke(() => UpdateLifeValuesForCurrentDevice());
        }

        public void UpdateLifeValuesForCurrentDevice()
        {
            TextBlock_CurrentValue.Text = DeviceManager_Obj.Devices[Global_SelectedDevice].DataType.ToString();
        }

        private void ComboBox_Interfaces_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox_Devices_DropDownOpened(null, null);
            TextBlock_InterfaceMessage.Visibility = Visibility.Visible;
            ComboBox_Interfaces.Items.Clear();
            string[] AvilibleInterfaces = DeviceManager_Obj.ScanAvilibleInterfaces().ToArray();
            foreach (string CurrentInterface in AvilibleInterfaces) ComboBox_Interfaces.Items.Add(CurrentInterface);
            TextBlock_Status.Text = "Сканирование интерфейсов успешно завершено";
            //ComboBox_Devices.Items.Clear();
            //ComboBox_Interfaces.Items.Clear();
            //ComboBox_Interfaces.Items.Add("Доступные интерфейсы:");
            //try
            //{
            //    string[] AvilibleInterfaces = DeviceManager_Obj.ScanAvilibleInterfaces().ToArray();
            //    //string[] AviliblePorts;
            //    //AviliblePorts = System.IO.Ports.SerialPort.GetPortNames();

            //    //ComboBox_Interfaces.Items.Clear();
            //    //foreach (string currentPort in AviliblePorts) ComboBox_Interfaces.Items.Add(currentPort);
            //    //var deviceList = DeviceList.Local.GetDevices(DeviceTypes.Hid).ToArray();
            //    //var deviceList2 = DeviceList.Local.GetHidDevices(vendorID: 6790, productID: 57352);

            //    //DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352); //UT71D
            //    //DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352); //UT71D
            //    //if (MainParam_HIDDevice != null) ComboBox_Interfaces.Items.Add(MainParam_HIDDevice.GetProductName());
            //    foreach (string CurrentInterface in AvilibleInterfaces) ComboBox_Interfaces.Items.Add(CurrentInterface);
            //    TextBlock_Status.Text = "Сканирование интерфейсов успешно завершено";
            //}
            //catch
            //{
            //    TextBlock_Status.Text = "Ошибка сканирования интерфейсов";
            //}
        }

        private void ComboBox_Interfaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock_InterfaceMessage.Visibility = ComboBox_Interfaces.SelectedItem == null ? Visibility.Visible : Visibility.Hidden;
            ////if ((string)((sender as ComboBox).Content) == "1") return;
            //if (ComboBox_Interfaces.SelectedIndex == -1 || ComboBox_Interfaces.Items.Count == 0 || ComboBox_Interfaces.SelectedItem.ToString() == "Интерфейсы")
            //{
            //    ComboBox_Interfaces.Items.Clear();
            //    ComboBox_Interfaces.Items.Add("Интерфейсы");
            //    ComboBox_Interfaces.SelectedIndex = 0;
            //    return;
            //}
            //if (!ComboBox_Interfaces.SelectedItem.ToString().Contains("-"))
            //{
            //    ComboBox_Interfaces.Items.Clear();
            //    ComboBox_Interfaces.Items.Add("Интерфейсы");
            //    ComboBox_Interfaces.SelectedIndex = 0;
            //    return;
            //    //Button_UserConnectToDevice.IsEnabled = false;
            //}
            //else
            //{
            //    ComboBox_Devices.Items.Clear();
            //    ComboBox_Devices.Items.Add("Устройства");
            //    ComboBox_Devices.SelectedIndex = 0;
            //    return;
            //}
            ////if (ComboBox_Interfaces.SelectedItem.ToString().Contains("COM"))
            ////{
            ////    //ComboBox_Devices.Items.Clear();

            ////    ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.HP53132A.ToString());
            ////    //ComboBox_Devices.SelectedIndex = 0;
            ////}
            ////if (ComboBox_Interfaces.SelectedItem.ToString().Contains("USB to Serial"))
            ////{
            ////    //ComboBox_Devices.Items.Clear();
            ////    //ComboBoxItem CBI = new ComboBoxItem(); 
            ////    ComboBox_Devices.Items.Add(new ComboBoxItem().Content=LabDevice.SupportedDevices.UT71D.ToString());
            ////    //ComboBox_Devices.SelectedIndex = 0;
            ////}
        }

        private void ComboBox_Devices_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox_Devices.Items.Clear();
            if (ComboBox_Interfaces.SelectedItem != null)
            {
                TextBlock_DeviceMessage.Visibility = Visibility.Visible;
                string[] AvilibleDevices = DeviceManager_Obj.ScanAvilibleDevicesOnInterface(ComboBox_Interfaces.SelectedItem.ToString()).ToArray();
                foreach (string CurrentDevice in AvilibleDevices) ComboBox_Devices.Items.Add(CurrentDevice);
                TextBlock_Status.Text = "Получение доступных устройств успешно завершено";
            }

            
            //ComboBox_Devices.Items.Clear();
            //ComboBox_Devices.Items.Add("Доступные устройства:");
            //if (ComboBox_Interfaces.SelectedIndex > 0 && ComboBox_Interfaces.SelectedItem.ToString().Contains('-'))
            //{
            //    if (ComboBox_Interfaces.SelectedItem.ToString().Split('-')[1].Contains("USB HID Device"))
            //    {
            //        ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.UT71D.ToString());
            //        ComboBox_Devices.Items.Add(LabDevice.SupportedDevices.UT61C.ToString());
            //    }
            //}

        }

        private void ComboBox_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock_DeviceMessage.Visibility = ComboBox_Devices.SelectedItem == null ? Visibility.Visible : Visibility.Hidden;
            //if (ComboBox_Devices.SelectedIndex == -1) return;
            //if (ComboBox_Devices.SelectedItem.ToString().Contains(LabDevice.SupportedDevices.UT71D.ToString()))
            //{
            //    Properties.Settings.Default.LastConnectedDevice = LabDevice.SupportedDevices.UT71D.ToString();
            //    Properties.Settings.Default.Save();
            //    System_ConnectToDeviceFromAppSettings();
            //    TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            //}
            //if (ComboBox_Devices.SelectedItem.ToString().Contains(LabDevice.SupportedDevices.HP53132A.ToString()))
            //{
            //    Properties.Settings.Default.LastConnectedDevice = LabDevice.SupportedDevices.HP53132A.ToString();
            //    Properties.Settings.Default.LastConfig = ComboBox_Interfaces.SelectedItem.ToString() + " 19200 ";
            //    Properties.Settings.Default.Save();
            //    System_ConnectToDeviceFromAppSettings();
            //    TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            //}
        }

        private void Button_UserConnectToDevice_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_Interfaces.SelectedItem != null && ComboBox_Devices.SelectedItem != null)
            {
                int newIDDev;
                if ((newIDDev = DeviceManager_Obj.ConnectToDeviceThroughInterface(ComboBox_Interfaces.SelectedItem.ToString(), ComboBox_Devices.SelectedItem.ToString())) > 0)
                {
                    TextBlock_Status.Text = "Подключение к устройству " + ComboBox_Interfaces.SelectedItem.ToString() + " успешно";
                    ComboBox_Devices_DropDownOpened(null, null);
                    ComboBox_Interfaces_DropDownOpened(null, null);
                    ListView_CurrentDev.Items.Add(new ListBoxItem()
                    {
                        Content = " #"+ newIDDev + " "+ DeviceManager_Obj.Devices[newIDDev].DeviceName.ToString() + " "+ DeviceManager_Obj.Devices[newIDDev].DataType.ToString()+ " ",
                        Background = new SolidColorBrush(Colors.Transparent),
                        Tag = newIDDev.ToString(),
                        //IsChecked = true,
                    });
                }
                else
                {
                    TextBlock_Status.Text = "Подключение к устройству " + ComboBox_Interfaces.SelectedItem.ToString() + " не выполнено";
                }
                if (DeviceManager_Obj.Devices.Count == 1) Global_SelectedDevice = newIDDev;
                RadioButton_ChangeAciveDevice_Checked(null, null);
            }

        }

        private void RadioButton_ChangeAciveDevice_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null)
            {
                foreach(ListBoxItem CurrentItem in ListView_CurrentDev.Items)
                {
                    if (Int32.Parse(CurrentItem.Tag.ToString()) == Global_SelectedDevice) CurrentItem.IsSelected = true;
                    else CurrentItem.IsSelected = false;
                }
            }
            else
            {
                RadioButton RadioButtonSender = sender as RadioButton;
                //MessageBox.Show(RadioButtonSender.Tag.ToString());
                Global_SelectedDevice = Int32.Parse(RadioButtonSender.Tag.ToString());
            }
        }

        private void Button_GenerateNewNameFile_Click(object sender, RoutedEventArgs e)
        {
            TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
        }

        private void Button_SetPathFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog.FileName = TextBox_FileName.Text;
            if (saveFileDialog.ShowDialog() == true)
                TextBox_FileName.Text = saveFileDialog.FileName;
        }

        private void RadioButton_FixedPoint_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)RadioButton_Scientific.IsChecked) YFormatter = value => LabDevice.ConvertScientific((decimal)value, MainParam_DataType);
            if ((bool)RadioButton_FixedPoint.IsChecked) YFormatter = value => LabDevice.ConvertFixedPoint((decimal)value, MainParam_DataType);
            //if (MainChart.AxisY.Count>0) MainChart.AxisY[0].LabelFormatter = YFormatter;
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
                IntegerUpDown_QtyMeas.IsEnabled = true;
                TimeSpanUpDown_Timer.IsEnabled = false;
            }
            if ((bool)RadioButton_Timer.IsChecked)
            {
                IntegerUpDown_QtyMeas.IsEnabled = false;
                TimeSpanUpDown_Timer.IsEnabled = true;
            }
        }

        private void RadioButton_Timer_Checked(object sender, RoutedEventArgs e)
        {
            if (IntegerUpDown_QtyMeas != null)
            {
                if ((bool)RadioButton_QtyMeas.IsChecked)
                {
                    IntegerUpDown_QtyMeas.IsEnabled = true;
                    TimeSpanUpDown_Timer.IsEnabled = false;
                }
                if ((bool)RadioButton_Timer.IsChecked)
                {
                    IntegerUpDown_QtyMeas.IsEnabled = false;
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
                //TextBox_FilePos.IsEnabled = true;
                //Button_GenerateNewNameFile.IsEnabled = true;
                Button_SetPathFile.IsEnabled = true;
                //CheckBox_ValueOnly.IsEnabled = true;
                //TextBox_FragmentSize.IsEnabled = true;

                MainParam_CounterMeasure_End = MainParam_CounterMeasure;
                //MainChart.AxisX[0].Sections.Add(new AxisSection
                //{
                //    Value = MainParam_CounterMeasure_End,
                //    StrokeThickness = 3,
                //    Stroke = new SolidColorBrush(Color.FromRgb(220, 30, 30)),
                //    DataLabel = true,
                //});

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
                    if (TextBox_FileName.Text.Contains("\\")) filepath = TextBox_FileName.Text;
                    else filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + TextBox_FileName.Text;
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
                    //TextBox_FilePos.IsEnabled = false;
                    //Button_GenerateNewNameFile.IsEnabled = false;
                    Button_SetPathFile.IsEnabled = false;
                    //CheckBox_ValueOnly.IsEnabled = false;
                    //TextBox_FragmentSize.IsEnabled = false;

                    MainParam_CounterMeasure_Start = MainParam_CounterMeasure;
                    MainParam_TimeStartMeasure = DateTime.Now;
                    ////MainChart.AxisX[0].Sections.Add(new AxisSection
                    ////{
                    ////    Value = MainParam_CounterMeasure_Start,
                    ////    StrokeThickness = 3,
                    ////    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 213, 72)),
                    ////    DataLabel = true,
                    ////});

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
                        MainParam_CounterMeasure_End = MainParam_CounterMeasure + (int)IntegerUpDown_QtyMeas.Value;
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

        //private void MainChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    MainChart.AxisX[0].MinValue = double.NaN;
        //    MainChart.AxisX[0].MaxValue = double.NaN;
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System_RefreshAllChart();
        }

        private void CheckBox_BurnStringChange(object sender, RoutedEventArgs e)
        {
            string StringForBurnToFile = "";
            Random rnd = new Random();
            decimal value = (decimal)rnd.NextDouble() * 3000;
            LabDevice.DataTypes type = (LabDevice.DataTypes)rnd.Next(Enum.GetValues(typeof(LabDevice.DataTypes)).Length);
            StringForBurnToFile = LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            if (CheckBox_BurnFixedPoint.IsChecked.Value) StringForBurnToFile += " " + LabDevice.ConvertFixedPoint(value, type);
            if (CheckBox_BurnCounter.IsChecked.Value) StringForBurnToFile += " " + rnd.Next(10000);//(MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString();
            if (CheckBox_BurnDate.IsChecked.Value) StringForBurnToFile += " " + DateTime.Now.ToString("dd.MM.yyyy");
            if (CheckBox_BurnTime.IsChecked.Value) StringForBurnToFile += " " + DateTime.Now.ToString("HH:mm:ss:") + string.Format("{0:d}", DateTime.Now.Millisecond);
            if (CheckBox_BurnRAW.IsChecked.Value) StringForBurnToFile += " " + "[RAW]";
            TextBlock_BurnString.Text = StringForBurnToFile;
        }

        private void Button_LogEvent_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MessageBox.Show(button.DataContext.ToString());
        }

        private void Button_SaveCurrentData_Click(object sender, RoutedEventArgs e)
        {
            //Random rnd = new Random();
            //System_AddValueToGraph((decimal)(rnd.NextDouble() * 100), LabDevice.DataTypes.Voltage);
        }

        private void Window_Shutdown(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //foreach(Driver_UT71D CurrentDevice in DeviceManager.Devices_UT71D)
            //{
            //    CurrentDevice.Disconnect();
            //}
            //foreach (Driver_UT61C CurrentDevice in DeviceManager.Devices_UT61C)
            //{
            //    CurrentDevice.Disconnect();
            //}
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        //private void CheckBox_AnimatedGraph_Click(object sender, RoutedEventArgs e)
        //{
        //    if (CheckBox_AnimatedGraph.IsChecked.Value) MainChart.DisableAnimations = false;
        //    else MainChart.DisableAnimations = true;
        //}
    }
}
