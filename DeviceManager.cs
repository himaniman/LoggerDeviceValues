using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Threading;
using HidSharp;

namespace LoggerDeviceValues
{
    public class DeviceManager
    {
        public struct MeasureStruct
        {
            public decimal Val;
            public LabDevice.DataTypes Typ;
            public DateTime TS;
            public int DrvID;
            public string RAW;
        }

        public Dictionary<int, LabDevice> Devices = new Dictionary<int, LabDevice>();
        //public List<Thread> ReadThreads;
        HidDevice[] HidDeviceList;
        //static public List<Driver_UT71D> Devices_UT71D = new List<Driver_UT71D>();
        //public List<Driver_UT61C> Devices_UT61C = new List<Driver_UT61C>();
        MainWindow.EventNewValueDelegate MainWindowEventNewValue;
        public Thread ThreadAwaitData_Discriptor;

        int DriversIDMax = 0;
        public List<Driver_VirtualDevice> Drivers_VirtualDevice = new List<Driver_VirtualDevice>();

        public ConcurrentQueue<MeasureStruct> QueueNewValues = new ConcurrentQueue<MeasureStruct>();

        //public delegate void NewValueDelegate(decimal value, LabDevice.DataTypes type, int _DriverId);

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
            //int CurrentIDSession = -1;
            //foreach (KeyValuePair<int, LabDevice> currentDev in Devices) CurrentIDSession = (currentDev.Value.IDTargetDriver == _DriverId) ? currentDev.Key : CurrentIDSession;
            //Devices[CurrentIDSession].NewValue(value, type);
            //MainWindowEventNewValue(value, type, DateTime.Now, CurrentIDSession);


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
                MeasureStruct measure;
                if (QueueNewValues.TryDequeue(out measure))
                {
                    int CurrentIDSession = -1;
                    foreach (KeyValuePair<int, LabDevice> currentDev in Devices) CurrentIDSession = (currentDev.Value.IDTargetDriver == measure.DrvID) ? currentDev.Key : CurrentIDSession;

                    if (CurrentIDSession > -1)
                    {
                        if (Devices[CurrentIDSession].DataType != measure.Typ)
                        {
                            //пришли данные нового типа, в старой нет данных, заменить старую сессию
                            if (Devices[CurrentIDSession].CounterMeasure == 0)
                            {
                                Devices[CurrentIDSession].DataType = measure.Typ;
                                Devices[CurrentIDSession].NewValue(measure);
                                MainWindowEventNewValue(measure.Val, measure.Typ, measure.TS, CurrentIDSession);
                            }
                            //пришли данные нового типа, но данных в старой слишком мало. удалить старую
                            else if (Devices[CurrentIDSession].CounterMeasure > 0 && Devices[CurrentIDSession].CounterMeasure < 10)
                            {
                                int NewIDSession = 1;
                                if (Devices.Count > 0) NewIDSession = Devices.Keys.Max() + 1;

                                Devices.Add(NewIDSession, new LabDevice(Devices[CurrentIDSession].DeviceName, measure.Typ, Devices[CurrentIDSession].IDTargetDriver));
                                Devices.Remove(CurrentIDSession);

                                Devices[NewIDSession].NewValue(measure);
                                MainWindowEventNewValue(measure.Val, measure.Typ, measure.TS, NewIDSession);
                            }
                            //Пришли новые данные другого типа, добавить новую сессию
                            else if (Devices[CurrentIDSession].CounterMeasure >= 10)
                            {
                                int NewIDSession = 1;
                                if (Devices.Count > 0) NewIDSession = Devices.Keys.Max() + 1;

                                Devices.Add(NewIDSession, new LabDevice(Devices[CurrentIDSession].DeviceName, measure.Typ, Devices[CurrentIDSession].IDTargetDriver));
                                Devices[CurrentIDSession].IDTargetDriver = -1;

                                Devices[NewIDSession].NewValue(measure);
                                MainWindowEventNewValue(measure.Val, measure.Typ, measure.TS, NewIDSession);
                            }
                        }
                        //просто пришли данные
                        else
                        {
                            if (!Devices[CurrentIDSession].ignore)
                            {
                                Devices[CurrentIDSession].NewValue(measure);
                                MainWindowEventNewValue(measure.Val, measure.Typ, measure.TS, CurrentIDSession);
                            }
                        }
                    }
                }
                foreach (int i in Devices.Keys.ToArray())
                {
                    if (Devices.ContainsKey(i))
                    {
                        if (Devices[i].active)
                        {
                            //случай если была пауза продилась как 3 раза * стандартное значение задержки между данными
                            //то значит надо создать новый лаб девайс чтобы новые данные если прийдут то лягут туды
                            if (Devices[i].MillsBetweenMeasure.Count >= 10 && !Devices[i].ignore)
                            {
                                if (Devices[i].PastMeasure +
                                    (TimeSpan.FromMilliseconds(Devices[i].MillsBetweenMeasure.Average() * 3) > TimeSpan.FromSeconds(2) ?
                                    TimeSpan.FromMilliseconds(Devices[i].MillsBetweenMeasure.Average() * 3) : TimeSpan.FromSeconds(2)) 
                                    < DateTime.Now)
                                {
                                    int NewIDSession = 1;
                                    if (Devices.Count > 0) NewIDSession = Devices.Keys.Max() + 1;

                                    Devices.Add(NewIDSession, new LabDevice(Devices[i].DeviceName, LabDevice.DataTypes.Abstract, Devices[i].IDTargetDriver));
                                    Devices[i].IDTargetDriver = -1;
                                    Devices[i].active = false;

                                    MainWindowEventNewValue(0, 0, DateTime.MinValue, i);

                                    break;
                                    //else
                                    //{
                                    //    //данные пришли меньше 10 штук, потом тишина. тип данных не менялись, просто вытащили штекер. те несколько значений будут висеть на экране?
                                    //    int NewIDSession = 1;
                                    //    if (Devices.Count > 0) NewIDSession = Devices.Keys.Max() + 1;

                                    //    Devices.Add(NewIDSession, new LabDevice(Devices[i].DeviceName, LabDevice.DataTypes.Abstract, Devices[i].IDTargetDriver));
                                    //    Devices.Remove(i);

                                    //    //MainWindowEventNewValue(0, 0, DateTime.MinValue, i);
                                    //    break;
                                    //}
                                }
                            }
                        }
                    }
                }
                    //for (int i = 0; i < Devices.Count; i++)
                    //{
                    //    if (!Devices.ElementAt(i).Value.QueueNewValues.IsEmpty)
                    //    {
                    //        LabDevice.MeasureStruct measure;
                    //        Devices.ElementAt(i).Value.QueueNewValues.TryDequeue(out measure);
                    //        if (measure.Typ != Devices.ElementAt(i).Value.DataType)
                    //        {
                    //            Devices.Add(Devices.Keys.Max() + 1, new LabDevice(Devices.ElementAt(i).Value, measure.Typ));
                    //            break;
                    //        }
                    //        else DelegateForNewValue(measure, Devices.ElementAt(i).Value);
                    //    }
                    //}
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.ToString());
                //}
            }
        }

        public void RemoveAndDisonnectDevice(int IDSession)
        {
            if (Devices[IDSession].active == false && Devices[IDSession].IDTargetDriver == -1)
            {
                Devices.Remove(IDSession);
                MainWindowEventNewValue(0, 0, DateTime.MinValue, -1);
            }
            else if (Devices[IDSession].active == true && Devices[IDSession].IDTargetDriver > -1)
            {

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
            int NewIDSession = 1;
            if (Devices.Count > 0) NewIDSession = Devices.Keys.Max() + 1;
            if (_Interface.Contains("Virtual"))
            {
                Devices.Add(NewIDSession, new LabDevice(LabDevice.SupportedDevices.Virtual));
                Drivers_VirtualDevice.Add(new Driver_VirtualDevice(QueueNewValues, DriversIDMax++));
                Drivers_VirtualDevice.Last().Connect();
                Devices[NewIDSession].IDTargetDriver = Drivers_VirtualDevice.Last().DriverID;
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
                
            return NewIDSession;
        }

    }
}
