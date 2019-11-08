﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using HidSharp;

namespace LoggerDeviceValues
{
    public class DeviceManager
    {
        //public struct MeasureStruct
        //{
        //    public decimal Val;
        //    public LabDevice.DataTypes Typ;
        //    public DateTime TS;
        //}

        public Dictionary<int, LabDevice> Devices = new Dictionary<int, LabDevice>();
        //public List<Thread> ReadThreads;
        HidDevice[] HidDeviceList;
        //static public List<Driver_UT71D> Devices_UT71D = new List<Driver_UT71D>();
        //public List<Driver_UT61C> Devices_UT61C = new List<Driver_UT61C>();
        MainWindow.EventNewValueDelegate MainWindowEventNewValue;
        public Thread ThreadAwaitData_Discriptor;

        int DriversIDMax = 0;
        public List<Driver_VirtualDevice> Drivers_VirtualDevice;

        //public ConcurrentQueue<MeasureStruct> QueueNewValues = new ConcurrentQueue<MeasureStruct>();

        public delegate void NewValueDelegate(decimal value, LabDevice.DataTypes type, int _DriverId);

        public DeviceManager(MainWindow.EventNewValueDelegate _delegate)
        {
            MainWindowEventNewValue = _delegate;
            ThreadAwaitData_Discriptor = new Thread(AwaitData);
            ThreadAwaitData_Discriptor.Start();
        }

        public void NewValue(decimal value, LabDevice.DataTypes type, int _DriverId)
        {
            //Debug.WriteLine(value);
            //if (type != DataType) DeviceManager
            ///QueueNewValues.Enqueue(new LabDevice.MeasureStruct { Val = value, Typ = type, TS = DateTime.Now });


            MainWindowEventNewValue(value, type, DateTime.Now, );


            //записать себе, чтобы потом с другого потока кто то мог взять эти данные, + отправить текущие данные в хост приложение
            //AddValueToFile(value, LabDevice.DataTypes.Abstract, "");
            //graph
            //file
            //MainWindow.System_AddValueToGraph(value, DataTypes.Abstract);
        }

        public void AwaitData()
        {
            while (true)
            {
                try
                {
                    for (int i = 0; i < Devices.Count; i++)
                    {
                        if (!Devices.ElementAt(i).Value.QueueNewValues.IsEmpty)
                        {
                            LabDevice.MeasureStruct measure;
                            Devices.ElementAt(i).Value.QueueNewValues.TryDequeue(out measure);
                            if (measure.Typ != Devices.ElementAt(i).Value.DataType)
                            {
                                Devices.Add(Devices.Keys.Max() + 1, new LabDevice(Devices.ElementAt(i).Value, measure.Typ));
                                break;
                            }
                            else DelegateForNewValue(measure, Devices.ElementAt(i).Value);
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public List<string> ScanAvilibleInterfaces()
        {
            List<string> Resoult = new List<string>();
            HidDeviceList = DeviceList.Local.GetHidDevices(vendorID: 6790, productID: 57352).ToArray();
            for (int i=0;i< HidDeviceList.Length;i++)
            {
                //Resoult.Add(i + " " + HidDeviceList[i].GetFriendlyName() + " (" + HidDeviceList[i].ReleaseNumber + ")");
                Resoult.Add(i + " - USB HID Device (" + "ver: "+HidDeviceList[i].ReleaseNumber + ")");
            }
            Resoult.Add("Virtual");
            return Resoult;
        }

        public List<string> ScanAvilibleDevicesOnInterface(string _Interface)
        {
            List<string> Resoult = new List<string>();
            if (_Interface.Contains("Virtual"))
            {
                Resoult.Add("Virtual");
            }
            if (_Interface.Contains("USB HID Device"))
            {
                Resoult.Add(LabDevice.SupportedDevices.UT71D.ToString());
                Resoult.Add(LabDevice.SupportedDevices.UT61C.ToString());
            }
            return Resoult;
        }

        public int ConnectToDeviceThroughInterface(string _Interface, string _Device)
        {
            int NewIDDevice = 1;
            if (Devices.Count > 0) NewIDDevice = Devices.Keys.Max() + 1;
            if (_Interface.Contains("Virtual"))
            {
                Devices.Add(NewIDDevice, new LabDevice(LabDevice.SupportedDevices.Virtual));
                Drivers_VirtualDevice.Add(new Driver_VirtualDevice(NewValue, DriversIDMax++));
                Drivers_VirtualDevice.Last().Connect();
            }
            if (_Interface.Contains("USB HID Device"))
            {
                //if (_Device == LabDevice.SupportedDevices.UT71D.ToString())
                //{
                //    //Devices_UT71D.check for equals может до этого мы уже подключались, обработать
                //    Devices.Add(new LabDevice(LabDevice.SupportedDevices.UT71D, LabDevice.DataTypes.Abstract));
                //    //Devices_UT71D.Add(new Driver_UT71D()); //сюда бросить ид девайса для того чтобы он знал куда бросать данные при приеме
                //    //Devices_UT71D.ElementAt(Devices_UT71D.Count - 1).Connect(HidDeviceList[Int32.Parse(_Interface.Split(' ')[0])]);
                //}
                //if (_Device == LabDevice.SupportedDevices.UT61C.ToString())
                //{
                //    //Devices_UT71D.check for equals может до этого мы уже подключались, обработать
                //    Devices.Add(new LabDevice(LabDevice.SupportedDevices.UT61C, LabDevice.DataTypes.Temperature));
                //    Devices_UT61C.Add(new Driver_UT61C(Devices.ElementAt(Devices.Count - 1))); //сюда бросить ид девайса для того чтобы он знал куда бросать данные при приеме
                //    Devices_UT61C.ElementAt(Devices_UT61C.Count - 1).Connect(HidDeviceList[Int32.Parse(_Interface.Split(' ')[0])]);
                //}

            }
                
            return NewIDDevice;
        }

    }
}
