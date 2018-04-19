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

import android.app.Activity;
import android.app.ActivityManager;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ConfigurationInfo;
import android.graphics.Bitmap;
import android.opengl.*;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.Toast;

import com.codyrobertson.graphicsviewer.GraphicsViewer;
import com.codyrobertson.graphicsviewer.R;
import com.sony.smarteyeglass.extension.util.SmartEyeglassControlUtils;
import com.sonyericsson.extras.liveware.aef.registration.Registration;

import java.io.IOException;
import java.io.PrintWriter;
import java.io.BufferedReader;
import java.net.ServerSocket;
import java.net.Socket;

/**
 * The Graphics Viewer activity provides a button on the phone that starts
 * the  SmartEyeglass app.
 *
 * For demonstration, this also displays messages sent in the intent.
 */

public final class GraphicsViewerActivity extends Activity {

    private GLSurfaceView mGLSurfaceView;

    @Override
    public void onCreate(final Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.phonelayout);


        // When button is clicked, run the SmartEyeglass app
        Button btnGlass = (Button) findViewById(R.id.btnglass);
        btnGlass.setOnClickListener(new OnClickListener() {

            @Override
            public void onClick(final View v) {
                startExtension();
            }
        });

        /*
         * Check if activity was started with a message in the intent
         * If there is a message, show it as a Toast message
         */
        Bundle extras = getIntent().getExtras();
        if (extras != null) {
            String message = extras.getString("Message");
            Toast.makeText(getApplicationContext(), message, Toast.LENGTH_LONG)
                    .show();
        }

        /*
         * Make sure ExtensionService of your SmartEyeglass app has already
         * started.
         * This is normally started automatically when user enters your app
         * on SmartEyeglass, although you can initialize it early using
         * request intent.
         */
        if (GraphicsViewerExtensionService.Object == null) {
            Intent intent = new Intent(Registration.Intents
                    .EXTENSION_REGISTER_REQUEST_INTENT);
            Context context = getApplicationContext();
            intent.setClass(context, GraphicsViewerExtensionService.class);
            context.startService(intent);
        }


        mGLSurfaceView = new GLSurfaceView(this);
        //mGLSurfaceView = new PixelBuffer(138, 138, this);

        // Check if the system supports OpenGL ES 2.0.
        final ActivityManager activityManager = (ActivityManager) getSystemService(Context.ACTIVITY_SERVICE);
        final ConfigurationInfo configurationInfo = activityManager.getDeviceConfigurationInfo();
        final boolean supportsEs2 = configurationInfo.reqGlEsVersion >= 0x20000;

        ObjectRenderer renderer = new ObjectRenderer();

        if (supportsEs2)
        {
            // Request an OpenGL ES 2.0 compatible context.
            mGLSurfaceView.setEGLContextClientVersion(2);

            // Set the renderer to our demo renderer, defined below.
            mGLSurfaceView.setRenderer(renderer);
        }
        else
        {
            // This is where you could create an OpenGL ES 1.x compatible
            // renderer if you wanted to support both ES 1 and ES 2.
            return;
        }

        setContentView(mGLSurfaceView);

        //renderer.SavePixels(0, 0, 30, 30);

        //com.codyrobertson.graphicsviewer.GraphicsViewer viewer = new com.codyrobertson.graphicsviewer.GraphicsViewer();

        try {
            ServerSocket ss = new ServerSocket(65);
            Socket s = ss.accept();
            PrintWriter out = new PrintWriter(s.getOutputStream());
            //BufferedReader br = new BufferedReader()
            while (true) {

            }
        }
        catch (IOException ioe) {
            System.out.println(ioe.getMessage());
        }


        //startExtension();
    }

    /**
     *  Start the app with the message "Hello extension"
     */
    public void startExtension() {
        // Check ExtensionService is ready and referenced
        if (GraphicsViewerExtensionService.Object != null) {
            GraphicsViewerExtensionService.Object
                    .sendMessageToExtension("Hello extension");
        }
    }

    @Override
    protected void onResume() {
        // The activity must call the GL surface view's onResume() on activity onResume().
        super.onResume();
        mGLSurfaceView.onResume();
    }

    @Override
    protected void onPause() {
        // The activity must call the GL surface view's onPause() on activity onPause().
        super.onPause();
        mGLSurfaceView.onPause();
    }
}

