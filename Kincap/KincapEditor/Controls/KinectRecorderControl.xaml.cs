using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Kincap.Models;
using Kincap.Views;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using Microsoft.Kinect;

namespace Kincap.Controls
{
    /// <summary>
    /// Logique d'interaction pour KinectRecorderControl.xaml
    /// </summary>
    public partial class KinectRecorderControl : UserControl
    {
  /// Active Kinect sensor
        private KinectSensor sensor;
        private short fpsEnd = 1;
        private BVHWriter BVHFile;
        Bitmap tempColorFrame;
        bool windowClosing = false;
        int initFrames = 1;

        MemoryStream ms_Stream;
        MemoryStream ms_Skeleton;

        KinectRecorder recorder;
        KinectReplay replay;

        SkeletonDisplayManager SkeletonDisplayManager;

        private Skeleton[] skeletons;

        public KinectRecorderControl()
        {
            InitializeComponent();

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            Models.Settings.OnSetSettings += SetSensorSettings;

            Initialize();
        }

        private void Initialize()
        {
            if (sensor == null)
                return;

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += KinectRuntimeColorFrameReady;

            sensor.SkeletonStream.Enable(Settings.SmoothingParam);
            sensor.SkeletonFrameReady += KinectRuntimeSkeletonFrameReady;

            SkeletonDisplayManager = new SkeletonDisplayManager(sensor, skeleton_canvas);

            image_stream.DataContext = colorManager;
        }

        public void ToggleButtonStreamChecked(object sender, RoutedEventArgs e)
        {
            ms_Stream = new MemoryStream();
            ms_Skeleton = new MemoryStream();

            StartSensor();
        }

        public void ToggleButtonStreamUnChecked(object sender, RoutedEventArgs e)
        {
            if (BVHFile != null)
            {
                Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Stream stopped"));
            }
            else
            {
                if (sensor != null)
                {
                    StopKinect(sensor);
                    Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Stream stopped"));
                    this.skeleton_canvas.Children.Clear();
                    this.image_stream.DataContext = null;
                }
            }
        }

        private void StartSensor()
        {
            // Start the sensor!
            try
            {
                this.sensor.Start();
                Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Stream started"));
            }
            catch (Exception)
            {
                this.sensor = null;
                Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Stream couldn't start"));
            }
        }

        void KinectRuntimeColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                if (recorder != null && ((recorder.Options & KinectRecordOptions.Color) != 0))
                {
                    recorder.Record(frame);
                }

