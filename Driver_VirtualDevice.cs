using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class Driver_VirtualDevice
    {
        public Thread ThreadRead_Discriptor;
        DeviceManager.NewValueDelegate DelegateForNewValue;
        public int DriverID;
        public Driver_VirtualDevice(DeviceManager.NewValueDelegate _valueDelegate, int _id)
        {
            DelegateForNewValue = _valueDelegate;
            DriverID = _id;
        }
        //public Driver_VirtualDevice(Driver_VirtualDevice oldDriver)
        //{
        //    DelegateForNewValue = oldDriver.DelegateForNewValue;
        //    oldDriver.DelegateForNewValue = null;
        //    ThreadRead_Discriptor = oldDriver.ThreadRead_Discriptor;
        //    oldDriver.Disconnect();
        //}
        public bool Connect()
        {
            ThreadRead_Discriptor = new Thread(ReadDataVirtual);
            ThreadRead_Discriptor.Start();
            return true;
        }
        public void Disconnect()
        {
            ThreadRead_Discriptor.Abort();
        }
        
        void ReadDataVirtual()
        {
            Random rnd = new Random();
            decimal value;
            LabDevice.DataTypes type;
            while (true)
            {
                Thread.Sleep(300);
                value = (decimal)rnd.Next(10, 50)/10;
                type = LabDevice.DataTypes.Voltage;
                //this.Dispatcher.Invoke(() => DelegateForNewValue(value));
                DelegateForNewValue?.Invoke(value, type, DriverID);
                //System_serialDataQueue.Enqueue(buffer);
                //защищенный вызов лаб девайса
            }
        }
    }
}
