﻿using System;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;


namespace SeawayChecker
{
    /// <summary>
    /// UWP app background task
    /// </summary>
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        private const int GREEN_LED_PIN = 3;
        private const int RED_LED_PIN = 6;
        private GpioPin greenPin;
        private GpioPin redPin;

        private ThreadPoolTimer timer;

        private const string url = "http://www.greatlakes-seaway.com/R2/jsp/mMaiBrdgStatus.jsp?language=E";
        private const string availabilityNode = "/html/body/div/div[2]/ul/li[2]/span[3]/font";
        private bool isOpen;

        /// <summary>
        /// Runs the main app
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            InitGPIO();
            GetStatus(null);
            timer = ThreadPoolTimer.CreatePeriodicTimer(GetStatus, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Initializes the GPIO pins for the LEDs
        /// </summary>
        private void InitGPIO()
        {
            greenPin = GpioController.GetDefault().OpenPin(GREEN_LED_PIN);
            greenPin.Write(GpioPinValue.High); // OFF
            greenPin.SetDriveMode(GpioPinDriveMode.Output);

            redPin = GpioController.GetDefault().OpenPin(RED_LED_PIN);
            redPin.Write(GpioPinValue.High); // OFF
            redPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        /// <summary>
        /// Gets html from the website and parses it to get the bridge status
        /// </summary>
        /// <param name="timer"></param>
        private async void GetStatus(ThreadPoolTimer timer)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(url);
            int nCharactersToRemove = 622; // Text to remove before start of html node
            html = html.Substring(nCharactersToRemove);

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);
            var htmlNode = htmlDoc.DocumentNode.SelectSingleNode(availabilityNode); 
            string availability = htmlNode.InnerText.Trim();

            if (availability == "Available")
                isOpen = true;
            else
                isOpen = false;

            UpdateLEDs();
        }

        /// <summary>
        /// Updates the LEDs according to the bridge status.
        /// </summary>
        private void UpdateLEDs()
        {
            GpioPinValue on = GpioPinValue.Low;
            GpioPinValue off = GpioPinValue.High;

            if (isOpen)
            {
                greenPin.Write(on); // Green ON
                redPin.Write(off); // Red OFF
            }
            else
            {
                redPin.Write(on); // Green ON
                greenPin.Write(off); // Red OFF
            }   
        }
    }
}

