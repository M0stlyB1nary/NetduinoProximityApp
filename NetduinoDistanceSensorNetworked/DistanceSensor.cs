using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace LanderNetduino
{
    class DistanceSensor
    {
        private OutputPort portOut;
        private InterruptPort interIn;
        private long beginTick;
        private long endTick;
        private long minTicks;  // System latency, subtracted off ticks to find actual sound travel time
        private double inchConversion;
        //private double version;

        /// <summary>
        /// Sets up the HC-SR04 distance sensor with the Trig and Echo pins
        /// </summary>
        /// <param name="pinTrig"></param>
        /// <param name="pinEcho"></param>
        public DistanceSensor(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
        {
            portOut = new OutputPort(pinTrig, false);
            interIn = new InterruptPort(pinEcho, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            interIn.OnInterrupt += new NativeEventHandler(interIn_OnInterrupt);
            minTicks = 6200L;
            inchConversion = 1440.0;
        }

        /// <summary>
        /// This interrupt will trigger when detector receives back reflected sonic pulse       
        /// </summary>
        /// <param name="data1">Not used</param>
        /// <param name="data2">Not used</param>
        /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
        void interIn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            // Save the ticks when pulse was received back
            endTick = time.Ticks;
        }

        /// <summary>
        /// Trigger a sensor reading
        /// Convert ticks to distance using TicksToInches below
        /// </summary>
        /// <returns>inches</returns>
        public double Ping()
        {
            // Reset Sensor
            portOut.Write(true);
            Thread.Sleep(1);

            // Start Clock
            endTick = 0L;
            beginTick = System.DateTime.Now.Ticks;
            // Trigger Sonic Pulse
            portOut.Write(false);

            // Wait 1/20 second (this could be set as a variable instead of constant)
            Thread.Sleep(50);

            if (endTick > 0L)
            {
                // Calculate Difference
                long elapsed = endTick - beginTick;

                // Subtract out fixed overhead (interrupt lag, etc.)
                elapsed -= minTicks;
                if (elapsed < 0L)
                {
                    elapsed = 0L;
                }

                // Return inches
                return TicksToInches(elapsed);
            }

            // Sonic pulse wasn't detected within 1/20 second
            //return -1L; **ticks**
            return 0;
        }

        #region GettersandSetters
        /// <summary>
        /// The system latency (minimum number of ticks)
        /// This number will be subtracted off to find actual sound travel time
        /// </summary>
        public long LatencyTicks
        {
            get
            {
                return minTicks;
            }
            set
            {
                minTicks = value;
            }
        }

        /// <summary>
        /// The ticks to inches conversion factor
        /// </summary>
        public double InchCoversionFactor
        {
            get
            {
                return inchConversion;
            }
            set
            {
                inchConversion = value;
            }
        }

        /// <summary>
        /// Convert ticks to inches
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public double TicksToInches(long ticks)
        {
            return (double)ticks / inchConversion;
        }
        #endregion
    }
}
