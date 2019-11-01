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
        LabDevice.NewValueDelegate DelegateForNewValue;
        public Driver_VirtualDevice(LabDevice.NewValueDelegate _valueDelegate)
        {
            DelegateForNewValue = _valueDelegate;
        }
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
                type = LabDevice.DataTypes.Abstract;
                //this.Dispatcher.Invoke(() => DelegateForNewValue(value));
                DelegateForNewValue(value, type);
                //System_serialDataQueue.Enqueue(buffer);
                //защищенный вызов лаб девайса
            }
        }
    }
}
