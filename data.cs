/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2026/1/19
 * 时间: 9:42
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using MathNet.Numerics.LinearAlgebra;
//using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Euclidean;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static PskModel.MorphInfo;

namespace PskModel
{
	public class ImportBone
	{
		public int index;
		public Bone psk_bone;
		public ImportBone parent;
		public MathNet.Spatial.Euclidean.Quaternion local_rotation;
		public Vector3D local_translation;
		public Matrix<double> world_rotation_matrix;
		public Matrix<double> world_matrix;
		public ImportBone(int input_index,Bone bone)
		{
			index = input_index;
			psk_bone = bone;
            Quaternion rr = bone.RRotation();

            local_rotation = rr.Conjugate();
			local_translation=new Vector3D(bone.Location.X,bone.Location.Y,bone.Location.Z);

        }
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct Color
	{
		public byte r, g, b, a;
		public Color(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
		public float[] Normalized()
		{
			return 	new float[] { r / 255f, g / 255f, b / 255f, a / 255f };
		}
		public override string ToString(){
			return string.Format("({0},{1},{2},{3})",r,g,b,a);
		}
	}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct Quaternion { public float x, y, z, w; public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }

    // Section 头（20字节name + 3×int）
    //public class SectionHeader
    //{
    //	public string Name;
    //	public int TypeFlags;
    //	public int DataSize;
    //	public int DataCount;
    //}
    public class Wedge : IEquatable<Wedge>
    {
        public int PointIndex { get; set; }
        public float U { get; set; }
        public float V { get; set; }
        public int MaterialIndex { get; set; }

        public Wedge(int pointIndex, float u, float v, int materialIndex = 0)
        {
            PointIndex = pointIndex;
            U = u;
            V = v;
            MaterialIndex = materialIndex;
        }

        // 模拟 __hash__：C# 中通过 GetHashCode + Equals 实现
        public override int GetHashCode()
        {
            // 使用类似 Python 的字符串哈希方式（简化版）
            string repr = string.Format("{0}-{1}-{2}-{3}", PointIndex, U, V, MaterialIndex);
            return repr.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Wedge);
        }

        public bool Equals(Wedge other)
        {
            if (other == null) return false;
            return PointIndex == other.PointIndex &&
                U == other.U &&
                V == other.V &&
                MaterialIndex == other.MaterialIndex;
        }
    }

    // ----------------------------
    // Structs (对应 ctypes.Structure)
    // ----------------------------

    [Serializable]
    public struct Wedge16
    {
        public uint PointIndex;     // c_uint32
        public float U;             // c_float
        public float V;             // c_float
        public byte MaterialIndex;  // c_uint8
        public sbyte Reserved;      // c_int8
        public short Padding2;      // c_int16
    }

    [Serializable]
    public struct Wedge32
    {
        public uint PointIndex;     // c_uint32
        public float U;             // c_float
        public float V;             // c_float
        public uint MaterialIndex;  // c_uint32
    }

    [Serializable]
    public struct Face
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] WedgeIndices; // c_uint16 * 3

        public byte MaterialIndex;        // c_uint8
        public byte AuxMaterialIndex;     // c_uint8
        public int SmoothingGroups;       // c_int32

