using System;
using System.Collections.Concurrent;
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
        //DeviceManager.NewValueDelegate DelegateForNewValue;
        ConcurrentQueue<DeviceManager.MeasureStruct> QueueNewValues;
        public int DriverID;
        public Driver_VirtualDevice(ConcurrentQueue<DeviceManager.MeasureStruct> _QueueNewValuesGlobal, int _id)
        {
            QueueNewValues = _QueueNewValuesGlobal;
            //DelegateForNewValue = _valueDelegate;
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
            LabDevice.DataTypes type = LabDevice.DataTypes.Abstract;
            int mul = rnd.Next(-4, 4);
            int ofs = rnd.Next(1, 1000);
            int del = rnd.Next(100, 200);
            int counterForDisable = 50;
            while (true)
            {
                Thread.Sleep(del);
                counterForDisable--;
                if (counterForDisable < 25 && counterForDisable > 5)
                {
                    type = LabDevice.DataTypes.Abstract;
                    continue;
                }
                //if (counterForDisable == 0) break;

                value = ((decimal)Math.Sin(((double)DateTime.Now.Ticks + ofs*100000) / (63700* ofs)) * (decimal)Math.Pow(10,mul)) + (decimal)(ofs * Math.Pow(10, mul)); //(decimal)rnd.Next(10, 50)/10;
                
                if (type == LabDevice.DataTypes.Abstract) type = (LabDevice.DataTypes)Enum.GetValues(typeof(LabDevice.DataTypes)).GetValue(rnd.Next(Enum.GetValues(typeof(LabDevice.DataTypes)).Length));
                if (type == LabDevice.DataTypes.Abstract) type = (LabDevice.DataTypes)Enum.GetValues(typeof(LabDevice.DataTypes)).GetValue(rnd.Next(Enum.GetValues(typeof(LabDevice.DataTypes)).Length));

                //type = LabDevice.DataTypes.Voltage;
                //this.Dispatcher.Invoke(() => DelegateForNewValue(value));
                //DelegateForNewValue?.Invoke(value, type, DriverID);
                QueueNewValues.Enqueue(new DeviceManager.MeasureStruct { Val = value, Typ = type, TS = DateTime.Now , DrvID = DriverID});
                //System_serialDataQueue.Enqueue(buffer);
                //защищенный вызов лаб девайса
                
            }
        }
    }
}
