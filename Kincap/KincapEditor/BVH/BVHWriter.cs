using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kincap.Controls;
using Microsoft.Kinect;

namespace Kincap
{
    class BVHWriter
    {
        private bool recording = false;
        StreamWriter file;
        private bool initializing = false;
        public int intializingCounter = 0;
        string fileName;
        BVHEditorControl bvhEditor;
        Stopwatch sw = new Stopwatch();

        private int frameCounter = 0;
        private double avgFrameRate = 0;
        private double elapsedTimeSec = 0;

        BVHSkeleton bvhSkeleton = new BVHSkeleton();
        BVHSkeleton bvhSkeletonWritten = new BVHSkeleton();
        double[,] tempOffsetMatrix;
        double[] tempMotionVektor;

        public BVHWriter(string fileName)
        {
            fileName = fileName + ".bvh";
            this.fileName = fileName;
            BVHKinectSkeleton.AddKinectSkeleton(bvhSkeleton);
            initializing = true;
            tempOffsetMatrix = new double[3, bvhSkeleton.Bones.Count];
            tempMotionVektor = new double[bvhSkeleton.Channels];

            if (File.Exists(fileName))
                File.Delete(fileName);
            file = File.CreateText(fileName);
            file.WriteLine("HIERARCHY");
            recording = true;
        }

        public void SetBvhEditor(BVHEditorControl bvhEC)
        {
            bvhEditor = bvhEC;
        }

        public void CloseBVHFile()
        {
            sw.Stop(); // finished recording
            file.Flush();
            file.Close();
            string text = File.ReadAllText(fileName);
            text = text.Replace("PLATZHALTERFRAMES", frameCounter.ToString());
            File.WriteAllText(fileName, text);

            recording = false;
        }

        public bool IsRecording
        {
            get { return recording; }
        }

        public bool IsInitializing
        {
            get { return initializing; }
        }

        public void Entry(Skeleton skel)
        {
            this.intializingCounter++;
            for (int k = 0; k < bvhSkeleton.Bones.Count; k++)
            {
                double[] bonevector = BVHKinectSkeleton.GetBoneVectorOutofJointPosition(bvhSkeleton.Bones[k], skel);
                {
                    if (this.intializingCounter == 1)
                    {
                        tempOffsetMatrix[0, k] = Math.Round(bonevector[0] * 100, 2);
                        tempOffsetMatrix[1, k] = Math.Round(bonevector[1] * 100, 2);
                        tempOffsetMatrix[2, k] = Math.Round(bonevector[2] * 100, 2);
                    }
                    else
                    {
                        tempOffsetMatrix[0, k] = (this.intializingCounter * tempOffsetMatrix[0, k] + Math.Round(bonevector[0] * 100, 2)) / (this.intializingCounter + 1);
                        tempOffsetMatrix[1, k] = (this.intializingCounter * tempOffsetMatrix[1, k] + Math.Round(bonevector[1] * 100, 2)) / (this.intializingCounter + 1);
                        tempOffsetMatrix[2, k] = (this.intializingCounter * tempOffsetMatrix[1, k] + Math.Round(bonevector[2] * 100, 2)) / (this.intializingCounter + 1);
                    }
                }
            }
        }

        public void StartWritingEntry()
        {
            for (int k = 0; k < bvhSkeleton.Bones.Count; k++)
            {
                double length = Math.Max(Math.Abs(tempOffsetMatrix[0, k]), Math.Abs(tempOffsetMatrix[1, k]));
                length = Math.Max(length, Math.Abs(tempOffsetMatrix[2, k]));
                length = Math.Round(length, 2);

                switch (bvhSkeleton.Bones[k].Axis)
                {
                    case TransAxis.X:
                        bvhSkeleton.Bones[k].SetTransOffset(length, 0, 0);
                        break;
                    case TransAxis.Y:
                        bvhSkeleton.Bones[k].SetTransOffset(0, length, 0);
                        break;
                    case TransAxis.Z:
                        bvhSkeleton.Bones[k].SetTransOffset(0, 0, length);
                        break;
                    case TransAxis.nX:
                        bvhSkeleton.Bones[k].SetTransOffset(-length, 0, 0);
                        break;
                    case TransAxis.nY:
                        bvhSkeleton.Bones[k].SetTransOffset(0, -length, 0);
                        break;
                    case TransAxis.nZ:
                        bvhSkeleton.Bones[k].SetTransOffset(0, 0, -length);
                        break;

                    default:
                        bvhSkeleton.Bones[k].SetTransOffset(tempOffsetMatrix[0, k], tempOffsetMatrix[1, k], tempOffsetMatrix[2, k]);
                        break;
                }
            }

            this.initializing = false;
            WriteEntry();
            file.Flush();
        }