        // 构造函数（可选，用于初始化数组）
        public Face(ushort i0, ushort i1, ushort i2, byte mat, byte aux, int smooth)
        {
            WedgeIndices = new ushort[3] { i0, i1, i2 };
            MaterialIndex = mat;
            AuxMaterialIndex = aux;
            SmoothingGroups = smooth;
        }
    }

    [Serializable] // _pack_ = 1
    public struct Face32
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] WedgeIndices; // c_uint32 * 3

        public byte MaterialIndex;        // c_uint8
        public byte AuxMaterialIndex;     // c_uint8
        public int SmoothingGroups;       // c_int32

        public Face32(uint i0, uint i1, uint i2, byte mat, byte aux, int smooth)
        {
            WedgeIndices = new uint[3] { i0, i1, i2 };
            MaterialIndex = mat;
            AuxMaterialIndex = aux;
            SmoothingGroups = smooth;
        }
    }

    [Serializable]
    public struct Material
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Name; // c_char * 64
        public int TextureIndex;
        public int PolyFlags;
        public int AuxMaterial;
        public int AuxFlags;
        public int LodBias;
        public int LodStyle;

        public string GetName()
        {
            int len = Array.IndexOf(Name, (byte)0);
            if (len < 0) len = 64;
            return System.Text.Encoding.ASCII.GetString(Name, 0, len);
        }

        public void SetName(string value)
        {
            for (int i = 0; i < 64; i++) Name[i] = 0;
            if (!string.IsNullOrEmpty(value))
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(value);
                Array.Copy(src, Name, Math.Min(src.Length, 64));
            }
        }
    }

    [Serializable]
    public struct Bone
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Name;
        public int Flags;
        public int ChildrenCount;
        public int ParentIndex;
        public LQuaternion Rotation;
        public LVector3D Location;
        public float Length;
        public LVector3D Size;
        public Quaternion RRotation()
        {
            return new Quaternion(Rotation.W, Rotation.X, Rotation.Y, Rotation.Z);
        }
        public string GetName()
        {
            int len = Array.IndexOf(Name, (byte)0);
            if (len < 0) len = 64;
            return System.Text.Encoding.ASCII.GetString(Name, 0, len);
        }

        public void SetName(string value)
        {
            for (int i = 0; i < 64; i++) Name[i] = 0;
            if (!string.IsNullOrEmpty(value))
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(value);
                Array.Copy(src, Name, Math.Min(src.Length, 64));
            }
        }
    }

    [Serializable]
    public struct Weight
    {
        public float WeightValue;   // renamed to avoid conflict
        public int PointIndex;
        public int BoneIndex;
    }

    [Serializable]
    public struct MorphInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Name;
        public int VertexCount;

        public string GetName()
        {
            int len = Array.IndexOf(Name, (byte)0);
            if (len < 0) len = 64;
            return System.Text.Encoding.ASCII.GetString(Name, 0, len);
        }

        public void SetName(string value)
        {
            for (int i = 0; i < 64; i++) Name[i] = 0;
            if (!string.IsNullOrEmpty(value))
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(value);
                Array.Copy(src, Name, Math.Min(src.Length, 64));
            }
        }
    }
    public struct LQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public LQuaternion(float x, float y, float z,float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
    public struct LVector3D
    {
        public float X;
        public float Y;
        public float Z;

        public LVector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    public struct LVector2D
    {
        public float X;
        public float Y;
        //public double Z;

        public LVector2D(float x, float y)
        {
            X = x;
            Y = y;
            //Z = z;
        }
    }


    [Serializable]
    public struct MorphData
    {
        public LVector3D PositionDelta;
        public LVector3D TangentZDelta;
        public int PointIndex;
    }
    [Serializable]
    public struct Section
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Name;
        public int TypeFlags;
        public int DataSize;
        public int DataCount;

        public Section(string name)
        {
            Name = new byte[20];
            if (!string.IsNullOrEmpty(name))
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(name);
                Array.Copy(src, Name, Math.Min(src.Length, 20));
            }

            TypeFlags = 1999801;
            DataSize = 0;
            DataCount = 0;
        }

        public string GetName()
        {
            int len = Array.IndexOf(Name, (byte)0);
            if (len < 0) len = 20;
            return System.Text.Encoding.ASCII.GetString(Name, 0, len);
        }

        public void SetName(string value)
        {
            for (int i = 0; i < 20; i++) Name[i] = 0;
            if (!string.IsNullOrEmpty(value))
            {
                byte[] src = System.Text.Encoding.ASCII.GetBytes(value);
                Array.Copy(src, Name, Math.Min(src.Length, 20));
            }
        }
    }
    public class Psk
	{
		public ArrayList wold_bones = new ArrayList();
		public List<LVector3D> points = new List<LVector3D>();
		public List<Wedge> wedges = new List<Wedge>();
		public List<Face> faces = new List<Face>();
		public List<Material> materials = new List<Material>();
		public List<Weight> weights = new List<Weight>();
		public List<Bone> bones = new List<Bone>();
		public List<LVector2D> extraUvs = new List<LVector2D>();
		public List<Color> vertexColors = new List<Color>();
		public List<LVector3D> vertexNormals = new List<LVector3D>();
		public List<MorphInfo> morphInfos = new List<MorphInfo>();
		public List<MorphData> morphData = new List<MorphData>();
		public List<string> materialReferences = new List<string>();

		public bool HasExtraUvs
		{
			get
			{
				return extraUvs.Count > 0;
			}
		}
		public bool HasVertexColors
		{
			get
			{
				return vertexColors.Count > 0;
			}
		}
		public bool HasVertexNormals
		{
			get
			{
				return vertexNormals.Count > 0;
			}
		}
		public bool HasMaterialReferences
		{
			get
			{
				return materialReferences.Count > 0;
			}
		}
		public bool HasMorphData
		{
			get
			{
				return morphInfos.Count > 0;
			}
		}
	}
}