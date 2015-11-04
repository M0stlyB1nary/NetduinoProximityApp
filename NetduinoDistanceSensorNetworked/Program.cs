using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
//using SecretLabs.NETMF.Hardware;
using System.IO;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Net;
using System.Net.Sockets;
using MicroLiquidCrystal;


namespace LanderNetduino
{
    public class Program
    {
        

        public static void Main()
        {
            #region Netduino Pin Setup
            //InputPort mainButton = new InputPort(Pins.ONBOARD_BTN, false, Port.ResistorMode.Disabled);
            OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false);
            OutputPort greenLed = new OutputPort(Pins.GPIO_PIN_D4, false);
            OutputPort yellowLed = new OutputPort(Pins.GPIO_PIN_D3, false);
            OutputPort redLed = new OutputPort(Pins.GPIO_PIN_D2, false);
            DistanceSensor sensor = new DistanceSensor(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1);
            //Setup Display
            var lcdProvider = new MicroLiquidCrystal.GpioLcdTransferProvider(Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10, Pins.GPIO_PIN_D11, Pins.GPIO_PIN_D12);
            //var lcd = new Lcd(lcdProvider);
            Display.DisplaySetup(lcdProvider);
            #endregion

            //Program Start blink
            BlinkLED(onboardLED, 100, 100, 3);
            bool buttonState = false;
            
            //set time
            //var result = Ntp.UpdateTimeFromNtpServer("nist.time.nosc.us", -5);  // Central Daylight Time
            var result = Ntp.UpdateTimeFromNtpServer("time.nist.gov", -5);  // Central Daylight Time
            Debug.Print(result ? "Time successfully updated" : "Time not updated");

            //while (true)
            //{
                Display.DisplayMessage("Server Start", "Time: " + System.DateTime.Now.Hour.ToString() + ":" + System.DateTime.Now.Minute.ToString() + ":" + System.DateTime.Now.Second.ToString());
                //Thread.Sleep(1000);
            //}


            //Kick off the webserver on it's own thread.
            ThreadStart delegateWebMain = new ThreadStart(WebServerThreadMain);
            Thread threadWorker = new Thread(delegateWebMain);
            threadWorker.Start();


            //while (true)
            //{
            //    buttonState = mainButton.Read();
            //    BlinkLED(redLed, 30, 30, 3);
            //}
            
            //Clear log file
            File.Delete(@"SD\ProximityLog.csv");
            logEvent("Start");

            


            double distanceInInches = 0;
            //int tempo = 25;
            while (true)
            {
                // Ping and get inches
                distanceInInches = sensor.Ping();
                // Do something fancy
                if (distanceInInches > 0)
                {
                    if (distanceInInches < 5)
                    {
                        BlinkLED(redLed, 30, 30, 3);
                        logEvent("Red");
                    }
                    if (distanceInInches >= 5 && distanceInInches < 15)
                    {
                        BlinkLED(yellowLed, 50, 50, 2);
                        logEvent("Yellow");
                    }
                    if (distanceInInches >= 15 && distanceInInches < 25)
                    {
                        BlinkLED(greenLed, 75, 0, 1);
                        logEvent("Green");
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        //Threads and such
        private static void WebServerThreadMain()
        {
            int port = 80;
            Thread.Sleep(5000);
            //display the ip
            Microsoft.SPOT.Net.NetworkInformation.NetworkInterface nic = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];
            nic.EnableStaticIP("192.168.0.80", "255.255.255.0", "192.168.0.1");
            
            Debug.Print("My IP is: " + nic.IPAddress.ToString());
            //Display.DisplayMessage("My IP is: ", nic.IPAddress.ToString()); 



            //Microsoft.SPOT.Hardware.Utility.SetLocalTime(

            Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint listenerEndPoint = new IPEndPoint(IPAddress.Any, port);
            listenerSocket.Bind(listenerEndPoint);
            listenerSocket.Listen(5);

            while (true)
            {
                Socket clientSocket = listenerSocket.Accept();
                bool dataReady = clientSocket.Poll(5000000, SelectMode.SelectRead);
                if (dataReady && clientSocket.Available > 0)
                {
                    byte[] buffer = new byte[clientSocket.Available];
                    int bytesRead = clientSocket.Receive(buffer);
                    string request = new string(System.Text.Encoding.UTF8.GetChars(buffer));
                    if (request.IndexOf("ON") >= 0)
                    {
                        //Show the contents of the SD card.
                        String logOutput = ShowLogContents();
                        string response =
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Type: text/html; charset=utf-8\r\n\r\n" +
                            "<html><head><title>Netduino Plus LED Sample</title></head>" +
                            "<body>Log: <ul>" + logOutput + "</ul></body></html>";
                        clientSocket.Send(System.Text.Encoding.UTF8.GetBytes(response));
                    }
                    else
                    {
                        string response =
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Type: text/html; charset=utf-8\r\n\r\n" +
                            "<html><head><title>Netduino Log</title></head>" +
                            "<body>Log: <ul>" + "All Your Base Are Belong to Us" + "</ul></body></html>";
                                                clientSocket.Send(System.Text.Encoding.UTF8.GetBytes(response));
                    }
                    clientSocket.Close();
                }
            }
        }

        private static void BlinkLED(OutputPort led, int onTime, int offTime, int blinkNum)
        {
            for (int i = 0; i < blinkNum; i++)
            {
                led.Write(true); //Turn on LED
                Thread.Sleep(onTime);
                led.Write(false); //Turn off LED
                Thread.Sleep(offTime);
            }
        }

        private static string ShowLogContents()
        {
            DirectoryInfo directory = new DirectoryInfo(@"\SD\");
            string line = "";           
            string logContents = "";
            if (directory.Exists)
            {
                //Debug.Print(directory.FullName);
                foreach (FileInfo file in directory.GetFiles())
                {
                    //Debug.Print(file.FullName);
                    using (StreamReader sr = new StreamReader(file.FullName))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            Debug.Print(line);
                            logContents = logContents + "<li>" + line + "</li>";
                        }
                    }
                }

                //foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                //{
                //    RecurseFolders(subDirectory);
                //}
            }
            return logContents;
        }

        private static void logEvent(String EventName)
        {
            StreamWriter sw = new StreamWriter(@"SD\ProximityLog.csv", true);
            sw.WriteLine(EventName + "," + System.DateTime.Now.ToString());
            sw.Close();
        }


    }
}