        private void WriteEntry()
        {
            List<List<BVHBone>> bonesListList = new List<List<BVHBone>>();
            List<BVHBone> resultList;

            while (bvhSkeleton.Bones.Count != 0)
            {
                if (bvhSkeletonWritten.Bones.Count == 0)
                {
                    resultList = bvhSkeleton.Bones.FindAll(i => i.Root == true);
                    bonesListList.Add(resultList);
                }
                else
                {
                    if (bvhSkeletonWritten.Bones.Last().End == false)
                    {
                        for (int k = 1; k <= bvhSkeletonWritten.Bones.Count; k++)
                        {
                            resultList = bvhSkeletonWritten.Bones[bvhSkeletonWritten.Bones.Count - k].Children;
                            if (resultList.Count != 0)
                            {
                                bonesListList.Add(resultList);
                                break;
                            }
                        }
                    }
                }

                BVHBone currentBone = bonesListList.Last().First();
                string tabs = CalcTabs(currentBone);
                if (currentBone.Root == true)
                    file.WriteLine("ROOT " + currentBone.Name);
                else if (currentBone.End == true)
                    file.WriteLine(tabs + "End Site");
                else
                    file.WriteLine(tabs + "JOINT " + currentBone.Name);

                file.WriteLine(tabs + "{");
                file.WriteLine(tabs + "\tOFFSET " + currentBone.translOffset[0].ToString().Replace(",", ".") +
                    " " + currentBone.translOffset[1].ToString().Replace(",", ".") +
                    " " + currentBone.translOffset[2].ToString().Replace(",", "."));

                if (currentBone.End == true)
                {
                    while (bonesListList.Count != 0 && bonesListList.Last().Count == 1)
                    {
                        tabs = CalcTabs(bonesListList.Last()[0]);
                        foreach (List<BVHBone> liste in bonesListList)
                        {
                            if (liste.Contains(bonesListList.Last()[0]))
                            {
                                liste.Remove(bonesListList.Last()[0]);
                            }
                        }
                        bonesListList.Remove(bonesListList.Last());
                        file.WriteLine(tabs + "}");
                    }

                    if (bonesListList.Count != 0)
                    {
                        if (bonesListList.Last().Count != 0)
                        {
                            bonesListList.Last().Remove(bonesListList.Last()[0]);
                        }
                        else
                        {
                            bonesListList.Remove(bonesListList.Last());
                        }
                        tabs = CalcTabs(bonesListList.Last()[0]);
                        file.WriteLine(tabs + "}");
                    }
                }
                else
                {
                    file.WriteLine(tabs + "\t" + WriteChannels(currentBone));
                }
                bvhSkeleton.Bones.Remove(currentBone);
                bvhSkeletonWritten.AddBone(currentBone);
            }
            bvhSkeletonWritten.CopyParameters(bvhSkeleton);
        }

