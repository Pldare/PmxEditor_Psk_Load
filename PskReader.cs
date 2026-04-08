/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2026/1/19
 * 时间: 9:47
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace PskModel
{
	public static class PskReader
	{
		//private const int EXPECTED_TYPE_FLAGS = 1999801;
        public static unsafe T ReadStruct<T>(byte[] buffer, int offset) where T : struct
        {
            fixed (byte* ptr = &buffer[offset])
            {
                return (T)Marshal.PtrToStructure(new IntPtr(ptr), typeof(T));
            }
        }
        public static unsafe void ReadTypes<T>(BinaryReader reader, Section section, List<T> output)
            where T : struct
        {
            int totalSize = section.DataSize * section.DataCount;
            if (totalSize <= 0) return;

            byte[] buffer = reader.ReadBytes(totalSize);
            int structSize = Marshal.SizeOf(typeof(T));

            // 验证大小是否匹配（可选）
            if (section.DataSize != structSize)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Section data_size ({0}) does not match struct size ({1}) for {2}",
                        section.DataSize,
                        structSize,
                        typeof(T).Name
                    )
                );
            }

            for (int i = 0; i < section.DataCount; i++)
            {
                int offset = i * structSize;
                T item = ReadStruct<T>(buffer, offset);
                output.Add(item);
            }
        }
        public static Psk Load(string filePath)
		{
			var psk = new Psk();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // 读取 Section（32 字节）
                    byte[] sectionBytes = reader.ReadBytes(Marshal.SizeOf(typeof(Section)));
                    if (sectionBytes.Length < Marshal.SizeOf(typeof(Section)))
                        break; // 文件结束或损坏

                    Section section = ReadStruct<Section>(sectionBytes, 0);

                    string blockName = section.GetName(); // 我们之前为 Section 添加了 GetName()
					

                    if (blockName == "ACTRHEAD")
                    {
                        // pass: do nothing
                    }
                    else if (blockName == "PNTS0000")
                    {
                        ReadTypes(reader, section, psk.points);
                    }
                    else if (blockName == "VTXW0000")
                    {
                        int wedge16Size = Marshal.SizeOf(typeof(Wedge16));
                        int wedge32Size = Marshal.SizeOf(typeof(Wedge32));

                        if (section.DataSize == wedge16Size)
                        {
                            // 读取 Wedge16，但目标是 psk.wedges（List<Wedge>）
                            var temp = new List<Wedge16>();
                            ReadTypes(reader, section, temp);
                            // 转换为 Psk.Wedge 对象（因为 psk.wedges 是 List<Wedge>）
                            foreach (var w in temp)
                            {
                                psk.wedges.Add(new Wedge(
                                    (int)w.PointIndex,
                                    w.U,
                                    w.V,
                                    w.MaterialIndex
                                ));
                            }
                        }
                        else if (section.DataSize == wedge32Size)
                        {
                            var temp = new List<Wedge32>();
                            ReadTypes(reader, section, temp);
                            foreach (var w in temp)
                            {
                                psk.wedges.Add(new Wedge(
                                    (int)w.PointIndex,
                                    w.U,
                                    w.V,
                                    (int)w.MaterialIndex
                                ));
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("Unrecognized wedge format");
                        }
                    }
                    else if (blockName == "FACE0000")
                    {
                        ReadTypes(reader, section, psk.faces);
                    }
                    else if (blockName == "MATT0000")
                    {
                        ReadTypes(reader, section, psk.materials);
                    }
                    else if (blockName == "REFSKELT")
                    {
                        ReadTypes(reader, section, psk.bones);
                    }
                    else if (blockName == "RAWWEIGHTS")
                    {
                        ReadTypes(reader, section, psk.weights);
                    }
                    else if (blockName == "FACE3200")
                    {
                        // 注意：psk.faces 是 List<Face>，但 Face32 是不同结构
                        // 你可能需要单独字段，或转换
                        // 这里假设你希望将 Face32 转为 Face（但数据不兼容！）
                        // 更合理做法：Psk 应支持两种 face 列表，或统一用 Face32
                        // 为简化，我们跳过或报错，或扩展 Psk

                        // 暂时：读取为 Face32 并忽略（或存储到新字段）
                        // 由于原 Python 也 append 到 psk.faces（类型不一致！），这可能是 bug
                        // 实际中，PSK 不会同时有 FACE0000 和 FACE3200

                        // 我们这里抛出警告或跳过
                        throw new NotSupportedException("FACE3200 is not supported in this implementation. Consider extending Psk.");
                    }
                    else if (blockName == "VERTEXCOLOR")
                    {
                        ReadTypes(reader, section, psk.vertexColors);
                    }
                    else if (blockName.StartsWith("EXTRAUVS"))
                    {
                        ReadTypes(reader, section, psk.extraUvs);
                    }
                    else if (blockName == "VTXNORMS")
                    {
                        ReadTypes(reader, section, psk.vertexNormals);
                    }
                    else if (blockName == "MRPHINFO")
                    {
                        ReadTypes(reader, section, psk.morphInfos);
                    }
                    else if (blockName == "MRPHDATA")
                    {
                        ReadTypes(reader, section, psk.morphData);
                    }
                    else
                    {
                        // Skip unknown section
                        long skipBytes = (long)section.DataSize * section.DataCount;
                        reader.BaseStream.Seek(skipBytes, SeekOrigin.Current);
                    }
                }
            }
            foreach (var posinfo in localbone2world(psk)) {
				psk.wold_bones.Add(new double[]{posinfo.X,posinfo.Y,posinfo.Z});
			}
			return psk;
		}
		public static Vector3D[] localbone2world(Psk model)
		{
			PskModel.ImportBone[] import_bones=new ImportBone[model.bones.Count];
			//var joints = new [model.bones.Count];
			Vector3D[] world_pos = new Vector3D[model.bones.Count];
			//var scatter = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerSize = 6 };
			
			for (int i = 0; i < model.bones.Count; i++) {
				//model.bones[i].ParentIndex = Math.Max(0,model.bones[i].ParentIndex);
				import_bones[i] = new ImportBone(i,model.bones[i]);
			}
			
			foreach (ImportBone bone in import_bones) {
				Matrix<double> local_matrix = psk_data.TransformUtils.TransformMatrixFromRotTrans(bone.local_rotation,bone.local_translation);
				//Console.WriteLine(local_matrix.ToString());
				if (bone.psk_bone.ParentIndex == -1) {
					bone.world_matrix = local_matrix;
					//Console.WriteLine(string.Format("{0} is -1",bone.psk_bone.Name));
				}
				else
				{
					ImportBone parent = import_bones[bone.psk_bone.ParentIndex];
					bone.world_matrix = parent.world_matrix * local_matrix;
				}
				bone.world_rotation_matrix = bone.world_matrix.SubMatrix(0, 3, 0, 3);
				double x = bone.world_matrix.At(0, 3);
				double y = bone.world_matrix.At(1, 3);
				double z = bone.world_matrix.At(2, 3);
				//Console.WriteLine(string.Format("=====\n{0}\n{1} {2} {3}\n ",bone.psk_bone.Name,x,y,z));
				//joints[i] = new DataPoint3D(x,y,z);
				//scatter.Points.Add(new ScatterPoint(x, z,1));
				world_pos[bone.index] = new Vector3D(x,y,z);
			}
			return world_pos;
		}
		//private static SectionHeader ReadSection(BinaryReader reader)
		//{
		//	var name = ReadFixedString(reader, 20);
		//	int typeFlags = reader.ReadInt32();
		//	int dataSize = reader.ReadInt32();
		//	int dataCount = reader.ReadInt32();
		//	return new SectionHeader { Name = name, TypeFlags = typeFlags, DataSize = dataSize, DataCount = dataCount };
		//}

		//private static SectionHeader PeekSection(BinaryReader reader)
		//{
		//	long pos = reader.BaseStream.Position;
		//	var sec = ReadSection(reader);
		//	//reader.BaseStream.Position = pos+se;
		//	return sec;
		//}

		//private static string ReadFixedString(BinaryReader reader, int length)
		//{
		//	byte[] bytes = reader.ReadBytes(length);
		//	int nullIndex = Array.IndexOf(bytes, (byte)0);
		//	if (nullIndex >= 0) Array.Resize(ref bytes, nullIndex);
		//	return Encoding.ASCII.GetString(bytes);
		//}

		//private static Psk.Wedge ReadWedge16(BinaryReader reader)
		//{
		//	uint pointIndex = reader.ReadUInt32();
		//	float u = reader.ReadSingle();
		//	float v = reader.ReadSingle();
		//	byte matIndex = reader.ReadByte();
		//	reader.ReadSByte();   // reserved
		//	reader.ReadInt16();    // padding
		//	return new Psk.Wedge((int)pointIndex, u, v, matIndex);
		//}

		//private static Psk.Wedge ReadWedge32(BinaryReader reader)
		//{
		//	uint pointIndex = reader.ReadUInt32();
		//	float u = reader.ReadSingle();
		//	float v = reader.ReadSingle();
		//	uint matIndex = reader.ReadUInt32();
		//	return new Psk.Wedge((int)pointIndex, u, v, (int)matIndex);
		//}

		//private static Psk.Face ReadFace16(BinaryReader reader)
		//{
		//	return new Psk.Face
		//	{
		//		I0 = reader.ReadUInt16(),
		//		I1 = reader.ReadUInt16(),
		//		I2 = reader.ReadUInt16(),
		//		MaterialIndex = reader.ReadByte(),
		//		AuxMaterialIndex = reader.ReadByte(),
		//		SmoothingGroups = reader.ReadInt32()
		//	};
		//}

		//private static Psk.Face ReadFace32(BinaryReader reader)
		//{
		//	return new Psk.Face
		//	{
		//		I0 = (ushort)reader.ReadUInt32(),
		//		I1 = (ushort)reader.ReadUInt32(),
		//		I2 = (ushort)reader.ReadUInt32(),
		//		MaterialIndex = reader.ReadByte(),
		//		AuxMaterialIndex = reader.ReadByte(),
		//		SmoothingGroups = reader.ReadInt32()
		//	};
		//}

		//private static Psk.Material ReadMaterial(BinaryReader reader)
		//{
		//	return new Psk.Material(ReadFixedString(reader, 64))
		//	{
		//		TextureIndex = reader.ReadInt32(),
		//		PolyFlags = reader.ReadInt32(),
		//		AuxMaterial = reader.ReadInt32(),
		//		AuxFlags = reader.ReadInt32(),
		//		LodBias = reader.ReadInt32(),
		//		LodStyle = reader.ReadInt32()
		//	};
		//}

		//private static Psk.Bone ReadBone(BinaryReader reader)
		//{
		//	string name = ReadFixedString(reader, 64);
		//	int flags = reader.ReadInt32();
		//	int ccount = reader.ReadInt32();
		//	int pindex = reader.ReadInt32();
		//	float rx = reader.ReadSingle();
		//	float ry = reader.ReadSingle();
		//	float rz = reader.ReadSingle();
		//	float rw = reader.ReadSingle();
		//	return new Psk.Bone
		//	{
		//		Name = name,
		//		Flags = flags,
		//		ChildrenCount = ccount,
		//		ParentIndex = pindex,
		//		Rotation = new Quaternion(rw,rx,ry,rz),
		//		Location = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
		//		Length = reader.ReadSingle(),
		//		Size = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
		//	};
		//}

		//private static Psk.Weight ReadWeight(BinaryReader reader)
		//{
		//	return new Psk.Weight
		//	{
		//		WeightValue = reader.ReadSingle(),
		//		PointIndex = reader.ReadInt32(),
		//		BoneIndex = reader.ReadInt32()
		//	};
		//}

		//private static Psk.MorphInfo ReadMorphInfo(BinaryReader reader)
		//{
		//	return new Psk.MorphInfo
		//	{
		//		Name = ReadFixedString(reader, 64),
		//		VertexCount = reader.ReadInt32()
		//	};
		//}

		//private static Psk.MorphData ReadMorphData(BinaryReader reader)
		//{
		//	return new Psk.MorphData
		//	{
		//		PositionDelta = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
		//		TangentZDelta = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
		//		PointIndex = reader.ReadInt32()
		//	};
		//}
	}
}
