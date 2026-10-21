using System;

namespace rt;

public class Quaternion(double w, double x, double y, double z)
{
    public static readonly Quaternion NONE = new(0, 1, 0, 0);
    public double W { get; set; } = w;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Z { get; set; } = z;

    public Quaternion Normalize()
    {
        var a = Math.Sqrt(W*W+X*X+Y*Y+Z*Z);
        W /= a;
        X /= a;
        Y /= a;
        Z /= a;
        return this;
    }

    public static Quaternion FromAxisAngle(double aa, Vector axis)
    {
        Vector norm_axis = axis.Normalize();
        
        // A quaternion for a rotation of angle aa around an axis uses half the angle:
        //     q = ( cos(aa/2),  sin(aa/2) * axis )
        var half_angle = aa / 2.0;
        var sin_half =  Math.Sin(half_angle);
        
        // Scalar part (W): cos(aa/2)
        var new_W = Math.Cos(half_angle);
        
        // Vector part (X, Y, Z): the unit axis scaled by sin(aa/2)
        var new_X = sin_half * norm_axis.X;
        var  new_Y = sin_half * norm_axis.Y;
        var  new_Z = sin_half * norm_axis.Z;
        
        return new  Quaternion(new_W, new_X, new_Y, new_Z);
}
}