using UnityEngine;

public static class IGB283Transform
{
    public static Matrix3x3 Rotate(float angle)
    {
        // Calculate sin and cos of the angle once
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        // Create a new matrix for rotation
        Matrix3x3 r = new Matrix3x3(
        new IGB283Vector(cos, -sin, 0),
        new IGB283Vector(sin, cos, 0),
        new IGB283Vector(0, 0, 1));
        return r;
    }

    public static Matrix3x3 Translate(IGB283Vector offset)
    {
        // Create a new matrix for rotation
        Matrix3x3 t = new Matrix3x3(
        new IGB283Vector(1, 0, offset.x),
        new IGB283Vector(0, 1, offset.y),
        new IGB283Vector(0, 0, 1));
        return t;
    }

    public static Matrix3x3 FlipX()
    {
        // Create a new matrix for flipping horizontally
        Matrix3x3 f = new Matrix3x3(
        new IGB283Vector(-1, 0, 0),
        new IGB283Vector(0, 1, 0),
        new IGB283Vector(0, 0, 1));
        return f;
    }
}