        public void Motion(Skeleton skel)
        {
            sw.Start(); // Begin recording movements

            for (int k = 0; k < bvhSkeletonWritten.Bones.Count; k++)
            {
                if (bvhSkeletonWritten.Bones[k].End == false)
                {
                    double[] degVec = new double[3];
                    degVec = BVHKinectSkeleton.GetEulerFromBone(bvhSkeletonWritten.Bones[k], skel);

                    int indexOffset = 0;
                    if (bvhSkeletonWritten.Bones[k].Root == true)
                    {
                        indexOffset = 3;
                    }

                    tempMotionVektor[bvhSkeletonWritten.Bones[k].MotionSpace + indexOffset] = degVec[0];
                    tempMotionVektor[bvhSkeletonWritten.Bones[k].MotionSpace + 1 + indexOffset] = degVec[1];
                    tempMotionVektor[bvhSkeletonWritten.Bones[k].MotionSpace + 2 + indexOffset] = degVec[2];

                    // set Textbox
                    string boneName = bvhSkeletonWritten.Bones[k].Name;
                    if (boneName == bvhEditor.DropDownJoint)
                    {
                        // Rotation
                        string textBox = Math.Round(degVec[0], 1).ToString() + " " + Math.Round(degVec[1], 1).ToString() + " " + Math.Round(degVec[2], 1).ToString();
                        bvhEditor.TextBoxAngles = textBox;

                        // Position
                        JointType KinectJoint = BVHKinectSkeleton.GetJointTypeFromBVHBone(bvhSkeletonWritten.Bones[k]);
                        double x = skel.Joints[KinectJoint].Position.X;
                        double y = skel.Joints[KinectJoint].Position.Y;
                        double z = skel.Joints[KinectJoint].Position.Z;
                        bvhEditor.TextPosition = Math.Round(x, 2).ToString() + " " + Math.Round(y, 2).ToString() + " " + Math.Round(z, 2).ToString();

                        // Length
                        BVHBone tempBone = bvhSkeletonWritten.Bones.Find(i => i.Name == KinectJoint.ToString());
                        double[] boneVec = BVHKinectSkeleton.GetBoneVectorOutofJointPosition(tempBone, skel);
                        double length = Math.Sqrt(Math.Pow(boneVec[0], 2) + Math.Pow(boneVec[1], 2) + Math.Pow(boneVec[2], 2));
                        length = Math.Round(length, 2);
                        bvhEditor.TextBoxLength = length.ToString();
                    }
                }

            }
            // Root motion
            tempMotionVektor[0] = -Math.Round(skel.Position.X * 100, 2);
            tempMotionVektor[1] = Math.Round(skel.Position.Y * 100, 2) + 120;
            tempMotionVektor[2] = 300 - Math.Round(skel.Position.Z * 100, 2);

            WriteMotion(tempMotionVektor);
            file.Flush();

            elapsedTimeSec = Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds) / 1000, 2);
            bvhEditor.TextBoxElapsedTime = elapsedTimeSec.ToString();
            bvhEditor.TextBoxCapturedFrames = frameCounter.ToString();
            avgFrameRate = Math.Round(frameCounter / elapsedTimeSec, 2);
            bvhEditor.TextBoxFrameRate = avgFrameRate.ToString();

        }

        private void WriteMotion(double[] tempMotionVektor)
        {
            string motionStringValues = "";

            if (frameCounter == 0)
            {
                file.WriteLine("MOTION");
                file.WriteLine("Frames: PLATZHALTERFRAMES");
                file.WriteLine("Frame Time: 0.0333333");
            }
            foreach (var i in tempMotionVektor)
            {
                motionStringValues += (Math.Round(i, 4).ToString().Replace(",", ".") + " ");
            }

            file.WriteLine(motionStringValues);

            frameCounter++;
        }

        private string WriteChannels(BVHBone bone)
        {
            string output = "CHANNELS " + bone.Channels.Length.ToString() + " ";

            for (int k = 0; k < bone.Channels.Length; k++)
            {
                output += bone.Channels[k].ToString() + " ";

            }
            return output;
        }

        private string CalcTabs(BVHBone currentBone)
        {
            int depth = currentBone.Depth;
            string tabs = "";
            for (int k = 0; k < currentBone.Depth; k++)
            {
                tabs += "\t";
            }
            return tabs;
        }

    }
}
