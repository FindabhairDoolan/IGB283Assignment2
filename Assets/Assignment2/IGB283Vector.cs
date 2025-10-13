using UnityEngine;

public class IGB283Vector
{
    public float x, y, z;

    //Constructor
    public IGB283Vector(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public IGB283Vector(Vector3 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
        this.z = vector.z;
    }

    public Vector3 ToUnityVector()
    {
        return new Vector3(x, y, z);
    }

    //Implicit conversions fixes when a Vector3/Vector2 is passed where IGB283Vector is expected
    public static implicit operator IGB283Vector(Vector3 v) => new IGB283Vector(v.x, v.y, v.z);
    public static implicit operator IGB283Vector(Vector2 v) => new IGB283Vector(v.x, v.y, 1f);

    //Explicit conversion back to vector2/3
    public static explicit operator Vector2(IGB283Vector v) => new Vector2(v.x, v.y);
    public static explicit operator Vector3(IGB283Vector v) => new Vector3(v.x, v.y, v.z);

    //Addition
    public static IGB283Vector operator +(IGB283Vector a, IGB283Vector b)
    {
        return new IGB283Vector(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    //Subtraction
    public static IGB283Vector operator -(IGB283Vector a, IGB283Vector b)
    {
        return new IGB283Vector(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    //Negation
    public static IGB283Vector operator -(IGB283Vector v)
    {
        return new IGB283Vector(-v.x, -v.y, -v.z);
    }

    //vector scalar multiplication
    public static IGB283Vector operator *(IGB283Vector v, float s) =>
    new IGB283Vector(v.x * s, v.y * s, v.z * s);

    //scalar vector multiplication
    public static IGB283Vector operator *(float s, IGB283Vector v) =>
        new IGB283Vector(v.x * s, v.y * s, v.z * s);

    //Equate
    public static bool operator ==(IGB283Vector a, IGB283Vector b)
    {
        return Mathf.Approximately(a.x, b.x) &&
               Mathf.Approximately(a.y, b.y) &&
               Mathf.Approximately(a.z, b.z);
    }

    //Not equal
    public static bool operator !=(IGB283Vector a, IGB283Vector b) => !(a == b);

    //Equals
    public override bool Equals(object obj)
    {
        return obj is IGB283Vector other && this == other;
    }

    //Hash
    public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;

    //Dot Product
    public static float Dot(IGB283Vector a, IGB283Vector b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    //Cross Product
    public static IGB283Vector Cross(IGB283Vector a, IGB283Vector b)
    {
        return new IGB283Vector(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    //indexer
    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                _ => throw new System.IndexOutOfRangeException("Invalid IGB283Vector index!")
            };
        }
        set
        {
            switch (index)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                case 2: z = value; break;
                default: throw new System.IndexOutOfRangeException("Invalid IGB283Vector index!");
            }
        }
    }

    //String for easy viewing
    public override string ToString() => $"({x:F3}, {y:F3}, {z:F3})";

    //Square Magnitude Helper for Transformation Script
    public static float SqrMagnitude(IGB283Vector v) => v.x * v.x + v.y * v.y + v.z * v.z;

    //Normalization
    public static IGB283Vector Normalize(IGB283Vector v)
    {
        float mag = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        if (mag < 1e-8f) return new IGB283Vector(0f, 0f, 0f);
        return new IGB283Vector(v.x / mag, v.y / mag, v.z / mag);
    }

    //Vector Scaler
    public static IGB283Vector Scale(IGB283Vector v, float s) =>
        new IGB283Vector(v.x * s, v.y * s, v.z * s);


}
