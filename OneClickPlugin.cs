/*
 * 由SharpDevelop创建
 * 用户： 吃爆米花的小熊
 * 日期: 1997/7/24
 * 
 * 遵循WTPFL协议
 * 删除空行^\s*\n
 */
using MathNet.Spatial.Euclidean;
using PEPlugin;
using PEPlugin.Form;
using PEPlugin.Pmd;
using PEPlugin.Pmx;
using PEPlugin.View;
using PEPlugin.Vmd;
using PEPlugin.Vme;
using psk_data;
using PskModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ocp //OneClickPlugin 一键发动式插件，无窗口
{
	public class OneClickPlugin:IPEPlugin, IPEPluginOption
	{
		/// <summary>
		/// PE操作接口声明
		/// </summary>
		//-----------------------------------------------------------ここから-----------------------------------------------------------
		public IPEPluginHost host;
		public IPEBuilder builder;
		public IPEShortBuilder bd;
		public IPEConnector connect;
		public IPEXPmd pex;
		public IPXPmx PMX;
		public IPEPmd PMD;
		public IPEFormConnector Form;
		public IPXPmxViewConnector PMXView;
		public IPEPMDViewConnector PMDView;
		//-----------------------------------------------------------ここまで-----------------------------------------------------------
		public string Description
		{
			get { return "ocp-psk"; }
		}
		public string Name
		{
			get { return "ocp-psk"; }
		}
		public PEPlugin.IPEPluginOption Option
		{
			get { return this; }
		}
		public void Run(PEPlugin.IPERunArgs args)
		{
			try
			{
				this.host = args.Host;
				this.builder = this.host.Builder;
				this.bd = this.host.Builder.SC;
				this.connect = this.host.Connector;
				this.pex = this.connect.Pmd.GetCurrentStateEx();
				this.PMD = this.connect.Pmd.GetCurrentState();
				this.PMX = this.connect.Pmx.GetCurrentState();
				this.Form = this.connect.Form;
				this.PMDView = this.connect.View.PMDView;
				//-----------------------------------------------------------ここから-----------------------------------------------------------
				// 编写主代码
				//MessageBox.Show("Hello World!");
				//Color testcolor = new Color(0x10,0x10,0x10,0x10);
				//MessageBox.Show(testcolor.ToString());
				OpenFileDialog dialog = new OpenFileDialog();
				dialog.Title = "请选择Psk";
				dialog.Filter = "Psk(*.psk,*.*)|*.psk;*.*";
				dialog.Multiselect = false; // 允许选择多个文件
				string file_name = "";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					file_name = dialog.FileName; // 获取选择的文件路径
				}
				float resize = 0.1f;
				Psk model = PskReader.Load(file_name);
				foreach (LVector3D point in model.points) {
					IPXVertex Vertex=(IPXVertex)PEStaticBuilder.Pmx.Vertex();
					Vertex.Position.X = (float)(point.X*resize);
					Vertex.Position.Y = (float)(point.Z*resize);
					Vertex.Position.Z = (float)(point.Y*resize);
					this.PMX.Vertex.Add(Vertex);
				}
				for (int i = 0; i < model.wedges.Count; i++)
				{
					PEPlugin.SDX.V2 wuv = new PEPlugin.SDX.V2(model.wedges[i].U, model.wedges[i].V);
					this.PMX.Vertex[i].UV = wuv;
				}
                for (int i = 0; i < model.bones.Count; i++)
				{
					//var bone = import_bones[i];
					double[] wpos = (double[])model.wold_bones[i];
					
                    //}
					IPXBone b =(IPXBone)PEStaticBuilder.Pmx.Bone();
					b.Name = Encoding.UTF8.GetString(model.bones[i].Name).TrimEnd('\0');
					b.Parent = this.PMX.Bone[model.bones[i].ParentIndex + 1];
                    b.Position.X = (float)wpos[0]* resize;
					b.Position.Y = (float)wpos[2] * resize;
                    b.Position.Z = (float)wpos[1] * resize;
                    this.PMX.Bone.Add(b);
                }
                //weight
                Dictionary<int, Dictionary<int, float>> weight_group = new Dictionary<int, Dictionary<int, float>>();
				foreach (Weight wei in model.weights)
				{
                    if (!weight_group.TryGetValue(wei.PointIndex, out var boneWeights))
                    {
                        boneWeights = new Dictionary<int, float>();
                        weight_group[wei.PointIndex] = boneWeights;
                    }

                    // 如果同一顶点+骨骼出现多次，可以选择累加或覆盖。这里选择覆盖（通常数据不会重复）
                    boneWeights[wei.BoneIndex] = wei.WeightValue;
                }
                foreach (int vertexId in weight_group.Keys)
				{
                    var boneWeights = weight_group[vertexId];
                    //var sortedBones = boneWeights.OrderBy(b => b.Key).ToList();
                    int boneCount = boneWeights.Count;
					//var boneIds = sortedBones.Select(b => b.Key);
                    //var weightStr = sortedBones.Select(b => b.Value);
                    if (boneCount==1)
					{
						var boneid1 = boneWeights.ElementAt(0).Key;
						var boneweight1 = boneWeights[boneid1];
						this.PMX.Vertex[vertexId].Bone1 = this.PMX.Bone[boneid1+1];
                        this.PMX.Vertex[vertexId].Weight1 = 1.0f;
                    }
					if (boneCount==2)
					{
                        var boneid1 = boneWeights.ElementAt(0).Key;
                        var boneweight1 = boneWeights[boneid1];
                        var boneid2 = boneWeights.ElementAt(1).Key;
                        var boneweight2 = boneWeights[boneid2];
                        this.PMX.Vertex[vertexId].Bone1 = this.PMX.Bone[boneid1 + 1];
						this.PMX.Vertex[vertexId].Weight1 = boneweight1;
                        this.PMX.Vertex[vertexId].Bone2 = this.PMX.Bone[boneid2 + 1];
                        this.PMX.Vertex[vertexId].Weight2 = 1.0f-boneweight1;
                    }
					if (boneCount==3)
					{
                        var boneid1 = boneWeights.ElementAt(0).Key;
                        var boneweight1 = boneWeights[boneid1];
                        var boneid2 = boneWeights.ElementAt(1).Key;
                        var boneweight2 = boneWeights[boneid2];
                        var boneid3 = boneWeights.ElementAt(2).Key;
                        var boneweight3 = boneWeights[boneid3];
                        //var boneid4 = boneWeights.ElementAt(3).Key;
                        //var boneweight4 = boneWeights[boneid4];
                        this.PMX.Vertex[vertexId].Bone1 = this.PMX.Bone[boneid1 + 1];
                        this.PMX.Vertex[vertexId].Weight1 = boneweight1;
                        this.PMX.Vertex[vertexId].Bone2 = this.PMX.Bone[boneid2 + 1];
                        this.PMX.Vertex[vertexId].Weight2 = boneweight2;
                        this.PMX.Vertex[vertexId].Bone3 = this.PMX.Bone[boneid3 + 1];
                        this.PMX.Vertex[vertexId].Weight3 = 1.0f - boneweight1 - boneweight2;
                        this.PMX.Vertex[vertexId].Bone4 = this.PMX.Bone[boneid1 + 1];
						this.PMX.Vertex[vertexId].Weight4 = 0.0f;
                    }
					if (boneCount==4)
					{
                        var boneid1 = boneWeights.ElementAt(0).Key;
                        var boneweight1 = boneWeights[boneid1];
                        var boneid2 = boneWeights.ElementAt(1).Key;
                        var boneweight2 = boneWeights[boneid2];
                        var boneid3 = boneWeights.ElementAt(2).Key;
                        var boneweight3 = boneWeights[boneid3];
                        var boneid4 = boneWeights.ElementAt(3).Key;
                        var boneweight4 = boneWeights[boneid4];
                        this.PMX.Vertex[vertexId].Bone1 = this.PMX.Bone[boneid1 + 1];
                        this.PMX.Vertex[vertexId].Weight1 = boneweight1;
                        this.PMX.Vertex[vertexId].Bone2 = this.PMX.Bone[boneid2 + 1];
                        this.PMX.Vertex[vertexId].Weight2 = boneweight2;
                        this.PMX.Vertex[vertexId].Bone3 = this.PMX.Bone[boneid3 + 1];
                        this.PMX.Vertex[vertexId].Weight3 = boneweight3;
                        this.PMX.Vertex[vertexId].Bone4 = this.PMX.Bone[boneid4 + 1];
                        this.PMX.Vertex[vertexId].Weight4 = 1.0f - boneweight1 - boneweight2 - boneweight3;
                    }
                }


                foreach (PskModel.Wedge Wedge in model.wedges) {
					this.PMX.Vertex[Wedge.PointIndex].UV.U=Wedge.U;
					this.PMX.Vertex[Wedge.PointIndex].UV.V=Wedge.V;
				}
				foreach (PskModel.Material material in model.materials) {
					IPXMaterial Material = (IPXMaterial)PEStaticBuilder.Pmx.Material();
					Material.Name =  Encoding.UTF8.GetString(material.Name).TrimEnd('\0');
					Material.Diffuse.R = 1.0f;
					Material.Diffuse.G = 1.0f;
					Material.Diffuse.B = 1.0f;
					Material.Diffuse.A = 1.0f;
					
					Material.Ambient.R = 0.25f;
					Material.Ambient.G = 0.5f;
					Material.Ambient.B = 0.5f;
					
					
					foreach (PskModel.Face face in model.faces)
					{
						IPXFace Face = (IPXFace)PEStaticBuilder.Pmx.Face();
						Face.Vertex1=this.PMX.Vertex[face.WedgeIndices[0]];
						Face.Vertex2=this.PMX.Vertex[face.WedgeIndices[1]];
						Face.Vertex3=this.PMX.Vertex[face.WedgeIndices[2]];
						Material.Faces.Add(Face);
					}
					this.PMX.Material.Add(Material);
				}
				//foreach (Psk.Face face in model.faces) {
				//face.WedgeIndices[0]
				//IPXVertex Face=(IPXVertex)PEStaticBuilder.Pmx.;
				//Face.
				//}
				//-----------------------------------------------------------ここまで-----------------------------------------------------------
				//モデル・画面を更新します。
				this.Update();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "报错", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		/// <summary>
		/// モデル・画面を更新します。
		/// </summary>
		public void Update()
		{
			this.connect.Pmx.Update(this.PMX);
			this.connect.Form.UpdateList(UpdateObject.All);
			this.connect.View.PMDView.UpdateModel();
			this.connect.View.PMDView.UpdateView();
		}
		public string Version
		{
			get { return "0.1 lisomn"; } //版本号
		}
		public void Dispose()
		{
		}
		public bool Bootup
		{
			get { return false; } //插件是否自启动，即PE启动后自动执行本插件
		}
		public bool RegisterMenu
		{
			get { return true; } //是否在插件面板注册显示
		}
		public string RegisterMenuText
		{
			get { return "ocp-psk"; } //在插件面板显示的插件名
		}
	}
}
