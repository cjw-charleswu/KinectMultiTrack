﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectSerializer;
using System.Collections.Concurrent;
using System.Net;

namespace Tiny
{
    class WorldCamera
    {
        public class User
        {
            private KinectCamera clientCamera;
            private SerializableBodyFrame worldBodyFrame;

            public User(KinectCamera clientCamera)
            {
                this.clientCamera = clientCamera;
                this.worldBodyFrame = clientCamera.CurrentBodyFrame;
            }

            public KinectCamera ClientCamera
            {
                get
                {
                    return this.clientCamera;
                }
            }

            public SerializableBodyFrame WorldBodyFrame
            {
                get
                {
                    return this.worldBodyFrame;
                }
                set
                {
                    this.worldBodyFrame = value;
                }
            }
        }

        private ConcurrentDictionary<IPEndPoint, User> users;

        public IEnumerable<SerializableBodyFrame> ClientBodyFrames
        {
            get
            {
                foreach (User user in this.users.Values)
                {
                    yield return user.ClientCamera.CurrentBodyFrame;
                }
            }
        }

        public IEnumerable<SerializableBodyFrame> ProcessedBodyFrames
        {
            get
            {
                foreach (User user in this.users.Values)
                {
                    yield return user.WorldBodyFrame;
                }
            }
        }

        public WorldCamera()
        {
            this.users = new ConcurrentDictionary<IPEndPoint, User>();
        }

        public void AddOrUpdateClientCamera(KinectCamera clientCamera)
        {
            User user = new User(clientCamera);
            this.users.AddOrUpdate(clientCamera.ClientIP, user, (key, oldValue) => user);
        }

        public void RemoveClientCamera(KinectCamera clientCamera)
        {
            User result;
            this.users.TryRemove(clientCamera.ClientIP, out result);
        }

        public void SynchronizeFrames()
        {
            foreach (User user in this.users.Values)
            {
                user.WorldBodyFrame = user.ClientCamera.CurrentBodyFrame;
            }
        }


    }
}
