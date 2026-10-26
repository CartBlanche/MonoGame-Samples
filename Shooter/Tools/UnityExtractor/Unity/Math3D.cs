namespace UnityExtractor.Unity;

public readonly struct Vec3
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public Vec3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double Length
    {
        get { return Math.Sqrt(X * X + Y * Y + Z * Z); }
    }
}

public readonly struct Quat
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;
    public readonly double W;

    public Quat(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static Quat Identity
    {
        get { return new Quat(0, 0, 0, 1); }
    }

    public Quat Normalized()
    {
        double n = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        if (n < 1e-12)
        {
            return Identity;
        }

        return new Quat(X / n, Y / n, Z / n, W / n);
    }

    // Hamilton product: applies b first, then a.
    public static Quat operator *(Quat a, Quat b)
    {
        return new Quat(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
    }
}

/// <summary>3x3 matrix plus translation: v' = M * v + T.</summary>
public readonly struct Affine
{
    public readonly double[] M; // row-major 3x3
    public readonly Vec3 T;

    public Affine(double[] m, Vec3 t)
    {
        M = m;
        T = t;
    }

    public static Affine Identity
    {
        get { return new Affine(new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }, new Vec3(0, 0, 0)); }
    }

    /// <summary>Local TRS matrix: scale first, then rotate, then translate.</summary>
    public static Affine FromTrs(Vec3 pos, Quat rot, Vec3 scale)
    {
        Quat q = rot.Normalized();
        double x = q.X;
        double y = q.Y;
        double z = q.Z;
        double w = q.W;
        double[] r =
        {
            1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w),
            2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w),
            2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)
        };

        double[] m = new double[9];
        for (int row = 0; row < 3; row++)
        {
            m[row * 3 + 0] = r[row * 3 + 0] * scale.X;
            m[row * 3 + 1] = r[row * 3 + 1] * scale.Y;
            m[row * 3 + 2] = r[row * 3 + 2] * scale.Z;
        }

        return new Affine(m, pos);
    }

    /// <summary>this (parent) composed with child: world = parent * local.</summary>
    public Affine Compose(Affine child)
    {
        double[] m = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                double sum = 0;
                for (int k = 0; k < 3; k++)
                {
                    sum += M[i * 3 + k] * child.M[k * 3 + j];
                }

                m[i * 3 + j] = sum;
            }
        }

        Vec3 ct = child.T;
        Vec3 t = new Vec3(
            M[0] * ct.X + M[1] * ct.Y + M[2] * ct.Z + T.X,
            M[3] * ct.X + M[4] * ct.Y + M[5] * ct.Z + T.Y,
            M[6] * ct.X + M[7] * ct.Y + M[8] * ct.Z + T.Z);
        return new Affine(m, t);
    }

    /// <summary>Lossy world scale (column lengths, sign from determinant on X).</summary>
    public Vec3 LossyScale()
    {
        double sx = new Vec3(M[0], M[3], M[6]).Length;
        double sy = new Vec3(M[1], M[4], M[7]).Length;
        double sz = new Vec3(M[2], M[5], M[8]).Length;
        double det =
            M[0] * (M[4] * M[8] - M[5] * M[7])
            - M[1] * (M[3] * M[8] - M[5] * M[6])
            + M[2] * (M[3] * M[7] - M[4] * M[6]);
        if (det < 0)
        {
            sx = -sx;
        }

        return new Vec3(sx, sy, sz);
    }
}
