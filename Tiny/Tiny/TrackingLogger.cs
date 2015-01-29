﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Tiny.WorldView;
using KinectSerializer;
using System.Diagnostics;
using System.Collections.Specialized;
using System.IO;

namespace Tiny
{
    public class TrackingLogger
    {
        private static readonly string FILE_RAW = "..\\..\\..\\..\\Logs\\raw.csv";
        private static readonly string FILE_DIFFERENCE = "..\\..\\..\\..\\Logs\\difference.csv";
        private static readonly string FILE_AVERAGE = "..\\..\\..\\..\\Logs\\average.csv";

        private static readonly string NA = "N/A";

        private static readonly string TIMESTAMP = "Timestamp";
        private static readonly string PERSON = "Person#";
        private static readonly string SKELETON = "Skeleton#";
        private static readonly string FOV = "FOV#";
        private static readonly string X = "X";
        private static readonly string Y = "Y";
        private static readonly string Z = "Z";

        private List<string> raw;
        private List<string> differences;
        private List<string> averages;

        private readonly object syncWriteLock = new object();

        public TrackingLogger()
        {
            this.raw = TrackingLogger.GetRawHeaders();
            this.differences = TrackingLogger.GetDifferencesHeaders();
            this.averages = TrackingLogger.GetAveragesHeaders();

            TrackingLogger.CreateFile(TrackingLogger.FILE_RAW, this.raw);
            TrackingLogger.CreateFile(TrackingLogger.FILE_DIFFERENCE, this.differences);
            TrackingLogger.CreateFile(TrackingLogger.FILE_AVERAGE, this.averages);
        }

        private static void AddJointHeaders(ref List<string> headers)
        {
            foreach (JointType jt in BodyStructure.Joints)
            {
                string x = jt.ToString() + "_" + TrackingLogger.X;
                string y = jt.ToString() + "_" + TrackingLogger.Y;
                string z = jt.ToString() + "_" + TrackingLogger.Z;
                headers.Add(x);
                headers.Add(y);
                headers.Add(z);
            }
        }

        private static List<string> GetDefaultHeaders()
        {
            List<string> headers = new List<string>();
            headers.Add(TrackingLogger.TIMESTAMP);
            headers.Add(TrackingLogger.PERSON);
            headers.Add(TrackingLogger.SKELETON);
            headers.Add(TrackingLogger.FOV);
            TrackingLogger.AddJointHeaders(ref headers);
            return headers;
        }

        private static List<string> GetRawHeaders()
        {
            return TrackingLogger.GetDefaultHeaders();
        }

        private static List<string> GetDifferencesHeaders()
        {
            return TrackingLogger.GetDefaultHeaders();
        }

        private static List<string> GetAveragesHeaders()
        {
            List<string> averages = new List<string>();
            averages.Add(TrackingLogger.TIMESTAMP);
            averages.Add(TrackingLogger.PERSON);
            TrackingLogger.AddJointHeaders(ref averages);
            return averages;
        }

        private static void WriteHeaders(string filepath, List<string> headers)
        {
            using (StreamWriter w = new StreamWriter(filepath, true))
            {
                string prefix = "";
                foreach (string h in headers)
                {
                    w.Write(prefix + h);
                    prefix = ", ";
                }
                w.WriteLine();
            }
        }

        private static void CreateFile(string filepath, List<string> headers)
        {
            using (StreamReader r = new StreamReader(File.Open(filepath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)))
            {
                string header = r.ReadLine();
                if (header == null)
                {
                    TrackingLogger.WriteHeaders(filepath, headers);
                }
            }
        }

