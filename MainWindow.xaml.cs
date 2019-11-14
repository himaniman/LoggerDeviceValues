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
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using OxyPlot.Annotations;

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
        public enum StateMachine
        {
            Init,
            NowNotConnectedDevice,
            HaveConectedDevice,
            BurningFileCurrentDevice,
            Burning,
        };
        public StateMachine GlobalState = StateMachine.Init;

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
        int Global_CounterForAllDev = -1;

        Dictionary<int, StreamWriter> Global_UserFileWriter = new Dictionary<int, StreamWriter>();

        DispatcherTimer Global_Timer;
        DateTime Global_TimeEndMeasure;
        DateTime Global_TimeStartMeasure;

        //var MainParam_CurrentDevice = new LabDevice();

        //public void System_ConnectToDeviceFromAppSettings()
        //{
        //    //try
        //    //{
        //    //    if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
        //    //    {
        //    //        return;
        //    //    }
        //    //    if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.HP53132A.ToString())
        //    //    {
        //    //        if (Properties.Settings.Default.LastConfig == "null" || Properties.Settings.Default.LastConfig == "" || Properties.Settings.Default.LastConfig.Count(x => x==' ') !=2)
        //    //        {
        //    //            return;
        //    //        }
        //    //        else {
        //    //            String COMName = Properties.Settings.Default.LastConfig.Split(' ')[0];
        //    //            int baudrate = int.Parse(Properties.Settings.Default.LastConfig.Split(' ')[1]);
        //    //            MainParam_SerialPort = new SerialPort(COMName, baudrate, Parity.None, 8, StopBits.One);
        //    //            MainParam_SerialPort.DataReceived += new SerialDataReceivedEventHandler(System_SerialDataReceived);
        //    //            if (!MainParam_SerialPort.IsOpen) MainParam_SerialPort.Open();
        //    //            if (MainParam_SerialPort.IsOpen)
        //    //            {
        //    //                TextBlock_Status.Text = "COM port открыт (" + COMName + ")";
        //    //                ComboBox_Interfaces.Items.Clear();
        //    //                ComboBox_Interfaces.Items.Add(COMName);
        //    //                ComboBox_Interfaces.SelectedIndex = 0;
        //    //                //ComboBox_Interfaces.Background = new SolidColorBrush(Colors.LightGreen);
        //    //                MainParam_DeviceName = LabDevice.SupportedDevices.HP53132A;
        //    //            }
        //    //        }
        //    //    }
        //    //    if (Properties.Settings.Default.LastConnectedDevice == LabDevice.SupportedDevices.UT71D.ToString())
        //    //    {
        //    //        DeviceList.Local.TryGetHidDevice(out MainParam_HIDDevice, vendorID: 6790, productID: 57352);
        //    //        if (MainParam_HIDDevice == null) { TextBlock_Status.Text = "Ошибка доступа к HID устройству"; return; }
        //    //        if (!MainParam_HIDDevice.TryOpen(out MainParam_HIDStream)) { TextBlock_Status.Text = "Ошибка получения потока чтения для HID устройства"; return; }
        //    //        //byte[] InitialConfigStructure = new byte[] { 0x00, 0x09, 0x60, 0x00, 0x00, 0x03 };
        //    //        byte[] InitialConfigStructure = new byte[] { 0x00, 0x4B, 0x00, 0x00, 0x00, 0x03 };
        //    //        MainParam_HIDStream.SetFeature(InitialConfigStructure);
        //    //        MainParam_HIDBuffer = new byte[11];

        //    //        string z = MainParam_HIDDevice.DevicePath;
        //    //        //MainParam_HIDStream.ReadAsync()
        //    //        //MainParam_HIDStream.BeginRead(MainParam_HIDBuffer, 0, 11, new AsyncCallback(System_HIDStreamDataReceived), null);
        //    //        Thread thread1 = new Thread(System_HIDStreamDataReceived);
        //    //        thread1.Start();
        //    //        TextBlock_Status.Text = "Успешное получения потока чтения для HID устройства";

        //    //        if (ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName()) == -1) ComboBox_Interfaces.Items.Add(MainParam_HIDDevice.GetProductName());
        //    //        ComboBox_Interfaces.SelectedIndex = ComboBox_Interfaces.Items.IndexOf(MainParam_HIDDevice.GetProductName());
        //    //        ComboBox_Devices.SelectedIndex = ComboBox_Devices.Items.IndexOf(LabDevice.SupportedDevices.UT71D.ToString());

        //    //        //ComboBox_Devices.Text = "asdsad";
        //    //        //((ComboBoxItem)ComboBox_Devices.Items[ComboBox_Devices.SelectedIndex]).Content = new ComboBoxItem().Content="23123";
        //    //        ComboBox_Devices.Background = new SolidColorBrush(Colors.LightGreen);
        //    //        MainParam_DeviceName = LabDevice.SupportedDevices.UT71D;
        //    //    }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Properties.Settings.Default.LastConfig = "null";
        //    //    Properties.Settings.Default.LastConnectedDevice = "null";
        //    //    Properties.Settings.Default.Save();
        //    //    TextBlock_Status.Text = "Ошибка подключения к COM порту "+ex.ToString().Take(50);
        //    //    return;
        //    //}
        //    ////try
        //    ////{
        //    ////    //TextBlock_Status.Text = "Try connect to port " + Properties.Settings.Default.COMName;
        //    ////    //if (MainParam_SerialPort.IsOpen) MainParam_SerialPort.Close();

        //    ////    //MainParam_SerialPort.PortName = Properties.Settings.Default.COMName;
        //    ////    //MainParam_SerialPort.Handshake = Handshake.None;
        //    ////    //MainParam_SerialPort.Open();

        //    ////    //if (MainParam_SerialPort.IsOpen)
        //    ////    //{
        //    ////    //    TextBlock_Status.Text = "COM port is open (" + Properties.Settings.Default.COMName + ")";
        //    ////    //    Label_StatusCOM.Content = "Подключено " + Properties.Settings.Default.COMName + " " + Properties.Settings.Default.COMBaudrate.ToString() + "bps";
        //    ////    //    Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGreen);
        //    ////    //    ComboBox_COMPorts.SelectedValue = Properties.Settings.Default.COMName;
        //    ////    //}
        //    ////}
        //    ////catch
        //    ////{
        //    ////    //TextBlock_Status.Text = "COM port connect error (" + Properties.Settings.Default.COMName + ")";
        //    ////    //Label_StatusCOM.Content = "Не подключено";
        //    ////    //Label_StatusCOM.Background = new SolidColorBrush(Colors.LightGray);
        //    ////}
        //}

        //void System_SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    SerialPort sp = sender as SerialPort;

        //    try
        //    {
        //        string Buffer = sp.ReadExisting();
        //        System_serialDataQueue.Enqueue(Buffer);
        //    }
        //    catch (TimeoutException ex)
        //    {
        //        MessageBox.Show(ex.ToString());
        //    }
        //    this.Dispatcher.Invoke(() => System_SerialDataProcessing());
        //}

        //void System_HIDStreamDataReceived()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            MainParam_HIDBuffer = MainParam_HIDStream.Read();
        //            System_HIDDataQueue.Enqueue(MainParam_HIDBuffer);
        //            this.Dispatcher.Invoke(() => System_HIDStreamDataProcessing());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //MessageBox.Show(ex.ToString());
        //        return;
        //    }
        //}

        //public void System_HIDStreamDataProcessing()
        //{
        //    if (MainParam_DeviceName == LabDevice.SupportedDevices.UT71D)
        //    {
        //        byte[] LocalBuffer;
        //        while (System_HIDDataQueue.TryDequeue(out LocalBuffer))
        //        {
        //            decimal value;
        //            string valueRAW;
        //            LabDevice.DataTypes type;
        //            //if (Driver_UT71D.TryParceData(LocalBuffer, out value, out type, out valueRAW))
        //            //{
        //            //    //Debug.WriteLine(valueRAW, type.ToString());
        //            //    System_NewValue(value, type, valueRAW);
        //            //}
        //        }
        //    }
        //}

        //public void System_SerialDataProcessing()
        //{
        //    if (MainParam_DeviceName == LabDevice.SupportedDevices.HP53132A)
        //    {
        //        string LocalBuffer;
        //        while (System_serialDataQueue.TryDequeue(out LocalBuffer))
        //        {
        //            MainParam_InputBuffer += LocalBuffer;
        //            if (MainParam_InputBuffer.Contains("\r\n"))
        //            {
        //                decimal value;
        //                Driver_HP53132A.TryParceData(new string(MainParam_InputBuffer.Take(MainParam_InputBuffer.IndexOf('\n')).ToArray()), out value);
        //                System_NewValue(value, LabDevice.DataTypes.Voltage, new string(MainParam_InputBuffer.Replace("\r", "").Take(MainParam_InputBuffer.IndexOf('\n')).ToArray()));
        //                MainParam_InputBuffer = new string(MainParam_InputBuffer.Skip(MainParam_InputBuffer.IndexOf('\n') + 1).ToArray());
        //            }
        //        }
        //    }
        //}

        //public void System_NewValue(decimal value, LabDevice.DataTypes type, string valueRAW = "")
        //{
        //    if (!AcceptProccessData) return;
        //    if (MainParam_DataType == LabDevice.DataTypes.Abstract)
        //    {
        //        MainParam_DataType = type;
        //        RadioButton_FixedPoint_Click(null, null);
        //    }
        //    if (MainParam_DataType != type)
        //    {
        //        AcceptProccessData = false;
        //        if (MessageBox.Show("Обнаружены данные другого типа, удалить предыдущие измерения?", "Изменился тип данных", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
        //        {
        //            AcceptProccessData = true;
        //            return;
        //        }
        //        else
        //        {
        //            AcceptProccessData = true;
        //            MainParam_DataType = type;
        //            System_ClearAllData();
        //            return;
        //        }
        //    }

        //    //System_AddValueToGraph(value, type);

        //    if ((bool)RadioButton_FixedPoint.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertFixedPoint(value, type);
        //    if ((bool)RadioButton_Scientific.IsChecked) TextBlock_CurrentValue.Text = LabDevice.ConvertScientific(value, type);
        //    //TextBlock_CounterMeasure.Text = MainParam_CounterMeasure.ToString();

        //    //if (MainChartValues.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString()))
        //    //    MainChartValues.RemoveAt(0) = (ChartValues<MeasureModel>)MainChartValues.Skip(MainChartValues.Count - int.Parse(((ComboBoxItem)ComboBox_SizeGraph.SelectedItem).Tag.ToString()));

        //    MainParam_MillsBetweenMeasure.Add((int)DateTime.Now.Subtract(MainParam_PastMeasure).TotalMilliseconds);
        //    MainParam_PastMeasure = DateTime.Now;
        //    if (MainParam_MillsBetweenMeasure.Count > 10) MainParam_MillsBetweenMeasure.RemoveAt(0);

        //    //if (MainParam_CounterMeasure > 0 && MainParam_MillsBetweenMeasure.Sum() > 0) TextBlock_FreqMeasure.Text = (60 / (MainParam_MillsBetweenMeasure.Average() / 1000)).ToString("F1") + " выб/мин";

        //    if (Button_FileBurnStart.Content.ToString() == "ЗАПИСЬ...")
        //    {
        //        System_AddValueToFile(value, type, valueRAW);

        //        if (MainParam_CounterMeasure_End == 0)
        //        {
        //            ProgressBar_Status.Value = ((double)(DateTime.Now.Ticks - MainParam_TimeStartMeasure.Ticks) / (double)(MainParam_TimeEndMeasure.Ticks - MainParam_TimeStartMeasure.Ticks)) * 100;
        //            TextBlock_ETA.Text = "До окончания таймера: " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
        //        }
        //        if (MainParam_CounterMeasure_End > 0)
        //        {
        //            ProgressBar_Status.Value = ((double)(MainParam_CounterMeasure - MainParam_CounterMeasure_Start) / (double)(MainParam_CounterMeasure_End - MainParam_CounterMeasure_Start)) * 100;
        //            TextBlock_ETA.Text = "Измерений осталось: " + (MainParam_CounterMeasure_End - MainParam_CounterMeasure).ToString() +
        //                //" (ETA: "+ DateTime.Now.AddMilliseconds((MainParam_CounterMeasure_End - (MainParam_CounterMeasure - MainParam_CounterMeasure_Start)) *(MainParam_SummTimeBetweenMeasure / (MainParam_CounterMeasure - MainParam_CounterMeasure_Start))).ToString("dd'.'hh':'mm':'ss");
        //                " (ETA: " + TimeSpan.FromMilliseconds(
        //                    (MainParam_CounterMeasure_End - MainParam_CounterMeasure) *
        //                    MainParam_MillsBetweenMeasure.Average()
        //                    ).ToString(@"d\.hh\:mm\:ss") + ")";
        //            if (MainParam_CounterMeasure_End == MainParam_CounterMeasure)
        //            {
        //                Button_FileBurnStart_Click(null, null);
        //            }
        //        }
        //    }
        //}

        //public void System_AddValueToFile(decimal value, LabDevice.DataTypes type, string valueRAW)
        //{
        //    string StringForBurnToFile = "";
        //    //if (CheckBox_ValueOnly.IsChecked.Value) StringForBurnToFile = LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
        //    //else
        //    //{
        //    //    StringForBurnToFile += LabDevice.ConvertFixedPoint(value, type) + "\t" + LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
        //    //    StringForBurnToFile += "\t" + string.Format("{0:u}", DateTime.Now).Replace("Z", "") + ":" + string.Format("{0:d}", DateTime.Now.Millisecond);
        //    //    if (valueRAW != "") StringForBurnToFile += "\t" + valueRAW;
        //    //}
        //    MainParam_StreamWriter.WriteLine(StringForBurnToFile);
        //    try
        //    {
        //        if (MainParam_CounterMeasure % Slider_FragmentSize.Value == 0)
        //        {
        //            MainParam_StreamWriter.Flush();
        //            if (MainParam_CounterMeasure_End == 0)
        //            {
        //                TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
        //                //TextBlock_StatusBurn.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "\nОкончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
        //            }
        //            else
        //            {
        //                TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание в ~" + DateTime.Now.AddMilliseconds((MainParam_CounterMeasure_End - MainParam_CounterMeasure) * MainParam_MillsBetweenMeasure.Average()).ToString("dd/MM/yyyy HH:mm:ss");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TextBlock_Status.Text = "Ошибка записи " + ex.ToString();
        //    }

        //}

        //public void System_RefreshAllChart()
        //{
        //    //List<MeasureModel> ListForAdding = new List<MeasureModel>();
        //    //for (int i=0;i< MainParam_Values.Count; i+=2)
        //    //{
        //    //    MeasureModel a = new MeasureModel {
        //    //        Label = i,
        //    //        Value = (double)MainParam_Values[i]
        //    //    };
        //    //    ListForAdding.Add(a);
        //    //}
        //    //AllChartValues.Clear();
        //    //AllChartValues.AddRange(ListForAdding);
        //}

        //public void System_ClearAllData()
        //{
        //    //MainChartValues.Clear();
        //    //MainChart.AxisX[0].Sections.Clear();
        //    //MainChart.Update();
        //    //MainParam_Values.Clear();
        //    if (Button_FileBurnStart.Content.ToString() == "ЗАПИСЬ...") Button_FileBurnStart_Click(null, null);
        //    MainParam_PastMeasure = DateTime.Now;
        //    MainParam_CounterMeasure = 0;
        //    MainParam_CounterMeasure_End = 0;
        //    MainParam_CounterMeasure_Start = 0;
        //    MainParam_MillsBetweenMeasure.Clear();
        //}

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
                MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Dot,
                //MinorStep = DateTimeAxis.ToDouble(DateTime.MinValue+TimeSpan.FromSeconds(1)),
            };

            //FunctionSeries fs = new FunctionSeries();

            MainChartModel = new PlotModel();

            //MainChartModel.Series.Add(fs);

            MainChartModel.Axes.Add(xAxis);
            //MainChartModel.Axes.Add(new LinearAxis());

            GlobalState = StateMachine.NowNotConnectedDevice;
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

            //if (Properties.Settings.Default.LastConnectedDevice == "null" || Properties.Settings.Default.LastConnectedDevice == "")
            //    TextBox_FileName.Text = "Logger_Measure " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
            //else TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";

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

            TextBox_FilePath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //System_ConnectToDeviceFromAppSettings();

            //System_LogMessage("asdas sdfsdfdsfdsf d", 'R', 1);

            //System_LogMessage("asdas2 sdfsdfdsfdsf dsfsdfsd sdfsdf sdfsdf sd sd");
            //System_LogMessage("asdas3 sdfsdfdsfdsf ds1");
            //System_LogMessage("asdas3 sdfsdfdsfdsf ds2");
            //System_LogMessage("asdas3 sdfsdfdsfdsf ds3");

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
            UpdateBlockUI();
            CheckBox_BurnStringChange(null, null);
        }

        public delegate void EventNewValueDelegate(DeviceManager.MeasureStruct measure, int IDSession);

        public void EventNewValue(DeviceManager.MeasureStruct measure, int IDSession)
        {
            if (measure.TS != DateTime.MinValue)
            {
                this.Dispatcher.Invoke(() => System_AddValueToGraph(measure.Val, measure.Typ, measure.TS, IDSession));
                this.Dispatcher.Invoke(() => System_UpdateLifeInfo(measure.TS, measure.Val, measure.Typ, IDSession));
                if (GlobalState == StateMachine.Burning) this.Dispatcher.Invoke(() => System_AddDataToFile_Manager(measure, IDSession));
            }
            else
            {
                this.Dispatcher.Invoke(() => System_UpdateLifeInfo(measure.TS, IDSession: IDSession));
            }
        }

        public void System_UpdateLifeInfo(DateTime TS, decimal Val=0, LabDevice.DataTypes Typ=0, int IDSession=-1)
        {
            foreach (ListBoxItem currentDevSession in ListView_CurrentDev.Items)
            {
                bool flagDeviceRemoved = true;
                foreach (KeyValuePair<int, LabDevice> currentDev in DeviceManager_Obj.Devices)
                {
                    if (Int32.Parse(currentDevSession.Tag.ToString()) == currentDev.Key) flagDeviceRemoved = false;
                }
                if (flagDeviceRemoved)
                {
                    ListView_CurrentDev.Items.Remove(currentDevSession);
                    System_UpdateGraph();
                    RadioButton_ChangeAciveDevice_Checked(null, null);
                    break;
                }
            }

            bool flagGenNewSession = true;
            bool flagHaveActiveDev = false;
            foreach (ListBoxItem currentDevSession in ListView_CurrentDev.Items)
            {
                int currentIDSession = Int32.Parse(currentDevSession.Tag.ToString());
                if (currentIDSession == IDSession) flagGenNewSession = false;
                if (DeviceManager_Obj.Devices[currentIDSession].active) flagHaveActiveDev = true;
                else if (!DeviceManager_Obj.Devices[currentIDSession].active && GlobalState == StateMachine.Burning && DeviceManager_Obj.Devices[currentIDSession].CounterMeasureForBurning != -1)
                {
                    Global_UserFileWriter[currentIDSession].WriteLine("Устройство перестало отвечать в " + DateTime.Now);
                    Global_UserFileWriter[currentIDSession].Flush();
                    DeviceManager_Obj.Devices[currentIDSession].CounterMeasureForBurning = -1;
                }
                if (currentIDSession == Global_SelectedDevice)
                {
                    if (IDSession == Global_SelectedDevice)
                    {
                        TextBlock_CurrentValue.Text = LabDevice.ConvertFixedPoint(Val, Typ);
                        //if (DeviceManager_Obj.Devices[IDSession].CounterMeasure > 0 && DeviceManager_Obj.Devices[IDSession].MillsBetweenMeasure.Sum() > 0) TextBlock_FreqMeasure.Text = (60 / (DeviceManager_Obj.Devices[IDSession].MillsBetweenMeasure.Average() / 1000)).ToString("F1") + " выб/мин";
                        //TextBlock_CounterMeasure.Text = LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[IDSession].Statistics_Max, Typ);
                    }
                    else if (IDSession == -1)
                    {
                        TextBlock_CurrentValue.Text = "N/A";
                        //if (DeviceManager_Obj.Devices[Global_SelectedDevice].CounterMeasure > 0 && DeviceManager_Obj.Devices[Global_SelectedDevice].MillsBetweenMeasure.Sum() > 0) TextBlock_FreqMeasure.Text = (60 / (DeviceManager_Obj.Devices[Global_SelectedDevice].MillsBetweenMeasure.Average() / 1000)).ToString("F1") + " выб/мин";
                        //TextBlock_CounterMeasure.Text = LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[Global_SelectedDevice].Statistics_Max, Typ);
                    }
                    if (IDSession != -1)
                    {
                        TextBlock_Statistics_Min.Text = "Min = " + LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].Statistics_Min, DeviceManager_Obj.Devices[currentIDSession].DataType);
                        TextBlock_Statistics_Max.Text = "Max = " + LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].Statistics_Max, DeviceManager_Obj.Devices[currentIDSession].DataType);
                        TextBlock_Statistics_Avr.Text = "Avr = " + LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].Statistics_Average, DeviceManager_Obj.Devices[currentIDSession].DataType);
                        TextBlock_Statistics_RMS.Text = "RMS = " + LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].Statistics_RMS, DeviceManager_Obj.Devices[currentIDSession].DataType);
                        TextBlock_Statistics_PtP.Text = "PtP = " + LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].Statistics_PeakToPeak, DeviceManager_Obj.Devices[currentIDSession].DataType);
                        //TextBlock_Statistics_FreqMeasure.Text = LabDevice.ConvertFixedPoint(DeviceManager_Obj.Devices[currentIDSession].MillsBetweenMeasure, DeviceManager_Obj.Devices[currentIDSession].DataType);
                    }
                        if (DeviceManager_Obj.Devices[currentIDSession].active)
                    {
                        RadioButton_StartMeasure.IsEnabled = true;
                        RadioButton_PauseMeasure.IsEnabled = true;
                        TextBlock_DataNotComing.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        RadioButton_StartMeasure.IsEnabled = false;
                        RadioButton_PauseMeasure.IsEnabled = false;
                        TextBlock_DataNotComing.Visibility = Visibility.Visible;
                    }
                    if (DeviceManager_Obj.Devices[currentIDSession].IDTargetDriver > -1) Button_DisableDriver.IsEnabled = true;
                    else Button_DisableDriver.IsEnabled = false;
                    if (DeviceManager_Obj.Devices[currentIDSession].ignore)
                    {
                        RadioButton_StartMeasure.IsChecked = false;
                        RadioButton_PauseMeasure.IsChecked = true;
                    }
                    else
                    {
                        RadioButton_StartMeasure.IsChecked = true;
                        RadioButton_PauseMeasure.IsChecked = false;
                    }
                }
                currentDevSession.Content = " #" + currentIDSession + " " + DeviceManager_Obj.Devices[currentIDSession].DeviceName.ToString() + " " + DeviceManager_Obj.Devices[currentIDSession].DataType.ToString() + " ";
                foreach (LineSeries currentSeries in MainChartModel.Series)
                {
                    if (currentSeries.Tag.ToString() == currentDevSession.Tag.ToString())
                    {
                        currentDevSession.Background = new SolidColorBrush(Color.FromArgb(currentSeries.ActualColor.A, currentSeries.ActualColor.R, currentSeries.ActualColor.G, currentSeries.ActualColor.B));
                        break;
                    }
                }
            }
            if (flagGenNewSession && IDSession!=-1)
            {
                System_AddDeviceInViewList(IDSession);
                System_AddSeriesToGraph(IDSession);
                Global_SelectedDevice = IDSession;
                RadioButton_ChangeAciveDevice_Checked(null, null);
            }
            if (TS == DateTime.MinValue && IDSession != -1)
            {
                System_LogMessage("Устройство " + DeviceManager_Obj.Devices[IDSession].DeviceName.ToString() + " перестало отвечать", 'R', IDSession);
            }
            if (!flagHaveActiveDev && GlobalState == StateMachine.HaveConectedDevice) GlobalState = StateMachine.NowNotConnectedDevice;
            if (flagHaveActiveDev && GlobalState == StateMachine.NowNotConnectedDevice) GlobalState = StateMachine.HaveConectedDevice;
            if (GlobalState == StateMachine.Burning)
            {
                if (Global_CounterForAllDev > -1)
                {
                    ProgressBar_Status.Value = ((double)((int)IntegerUpDown_QtyMeas.Value - ((int)IntegerUpDown_QtyMeas.Value - Global_CounterForAllDev)) / (double)(IntegerUpDown_QtyMeas.Value)) * 100;
                    TextBlock_ETA.Text = "Измерений осталось: " + ((int)IntegerUpDown_QtyMeas.Value - ((int)IntegerUpDown_QtyMeas.Value - Global_CounterForAllDev)).ToString();
                    if (DeviceManager_Obj.Devices[Global_SelectedDevice].active && !DeviceManager_Obj.Devices[Global_SelectedDevice].ignore && 
                        DeviceManager_Obj.Devices[Global_SelectedDevice].MillsBetweenMeasure.Count > 0)
                    {
                        TextBlock_ETA.Text += " (ETA: " + TimeSpan.FromMilliseconds(
                            ((int)IntegerUpDown_QtyMeas.Value - ((int)IntegerUpDown_QtyMeas.Value - Global_CounterForAllDev)) *
                            DeviceManager_Obj.Devices[Global_SelectedDevice].MillsBetweenMeasure.Average()
                            ).ToString(@"d\.hh\:mm\:ss") + ")";
                    }

                    if (Global_CounterForAllDev == 0)
                    {
                        Button_FileBurnStart_Click(null, null);
                    }
                }
            }
            UpdateBlockUI();
        }

        public void System_AddValueToGraph(decimal value, LabDevice.DataTypes type, DateTime time, int IDSession)
        {

            foreach (LineSeries CurrentSeries in MainChartModel.Series)
            {
                if (CurrentSeries.Tag.ToString() == IDSession.ToString())
                {
                    if (time != DateTime.MinValue)
                    {
                        CurrentSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), (double)value));
                        while (CurrentSeries.Points.Count > int.Parse(((ComboBoxItem)(ComboBox_SizeGraph.SelectedItem)).Tag.ToString())) CurrentSeries.Points.RemoveAt(0);
                        MainChartModel.GetAxis(IDSession.ToString()).Title = type.ToString();
                        //CurrentSeries.YAxis.Title = type.ToString();
                    }
                    else
                    {
                        CurrentSeries.Points.Add(DataPoint.Undefined);
                    }
                }
                else
                {

                }
            }
            System_UpdateGraph();
        }

        public void System_UpdateGraph()
        {
            foreach (LineSeries CurrentSeries in MainChartModel.Series)
            {
                bool flagDeviceRemoved = true;
                foreach (KeyValuePair<int, LabDevice> currentDev in DeviceManager_Obj.Devices)
                {
                    if (CurrentSeries.Tag.ToString() == currentDev.Key.ToString()) flagDeviceRemoved = false;
                }
                if (flagDeviceRemoved)
                {
                    MainChartModel.Axes.Remove(MainChartModel.GetAxis(CurrentSeries.Tag.ToString()));
                    MainChartModel.Series.Remove(CurrentSeries);
                    break;
                }
            }

            foreach (LineSeries CurrentSeries in MainChartModel.Series)
            {
                if (CurrentSeries.Tag.ToString() == Global_SelectedDevice.ToString())
                {
                    CurrentSeries.LineStyle = LineStyle.Solid;
                    CurrentSeries.StrokeThickness = 2;
                    MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).MajorGridlineStyle = LineStyle.Solid;
                    MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).MinorGridlineStyle = LineStyle.DashDotDot;
                }
                else
                {
                    CurrentSeries.LineStyle = LineStyle.Dot;
                    CurrentSeries.StrokeThickness = 1;
                    MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).MajorGridlineStyle = LineStyle.None;
                    MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).MinorGridlineStyle = LineStyle.None;
                }
                MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).MajorGridlineColor = CurrentSeries.ActualColor;
                MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).TitleColor = CurrentSeries.ActualColor;
                MainChartModel.GetAxis(CurrentSeries.Tag.ToString()).TextColor = CurrentSeries.ActualColor;
            }
            MainChart.InvalidatePlot();
        }

        public void System_AddSeriesToGraph(int IDSession)
        {
            LinearAxis yAxis = new LinearAxis
            {
                Key = IDSession.ToString(),
                Position = MainChartModel.Series.Count % 2 == 0 ? AxisPosition.Left : AxisPosition.Right,
                PositionTier = IDSession,
                Title = IDSession.ToString(),
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.DashDotDot,
            };

            LineSeries fs = new LineSeries()
            {
                YAxisKey = IDSession.ToString(),
                //InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,
                CanTrackerInterpolatePoints = false,
            };

            MainChartModel.Series.Add(fs);
            MainChartModel.Series.Last().Tag = IDSession;

            MainChartModel.Axes.Add(yAxis);
        }

        public void System_AddDeviceInViewList(int IDSession)
        {
            ListView_CurrentDev.Items.Add(new ListBoxItem()
            {
                Content = " #" + IDSession + " " + DeviceManager_Obj.Devices[IDSession].DeviceName.ToString() + " " + DeviceManager_Obj.Devices[IDSession].DataType.ToString() + " ",
                //Background = new SolidColorBrush(Colors.Red),
                Tag = IDSession.ToString(),
                //IsChecked = true,
            });
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
                TextBlock_Status.Text = "Получение списка доступных устройств успешно завершено";
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
                int newIDSession;
                if ((newIDSession = DeviceManager_Obj.ConnectToDeviceThroughInterface(ComboBox_Interfaces.SelectedItem.ToString(), ComboBox_Devices.SelectedItem.ToString())) > 0)
                {
                    System_LogMessage("Подключение к устройству " + ComboBox_Interfaces.SelectedItem.ToString() + " успешно", 'I', newIDSession);
                    TextBlock_Status.Text = "Подключение к устройству " + ComboBox_Interfaces.SelectedItem.ToString() + " успешно";
                    ComboBox_Devices_DropDownOpened(null, null);
                    ComboBox_Interfaces_DropDownOpened(null, null);
                    System_AddDeviceInViewList(newIDSession);
                    System_AddSeriesToGraph(newIDSession);
                }
                else
                {
                    System_LogMessage("Подключение к устройству " + ComboBox_Interfaces.SelectedItem.ToString() + " не выполнено", 'E', newIDSession);
                }
                if (DeviceManager_Obj.Devices.Count == 1) Global_SelectedDevice = newIDSession;
                RadioButton_ChangeAciveDevice_Checked(null, null);
            }

        }

        private void RadioButton_ChangeAciveDevice_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null)
            {
                bool HaveSelectedDev = false;
                foreach(ListBoxItem CurrentItem in ListView_CurrentDev.Items)
                {
                    if (Int32.Parse(CurrentItem.Tag.ToString()) == Global_SelectedDevice) { CurrentItem.IsSelected = true; HaveSelectedDev = true; }
                    else CurrentItem.IsSelected = false;
                }
                if (!HaveSelectedDev && ListView_CurrentDev.Items.Count>0) { (ListView_CurrentDev.Items[0] as ListBoxItem).IsSelected = true; Global_SelectedDevice = Int32.Parse((ListView_CurrentDev.Items[0] as ListBoxItem).Tag.ToString()); }
            }
            else
            {
                RadioButton RadioButtonSender = sender as RadioButton;
                //MessageBox.Show(RadioButtonSender.Tag.ToString());
                Global_SelectedDevice = Int32.Parse(RadioButtonSender.Tag.ToString());
            }
            System_UpdateGraph();
            System_UpdateLifeInfo(DateTime.MinValue);
        }







        public void System_LogMessage(String _text, char type, int IDSession)
        {
            string text="";
            if (IDSession > -1) text = "#" + IDSession.ToString() + " " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " " + _text;
            else text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " " + _text;
            SolidColorBrush newcolor = new SolidColorBrush(Colors.Transparent);
            if (type == 'R') newcolor = new SolidColorBrush(Colors.LightPink);
            ListBox_Log.Items.Add(new ListBoxItem()
            {
                Content = text,
                Tag = type+ IDSession.ToString(),
                Background = newcolor,
            });
        }

        private void Button_LogEvent_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            foreach (ListBoxItem CurrentItem in ListBox_Log.Items)
            {
                if ((button.DataContext as ListBoxItem).Tag.ToString() == CurrentItem.Tag.ToString())
                {
                    char type = (button.DataContext as ListBoxItem).Tag.ToString()[0];
                    int IDSession = Int32.Parse((button.DataContext as ListBoxItem).Tag.ToString().Substring(1));
                    if (type == 'I')
                    {
                        ListBox_Log.Items.Remove(CurrentItem);
                        break;
                    }
                    if (type == 'R')
                    {
                        if (MessageBox.Show("Устройство "+ DeviceManager_Obj.Devices[IDSession].DeviceName.ToString() + 
                            " перестало присылать данные. Автоматически сегенерирована новая сессия для новых данные с этого прибора. Сохранить текущую сессию №"
                            + IDSession.ToString() + " с " + DeviceManager_Obj.Devices[IDSession].CounterMeasure.ToString() + " измерениями в файл?", 
                            "Прибор перестал присылать данные", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Global_SelectedDevice = IDSession;
                            RadioButton_ChangeAciveDevice_Checked(null, null);
                            Button_SaveCurrentData_Click(null, null);
                            DeviceManager_Obj.RemoveAndDisonnectDevice(IDSession);
                            ListBox_Log.Items.Remove(CurrentItem);
                        }
                        else
                        {
                            DeviceManager_Obj.RemoveAndDisonnectDevice(IDSession);
                            ListBox_Log.Items.Remove(CurrentItem);
                        }
                        break;
                    }
                }
            }
        }

        private void backgroundWorker_SaveLabDevDataToFile(object sender, DoWorkEventArgs e)
        {
            List<object> genericlist = e.Argument as List<object>;

            LabDevice currentdev = genericlist[0] as LabDevice;
            StreamWriter localStreamWriter;
            localStreamWriter = File.CreateText(genericlist[1] as string);

            localStreamWriter.WriteLine("Выгрузка измерений " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            localStreamWriter.WriteLine("Название устройства " + currentdev.DeviceName.ToString());
            localStreamWriter.WriteLine("Тип данных " + currentdev.DataType.ToString());
            localStreamWriter.WriteLine("Количество измерений " + currentdev.CounterMeasure.ToString());
            localStreamWriter.Flush();
            localStreamWriter.AutoFlush = false;

            string delemiter = genericlist[2] as string;
            string StringForBurnToFile = "";

            for (int i = 0; i < currentdev.MeasuresBase.Count; i++)
            {
                StringForBurnToFile = LabDevice.ConvertScientific(currentdev.MeasuresBase[i].Val, LabDevice.DataTypes.Abstract);
                if ((bool)genericlist[3]) StringForBurnToFile += delemiter + LabDevice.ConvertFixedPoint(currentdev.MeasuresBase[i].Val, currentdev.MeasuresBase[i].Typ);
                if ((bool)genericlist[4]) StringForBurnToFile += delemiter + i.ToString();//(MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString();
                if ((bool)genericlist[5]) StringForBurnToFile += delemiter + currentdev.MeasuresBase[i].TS.ToString("dd.MM.yyyy");
                if ((bool)genericlist[6]) StringForBurnToFile += delemiter + currentdev.MeasuresBase[i].TS.ToString("HH:mm:ss:") + string.Format("{0:d}", DateTime.Now.Millisecond);
                if ((bool)genericlist[7]) StringForBurnToFile += delemiter + currentdev.MeasuresBase[i].RAW;

                localStreamWriter.WriteLine(StringForBurnToFile);

                if (i % 10000 == 0)
                {
                    (sender as BackgroundWorker).ReportProgress((int)((double)i / (double)currentdev.MeasuresBase.Count * 100));
                }
            }
            localStreamWriter.Flush();
            localStreamWriter.Close();
            e.Result = genericlist[8] as string; //DeviceManager_Obj.Devices.FirstOrDefault(x => x.Value == currentdev).Key;
        }

        private void backgroundWorker_SaveLabDevDataToFile_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar_Status.Value = e.ProgressPercentage;
            TextBlock_ETA.Text = "Запись в файл " + e.ProgressPercentage.ToString() + "%";
        }

        private void backgroundWorker_SaveLabDevDataToFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                
            }
            else if (e.Error != null)
            {
                
            }
            else
            {
                System_LogMessage("Текущее сессия записана в файл", 'I', Int32.Parse(e.Result.ToString()));
                Button_SaveCurrentData.IsEnabled = true;
                TextBlock_ETA.Text = "Время / Измерения до окончания";
                ProgressBar_Status.Value = 0;
                GlobalState = StateMachine.HaveConectedDevice;
                UpdateBlockUI();
            }
        }

        private void Button_SaveCurrentData_Click(object sender, RoutedEventArgs e)
        {
            //Random rnd = new Random();
            //System_AddValueToGraph((decimal)(rnd.NextDouble() * 100), LabDevice.DataTypes.Voltage);
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|CSV (*.csv)|*.csv";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            foreach (ListBoxItem CurrentItem in ListView_CurrentDev.Items)
            {
                if (CurrentItem.IsSelected == true && Int32.Parse(CurrentItem.Tag.ToString()) == Global_SelectedDevice)
                {
                    saveFileDialog.FileName = "LoggerDevice S" + CurrentItem.Tag.ToString() + " " +
                        DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") + " " + DeviceManager_Obj.Devices[Int32.Parse(CurrentItem.Tag.ToString())].DeviceName.ToString();
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        GlobalState = StateMachine.BurningFileCurrentDevice;
                        UpdateBlockUI();

                        BackgroundWorker bgw;
                        bgw = new BackgroundWorker();
                        bgw.WorkerReportsProgress = true;
                        bgw.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_SaveLabDevDataToFile_ProgressChanged);
                        bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_SaveLabDevDataToFile_RunWorkerCompleted);
                        bgw.DoWork += new DoWorkEventHandler(backgroundWorker_SaveLabDevDataToFile);

                        List<object> arguments = new List<object>();
                        arguments.Add(DeviceManager_Obj.Devices[Int32.Parse(CurrentItem.Tag.ToString())]);
                        arguments.Add(saveFileDialog.FileName);
                        arguments.Add(saveFileDialog.FilterIndex == 1 ? " " : ";");
                        arguments.Add(CheckBox_BurnFixedPoint.IsChecked.Value);
                        arguments.Add(CheckBox_BurnCounter.IsChecked.Value);
                        arguments.Add(CheckBox_BurnDate.IsChecked.Value);
                        arguments.Add(CheckBox_BurnTime.IsChecked.Value);
                        arguments.Add(CheckBox_BurnRAW.IsChecked.Value);
                        arguments.Add(CurrentItem.Tag.ToString());

                        bgw.RunWorkerAsync(arguments);

                        Button_SaveCurrentData.IsEnabled = false;
                        //System_SaveLabDevDataToFile(DeviceManager_Obj.Devices[Int32.Parse(CurrentItem.Tag.ToString())], saveFileDialog.FileName);
                        //System_LogMessage("Текущее сессия записана в файл", 'I', Int32.Parse(CurrentItem.Tag.ToString()));
                    }
                    break;
                }
            }   
        }

        private void System_AddDataToFile_Manager(DeviceManager.MeasureStruct measure, int IDSession)
        {
            LabDevice currentdev = DeviceManager_Obj.Devices[IDSession];

            if (!Global_UserFileWriter.ContainsKey(IDSession))
            {
                string FileName = TextBox_FilePath.Text + "\\LoggerDevice S" + IDSession.ToString() + " " +
                        DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") + " " + DeviceManager_Obj.Devices[IDSession].DeviceName.ToString();
                FileName += (bool)RadioButton_BurnTXT.IsChecked ? ".txt" : ".csv";
                Global_UserFileWriter.Add(IDSession, File.CreateText(FileName));

                Global_UserFileWriter[IDSession].WriteLine("Начало сохранения измерений в " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                Global_UserFileWriter[IDSession].WriteLine("Название устройства " + currentdev.DeviceName.ToString());
                Global_UserFileWriter[IDSession].WriteLine("Тип данных " + currentdev.DataType.ToString());
                if (currentdev.MillsBetweenMeasure.Count>0) Global_UserFileWriter[IDSession].WriteLine("Среднее время между измеренийми " + currentdev.MillsBetweenMeasure.Average().ToString() + " мс.");
                Global_UserFileWriter[IDSession].Flush();
                Global_UserFileWriter[IDSession].AutoFlush = false;
                System_LogMessage("Начата запись в файл '" + "LoggerDevice S" + IDSession.ToString() + " " +
                        DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") + " " + DeviceManager_Obj.Devices[IDSession].DeviceName.ToString() + ((bool)RadioButton_BurnTXT.IsChecked ? ".txt" : ".csv") + "'", 'I', IDSession);
            }

            string delemiter = (bool)RadioButton_BurnTXT.IsChecked ? " " : ";";

            string StringForBurnToFile = LabDevice.ConvertScientific(measure.Val, LabDevice.DataTypes.Abstract);
            if (CheckBox_BurnFixedPoint.IsChecked.Value) StringForBurnToFile += delemiter + LabDevice.ConvertFixedPoint(measure.Val, measure.Typ);
            if (CheckBox_BurnCounter.IsChecked.Value) StringForBurnToFile += delemiter + currentdev.CounterMeasureForBurning.ToString();//(MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString();
            if (CheckBox_BurnDate.IsChecked.Value) StringForBurnToFile += delemiter + measure.TS.ToString("dd.MM.yyyy");
            if (CheckBox_BurnTime.IsChecked.Value) StringForBurnToFile += delemiter + measure.TS.ToString("HH:mm:ss:") + string.Format("{0:d}", DateTime.Now.Millisecond);
            if (CheckBox_BurnRAW.IsChecked.Value) StringForBurnToFile += delemiter + measure.RAW;

            currentdev.CounterMeasureForBurning++;
            Global_CounterForAllDev--;

            Global_UserFileWriter[IDSession].WriteLine(StringForBurnToFile);
                
            Global_UserFileWriter[IDSession].Flush();
        }

        private void Button_SetPathFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) TextBox_FilePath.Text = dialog.FileName;
        }

        private void Button_FileBurnStart_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalState == StateMachine.HaveConectedDevice)
            {
                GlobalState = StateMachine.Burning;
                UpdateBlockUI();
                TabItem_CurrentMeasurment.IsSelected = true;
                Button_FileBurnStart.Content = "ЗАПИСЬ...";
                Button_FileBurnStart.Background = new SolidColorBrush(Colors.OrangeRed);
                if ((bool)RadioButton_QtyMeas.IsChecked) Global_CounterForAllDev = (int)IntegerUpDown_QtyMeas.Value;
                if ((bool)RadioButton_Timer.IsChecked)
                {
                    Global_CounterForAllDev = -1;
                    if (Global_Timer != null) Global_Timer.Stop();
                    Global_Timer = new DispatcherTimer();
                    Global_Timer.Interval = TimeSpan.FromSeconds(1);//TimeSpanUpDown_Timer.Value.Value;
                    Global_Timer.Start();
                    Global_Timer.Tick += MainParam_Timer_Tick;
                    Global_TimeEndMeasure = DateTime.Now.AddSeconds(TimeSpanUpDown_Timer.Value.Value.TotalSeconds);
                }

                var annotation = new LineAnnotation();
                annotation.Color = OxyColors.Green;
                annotation.X = DateTimeAxis.ToDouble(DateTime.Now);
                annotation.LineStyle = LineStyle.LongDashDot;
                annotation.StrokeThickness = 3;
                annotation.Type = LineAnnotationType.Vertical;
                MainChartModel.Annotations.Add(annotation);

                System_LogMessage("Начата запись с всех активных устройств", 'I', -1);
                return;
            }
            if (GlobalState == StateMachine.Burning)
            {
                GlobalState = StateMachine.HaveConectedDevice;
                UpdateBlockUI();
                RadioButton_QtyMeas_Checked(null, null);
                System_UpdateLifeInfo(DateTime.MaxValue, IDSession: Global_SelectedDevice);
                TabItem_CurrentMeasurment.IsSelected = true;
                Button_FileBurnStart.Content = "Начать запись";
                ProgressBar_Status.Value = 0;
                TextBlock_ETA.Text = "Время/Измерения до окончания";
                Button_FileBurnStart.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
                if (Global_Timer!=null) Global_Timer.Stop();
                Global_CounterForAllDev = -1;

                var annotation = new LineAnnotation();
                annotation.Color = OxyColors.Red;
                annotation.X = DateTimeAxis.ToDouble(DateTime.Now);
                annotation.LineStyle = LineStyle.LongDashDot;
                annotation.StrokeThickness = 3;
                annotation.Type = LineAnnotationType.Vertical;
                MainChartModel.Annotations.Add(annotation);

                foreach (int streamID in Global_UserFileWriter.Keys)
                {
                    Global_UserFileWriter[streamID].WriteLine("Завершение записи в " + DateTime.Now);
                    Global_UserFileWriter[streamID].Flush();
                    Global_UserFileWriter[streamID].Close();
                    System_LogMessage("Файл для сессии " + streamID + " успешно закрыт", 'I', streamID);
                }
                Global_UserFileWriter.Clear();
                return;
            }
        }

        private void MainParam_Timer_Tick(object s, EventArgs e)
        {
                ProgressBar_Status.Value = ((double)(DateTime.Now.Ticks - Global_TimeStartMeasure.Ticks) / (double)(Global_TimeEndMeasure.Ticks - Global_TimeStartMeasure.Ticks)) * 100;
                TextBlock_ETA.Text = "До окончания записи: " + (Global_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");

            if (Global_TimeEndMeasure < DateTime.Now) Button_FileBurnStart_Click(null, null);
        }





        private void RadioButton_ChangeSkipDataAction(object sender, RoutedEventArgs e)
        {
            if (RadioButton_StartMeasure.IsChecked == false)
            {
                if (DeviceManager_Obj.Devices[Global_SelectedDevice].ignore == false) DeviceManager_Obj.Devices[Global_SelectedDevice].ignore = true;
            }
            else
            {
                if (DeviceManager_Obj.Devices[Global_SelectedDevice].ignore == true)
                {
                    System_AddValueToGraph(0, 0, DateTime.MinValue, Global_SelectedDevice);
                    DeviceManager_Obj.Devices[Global_SelectedDevice].PastMeasure = DateTime.Now;
                    DeviceManager_Obj.Devices[Global_SelectedDevice].MillsBetweenMeasure.Clear();
                    DeviceManager_Obj.Devices[Global_SelectedDevice].ignore = false;
                }
            }
        }

        private void Button_DisableDriver_Click(object sender, RoutedEventArgs e)
        {
            DeviceManager_Obj.RemoveAndDisonnectDevice(Global_SelectedDevice);
            ListBox_Log.Items.Remove(Global_SelectedDevice);
            RadioButton_ChangeAciveDevice_Checked(null, null);
        }







        public void UpdateBlockUI()
        {
            switch (GlobalState)
            {
                case StateMachine.NowNotConnectedDevice:
                    GroupBox_Burning.IsEnabled = false;
                    Button_SaveCurrentData.IsEnabled = true;
                    TabItem_Settings.IsEnabled = true;
                    break;
                case StateMachine.HaveConectedDevice:
                    GroupBox_Burning.IsEnabled = true;
                    Button_FileBurnStart.IsEnabled = true;
                    Button_SaveCurrentData.IsEnabled = true;
                    RadioButton_QtyMeas.IsEnabled = true;
                    RadioButton_Timer.IsEnabled = true;
                    //TimeSpanUpDown_Timer.IsEnabled = true;
                    //IntegerUpDown_QtyMeas.IsEnabled = true;
                    TabItem_Settings.IsEnabled = true;
                    break;
                case StateMachine.BurningFileCurrentDevice:
                    Button_FileBurnStart.IsEnabled = false;
                    Button_SaveCurrentData.IsEnabled = false;
                    RadioButton_QtyMeas.IsEnabled = true;
                    RadioButton_Timer.IsEnabled = true;
                    //TimeSpanUpDown_Timer.IsEnabled = true;
                    //IntegerUpDown_QtyMeas.IsEnabled = true;
                    TabItem_Settings.IsEnabled = true;
                    break;
                case StateMachine.Burning:
                    Button_SaveCurrentData.IsEnabled = false;
                    RadioButton_QtyMeas.IsEnabled = false;
                    RadioButton_Timer.IsEnabled = false;
                    TimeSpanUpDown_Timer.IsEnabled = false;
                    IntegerUpDown_QtyMeas.IsEnabled = false;
                    TabItem_Settings.IsEnabled = false;
                    RadioButton_StartMeasure.IsEnabled = false;
                    RadioButton_PauseMeasure.IsEnabled = false;
                    Button_DisableDriver.IsEnabled = false;
                    break;
            }
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
            if (CheckBox_BurnRAW.IsChecked.Value) StringForBurnToFile += " " + "0x00 0xff 0x00 0xff";
            TextBlock_BurnString.Text = StringForBurnToFile;
        }










        private void Button_GenerateNewNameFile_Click(object sender, RoutedEventArgs e)
        {
            //TextBox_FileName.Text = "Logger_Measure_" + Properties.Settings.Default.LastConnectedDevice + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt";
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

        //private void MainChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    MainChart.AxisX[0].MinValue = double.NaN;
        //    MainChart.AxisX[0].MaxValue = double.NaN;
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //System_RefreshAllChart();
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
