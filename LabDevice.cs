using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class LabDevice
    {
        //public enum DeviceTypes
        //{
        //    Serial, USB_HID
        //}

        public enum SupportedDevices
        {
            HP53132A, UT71D, UT61C, Virtual
        }

        public enum DataTypes
        {
            Freq, Voltage, Resistance, Capacity, Temperature, Current, Abstract
        }

        public struct MeasureStruct
        {
            public decimal Val;
            public DataTypes Typ;
            public DateTime TS;
        }

        StreamWriter StreamWriter;
        public SupportedDevices DeviceName;
        public DataTypes DataType;
        public int CounterMeasure;
        public List<int> MillsBetweenMeasure;
        public DateTime PastMeasure;

        public ConcurrentQueue<MeasureStruct> QueueNewValues = new ConcurrentQueue<MeasureStruct>();

        //public List<Driver_UT71D> Devices_UT71D = new List<Driver_UT71D>();
        public Driver_VirtualDevice Obj_Driver_VirtualDevice;

        //public LabDevice()
        //{

        //}

        public LabDevice(SupportedDevices _currentDevice, DataTypes _dataType = DataTypes.Abstract)
        {
            MillsBetweenMeasure = new List<int>();
            DeviceName = _currentDevice;
            DataType = _dataType;
            //if (_currentDevice == SupportedDevices.UT71D) Devices_UT71D.Add(new Driver_UT71D(NewValue)); //binding method for compute and storage new value
            if (_currentDevice == SupportedDevices.Virtual) { Obj_Driver_VirtualDevice = new Driver_VirtualDevice(NewValue); Obj_Driver_VirtualDevice.Connect(); }
        }

        public LabDevice(LabDevice oldLabDev, DataTypes _dataType)
        {
            DeviceName = oldLabDev.DeviceName;
            DataType = _dataType;
            if (oldLabDev.DeviceName == SupportedDevices.Virtual)
            {
                Obj_Driver_VirtualDevice = new Driver_VirtualDevice(oldLabDev.Obj_Driver_VirtualDevice);
            }
            MeasureStruct temp;
            while (oldLabDev.QueueNewValues.TryDequeue(out temp));
        }

        public delegate void NewValueDelegate(decimal value, DataTypes type);
        public void NewValue(decimal value, DataTypes type) //exec from thread in driver
        {
            //Debug.WriteLine(value);
            //if (type != DataType) DeviceManager
            QueueNewValues.Enqueue(new MeasureStruct { Val = value, Typ = type, TS = DateTime.Now });

            MillsBetweenMeasure.Add((int)DateTime.Now.Subtract(PastMeasure).TotalMilliseconds);
            PastMeasure = DateTime.Now;
            if (MillsBetweenMeasure.Count > 10) MillsBetweenMeasure.RemoveAt(0);

            //записать себе, чтобы потом с другого потока кто то мог взять эти данные, + отправить текущие данные в хост приложение
            //AddValueToFile(value, LabDevice.DataTypes.Abstract, "");
            //graph
            //file
            //MainWindow.System_AddValueToGraph(value, DataTypes.Abstract);
        }

        public void AddValueToFile(decimal value, LabDevice.DataTypes type, string valueRAW)
        {
            if (StreamWriter == null) StreamWriter = File.CreateText(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" +
                "Log_Measure_" + DeviceName.ToString() + " " + DateTime.Now.ToString("dd/MM/yyyy HH-mm-ss") + ".txt");


            string StringForBurnToFile = "";
            //if (CheckBox_ValueOnly.IsChecked.Value) StringForBurnToFile = LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            //else
            //{
            //    StringForBurnToFile += LabDevice.ConvertFixedPoint(value, type) + "\t" + LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            //    StringForBurnToFile += "\t" + string.Format("{0:u}", DateTime.Now).Replace("Z", "") + ":" + string.Format("{0:d}", DateTime.Now.Millisecond);
            //    if (valueRAW != "") StringForBurnToFile += "\t" + valueRAW;
            //}
            StringForBurnToFile += LabDevice.ConvertFixedPoint(value, type) + "\t" + LabDevice.ConvertScientific(value, LabDevice.DataTypes.Abstract);
            StringForBurnToFile += "\t" + string.Format("{0:u}", DateTime.Now).Replace("Z", "") + ":" + string.Format("{0:d}", DateTime.Now.Millisecond);
            StringForBurnToFile += "\t" + DateTime.Now.TimeOfDay.TotalSeconds.ToString();
            StreamWriter.WriteLine(StringForBurnToFile);
            StreamWriter.Flush();
            //try
            //{
            //    if (MainParam_CounterMeasure % Slider_FragmentSize.Value == 0)
            //    {
            //        MainParam_StreamWriter.Flush();
            //        if (MainParam_CounterMeasure_End == 0)
            //        {
            //            TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
            //            //TextBlock_StatusBurn.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "\nОкончание через " + (MainParam_TimeEndMeasure - DateTime.Now).ToString("dd'.'hh':'mm':'ss");
            //        }
            //        else
            //        {
            //            TextBlock_Status.Text = "Записано значений " + (MainParam_CounterMeasure - MainParam_CounterMeasure_Start).ToString() + "   Окончание в ~" + DateTime.Now.AddMilliseconds((MainParam_CounterMeasure_End - MainParam_CounterMeasure) * MainParam_MillsBetweenMeasure.Average()).ToString("dd/MM/yyyy HH:mm:ss");
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    TextBlock_Status.Text = "Ошибка записи " + ex.ToString();
            //}

        }

        public static String ConvertScientific(decimal value, DataTypes type)
        {
            if (type == DataTypes.Abstract)
                return string.Format("{0:E}", value);
            if (type == DataTypes.Freq)
                return string.Format("{0:E}", value) + " Hz";
            if (type == DataTypes.Voltage)
                return string.Format("{0:E}", value) + " V";
            if (type == DataTypes.Resistance)
                return string.Format("{0:E}", value) + " Om";
            if (type == DataTypes.Capacity)
                return string.Format("{0:E}", value) + " F";
            if (type == DataTypes.Current)
                return string.Format("{0:E}", value) + " A";
            if (type == DataTypes.Temperature)
                return string.Format("{0:E}", value) + " °C";
            return "";
        }

        public static String ConvertFixedPoint(decimal value, DataTypes type)
        {
            if (type == DataTypes.Abstract)
                return string.Format("{0:0.#############}", value);
            else
            {
                string output = "";
                if (Math.Abs(value) < (decimal)1e12 && Math.Abs(value) >= (decimal)1e9) output = string.Format("{0:0.######}", value / (decimal)1e9) + " G";
                if (Math.Abs(value) < (decimal)1e9 && Math.Abs(value) >= (decimal)1e6) output = string.Format("{0:0.######}", value / (decimal)1e6) + " M";
                if (Math.Abs(value) < (decimal)1e6 && Math.Abs(value) >= (decimal)1e3) output = string.Format("{0:0.######}", value / (decimal)1e3) + " k";
                if (Math.Abs(value) < (decimal)1e3 && Math.Abs(value) >= 1) output = string.Format("{0:0.######}", value) + " ";
                if (Math.Abs(value) < 1 && Math.Abs(value) >= (decimal)1e-3) output = string.Format("{0:0.######}", value * (decimal)1e3) + " m";
                if (Math.Abs(value) < (decimal)1e-3 && Math.Abs(value) >= (decimal)1e-6) output = string.Format("{0:0.######}", value * (decimal)1e6) + " u";
                if (Math.Abs(value) < (decimal)1e-6 && Math.Abs(value) >= (decimal)1e-9) output = string.Format("{0:0.######}", value * (decimal)1e9) + " n";
                if (Math.Abs(value) < (decimal)1e-9 && Math.Abs(value) >= (decimal)1e-12) output = string.Format("{0:0.######}", value * (decimal)1e12) + " p";
                if (value == 0) output = "0 ";
                if (type == DataTypes.Freq) return output += "Hz";
                if (type == DataTypes.Voltage) return output += "V";
                if (type == DataTypes.Resistance) return output += "Om";
                if (type == DataTypes.Capacity) return output += "F";
                if (type == DataTypes.Current) return output += "A";
                if (type == DataTypes.Temperature) return output += "°C";
            }
            return "";
        }

        //public static String ConvertNative(decimal value, DataTypes type)
        //{
        //    if (type == DataTypes.Freq)
        //    {
        //        if (value>=1000 && value<100000) return string.Format("{0:R}", value/1000) + " kHz";
        //    }
        //    return "";
        //}
    }
}
