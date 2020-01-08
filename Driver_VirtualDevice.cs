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
        public enum TypesSignal
        {
            line, sin, noize
        };
        
        public Thread ThreadRead_Discriptor;
        ConcurrentQueue<DeviceManager.MeasureStruct> QueueNewValues;
        public int DriverID;

        public TypesSignal TypeSignal = TypesSignal.line;
        public LabDevice.DataTypes type;
        public double MeasPerMin;
        public double CoefMinY;
        public double CoefMaxY;
        public double CoefPulseWith;
        public double CoefSinOfsPulse;

        public Driver_VirtualDevice(ConcurrentQueue<DeviceManager.MeasureStruct> _QueueNewValuesGlobal, int _id)
        {
            QueueNewValues = _QueueNewValuesGlobal;
            DriverID = _id;

            Random rnd = new Random();
            MeasPerMin = rnd.Next(60,150);
            CoefMinY = rnd.Next(10, 40);
            CoefMaxY = rnd.Next(50, 100);
            CoefPulseWith = rnd.Next(4000, 6000);
            CoefSinOfsPulse = rnd.Next(50, 150);

            LabDevice.DataTypes type = (LabDevice.DataTypes)rnd.Next(Enum.GetValues(typeof(LabDevice.DataTypes)).Length);
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
            decimal value = 0;

            while (true)
            {
                Thread.Sleep((int)(1000 / (MeasPerMin/60)));
                double time = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds + CoefSinOfsPulse;
                

                if (TypeSignal == TypesSignal.line)
                {
                    if (time % CoefPulseWith <= CoefPulseWith / 2)
                    {
                        value = (decimal)(((CoefMinY - CoefMaxY) / (CoefPulseWith / 2)) * (time % CoefPulseWith - CoefPulseWith / 2) + CoefMinY);
                    }
                    else
                    {
                        value = (decimal)(-((CoefMinY - CoefMaxY) / (CoefPulseWith / 2)) * (time % CoefPulseWith - CoefPulseWith / 2) + CoefMinY);
                    }
                }

                if (TypeSignal == TypesSignal.sin)
                {
                    value = (decimal)((Math.Sin(((time % CoefPulseWith) / CoefPulseWith) * 2*Math.PI)/2 + 0.5) * Math.Abs(CoefMinY-CoefMaxY) + CoefMinY); //(decimal)rnd.Next(10, 50)/10;
                }

                if (TypeSignal == TypesSignal.noize)
                {
                    value = (decimal)(rnd.NextDouble() * Math.Abs(CoefMinY - CoefMaxY) + CoefMinY);
                }

                QueueNewValues.Enqueue(new DeviceManager.MeasureStruct { Val = value, Typ = type, TS = DateTime.Now , DrvID = DriverID, RAW = BitConverter.ToString((BitConverter.GetBytes((double)value))) });

            }
        }

    }
}
