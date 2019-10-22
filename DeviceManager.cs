using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using HidSharp;

namespace LoggerDeviceValues
{
    public class DeviceManager
    {
        static public List<LabDevice> Devices = new List<LabDevice>();
        //public List<Thread> ReadThreads;
        static HidDevice[] HidDeviceList;
        static List<Driver_UT71D> Devices_UT71D = new List<Driver_UT71D>();
        static List<Driver_UT61C> Devices_UT61C = new List<Driver_UT61C>();

        public static List<string> ScanAvilibleInterfaces()
        {
            List<string> Resoult = new List<string>();
            HidDeviceList = DeviceList.Local.GetHidDevices(vendorID: 6790, productID: 57352).ToArray();
            for (int i=0;i< HidDeviceList.Length;i++)
            {
                //Resoult.Add(i + " " + HidDeviceList[i].GetFriendlyName() + " (" + HidDeviceList[i].ReleaseNumber + ")");
                Resoult.Add(i + " - USB HID Device (" + "ver: "+HidDeviceList[i].ReleaseNumber + ")");
            }

            return Resoult;
        }

        public static bool ConnectToDeviceThroughInterface(string _Interface, string _Device)
        {
            if (_Interface.Split('-')[1].Contains("USB HID Device"))
            {
                if (_Device == LabDevice.SupportedDevices.UT71D.ToString())
                {
                    //Devices_UT71D.check for equals может до этого мы уже подключались, обработать
                    Devices.Add(new LabDevice(LabDevice.SupportedDevices.UT71D, LabDevice.DataTypes.Abstract));
                    Devices_UT71D.Add(new Driver_UT71D(Devices.ElementAt(Devices.Count-1))); //сюда бросить ид девайса для того чтобы он знал куда бросать данные при приеме
                    Devices_UT71D.ElementAt(Devices_UT71D.Count - 1).Connect(HidDeviceList[Int32.Parse(_Interface.Split(' ')[0])]);
                }
                if (_Device == LabDevice.SupportedDevices.UT61C.ToString())
                {
                    //Devices_UT71D.check for equals может до этого мы уже подключались, обработать
                    Devices.Add(new LabDevice(LabDevice.SupportedDevices.UT61C, LabDevice.DataTypes.Temperature));
                    Devices_UT61C.Add(new Driver_UT61C(Devices.ElementAt(Devices.Count - 1))); //сюда бросить ид девайса для того чтобы он знал куда бросать данные при приеме
                    Devices_UT61C.ElementAt(Devices_UT61C.Count - 1).Connect(HidDeviceList[Int32.Parse(_Interface.Split(' ')[0])]);
                }

            }
                
            return true;
        }
    }
}
