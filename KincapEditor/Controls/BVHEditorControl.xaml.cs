using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Kincap.Controls
{
    /// <summary>
    /// Logique d'interaction pour BVHEditor.xaml
    /// </summary>
    public partial class BVHEditorControl : UserControl
    {
        public BVHEditorControl()
        {
            InitializeComponent();

            string[] tabJoint = new string[] {
            "HipCenter",
            "HipCenter2",
            "Spine",
            "ShoulderCenter",
            "CollarRight",
            "ShoulderRight",
            "ElbowRight",
            "WristRight",
            "HandRight",
            "CollarLeft",
            "ShoulderLeft",
            "ElbowLeft",
            "WristLeft",
            "HandLeft",
            "Neck",
            "Head",
            "HipRight",
            "KneeRight",
            "AnkleRight",
            "HipLeft",
            "KneeLeft",
            "AnkleLeft"};

            foreach (string s in tabJoint)
            {
                this.dropDown_joint.Items.Add(s);
            }
        }

        public string TextBoxElapsedTime { get { return textBox_elapsedTime.Text; } set { textBox_elapsedTime.Text = value; } }
        public string TextBoxCapturedFrames { get { return textBox_capturedFrames.Text; } set { textBox_capturedFrames.Text = value; } }
        public string TextBoxFrameRate { get { return textBox_frameRate.Text; } set { textBox_frameRate.Text = value; } }
        public string TextBoxAngles { get { return textBox_angles.Text; } set { textBox_angles.Text = value; } }
        public string TextBoxLength { get { return textBox_length.Text; } set { textBox_length.Text = value; } }
        public string TextPosition { get { return textBox_position.Text; } set { textBox_position.Text = value; } }
        public string DropDownJoint { get { return dropDown_joint.Text; } }
    }
}
