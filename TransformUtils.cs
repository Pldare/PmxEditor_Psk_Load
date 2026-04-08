/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2026/1/19
 * 时间: 13:58
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using System;

namespace psk_data
{
	/// <summary>
	/// Description of TransformUtils.
	/// </summary>
	public static class TransformUtils
	{
		/// <summary>
		/// 将单位四元数 (x, y, z, w) 转换为 3x3 旋转矩阵。
		/// 注意：假设输入四元数已归一化（或近似单位长度）。
		/// </summary>
		public static Matrix<double> QuatToMatrix(double x, double y, double z, double w)
		{
			// 可选：归一化四元数（提高数值稳定性）
			double norm = Math.Sqrt(x * x + y * y + z * z + w * w);
			if (Math.Abs(norm) > 1e-12)
			{
				x /= norm;
				y /= norm;
				z /= norm;
				w /= norm;
			}

			// 计算旋转矩阵元素（标准公式）
			double xx = x * x, yy = y * y, zz = z * z;
			double xy = x * y, xz = x * z, xw = x * w;
			double yz = y * z, yw = y * w, zw = z * w;

			var R = Matrix<double>.Build.DenseOfArray(new double[,]
			                                          {
			                                          	{ 1 - 2 * (yy + zz),     2 * (xy - zw),       2 * (xz + yw) },
			                                          	{ 2 * (xy + zw),         1 - 2 * (xx + zz),   2 * (yz - xw) },
			                                          	{ 2 * (xz - yw),         2 * (yz + xw),       1 - 2 * (xx + yy) }
			                                          });

			return R;
		}

		/// <summary>
		/// 从四元数和平移向量构建 4x4 齐次变换矩阵。
		/// </summary>
		/// <param name="rotQuat">四元数 (x, y, z, w)</param>
		/// <param name="transVec">平移向量 (tx, ty, tz)</param>
		public static Matrix<double> TransformMatrixFromRotTrans(
			Quaternion rotQuat,
			Vector3D transVec)
		{
			var M = Matrix<double>.Build.DenseIdentity(4);

			// 提取 xyzw
			double x = rotQuat.ImagX, y = rotQuat.ImagY, z = rotQuat.ImagZ, w = rotQuat.Real;

			// 构建旋转矩阵（同前）
			double xx = x * x, yy = y * y, zz = z * z;
			double xy = x * y, xz = x * z, xw = x * w;
			double yz = y * z, yw = y * w, zw = z * w;

			M[0, 0] = 1 - 2 * (yy + zz);
			M[0, 1] = 2 * (xy - zw);
			M[0, 2] = 2 * (xz + yw);

			M[1, 0] = 2 * (xy + zw);
			M[1, 1] = 1 - 2 * (xx + zz);
			M[1, 2] = 2 * (yz - xw);

			M[2, 0] = 2 * (xz - yw);
			M[2, 1] = 2 * (yz + xw);
			M[2, 2] = 1 - 2 * (xx + yy);

			// 平移
			M[0, 3] = transVec.X;
			M[1, 3] = transVec.Y;
			M[2, 3] = transVec.Z;

			return M;
		}
	}
}
