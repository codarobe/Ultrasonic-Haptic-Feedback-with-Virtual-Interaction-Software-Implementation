using System;

public interface GraphicsManipulator
{
    void Translate(float x, float y, float z);

    void Rotate(float x, float y, float z);

    void Scale(float factor);
}
