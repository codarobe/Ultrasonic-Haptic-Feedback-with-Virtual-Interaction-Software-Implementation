/*
Copyright (c) 2011, Sony Mobile Communications Inc.
Copyright (c) 2014, Sony Corporation

 All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
 list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
 this list of conditions and the following disclaimer in the documentation
 and/or other materials provided with the distribution.

 * Neither the name of the Sony Mobile Communications Inc.
 nor the names of its contributors may be used to endorse or promote
 products derived from this software without specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */


package com.codyrobertson.graphicsviewer;

import java.util.List;
import java.util.Timer;
import java.util.TimerTask;

import android.app.ActivityManager;
import android.content.Context;
import android.content.pm.ConfigurationInfo;
import android.hardware.SensorManager;
import android.os.Handler;
import android.util.Log;
import android.view.View;

//import com.example.sonymobile.extension.Constants;
//import com.example.sonymobile.extension.GLRenderExtensionService;
//import com.example.sonymobile.graphics.ModelRenderer;
//import com.example.sonymobile.graphics.PixelBuffer;
import android.opengl.GLSurfaceView.Renderer;
import com.sony.smarteyeglass.SmartEyeglassControl;
import com.sony.smarteyeglass.extension.util.SmartEyeglassControlUtils;
import com.sony.smarteyeglass.extension.util.SmartEyeglassEventListener;
import com.sonyericsson.extras.liveware.aef.control.Control;
import com.sonyericsson.extras.liveware.aef.registration.Registration.SensorTypeValue;
import com.sonyericsson.extras.liveware.aef.sensor.Sensor;
import com.sonyericsson.extras.liveware.extension.util.control.ControlExtension;
import com.sonyericsson.extras.liveware.extension.util.sensor.AccessorySensor;
import com.sonyericsson.extras.liveware.extension.util.sensor.AccessorySensorEvent;
import com.sonyericsson.extras.liveware.extension.util.sensor.AccessorySensorEventListener;
import com.sonyericsson.extras.liveware.extension.util.sensor.AccessorySensorException;
import com.sonyericsson.extras.liveware.extension.util.sensor.AccessorySensorManager;

//import javax.microedition.khronos.egl.EGLConfig;
//import javax.microedition.khronos.opengles.GL10;

/**
 * This is the SmartEyeglass control extension for 3D Model Viewer.
 */
public final class GLRenderControlSEG extends ControlExtension {
    // OpenGL related variables
    //ModelRenderer mModelRenderer;
    Renderer renderer;
    PixelBuffer pixelBuffer;
    Timer frameUpdateTimer;
    Context context;

    // Sensor and rotation related variables
    public static float[] mRotationMatrix = new float[16];
    public static float[] orientationVector = new float[3];
    public static float[] headingVector = new float[3];
    public boolean updateHeading=true;
    AccessorySensorManager sensorManager;
    AccessorySensor mRotationVectorSensor;

    // UI Management Values
    public float rotationFactor=2.5f;
    public int modelCursor=0;
    public boolean isExtensionRunning = false;

    private final SmartEyeglassControlUtils utils;

    // Listens and passes incoming sensor data to sensorDataOperation method
    private final AccessorySensorEventListener listener = new AccessorySensorEventListener() {
        @Override
        public void onSensorEvent(final AccessorySensorEvent sensorEvent) {
            sensorDataOperation(sensorEvent);
        }
    };

    // Listens for SmartEyeglass generic events
    private final SmartEyeglassEventListener eventListener = new SmartEyeglassEventListener() {
        // Stops extension when no 3d model found in the library.
        @Override
        public void onDialogClosed(final int code) {
            requestStop();
        }
    };

    // This TimerTask calls for display refreshing method update
    private class UpdateCaller extends TimerTask {
        Handler handler;
        GLRenderControlSEG ref;

        public UpdateCaller(Handler handler, GLRenderControlSEG ref) {
            super();
            this.handler = handler;
            this.ref = ref;
        }

        @Override
        public void run() {
            handler.post(new Runnable() {
                @Override
                public void run() {
                    ref.update();
                }
            });
        }
    }

