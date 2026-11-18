namespace GodotExtract;

/// <summary>Row-major 4x4 matrix using the XNA/MonoGame row-vector convention (v' = v * M, translation in M41..M43).</summary>
public sealed class Mat4
{
    public readonly double[] M = new double[16];
    public static Mat4 Identity() { var m = new Mat4(); m.M[0] = m.M[5] = m.M[10] = m.M[15] = 1; return m; }

    /// <summary>
    /// .tscn Transform3D(r0x,r0y,r0z, r1x,r1y,r1z, r2x,r2y,r2z, ox,oy,oz): the Basis is stored as three ROWS and Godot
    /// uses column vectors (v' = B*v + o). XNA uses row vectors, so M = B^T: M row i = B column i = (r0i, r1i, r2i).
    /// </summary>
    public static Mat4 FromTransform3D(IReadOnlyList<double> t)
    {
        var m = new Mat4();
        for (int i = 0; i < 3; i++)       // i = axis (column of B)
            for (int j = 0; j < 3; j++)   // j = component
                m.M[i * 4 + j] = t[j * 3 + i];
        m.M[12] = t[9]; m.M[13] = t[10]; m.M[14] = t[11]; m.M[15] = 1;
        return m;
    }

    public static Mat4 Translation(double x, double y, double z) { var m = Identity(); m.M[12] = x; m.M[13] = y; m.M[14] = z; return m; }

    /// <summary>a * b: apply a first, then b. World = child * parent.</summary>
    public static Mat4 Mul(Mat4 a, Mat4 b)
    {
        var r = new Mat4();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                double s = 0;
                for (int k = 0; k < 4; k++) s += a.M[i * 4 + k] * b.M[k * 4 + j];
                r.M[i * 4 + j] = s;
            }
        return r;
    }

    public double[] TransformPoint(double x, double y, double z) => new[]
    {
        x * M[0] + y * M[4] + z * M[8] + M[12],
        x * M[1] + y * M[5] + z * M[9] + M[13],
        x * M[2] + y * M[6] + z * M[10] + M[14],
    };

    public Mat4 Clone() { var m = new Mat4(); Array.Copy(M, m.M, 16); return m; }
    public float[] ToFloats() => M.Select(d => (float)d).ToArray();
}
