using UnityEngine;

public class Edge
{
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Displacement { get { return End - Start; } }

    public Edge()
    {
        Start = Vector2.zero;
        End = Vector2.zero;
    }
    public Edge(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    private static float Cross2D(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    // Operators
    public static bool operator ==(Edge e1, Edge e2)
    {
        return (e1.Start == e2.Start && e1.End == e2.End)
        || (e1.Start == e2.End && e1.End == e2.Start); // Reversed
    }

    public static bool operator !=(Edge e1, Edge e2)
    {
        return !(e1 == e2);
    }

    // Override methods
    public override bool Equals(object obj)
    {
        Edge other = obj as Edge;
        if (other == null)
        {
            return false;
        }
        else
        {
            return this == other;
        }
    }

    public override int GetHashCode()
    {
        return Start.GetHashCode() & End.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("Line from {0} to {1}\nDisplacement={2}",
        Start.ToString(), End.ToString(), Displacement.ToString());
    }

    public bool CollidesWith(Edge other)
    {
        Vector2 e1 = this.Start;
        Vector2 s1 = this.Displacement;
        Vector2 e2 = other.Start;
        Vector2 s2 = other.Displacement;

        float denom = Cross2D(s1, s2);

        //If denom == 0 the edges are parallel
        if (Mathf.Approximately(denom, 0f))
            return false;

        //Numerators for t and u
        Vector2 e2e1 = e2 - e1;
        float t = Cross2D(e2e1, s2) / denom;
        float u = Cross2D(e2e1, s1) / denom;

        //if pass all conditions
        return (t >= 0f && t <= 1f && u >= 0f && u <= 1f);
    }
}
