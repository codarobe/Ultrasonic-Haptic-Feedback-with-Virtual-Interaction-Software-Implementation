using System;

public interface GraphicsManipulator
{
    public void translate(double x, double y, double z);

    public void rotate(double x, double y, double z);

    public void scale(double x, double y, double z);
}
