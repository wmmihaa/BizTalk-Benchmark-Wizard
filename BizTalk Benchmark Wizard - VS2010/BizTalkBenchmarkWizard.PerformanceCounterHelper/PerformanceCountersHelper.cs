using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;

namespace BizTalkBenchmarkWizard.PerformanceCounterHelper
{
    public class PerformanceCounterLogger
    {
        PerformanceCounter _inCallTransmitCounter;
        PerformanceCounter _callTimeTransmitCounter;
        PerformanceCounter _totalCallTransmitCounter;
        PerformanceCounter _callPerSecondTransmitCounter;

        PerformanceCounter _inCallProcessedCounter;
        PerformanceCounter _callTimeProcessedCounter;
        PerformanceCounter _totalCallProcessedCounter;
        PerformanceCounter _callPerSecondProcessedCounter;

        public static long counterInCall;
        public enum ServiceType { Consumer, Server, Both }
        public PerformanceCounter CallPerSecondTransmitCounter
        {
            get { return _callPerSecondTransmitCounter; }
        }
        public PerformanceCounter CallPerSecondProcessedCounter
        {
            get { return _callPerSecondProcessedCounter; }
        }
        string counterCategory = "BizTalk Benchmark Wizard";

        public PerformanceCounterLogger(ServiceType serviceType)
        {
            this.CreateCategories();
            if(serviceType==ServiceType.Consumer)
                this.InitTransmitCounters();
            else if (serviceType == ServiceType.Server)
                this.InitProcessedCounters();
            else
            {
                this.InitTransmitCounters();
                this.InitProcessedCounters();
            }
        }
        void CreateCategories()
        {
            try
            {
                if (System.Diagnostics.PerformanceCounterCategory.Exists(counterCategory))
                    return;

                // Create a collection of type CounterCreationDataCollection.
                System.Diagnostics.CounterCreationDataCollection CounterDatas = new System.Diagnostics.CounterCreationDataCollection();
                // Create the counters and set their properties.

                System.Diagnostics.CounterCreationData cdCounter1 = new System.Diagnostics.CounterCreationData()
                { 
                    CounterName = "In Call",
                    CounterHelp = "Number of simultaneous messages submitted from the BizTalk Benchmark Wizard application",
                    CounterType = System.Diagnostics.PerformanceCounterType.NumberOfItems64
                };
                System.Diagnostics.CounterCreationData cdCounter2 = new System.Diagnostics.CounterCreationData()
                { 
                    CounterName = "Call Time",
                    CounterHelp = "Elasped time for call (msecs)",
                    CounterType = System.Diagnostics.PerformanceCounterType.NumberOfItems64
                };
                System.Diagnostics.CounterCreationData cdCounter3 = new System.Diagnostics.CounterCreationData()
                { 
                    CounterName = "Total Calls",
                    CounterHelp = "Total number of messages submitted from the BizTalk Benchmark Wizard application",
                    CounterType = System.Diagnostics.PerformanceCounterType.NumberOfItems64
                };
                System.Diagnostics.CounterCreationData cdCounter4 = new System.Diagnostics.CounterCreationData()
                {
                    CounterName = "Calls/sec",
                    CounterHelp = "Number of messages submitted from the BizTalk Benchmark Wizard application",
                    CounterType = System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32
                };
               
                // Add both counters to the collection.
                CounterDatas.Add(cdCounter1);
                CounterDatas.Add(cdCounter2);
                CounterDatas.Add(cdCounter3);
                CounterDatas.Add(cdCounter4);

                // Create the category and pass the collection to it.
                //if (System.Diagnostics.PerformanceCounterCategory.Exists(counterCategory))
                //    System.Diagnostics.PerformanceCounterCategory.Delete(counterCategory);

                PerformanceCounterCategory cat = System.Diagnostics.PerformanceCounterCategory.Create(
                    counterCategory,
                    counterCategory,
                    PerformanceCounterCategoryType.MultiInstance,
                    CounterDatas);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        void InitTransmitCounters()
        {
            _inCallTransmitCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "In Call",
                MachineName = ".",
                InstanceName="Message Transmitter",
                ReadOnly = false
            };
            _callTimeTransmitCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Call Time",
                MachineName = ".",
                InstanceName = "Message Transmitter",
                ReadOnly = false
            };
            _totalCallTransmitCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Total Calls",
                MachineName = ".",
                InstanceName = "Message Transmitter",
                ReadOnly = false
            };
            _callPerSecondTransmitCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Calls/sec",
                MachineName = ".",
                InstanceName = "Message Transmitter",
                ReadOnly = false
            };   
        }
        void InitProcessedCounters()
        {
            _inCallProcessedCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "In Call",
                MachineName = ".",
                InstanceName = "Message Processor",
                ReadOnly = false
            };
            _callTimeProcessedCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Call Time",
                MachineName = ".",
                InstanceName = "Message Processor",
                ReadOnly = false
            };
            _totalCallProcessedCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Total Calls",
                MachineName = ".",
                InstanceName = "Message Processor",
                ReadOnly = false
            };
            _callPerSecondProcessedCounter = new PerformanceCounter()
            {
                CategoryName = counterCategory,
                CounterName = "Calls/sec",
                MachineName = ".",
                InstanceName = "Message Processor",
                ReadOnly = false
            };
        }
        public void UpdateTransmitEntryCounters()
        {
            _inCallTransmitCounter.Increment();
            _totalCallTransmitCounter.Increment();
            _callPerSecondTransmitCounter.Increment();
        }
        public void UpdateTransmitExitCounters()
        {
            _inCallTransmitCounter.Decrement();
        }
        public void UpdateProcessedEntryCounters()
        {
            _inCallProcessedCounter.Increment();
            _totalCallProcessedCounter.Increment();
            _callPerSecondProcessedCounter.Increment();
        }
        public void UpdateProcessedExitCounters()
        {
            _inCallProcessedCounter.Decrement();
        }

    }
   
}
