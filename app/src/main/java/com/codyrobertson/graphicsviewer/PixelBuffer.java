package com.codyrobertson.graphicsviewer;

import static javax.microedition.khronos.egl.EGL10.*;
import java.nio.IntBuffer;

import javax.microedition.khronos.egl.EGL10;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.egl.EGLContext;
import javax.microedition.khronos.egl.EGLDisplay;
import javax.microedition.khronos.egl.EGLSurface;
import javax.microedition.khronos.opengles.GL10;

import android.graphics.Bitmap;
import android.graphics.Matrix;
import android.opengl.EGL14;
//import android.opengl.GLES20;
import android.opengl.GLSurfaceView;
import android.util.Log;
import android.content.Context;

public class PixelBuffer extends GLSurfaceView {
    final static String TAG = "PixelBuffer";
    final static boolean LIST_CONFIGS = false;

    GLSurfaceView.Renderer mRenderer; // borrow this interface
    int mWidth, mHeight;
    Bitmap mBitmap;

    EGL10 mEGL;
    EGLDisplay mEGLDisplay;
    EGLConfig[] mEGLConfigs;
    EGLConfig mEGLConfig;
    EGLContext mEGLContext;
    EGLSurface mEGLSurface;
    GL10 mGL;
    //GLES20 newGL;

    String mThreadOwner;

    public PixelBuffer(int width, int height, Context context) {
        super(context);
        mWidth = width;
        mHeight = height;

        int[] version = new int[2];
        int[] attribList = new int[] {
                EGL_WIDTH, mWidth,
                EGL_HEIGHT, mHeight,
                EGL_NONE
        };

        setEGLContextClientVersion(2);

        // No error checking performed, minimum required code to demonstrate logic
        mEGL = (EGL10) EGLContext.getEGL();
        mEGLDisplay = mEGL.eglGetDisplay(EGL_NO_DISPLAY);
        mEGL.eglInitialize(mEGLDisplay, version);
        mEGLConfig = chooseConfig(); // Choosing a config is a little more complicated
        mEGLContext = mEGL.eglCreateContext(mEGLDisplay, mEGLConfig, EGL_NO_CONTEXT, null);
        mEGLSurface = mEGL.eglCreatePbufferSurface(mEGLDisplay, mEGLConfig,  attribList);
        mEGL.eglMakeCurrent(mEGLDisplay, mEGLSurface, mEGLSurface, mEGLContext);
        mGL = (GL10) mEGLContext.getGL();
        //newGL = (GLES20) mEGLContext.getGL();
        // Record thread owner of OpenGL context
        mThreadOwner = Thread.currentThread().getName();
    }


    public void setRenderer(GLSurfaceView.Renderer renderer) {
        mRenderer = renderer;

        // Checking if current thread owns OpenGL context
        if (!Thread.currentThread().getName().equals(mThreadOwner)) {
            Log.e(TAG, "setRenderer: This thread does not own the OpenGL context.");
            return;
        }

        // Call the renderer initialization routines
        mRenderer.onSurfaceCreated(mGL, mEGLConfig);
        mRenderer.onSurfaceChanged(mGL, mWidth, mHeight);
    }

    public Bitmap getBitmap() {
        // Check if Renderer is not set
        if (mRenderer == null) {
            Log.e(TAG, "getBitmap: Renderer was not set.");
            return null;
        }

        // Checking if current thread owns OpenGL context
        if (!Thread.currentThread().getName().equals(mThreadOwner)) {
            Log.e(TAG, "getBitmap: This thread does not own the OpenGL context.");
            return null;
        }

        // Call renderer to draw frame
        mRenderer.onDrawFrame(mGL);
        return SavePixels(0, 0, mWidth, mHeight, mGL);
    }

    static IntBuffer buffer;
    static Bitmap inBitmap;

    // Saves rendered frame from OpenGL as a bitmap
    public static Bitmap SavePixels(int x, int y, int width, int height, GL10 gl) {
        if (inBitmap == null) {
            inBitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
        }
        if (buffer == null || buffer.capacity() != width * height) {
            buffer = IntBuffer.allocate(inBitmap.getByteCount());
        }
        gl.glReadPixels(0, 0, width, height, gl.GL_RGBA, gl.GL_UNSIGNED_BYTE, buffer);

        inBitmap.copyPixelsFromBuffer(buffer); // Create bitmap with upside down image

        Matrix matrix = new Matrix();
        matrix.preScale(1.0f, -1.0f); // Scaling: x = x, y = -y, i.e. vertically flip

        inBitmap = Bitmap.createBitmap(inBitmap, 0, 0, width, height, matrix, true); // New bitmap, using the matrix to flip it

        return inBitmap;
    }

    private EGLConfig chooseConfig() {
        int[] attribList = new int[] {
                EGL_DEPTH_SIZE, 0,
                //EGL_RENDERABLE_TYPE, EGL14.EGL_OPENGL_ES2_BIT,
                EGL_STENCIL_SIZE, 0,
                EGL_RED_SIZE, 8,
                EGL_GREEN_SIZE, 8,
                EGL_BLUE_SIZE, 8,
                EGL_ALPHA_SIZE, 8,
                EGL_NONE
        };

        // No error checking performed, minimum required code to demonstrate logic
        int[] numConfig = new int[1];
        mEGL.eglChooseConfig(mEGLDisplay, attribList, null, 0, numConfig);
        int configSize = numConfig[0];
        mEGLConfigs = new EGLConfig[configSize];
        mEGL.eglChooseConfig(mEGLDisplay, attribList, mEGLConfigs, configSize, numConfig);

        if (LIST_CONFIGS) {
            listConfig();
        }

        return mEGLConfigs[0];  // Best match is usually the first configuration
    }

    private void listConfig() {
        Log.i(TAG, "Config List {");

        for (EGLConfig config : mEGLConfigs) {
            int d, s, r, g, b, a;

            // You can expand on this logic to dump other attributes
            d = getConfigAttrib(config, EGL_DEPTH_SIZE);
            s = getConfigAttrib(config, EGL_STENCIL_SIZE);
            r = getConfigAttrib(config, EGL_RED_SIZE);
            g = getConfigAttrib(config, EGL_GREEN_SIZE);
            b = getConfigAttrib(config, EGL_BLUE_SIZE);
            a = getConfigAttrib(config, EGL_ALPHA_SIZE);
            Log.i(TAG, "    <d,s,r,g,b,a> = <" + d + "," + s + "," +
                    r + "," + g + "," + b + "," + a + ">");
        }

        Log.i(TAG, "}");
    }

    private int getConfigAttrib(EGLConfig config, int attribute) {
        int[] value = new int[1];
        return mEGL.eglGetConfigAttrib(mEGLDisplay, config,attribute, value)? value[0] : 0;
    }

}
