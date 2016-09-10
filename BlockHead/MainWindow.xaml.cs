//--------------------------------------------------------------------------------------
// Copyright 2014-2015 Intel Corporation
// All Rights Reserved
//
// Permission is granted to use, copy, distribute and prepare derivative works of this
// software for any purpose and without fee, provided, that the above copyright notice
// and this statement appear in all copies.  Intel makes no representations about the
// suitability of this software for any purpose.  THIS SOFTWARE IS PROVIDED "AS IS."
// INTEL SPECIFICALLY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, AND ALL LIABILITY,
// INCLUDING CONSEQUENTIAL AND OTHER INDIRECT DAMAGES, FOR THE USE OF THIS SOFTWARE,
// INCLUDING LIABILITY FOR INFRINGEMENT OF ANY PROPRIETARY RIGHTS, AND INCLUDING THE
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  Intel does not
// assume any responsibility for any errors which may appear in this software nor any
// responsibility to update it.
//--------------------------------------------------------------------------------------
// September 2016, Revised & Modified by Alex Milman
// as part of a Tel-Aviv Academic College Project. 
//--------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HeadControlApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int m_numRows = 5; // set to initial defualt value
        private const int m_numCols = 3; // set to initial defualt value

        private Thread processingThread;
        private PXCMSenseManager senseManager;
        private PXCMFaceModule face;
        private PXCMFaceConfiguration faceConfiguration;
        private PXCMFaceConfiguration.ExpressionsConfiguration expressionConfiguration;
        private List<System.Windows.Shapes.Rectangle> m_sections = new List<System.Windows.Shapes.Rectangle>();
        private List<String> m_websites = new List<string>(){ 
            "www.google.com", 
            "www.youtube.com", 
            "www.facebook.com", 
            "www.twitter.com",
            "www.ebay.com"
        };
        private int m_slectedRectIndex = 0;
        private Int32 numberTrackedFaces;
        private int faceRectangleHeight;
        private int faceRectangleWidth;
        private int faceRectangleX;
        private int faceRectangleY;
        private float faceAverageDepth;
        private float headRoll;
        private float headPitch;
        private float headYaw;
        private const int TotalExpressions = 5;
        private int[] expressionScore = new int[TotalExpressions];
        SolidColorBrush m_blackBrush = new SolidColorBrush();
        SolidColorBrush m_blueBrush = new SolidColorBrush();
        SolidColorBrush m_whiteBrush = new SolidColorBrush();
        System.Drawing.Point m_mouseCurrPoint = new System.Drawing.Point();
        ProcessStartInfo m_chromeBrowserInfo;
        bool m_isChromeRunning = false;

        private enum FaceExpression
        {
            None,
            Kiss,
            Open,
            Smile,
            Tongue
        };

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        public MainWindow()
        {
            InitializeComponent();

            senseManager = PXCMSenseManager.CreateInstance();

            senseManager.EnableFace();

            if (pxcmStatus.PXCM_STATUS_INIT_FAILED == senseManager.Init()) 
            {
                System.Console.WriteLine("INIT FAILED!");
            }

            initBrushes();

            createRectangles();

            AutoClosingMessageBox.Show("Tilt Your Head and Blow a Kiss To Select a Website", "Welcome To HCA!", 7000);

            ConfigureFaceTracking();

            processingThread = new Thread(new ThreadStart(ProcessingThread));

            processingThread.Start();
        }


        private void initBrushes()
        {
            m_blackBrush.Color = Colors.Black;
            m_blueBrush.Color = Colors.Aqua;
            m_whiteBrush.Color = Colors.White;

            m_whiteBrush.Opacity = 0.9;
            m_blackBrush.Opacity = 0.7;
            m_blueBrush.Opacity = 0.5;
        }

        private void createRectangles()
        {
            int height = (int)System.Windows.SystemParameters.PrimaryScreenHeight / m_websites.Count;
            int width = (int)System.Windows.SystemParameters.PrimaryScreenWidth / m_numCols;

            int xPos = (int)System.Windows.SystemParameters.PrimaryScreenWidth / 3;
            int yPos = 0;

            for (int i = 0; i < m_numRows; i++)
            {

                m_sections.Add(createRectangle(width, height, xPos, yPos));

                Text(xPos + 40, yPos + (height / 2), m_websites[i], System.Windows.Media.Color.FromRgb(0, 120, 70));

                yPos += height;
            }
            setSelected(0);
        }


        private void Text(double x, double y, string text, System.Windows.Media.Color color)
        {

            TextBlock textBlock = new TextBlock();

            textBlock.Text = text;

            textBlock.Foreground = new SolidColorBrush(color);

            textBlock.FontSize = 35;

            textBlock.FontWeight = System.Windows.FontWeights.UltraBold;

            textBlock.TextAlignment = TextAlignment.Center;

            Canvas.SetLeft(textBlock, x);

            Canvas.SetTop(textBlock, y);

            canvas.Children.Add(textBlock);

        }
        
        private System.Windows.Shapes.Rectangle createRectangle(int i_width, int i_height, int xPos, int yPos)
        {
            // Create a Rectangle
            System.Windows.Shapes.Rectangle blueRectangle = new System.Windows.Shapes.Rectangle();
            blueRectangle.Height = i_height;
            blueRectangle.Width = i_width;
            Canvas.SetLeft(blueRectangle, xPos);
            Canvas.SetTop(blueRectangle, yPos);

            // Set Rectangle's width and color
            blueRectangle.StrokeThickness = 3;
            blueRectangle.Stroke = m_blackBrush;
            // Fill rectangle with color
            blueRectangle.Fill = m_blueBrush;

            // Add Rectangle to the Grid.
            canvas.Children.Add(blueRectangle);

            return blueRectangle;
        }

        private void setSelected(int i_slectedIndex)
        {
            this.Dispatcher.Invoke(() =>
            {

                // restore the last selected rect
                m_sections[m_slectedRectIndex].Fill = m_blueBrush;

                // fill the selected rect
                m_sections[i_slectedIndex].Fill = m_whiteBrush;
                m_slectedRectIndex = i_slectedIndex; // update index
                System.Drawing.Point tempPoint = getRectCenter(m_sections[m_slectedRectIndex]);
                SetCursorPos(tempPoint.X, tempPoint.Y);
                m_mouseCurrPoint.X = tempPoint.X;
                m_mouseCurrPoint.Y = tempPoint.Y;
                System.Threading.Thread.Sleep(500);
            });
        }

        private System.Drawing.Point getRectCenter(System.Windows.Shapes.Rectangle i_rect)
        {
            return new System.Drawing.Point((int)(Canvas.GetLeft(i_rect) + (i_rect.Width / 2)), (int)(Canvas.GetTop(i_rect) + (i_rect.Height / 2)));
        }

        private int incrementSelected(int i_incBy)
        {
            int res = m_slectedRectIndex;

            if (res + i_incBy < m_sections.Count)
            {
                res += i_incBy;
            }
            else
            {
                i_incBy -= (m_sections.Count) - res;
                res = 0 + i_incBy;
            }
            return res;
        }

        private int decrementSelected(int i_decBy)
        {
            int res = m_slectedRectIndex;

            if (res - i_decBy < 0)
            {
                i_decBy -= res;
                res = (m_sections.Count) - i_decBy;
            }
            else
            {
                res -= i_decBy;
            }
            return res;
        }

        private void ConfigureFaceTracking()
        {
            face = senseManager.QueryFace();
            faceConfiguration = face.CreateActiveConfiguration();
            faceConfiguration.detection.isEnabled = true;

            expressionConfiguration = faceConfiguration.QueryExpressions();
            expressionConfiguration.Enable();
            expressionConfiguration.EnableAllExpressions();

            faceConfiguration.EnableAllAlerts();
            faceConfiguration.ApplyChanges();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //imgFace.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            processingThread.Abort();
            faceConfiguration.Dispose();
            senseManager.Dispose();
        }

        private void ProcessingThread()
        {
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMCapture.Sample sample = senseManager.QuerySample();
        
                int topScore = 0;
                FaceExpression expression = FaceExpression.None;

                // Get a face instance
                face = senseManager.QueryFace();

                if (face != null)
                {
                    // Get face tracking processed data
                    PXCMFaceData faceData = face.CreateOutput();
                    faceData.Update();
                    numberTrackedFaces = faceData.QueryNumberOfDetectedFaces();

                    // Retrieve the face location data instance
                    PXCMFaceData.Face faceDataFace = faceData.QueryFaceByIndex(0);

                    if (faceDataFace != null)
                    {
                        // Retrieve face location data
                        PXCMFaceData.DetectionData faceDetectionData = faceDataFace.QueryDetection();
                        if (faceDetectionData != null)
                        {
                            PXCMRectI32 faceRectangle;
                            faceDetectionData.QueryFaceAverageDepth(out faceAverageDepth);
                            faceDetectionData.QueryBoundingRect(out faceRectangle);
                            faceRectangleHeight = faceRectangle.h;
                            faceRectangleWidth = faceRectangle.w;
                            faceRectangleX = faceRectangle.x;
                            faceRectangleY = faceRectangle.y;
                        }

                        // Retrieve pose estimation data
                        PXCMFaceData.PoseData facePoseData = faceDataFace.QueryPose();
                        if (facePoseData != null)
                        {
                            PXCMFaceData.PoseEulerAngles headAngles;
                            facePoseData.QueryPoseAngles(out headAngles);

                            ///////////////////////////////////////
                            headRoll = headAngles.roll;
                            headPitch = headAngles.pitch;
                            headYaw = headAngles.yaw;
                            ////////////////////////////////////

                            if (m_isChromeRunning == true) // only if browser in currently running
                            {
                                if (headRoll > 15.0f) // head roll right
                                {
                                     SendKeys.SendWait("{PGDN}");
                                     System.Threading.Thread.Sleep(500);
                                }
                                else if (headRoll < -15.0f) // head roll left
                                {
                                    SendKeys.SendWait("{PGUP}");
                                    System.Threading.Thread.Sleep(500);
                                }

                                //////////////////////////////////

                                if (headPitch > 25.0f) // head nodd up
                                {
                                    SendKeys.SendWait("{TAB}"); // shift focus to next element
                                    System.Threading.Thread.Sleep(500);
                                }
                                else if (headPitch < -15.0f) // head nodd down
                                {
                                   
                                }
                            }
                            else // if its the Menu
                            {
                                if (headRoll < -15.0f) // head roll right
                                {
                                    setSelected(decrementSelected(1));
                                    System.Threading.Thread.Sleep(500);
                                }
                                else if (headRoll > 15.0f) // head roll left
                                {
                                    setSelected(incrementSelected(1));
                                    System.Threading.Thread.Sleep(500);
                                }
                            }
                        }

                        // Retrieve expression data
                        PXCMFaceData.ExpressionsData expressionData = faceDataFace.QueryExpressions();

                        if (expressionData != null)
                        {
                            PXCMFaceData.ExpressionsData.FaceExpressionResult score;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Kiss)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Open)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Smile)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Tongue)] = score.intensity;

                            // Determine the highest scoring expression
                            for (int i = 1; i < TotalExpressions; i++)
                            {
                                if (expressionScore[i] > topScore) { expression = (FaceExpression)i; }
                            }
                            
                            if (expression == FaceExpression.Smile)
                            {
                                SendKeys.SendWait("{F6}");
                                System.Threading.Thread.Sleep(1000);
                            }
                            else if (expression == FaceExpression.Kiss)
                            {
                                if (m_isChromeRunning == false)
                                {
                                    m_chromeBrowserInfo = new ProcessStartInfo(@"chrome.exe", m_websites[m_slectedRectIndex]);

                                    AutoClosingMessageBox.Show("Instructions:\n\n1) Tilt Your Head Right Or Left To Scroll\n\n2) Blow a Kiss To Click\n\n3) Nod Your Head Up To Move The Focus To The Next Element.", "Starting Chrome...", 5000);

                                    System.Diagnostics.Process.Start(m_chromeBrowserInfo);

                                    m_isChromeRunning = true;
                                }
                                else
                                {
                                    SendKeys.SendWait("{ENTER}");
                                    System.Threading.Thread.Sleep(1000);
                                }
                            }
                        }
                    }

                    faceData.Dispose();
                }
                
                // Release the frame
                senseManager.ReleaseFrame();
            }
        }
    }

 

    public class AutoClosingMessageBox
    {
        System.Threading.Timer _timeoutTimer;
        string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            using (_timeoutTimer)
                System.Windows.MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}