    public GLRenderControlSEG(final Context context,final String hostAppPackageName) {
        super(context, hostAppPackageName);
        System.out.println("Began smarteyeglass controller");
        utils = new SmartEyeglassControlUtils(hostAppPackageName, eventListener);
        utils.activate(context);
        sensorManager = new AccessorySensorManager(context, hostAppPackageName);

        // Get RotationVector sensor
        mRotationVectorSensor = sensorManager.getSensor(SensorTypeValue.ROTATION_VECTOR);

        // Initialize rotation matrix
        mRotationMatrix[ 0] = 1;
        mRotationMatrix[ 4] = 1;
        mRotationMatrix[ 8] = 1;
        mRotationMatrix[12] = 1;

        final ActivityManager activityManager = (ActivityManager) context.getSystemService(Context.ACTIVITY_SERVICE);
        final ConfigurationInfo configurationInfo = activityManager.getDeviceConfigurationInfo();
        final boolean supportsEs2 = configurationInfo.reqGlEsVersion >= 0x20000;

        if (!supportsEs2) {
            throw new UnsupportedOperationException("ES2 not supported");
        }

        this.context = context;

        renderer = new ObjectRenderer();
        initPixelBuffer();
        startViewer();

    }

    // Initiates necessary operations to keep rendering in relation to sensor data
    public void startViewer()
    {
        if(renderer != null && isExtensionRunning == false)
        {
            // Set screen to stay awake
            setScreenState(Control.Intents.SCREEN_STATE_ON);

            // Calls to register sensor
            register();

            // Clear display
            clearDisplay();

            // Initializes and starts the Timer
            frameUpdateTimer = new Timer();
            frameUpdateTimer.schedule(new UpdateCaller(new Handler(), this), 200, 200);
            isExtensionRunning = true;
        }
    }

    public void endViewer()
    {
        // If frame update timer is set, we cancel and purge the task to free memory before extension is destroyed.
        if(frameUpdateTimer != null)
        {
            frameUpdateTimer.cancel();
            frameUpdateTimer.purge();
        }
        mRotationVectorSensor.unregisterListener();
        isExtensionRunning = false;

        setScreenState(Control.Intents.SCREEN_STATE_AUTO);

        updateHeading=true;
    }

    // Creates an instance of PixelBuffer class and sets current ModelRenderer as Renderer
    public void initPixelBuffer()
    {
        // You can set it to any resolution up to 419x138, which is the resolution of SmartEyeglass
        pixelBuffer=new PixelBuffer(138,138, context);
        pixelBuffer.setRenderer(renderer);
    }

    // Handles RotationVector sensor data
    public void sensorDataOperation(AccessorySensorEvent event)
    {
        // Convert RotationVector from Quaternion to Euclidian geometry
        float[] quat = new float[4];
        SensorManager.getQuaternionFromVector(quat, event.getSensorValues());
        float[] switchedQuat = new float[] {quat[1], quat[2], quat[3], quat[0]};
        float[] rotationMatrix = new float[16];
        SensorManager.getRotationMatrixFromVector(rotationMatrix, switchedQuat);
        SensorManager.getOrientation(rotationMatrix, orientationVector);

        // Update initial heading if it is needed
        if(updateHeading)
        {
            headingVector=orientationVector.clone();
            updateHeading=false;
        }

        // Substract initial heading from current rotation to calculate required rotation
        orientationVector[0]=orientationVector[0]-headingVector[0];
        orientationVector[1]=orientationVector[1]-headingVector[1];
        orientationVector[2]=orientationVector[2]-headingVector[2];

        // Convert radian to degrees
        orientationVector[0]=(float) Math.toDegrees(GLRenderControlSEG.orientationVector[0])*rotationFactor;
        orientationVector[1]=(float) Math.toDegrees(GLRenderControlSEG.orientationVector[1])*rotationFactor;
        orientationVector[2]=(float) Math.toDegrees(GLRenderControlSEG.orientationVector[2])*rotationFactor;
    }

    // Requests for a new frame render and sends rendered frame to SmartEyeglass
    public void update()
    {
        utils.showBitmap(pixelBuffer.getBitmap(),140,0);
    }

    @Override
    public void onResume() {
        startViewer();
    }

    @Override
    public void onPause() {
        endViewer();
    }

    @Override
    public void onDestroy() {
        endViewer();
        utils.deactivate();
    }

    @Override
    public void onTap(final int action, final long timeStamp) {
        // Setting updateHeading true, will make sure heading will be updated on next sensor event.
        updateHeading=true;
    }


    // Registers RotationVector sensor with fastest rate.
    private void register() {
        try {
            mRotationVectorSensor.registerFixedRateListener(listener,Sensor.SensorRates.SENSOR_DELAY_FASTEST);
        } catch (AccessorySensorException e) {
            Log.d(Constants.LOG_TAG, "Failed to register listener", e);
        }
    }

    // for smart watch?
    public void requestStart()
    {
        startRequest();
    }

    /*
     *  Used to exit application:
     *  When there is no 3d model in the library
     *  When SmartWatch 2 application requests it to stop
     */
    public void requestStop()
    {
        // Stop the extension
        stopRequest();
    }
}