                colorManager.Update(frame);
            }
        }

        void KinectRuntimeSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;


            if (fpsEnd == 1)
            {
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame == null)
                        return;

                    fpsEnd = SelectFPS();

                    Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.GetSkeletons(ref skeletons);
                    if (skeletons.Length != 0)
                    {
                        foreach (Skeleton skel in skeletons)
                        {
                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                if (BVHFile != null)
                                {
                                    if (BVHFile.IsRecording == true && BVHFile.IsInitializing == true)
                                    {
                                        BVHFile.Entry(skel);

                                        if (BVHFile.intializingCounter > initFrames)
                                        {
                                            BVHFile.StartWritingEntry();
                                        }

                                    }

                                    if (BVHFile.IsRecording == true && BVHFile.IsInitializing == false)
                                    {
                                        BVHFile.Motion(skel);
                                        Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Recording..."));
                                    }
                                }
                            }
                        }
                    }


                    if (recorder != null && ((recorder.Options & KinectRecordOptions.Skeletons) != 0))
                        recorder.Record(frame);

                    if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                        return;

                    ProcessFrame(frame);
                }
            }
            else
            {
                fpsEnd -= 1;
            }
        }

        void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (windowClosing)
                return;

            if (replay != null && !replay.IsFinished)
                return;
           
            if (fpsEnd == 1)
            {
                fpsEnd = SelectFPS();
                
                using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
                {

                    if (colorFrame != null)
                    {
                        System.Drawing.Image tempStreamFrame = new Bitmap((int)this.image_stream.MinWidth, (int)this.image_stream.MinHeight);

                        // Kinect Color Frame to Bitmap
                        tempColorFrame = ColorImageFrameToBitmap(colorFrame);
                        tempStreamFrame = tempColorFrame;
                        
                        // Help to translate system Drawing.Image to Windows.Media.ImageSource
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        // Save to a memory stream...
                        tempStreamFrame.Save(ms_Stream, ImageFormat.Bmp);
                        // Rewind the stream...
                        ms_Stream.Seek(0, SeekOrigin.Begin);
                        // Tell the WPF image to use this stream...
                        bi.StreamSource = ms_Stream;
                        bi.EndInit();
                        this.image_stream.Source = bi;

                        // Record
                        if (recorder != null && recorder.Options != 0)
                            recorder.Record(colorFrame);
                        
                    }
                }

                using (SkeletonFrame skelFrame = e.OpenSkeletonFrame())
                {
                    if (skelFrame != null)
                    {
                        Skeleton[] skeletons = new Skeleton[skelFrame.SkeletonArrayLength];
                        skelFrame.CopySkeletonDataTo(skeletons);
                        if (skeletons.Length != 0)
                        {
                            foreach (Skeleton skel in skeletons)
                            {
                                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                                {
                                    if (BVHFile != null)
                                    {
                                        if (BVHFile.IsRecording == true && BVHFile.IsInitializing == true)
                                        {
                                            BVHFile.Entry(skel);

                                            if (BVHFile.intializingCounter > initFrames)
                                            {
                                                BVHFile.StartWritingEntry();
                                            }

                                        }

                                        if (BVHFile.IsRecording == true && BVHFile.IsInitializing == false)
                                        {
                                            BVHFile.Motion(skel);
                                            Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now,"Recording..."));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                fpsEnd -= 1;
            }
        }

        private Bitmap ColorImageFrameToBitmap(ColorImageFrame colorFrame)
        {
            byte[] pixelBuffer = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo(pixelBuffer);
            Bitmap bitmapFrame = ArrayToBitmap(pixelBuffer, colorFrame.Width, colorFrame.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            return bitmapFrame;
        }

        private void DrawSkeletons(System.Drawing.Image backgroundImage, Skeleton skel)
        {
            Graphics graphicBox = Graphics.FromImage(backgroundImage);
            float width = (float)(backgroundImage.Width / 640F);
            float height = (float)(backgroundImage.Height / 480F);
            graphicBox.ScaleTransform(width, height);
            this.DrawBonesAndJoints(skel, graphicBox);
        }

        private void MainWindowFormClosing(object sender, CancelEventArgs e)
        {
            windowClosing = true;
            StopKinect(sensor);
        }

        private void MainWindowFormClosed(object sender, EventArgs e)
        {
            StopKinect(sensor);
        }

        //TODO: possibly outsource the following functions
        private void DrawBonesAndJoints(Skeleton skeleton, Graphics graphicBox)
        {
            /// Brush used to draw skeleton center point
            System.Drawing.Brush centerPointBrush = System.Drawing.Brushes.Blue;

            /// Brush used for drawing joints that are currently tracked
            System.Drawing.Pen trackedJointPen = new System.Drawing.Pen(System.Drawing.Color.GreenYellow);

            /// Brush used for drawing joints that are currently inferred      
            System.Drawing.Pen inferredJointPen = new System.Drawing.Pen(System.Drawing.Color.Yellow);

            // Paint found points as circles
            foreach (Joint joint in skeleton.Joints)
            {
                System.Drawing.Pen drawPen = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawPen = trackedJointPen;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawPen = inferredJointPen;
                }

                if (drawPen != null)
                {
                    graphicBox.DrawEllipse(drawPen, new System.Drawing.Rectangle(this.SkeletonPointToScreen(joint.Position), new System.Drawing.Size(10, 10)));
                }
            }

            // Draw connections between points
            // Render Torso
            this.DrawBone(skeleton, graphicBox, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, graphicBox, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, graphicBox, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, graphicBox, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, graphicBox, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, graphicBox, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, graphicBox, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, graphicBox, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, graphicBox, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, graphicBox, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, graphicBox, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, graphicBox, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, graphicBox, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, graphicBox, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, graphicBox, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, graphicBox, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, graphicBox, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, graphicBox, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, graphicBox, JointType.AnkleRight, JointType.FootRight);

            //Kopf malen
            if (skeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked)
            {
                graphicBox.DrawEllipse(new System.Drawing.Pen(System.Drawing.Color.GreenYellow), this.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position).X - 50,
                    this.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position).Y - 50, 100, 100);
            }


            return;
        }

        private void DrawBone(Skeleton skeleton, Graphics graphicBox, JointType jointType0, JointType jointType1)
        {
            /// Pen used for drawing bones that are currently tracked
            System.Drawing.Pen trackedBonePen = new System.Drawing.Pen(System.Drawing.Brushes.Green, 6);

            /// Pen used for drawing bones that are currently inferred      
            System.Drawing.Pen inferredBonePen = new System.Drawing.Pen(System.Drawing.Brushes.Gray, 1);

            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            System.Drawing.Pen drawPen = inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = trackedBonePen;
            }


            System.Drawing.Point startPixel = SkeletonPointToScreen(joint0.Position);
            System.Drawing.Point endPixel = SkeletonPointToScreen(joint1.Position);
            double distanceBtw2Joints = Math.Round(CalcDistanceBtw2Points(joint0.Position, joint1.Position) * 100) / 100;

            // Line between two joints is drawn
            graphicBox.DrawLine(drawPen, startPixel, endPixel);

            // Length of Bones is written next to it
            int textPosPixelX = Convert.ToInt32(Math.Abs(Math.Round(0.5 * (startPixel.X + endPixel.X))));
            int textPosPixelY = Convert.ToInt32(Math.Abs(Math.Round(0.5 * (startPixel.Y + endPixel.Y))));
            PointF textPos = new PointF(textPosPixelX, textPosPixelY);

            return;
        }

        private System.Drawing.Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new System.Drawing.Point(depthPoint.X, depthPoint.Y);
        }

        private double CalcDistanceBtw2Points(SkeletonPoint Joint1, SkeletonPoint Joint2)
        {
            double distanceBtwJoints = Math.Sqrt(Math.Pow(Joint1.X - Joint2.X, 2) + Math.Pow(Joint1.Y - Joint2.Y, 2) + Math.Pow(Joint1.Z - Joint2.Z, 2));
            return distanceBtwJoints;
        }

        Bitmap ArrayToBitmap(byte[] array, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            Bitmap bitmapFrame = new Bitmap(width, height, pixelFormat);

            BitmapData bitmapData = bitmapFrame.LockBits(new System.Drawing.Rectangle(0, 0,
            width, height), ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);

            IntPtr intPointer = bitmapData.Scan0;
            Marshal.Copy(array, 0, intPointer, array.Length);

            bitmapFrame.UnlockBits(bitmapData);
            return bitmapFrame;
        }

        public void ToggleButtonRecChecked(object sender, RoutedEventArgs e)
        {
            Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Initialization"));

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };

            if (saveFileDialog.ShowDialog() == true)
            {
                Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Recording Stream"));
                DirectRecord(saveFileDialog.FileName);

                if (BVHFile == null && sensor != null)
                {
                    Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Recording BHV"));

                    string txtFileName = saveFileDialog.FileName.Replace(".replay", "");
                    BVHFile = new BVHWriter(txtFileName);
                    Window w = Window.GetWindow(this);
                    MainWindow mw = (MainWindow)w;
                    BVHFile.SetBvhEditor(mw.bvhEditor);
                }
            }
        }

        public void ToggleButtonRecUnChecked(object sender, RoutedEventArgs e)
        {
            if (BVHFile != null)
            {
                BVHFile.CloseBVHFile();
                Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Record saved"));
                BVHFile = null;
            }

            if (recorder != null)
            {
                StopRecord();
            }
        }
        
        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();
                }
            }
        }

        //KINECT OPTIONS

        public void SetSensorSettings()
        {
            if (sensor == null)
                return;

            if(Models.Settings.NearMode)
            {
                sensor.DepthStream.Range = DepthRange.Near;
                sensor.SkeletonStream.EnableTrackingInNearRange = true;
            }
            else
            {
                sensor.DepthStream.Range = DepthRange.Default;
                sensor.SkeletonStream.EnableTrackingInNearRange = false;
            }

            if(Models.Settings.SeatedMode)
            {
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            }
            else
            {
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }
        }

        //RECORD & REPLAY
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        private void ReplayButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };

            if (openFileDialog.ShowDialog() == true)
            {
                if (replay != null)
                {
                    replay.SkeletonFrameReady -= ReplaySkeletonFrameReady;
                    replay.ColorImageFrameReady -= ReplayColorImageFrameReady;
                    replay.Stop();
                }

                this.image_stream.DataContext = colorManager;
                Stream recordStream = File.OpenRead(openFileDialog.FileName);

                replay = new KinectReplay(recordStream);

                replay.SkeletonFrameReady += ReplaySkeletonFrameReady;
                replay.ColorImageFrameReady += ReplayColorImageFrameReady;

                replay.Start();
            }
        }

        void ReplayColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            colorManager.Update(e.ColorImageFrame);
        }

        void ReplaySkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            if (fpsEnd == 1)
            {
                if (e.SkeletonFrame == null)
                    return;

                fpsEnd = SelectFPS();

                Skeleton[] skeletons = e.SkeletonFrame.Skeletons;
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            if (BVHFile != null)
                            {
                                if (BVHFile.IsRecording == true && BVHFile.IsInitializing == true)
                                {
                                    BVHFile.Entry(skel);

                                    if (BVHFile.intializingCounter > initFrames)
                                    {
                                        BVHFile.StartWritingEntry();
                                    }
                                }

                                if (BVHFile.IsRecording == true && BVHFile.IsInitializing == false)
                                {
                                    BVHFile.Motion(skel);
                                    Controls.ConsoleControl.LogEntries.Add(new LogEntry(DateTime.Now, "Recording..."));
                                }
                            }
                        }
                    }
                }

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;

                ProcessFrame(e.SkeletonFrame);
                
            }
            else
            {
                fpsEnd -= 1;
            }
        }

        void ProcessFrame(ReplaySkeletonFrame frame)
        {
            SkeletonDisplayManager.Draw(frame.Skeletons, Models.Settings.SeatedMode);
        }

        void DirectRecord(string targetFileName)
        {
            Stream recordStream = File.Create(targetFileName);
            recorder = new KinectRecorder(KinectRecordOptions.Skeletons | KinectRecordOptions.Color, recordStream);
        }

        void StopRecord()
        {
            if (recorder != null)
            {
                recorder.Stop();
                recorder = null;
                return;
            }
        }

        // MISC

        short SelectFPS()
        {
            short fpsEnd = 30;
            //FPS selection. At low frame rate, received frames are skipped (not displayed)
            switch (Settings.FpsSetting)
            {
                case 30:
                    fpsEnd = 1;
                    break;
                case 15:
                    fpsEnd = 2;
                    break;
                case 10:
                    fpsEnd = 3;
                    break;
                case 5:
                    fpsEnd = 6;
                    break;
                case 1:
                    fpsEnd = 30;
                    break;
            }

            return fpsEnd;
        }
    }
}
