﻿using SPIPware.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static SPIPware.Communication.PeripheralControl;

namespace SPIPware
{
    partial class MainWindow
    {
        MachineControl machineControl = new MachineControl();
        private decimal targetLocation = 0;
        private int currentIndex = 0;
        public static bool runningCycle = false;
        private bool firstRun = true;

        bool foundPlate = false;
        bool growLightsOn = false;

        private List<int> imagePositions = new List<int>();

        private void updatePositionList()
        {
            imagePositions = new List<int>();
            for (var i = 0; i < checkBoxes.Count; i++)
            {
                if (checkBoxes[i].IsChecked == true)
                {
                    imagePositions.Add(i);
                }
            }

        }
        private bool FindCheckedBox(int index, bool firstBox)
        {
            if (!firstBox)
            {
                index++;
            }
            while (index < Properties.Settings.Default.NumLocations)
            {

                if (checkBoxes[index].IsChecked == true)
                {
                    Console.WriteLine("Box " + (index + 1) + " checked");
                    currentIndex = index;
                    return true;
                }
                else
                {
                    index++;
                }

            }
            Console.WriteLine("No more boxes checked");
            return false;
        }
        public void startCycle()
        {
            //if (machine.Connected)
            {

                btnRunCycle.IsEnabled = false;
                if (runningCycle) stopCycle();
                runningCycle = true;
                updateCycleStatus(runningCycle);
                currentIndex = 0;
                bool isPlateFound = FindCheckedBox(currentIndex, true);
                if (!isPlateFound) return;

                if (firstRun)
                {
                    machine.SendLine("$H");
                    peripheralControl.SetLight(Peripheral.GrowLight,false, false);
                    //Properties.Settings.Default.CurrentPlate = 1;
                    firstRun = false;
                }
                else
                {
                    peripheralControl.SetLight(Peripheral.Backlight, true);
                    peripheralControl.SetLight(Peripheral.GrowLight, false, false);
                    Thread.Sleep(300);
                    targetLocation = machine.sendMotionCommand(currentIndex);
                }
            }
            //else
            //{
            //    Machine_NonFatalException("Machine not connected");
            //}


        }
        public void endCycle()
        {
            runningCycle = false;
            updateCycleStatus(runningCycle);
            currentIndex = 0;
            //machine.sendMotionCommand(0);
            machine.SendLine("$H");
            enableMachineControlButtons();
            peripheralControl.SetLight(Peripheral.Backlight, false);
            peripheralControl.SetLight(Peripheral.GrowLight, true, !isNightTime());
        }
        public void stopCycle()
        {
            endCycle();
            if (machine.Connected)
            {
                machine.SoftReset();
            }

        }
        public void checkCycle()
        {

            if (runningCycle)
            {
                if (machine.Status == "Home")
                {
                    //cameraControl.home = true;
                    //cameraControl.firstRun = false;
                    peripheralControl.SetLight(Peripheral.Backlight, true);
                    //Thread.Sleep(300);
                    targetLocation = machine.sendMotionCommand(currentIndex);
                }

                else if (machine.WorkPosition.X == (double)targetLocation && machine.Status == "Idle")
                {
                    Console.WriteLine("Current Index" + currentIndex);
                    peripheralControl.SetLight(Peripheral.Backlight, true);
                    cameraControl.CapSaveImage();
                    if (Properties.Settings.Default.CurrentPlate < Properties.Settings.Default.TotalPlates)
                    {
                        Properties.Settings.Default.CurrentPlate++;
                    }
                    else
                    {
                        Properties.Settings.Default.CurrentPlate = 1;
                    }

                    //Console.WriteLine("Found Plate: " + foundPlate);
                    foundPlate = FindCheckedBox(currentIndex, false);
                    if (foundPlate)
                    {
                        targetLocation = machine.sendMotionCommand(currentIndex);
                    }
                    else endCycle();

                }

            }
            else
            {
                return;
            }
        }
        public void updateCycleStatus(bool cycleRunning)
        {
            if (!cycleRunning)
            {
                enableMachineControlButtons();
                cycleStatusIcon.Source = YELLOW_IMAGE;
            }
            else
            {
                cycleStatusIcon.Source = GREEN_IMAGE;
            }

            cycleStatus.Text = parseStatus(cycleRunning);
            toggleButtonVisibility(btnRunCycle, btnStopCycle, cycleRunning);

        }
        //public void toggleImageCycleButton(bool running)
        //{
        //    if (running)
        //    {
        //        btnRunCycle.Visibility = Visibility.Collapsed;
        //        btnStopCycle.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        btnRunCycle.Visibility = Visibility.Visible;
        //        btnStopCycle.Visibility = Visibility.Collapsed;
        //    }

        //}
        private void disableMachineControlButtons()
        {
            btnMove.IsEnabled = false;
            btnRunCycle.IsEnabled = false;
            btnRunTimeLapse.IsEnabled = false;
            btnTestFirst.IsEnabled = false;
            btnTestBetween.IsEnabled = false;
        }
        private void enableMachineControlButtons()
        {
            btnMove.IsEnabled = true;
            btnRunCycle.IsEnabled = true;
            btnRunTimeLapse.IsEnabled = true;
            btnTestFirst.IsEnabled = true;
            btnTestBetween.IsEnabled = true;
        }

        private void BtnTestFirst_Click(object sender, RoutedEventArgs e)
        {
            machine.sendMotionCommand(0);

        }
        private void btnTestBetween_Click(object sender, RoutedEventArgs e)
        {
            machine.sendMotionCommand(1);
        }
    }
}