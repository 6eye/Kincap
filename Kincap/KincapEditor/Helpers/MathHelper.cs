using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Windows.Media.Media3D;

namespace Kincap
{
    class MathHelper
    {

        public static double[] Quat2Deg(Quaternion vec)
        {
            double[] value = new double[3];
            value[0] = Math.Atan2(2 * (vec.W * vec.X + vec.Y * vec.Z), 1 - 2 * (Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2)));
            value[1] = Math.Asin(2 * (vec.W * vec.Y - vec.Z * vec.X));
            value[2] = Math.Atan2(2 * (vec.W * vec.Z + vec.X * vec.Y), 1 - 2 * (Math.Pow(vec.Y, 2) + Math.Pow(vec.Z, 2)));
            value[0] = value[0] * (180 / Math.PI);
            value[1] = value[1] * (180 / Math.PI);
            value[2] = value[2] * (180 / Math.PI);
            return value;
        }

        public static Matrix3D GetRotationMatrixX(double angle)
        {
            if (angle == 0.0)
            {
                return Matrix3D.Identity;
            }
            double sin = (double)Math.Sin(angle);
            double cos = (double)Math.Cos(angle);
            return new Matrix3D(
                                 1  , 0 , 0 , 0,
                                 0  , cos,-sin, 0,
                                 0  , sin, cos, 0,
                                 0  , 0 , 0 , 1
                                 );
        }


        public static Matrix3D GetRotationMatrixY(double angle)
        {
            if (angle == 0.0)
            {
                return Matrix3D.Identity;
            }
            double sin = (double)Math.Sin(angle);
            double cos = (double)Math.Cos(angle);
            return new Matrix3D(
                                cos , 0 , sin, 0,
                                0   , 1 , 0  , 0,
                                -sin, 0 , cos, 0,
                                0   , 0 , 0  , 1
                                );
        }

        public static Matrix3D GetRotationMatrixZ(double angle)
        {
            if (angle == 0.0)
            {
                return Matrix3D.Identity;
            }
            double sin = (double)Math.Sin(angle);
            double cos = (double)Math.Cos(angle);
            return new Matrix3D(
                                 cos, -sin, 0, 0,
                                 sin, cos, 0, 0,
                                 0, 0, 1, 0,
                                 0, 0, 0, 1);
        }

        public static Matrix3D GetRotationMatrix(double ax, double ay, double az)
        {
            Matrix3D my = Matrix3D.Identity;
            Matrix3D mz = Matrix3D.Identity;
            Matrix3D result = Matrix3D.Identity;

            if (ax != 0.0)
                result = GetRotationMatrixX(ax);

            if (ay != 0.0)
                my = GetRotationMatrixY(ay);

            if (az != 0.0)
                mz = GetRotationMatrixZ(az);

            if (my != null)
            {
                if (result != null)
                    result *= my;
                else
                    result = my;
            }

            if (mz != null)
            {
                if (result != null)
                    result *= mz;
                else
                    result = mz;
            }

            if (result != null)
                return result;
            else
                return Matrix3D.Identity;
        }


        public static Quaternion GetQuaternion(Vector3D v0, Vector3D v1)
        {
            Quaternion q = new Quaternion();
            // Copy, since cannot modify local
            v0.Normalize();
            v1.Normalize();

            double d = Vector3D.DotProduct(v0, v1);
            // If dot == 1, vectors are the same
            if (d >= 1.0f)
            {
                return Quaternion.Identity;
            }

            double s = Math.Sqrt((1 + d) * 2);
            double invs = 1 / s;

            Vector3D c = Vector3D.CrossProduct(v0, v1);

            q.X = c.X * invs;
            q.Y = c.Y * invs;
            q.Z = c.Z * invs;
            q.W = s * 0.5f;
            q.Normalize();

            return q;
        }

        public static Quaternion Vector42Quat(Vector4 vec)
        {
            Quaternion quat = new Quaternion();
            quat.W = vec.W;
            quat.X = vec.X;
            quat.Y = vec.Y;
            quat.Z = vec.Z;
            return quat;
        }

        public static double[] RotMatrix2Deg(Matrix4 mat)
        {
            double[] value = new double[3];
            // Source : http://social.msdn.microsoft.com/Forums/en-US/b644698d-bdec-47a2-867e-574cf84e5db7/what-is-the-default-sequence-of-hierarchical-rotation-matrix-eg-xyz-#b3946d0d-9658-4c2b-b14b-69e79070c7d2
            // https://en.wikipedia.org/wiki/Euler_angles#Rotation_matrix
            // Kinect matrix has the Tait-Bryan Convention with Y1 X2 Z3
            // Problem: There are always 2 options for rotation in 3D space, and therefore the degree values do not always correspond to the quaternions values!
            // Rotation around the y-axis
            value[0] = Math.Asin(-mat.M23);
            // Rotation around the x-axis
            value[1] = Math.Atan2(mat.M13 / Math.Cos(value[0]), mat.M33 / Math.Cos(value[0]));
            // Rotation around the z-axis
            value[2] = Math.Atan2(mat.M21 / Math.Cos(value[0]), mat.M22 / Math.Cos(value[0]));


            // To arrive at the same angle as the quaternions, you have to negate the angle
            value[0] = value[0] * -(180 / Math.PI);
            value[1] = value[1] * -(180 / Math.PI);
            value[2] = value[2] * -(180 / Math.PI);
            return value;
        }

        public static double[] Quat2Deg(Vector4 vec)
        {
            double[] value = new double[3];
            value[0] = Math.Atan2(2 * (vec.W * vec.X + vec.Y * vec.Z), 1 - 2 * (Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2)));
            value[1] = Math.Asin(2 * (vec.W * vec.Y - vec.Z * vec.X));
            value[2] = Math.Atan2(2 * (vec.W * vec.Z + vec.X * vec.Y), 1 - 2 * (Math.Pow(vec.Y, 2) + Math.Pow(vec.Z, 2)));
            value[0] = value[0] * (180 / Math.PI);
            value[1] = value[1] * (180 / Math.PI);
            value[2] = value[2] * (180 / Math.PI);
            return value;
        }

        public static double[] AddArray(double[] array1, double[] array2)
        {
            double[] result = new double[3]
            {
                array1[0] + array2[0],
                array1[1] + array2[1],
                array1[2] + array2[2]
            };
            return result;
        }

    }
}
