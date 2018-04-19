using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace GestureRecognition
{
    class GestureRecognizer
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// GraphicsManipulator to send gesture data to
        /// </summary>
        private GraphicsManipulator manipulator;

        /// <summary>
        /// Enumeration for maintaining current gesture action
        /// </summary>
        private enum ActionState
        {
            TRANSLATE,
            ROTATE,
            SCALE,
            START,
            NONE,
        }

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Maintains record of current gesture action in progress
        /// </summary>
        private ActionState currentAction = ActionState.NONE;

        /// <summary>
        /// Maintain record of previous state of the left hand to calculate gesture
        /// </summary>
        private HandState leftHandState;

        /// <summary>
        /// Maintain record of last position of the left hand to calculate gesture
        /// </summary>
        private CameraSpacePoint leftHandPosition;

        /// <summary>
        /// Maintain record of previous state of the right hand to calculate gesture
        /// </summary>
        private HandState rightHandState;

        /// <summary>
        /// Maintain record of last position of the right hand to calculate gesture
        /// </summary>
        private CameraSpacePoint rightHandPosition;

       

        public GestureRecognizer(GraphicsManipulator graphicsManipulator)
        {
            // save reference to graphicsManipulator
            manipulator = graphicsManipulator;

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // open the sensor
            this.kinectSensor.Open();

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {

                foreach (Body body in this.bodies)
                {
                    if (body.IsTracked)
                    {
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                        Dictionary<JointType, CameraSpacePoint> cameraPoints = new Dictionary<JointType, CameraSpacePoint>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            cameraPoints[jointType] = position;
                        }

                        // perform operation user is gesturing
                        if (currentAction != ActionState.NONE)
                        {
                            CameraSpacePoint currentLeftPosition = cameraPoints[JointType.HandLeft];
                            CameraSpacePoint currentRightPosition = cameraPoints[JointType.HandRight];

                            if (leftHandState == HandState.Closed && rightHandState == HandState.Closed && currentAction != ActionState.ROTATE)
                            {
                                // calculate distance in x,y,z from previous point in meters

                                float handDistanceX = currentLeftPosition.X - currentRightPosition.X;
                                float handDistanceY = currentLeftPosition.Y - currentRightPosition.Y;
                                float handDistanceZ = currentLeftPosition.Z - currentRightPosition.Z;

                                float prevDistanceX = leftHandPosition.X - rightHandPosition.X;
                                float prevDistanceY = leftHandPosition.Y - rightHandPosition.Y;
                                float prevDistanceZ = leftHandPosition.Z - rightHandPosition.Z;

                                float changeX = Math.Abs(handDistanceX - prevDistanceX);
                                float changeY = Math.Abs(handDistanceY - prevDistanceY);
                                float changeZ = Math.Abs(handDistanceZ - prevDistanceZ);

                                // if significant distance change between hands, perform scaling operation
                                if (changeX > .05 || changeY > .05 || changeZ > .05)
                                {
                                    // scale by largest distance change
                                    Console.WriteLine("Perform scaling");
                                    if (changeX > changeY && changeX > changeZ)
                                    {
                                        manipulator.Scale(prevDistanceX - handDistanceX);
                                    }
                                    else if (changeY > changeX && changeY > changeZ)
                                    {
                                        manipulator.Scale(prevDistanceY - handDistanceY);
                                    }
                                    else
                                    {
                                        manipulator.Scale(prevDistanceZ - handDistanceZ);
                                    }
                                }
                                else
                                {
                                    // translate by movement from last check
                                    Console.WriteLine("Perform translation");
                                    manipulator.Translate(currentLeftPosition.X - leftHandPosition.X, currentLeftPosition.Y - leftHandPosition.Y, currentLeftPosition.Z - leftHandPosition.Z);
                                }


                            }
                            else if (rightHandState == HandState.Closed)
                            {
                                Console.WriteLine("Rotate right hand");
                                // calculate distance in x,y,z from previous point in meters
                                float deltaX = currentRightPosition.X - rightHandPosition.X;
                                float deltaY = currentRightPosition.Y - rightHandPosition.Y;
                                float deltaZ = currentRightPosition.Z - rightHandPosition.Z;

                                // perform rotation by change in meters
                                manipulator.Rotate(deltaZ, deltaX, deltaZ);
                            }
                            else if (leftHandState == HandState.Closed)
                            {
                                Console.WriteLine("Rotate left hand");
                                // calculate distance in x,y,z from previous point in meters
                                float deltaX = currentRightPosition.X - rightHandPosition.X;
                                float deltaY = currentRightPosition.Y - rightHandPosition.Y;
                                float deltaZ = currentRightPosition.Z - rightHandPosition.Z;

                                // perform rotation by change in meters
                                manipulator.Rotate(deltaZ, deltaX, deltaY);
                            }



                        }

                        int handsClosed = 0;

                        switch (body.HandLeftState)
                        {
                            case HandState.Open:
                                break;
                            case HandState.Closed:
                                if (currentAction == ActionState.NONE)
                                {
                                    handsClosed++;
                                }
                                leftHandState = HandState.Closed;
                                leftHandPosition = cameraPoints[JointType.HandLeft];
                                break;
                            case HandState.Lasso:
                                break;
                            case HandState.NotTracked:
                                break;
                            case HandState.Unknown:
                                break;
                        }

                        switch (body.HandRightState)
                        {
                            case HandState.Open:
                                
                                break;
                            case HandState.Closed:
                                if (currentAction == ActionState.NONE)
                                {
                                    handsClosed++;
                                }
                                rightHandState = HandState.Closed;
                                rightHandPosition = cameraPoints[JointType.HandRight];
                                break;
                            case HandState.Lasso:
                                break;
                            case HandState.NotTracked:
                                break;
                            case HandState.Unknown:
                                break;
                        }

                        if (handsClosed == 0)
                        {
                            currentAction = ActionState.NONE;
                        }
                        else if (handsClosed == 1)
                        {
                            currentAction = ActionState.ROTATE;
                        }
                        else
                        {
                            currentAction = ActionState.START;
                        }
                    }
                }


            }
        }


    }
}
