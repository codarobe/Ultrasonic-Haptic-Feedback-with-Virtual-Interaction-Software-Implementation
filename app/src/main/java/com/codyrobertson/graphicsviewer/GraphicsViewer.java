package com.codyrobertson.graphicsviewer;


import android.opengl.EGL14;
import android.util.Log;

import javax.microedition.khronos.egl.EGL10;
import javax.microedition.khronos.egl.EGLContext;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.egl.EGLSurface;
import javax.microedition.khronos.egl.EGLDisplay;

public class GraphicsViewer {

    EGLConfig eglConfig;
    EGLSurface eglSurface;
    EGLContext eglContext;
    EGLDisplay eglDisplay;

    EGL10 mEGL;

    public GraphicsViewer() {
        System.out.println("Cast EGL instance");
        mEGL = (EGL10) javax.microedition.khronos.egl.EGLContext.getEGL();
        setupEGL(30, 30);
    }

    public void setupEGL(int w, int h) {
        System.out.println("Set up EGL instance");
        final int confAttr[] = {
                EGL10.EGL_RENDERABLE_TYPE, EGL14.EGL_OPENGL_ES2_BIT,
                EGL10.EGL_SURFACE_TYPE, EGL10.EGL_PBUFFER_BIT,
                EGL10.EGL_RED_SIZE, 8,
                EGL10.EGL_GREEN_SIZE, 8,
                EGL10.EGL_BLUE_SIZE, 8,
                EGL10.EGL_ALPHA_SIZE, 8,
                EGL10.EGL_DEPTH_SIZE, 16,
                EGL10.EGL_NONE
        };

        final int ctxAttr[] = {
                EGL14.EGL_CONTEXT_CLIENT_VERSION, 2,
                EGL10.EGL_NONE
        };

        int[] eglMajorMinorVers = new int[2];
        int numConfigs;

        eglDisplay = mEGL.eglGetDisplay(EGL10.EGL_DEFAULT_DISPLAY);
        mEGL.eglInitialize(eglDisplay, eglMajorMinorVers);

        Log.i("GraphicsViewer","EGL init with version " + eglMajorMinorVers[0] + "." + eglMajorMinorVers[1]);

        //mEGL.eglChooseConfig(eglDisplay, confAttr, 1, 2)
    }
}
