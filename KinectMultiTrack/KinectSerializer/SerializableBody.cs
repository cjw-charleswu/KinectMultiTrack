﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Permissions;
using System.Runtime.Serialization;
using Microsoft.Kinect;
using System.IO;
using System.Collections.ObjectModel;

namespace KinectSerializer
{
    [Serializable]
    public class SBody : ISerializable
    {
        public const string NameIsTracked = "IsTracked";
        public const string NameTrackingId = "TrackingId";
        public const string NameJointsDictionary = "JointsDict";

        public bool IsTracked { get; private set; }
        public ulong TrackingId { get; private set; }
        public Dictionary<JointType, SJoint> Joints { get; private set; }

        public SBody(bool isTracked, ulong trackingId)
        {
            this.IsTracked = isTracked;
            this.TrackingId = trackingId;
            this.Joints = new Dictionary<JointType, SJoint>();
        }

        protected SBody(SerializationInfo info, StreamingContext ctx)
        {
            this.IsTracked = (bool)info.GetValue(SBody.NameIsTracked, typeof(bool));
            this.TrackingId = (ulong)info.GetValue(SBody.NameTrackingId, typeof(ulong));
            this.Joints = (Dictionary<JointType, SJoint>)info.GetValue(SBody.NameJointsDictionary, typeof(Dictionary<JointType, SJoint>));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            info.AddValue(SBody.NameIsTracked, this.IsTracked, typeof(bool));
            info.AddValue(SBody.NameTrackingId, this.TrackingId, typeof(ulong));
            info.AddValue(SBody.NameJointsDictionary, this.Joints, typeof(Dictionary<JointType, SJoint>));
        }

        public static SBody Copy(SBody body)
        {
            SBody copy = new SBody(body.IsTracked, body.TrackingId);
            Dictionary<JointType, SJoint> jointsDict = body.Joints;
            foreach (JointType jointType in jointsDict.Keys)
            {
                copy.Joints[jointType] = SJoint.Copy(jointsDict[jointType]);
            }
            return body;
        }
    }
}