        public void Write(Tracker.Result result)
        {
            lock (syncWriteLock)
            {
                using (StreamWriter rawFile = new StreamWriter(TrackingLogger.FILE_RAW, true))
                {
                    foreach (Tracker.Result.Person person in result.People)
                    {
                        IEnumerable<Tuple<Tracker.Result.SkeletonMatch, Dictionary<JointType, CameraSpacePoint>>> rawCoordinates = TrackingUtils.GetRawCoordinates(person);
                        foreach (Tuple<Tracker.Result.SkeletonMatch, Dictionary<JointType, CameraSpacePoint>> coordinateTuple in rawCoordinates)
                        {
                            Tracker.Result.SkeletonMatch match = coordinateTuple.Item1;
                            Dictionary<JointType, CameraSpacePoint> coordinates = coordinateTuple.Item2;

                            // Timestamp, Person#, Skeleton#, FOV#
                            rawFile.Write(result.Timestamp + ", " + person.Id + ", " + match.Id + ", " + match.FOV.Id);
                            // Joint_X, Joint_Y, Joint_Z
                            string prefix = ",";
                            foreach (JointType jt in BodyStructure.Joints)
                            {
                                rawFile.Write(prefix);
                                if (coordinates.ContainsKey(jt))
                                {
                                    rawFile.Write(coordinates[jt].X + ", " + coordinates[jt].Y + ", " + coordinates[jt].Z);
                                }
                                else
                                {
                                    rawFile.Write(TrackingLogger.NA + ", " + TrackingLogger.NA + ", " + TrackingLogger.NA);
                                }
                            }
                            // newline
                            rawFile.WriteLine();
                        }
                        // Averages
                        Dictionary<JointType, CameraSpacePoint> averages = TrackingUtils.GetAverages(rawCoordinates);
                        this.WriteAverages(averages, result.Timestamp, person.Id);

                        // Differences
                        IEnumerable<Tuple<Tracker.Result.SkeletonMatch, Dictionary<JointType, CameraSpacePoint>>> differences = TrackingUtils.GetDifferences(rawCoordinates, averages);
                        this.WriteDifferences(differences, result.Timestamp, person.Id);
                    } // Person
                } // Stream Writer
            }
        }

        //private void WriteAverages(Dictionary<JointType, CameraSpacePoint> averages, long timestamp, uint personId)
        //{
        //    using (StreamWriter averageData = new StreamWriter(TrackingLogger.FILE_AVERAGE, true))
        //    {
        //        // Timestamp, Person#
        //        averageData.Write(timestamp + ", " + personId);

        //        string prefix = ", ";
        //        foreach (JointType jt in BodyStructure.Joints)
        //        {
        //            if (averages[jt].X != 0 && averages[jt].Y != 0 && averages[jt].Z != 0)
        //            {
        //                averageData.Write(prefix);
        //                CameraSpacePoint joint = averages[jt];
        //                // Joint_X, Joint_Y, Joint_Z
        //                averageData.Write(joint.X + ", " + joint.Y + ", " + joint.Z);
        //            }
        //            else
        //            {
        //                averageData.Write(TrackingLogger.NA + ", " + TrackingLogger.NA + ", " + TrackingLogger.NA);
        //            }
        //        }
        //        averageData.WriteLine();
        //    }
        //}
        //private void WriteDifferences(Dictionary<JointType, CameraSpacePoint> averages, long timestamp, Tracker.Result.Person person)
        //{
        //    using (StreamWriter differenceData = new StreamWriter(TrackingLogger.FILE_DIFFERENCE, true))
        //    {
        //        Tracker.Result.SkeletonMatch reference = person.SkeletonMatches.First();
        //        Tracker.Result.KinectFOV referenceFOV = reference.FOV;
        //        TrackingSkeleton referenceSkeleton = reference.Skeleton;
        //        double referenceAngle = referenceSkeleton.InitialAngle;
        //        WCoordinate referencePosition = referenceSkeleton.InitialPosition;

        //        foreach (Tracker.Result.SkeletonMatch match in person.SkeletonMatches)
        //        {
        //            // Timestamp, Person#, Skeleton#, FOV#
        //            differenceData.Write(timestamp + ", " + person.Id + ", " + match.Id + ", " + match.FOV.Id);

        //            TrackingSkeleton matchSkeleton = match.Skeleton;
        //            WBody position = matchSkeleton.CurrentPosition.Worldview;
        //            KinectBody body = WBody.TransformToKinectBody(position, referenceAngle, referencePosition);

        //            string prefix = ", ";
        //            foreach (JointType jt in BodyStructure.Joints)
        //            {
        //                differenceData.Write(prefix);
        //                if (body.Joints.ContainsKey(jt))
        //                {
        //                    CameraSpacePoint joint = body.Joints[jt];
        //                    CameraSpacePoint average = averages[jt];
        //                    CameraSpacePoint difference = new CameraSpacePoint();
        //                    // Square difference
        //                    difference.X = (float)Math.Pow(joint.X - average.X, 2);
        //                    difference.Y = (float)Math.Pow(joint.Y - average.Y, 2);
        //                    difference.Z = (float)Math.Pow(joint.Z - average.Z, 2);
        //                    // Joint_X, Joint_Y, Joint_Z
        //                    differenceData.Write(difference.X + ", " + difference.Y + ", " + difference.Z);
        //                }
        //                else
        //                {
        //                    differenceData.Write(TrackingLogger.NA + ", " + TrackingLogger.NA + ", " + TrackingLogger.NA);
        //                }
        //            }
        //            differenceData.WriteLine();
        //        } // Skeleton Match
        //    }
        //}
    }
}