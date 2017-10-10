using RSDKv5;
using System;
using System.IO;
using System.Linq;
using RSDKColor = RSDKv5.Color;
using DrawingColor = System.Drawing.Color;

namespace ManiaBSEdit
{
	public class LayoutData
	{
		private Scene scene;
		public SphereType[,] Layout { get; private set; }
		public byte Angle { get; set; }
		public ushort StartX { get; set; } = 0xF;
		public ushort StartY { get; set; } = 0xF;
		public int Perfect { get { return Layout.OfType<SphereType>().Count(a => a.HasFlag(SphereType.RingFlag)); } }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }
		public bool HasPal { get; private set; }
		public uint PaletteID { get; set; }
		public byte SkyAlpha { get; set; }
		public byte GlobeAlpha { get; set; }
		public DrawingColor PlayfieldA { get; set; }
		public DrawingColor PlayfieldB { get; set; }
		public DrawingColor BGColor1 { get; set; }
		public DrawingColor BGColor2 { get; set; }
		public DrawingColor BGColor3 { get; set; }

		public LayoutData()
		{
			using (MemoryStream ms = new MemoryStream(Properties.Resources.EmptyScene))
				scene = new Scene(ms);
			Init();
		}

		public LayoutData(string filename)
		{
			scene = new Scene(filename);
			Init();
		}

		private void Init()
		{
			SceneLayer layer = scene.Layers.Single(a => a.Name == "Playfield\0");
			Width = layer.Width;
			Height = layer.Height;
			Layout = new SphereType[Width, Height];
			for (ushort y = 0; y < Height; y++)
				for (ushort x = 0; x < Width; x++)
				{
					SphereType sp = (SphereType)(layer.Tiles[y][x] & 0x3FF);
					switch (sp)
					{
						case SphereType.StartN:
						case SphereType.StartW:
						case SphereType.StartS:
						case SphereType.StartE:
							Angle = (byte)(sp - SphereType.StartN);
							StartX = x;
							StartY = y;
							break;
						case (SphereType)0x3FF:
							break;
						default:
							Layout[x, y] = sp;
							break;
					}
				}
			SceneLayer rings = scene.Layers.Single(a => a.Name == "Ring Count\0");
			for (int y = 0; y < Math.Min(rings.Height, Height); y++)
				for (int x = 0; x < Math.Min(rings.Width, Width); x++)
					if ((SphereType)(rings.Tiles[y][x] & 0x3FF) == SphereType.Ring)
						Layout[x, y] |= SphereType.RingFlag;
			SceneEntity palent = scene.Objects.SingleOrDefault(a => a.Name.Name == "BSS_Palette")?.Entities[0];
			if (palent != null)
			{
				HasPal = true;
				PaletteID = palent.GetAttribute("paletteID").ValueVar;
				SkyAlpha = palent.GetAttribute("skyAlpha").ValueUInt8;
				GlobeAlpha = palent.GetAttribute("globeAlpha").ValueUInt8;
				RSDKColor tmp = palent.GetAttribute("playfieldA").ValueColor;
				PlayfieldA = DrawingColor.FromArgb(tmp.R, tmp.G, tmp.B);
				tmp = palent.GetAttribute("playfieldB").ValueColor;
				PlayfieldB = DrawingColor.FromArgb(tmp.R, tmp.G, tmp.B);
				tmp = palent.GetAttribute("bgColor1").ValueColor;
				BGColor1 = DrawingColor.FromArgb(tmp.R, tmp.G, tmp.B);
				tmp = palent.GetAttribute("bgColor2").ValueColor;
				BGColor2 = DrawingColor.FromArgb(tmp.R, tmp.G, tmp.B);
				tmp = palent.GetAttribute("bgColor3").ValueColor;
				BGColor3 = DrawingColor.FromArgb(tmp.R, tmp.G, tmp.B);
			}
		}

		public void Save(string filename)
		{
			SceneLayer layer = scene.Layers.Single(a => a.Name == "Playfield\0");
			for (ushort y = 0; y < Height; y++)
				for (ushort x = 0; x < Width; x++)
					layer.Tiles[y][x] = (ushort)(Layout[x, y] & ~SphereType.RingFlag);
			layer.Tiles[StartY][StartX] = (ushort)(SphereType.StartN + Angle);
			SceneLayer rings = scene.Layers.Single(a => a.Name == "Ring Count\0");
			for (int y = 0; y < Math.Min(rings.Height, Height); y++)
				for (int x = 0; x < Math.Min(rings.Width, Width); x++)
					rings.Tiles[y][x] = Layout[x, y].HasFlag(SphereType.RingFlag) ? (ushort)SphereType.Ring : (ushort)0;
			if (HasPal)
			{
				SceneEntity palent = scene.Objects.Single(a => a.Name.Name == "BSS_Palette").Entities[0];
				palent.GetAttribute("paletteID").ValueVar = PaletteID;
				palent.GetAttribute("skyAlpha").ValueUInt8 = SkyAlpha;
				palent.GetAttribute("globeAlpha").ValueUInt8 = GlobeAlpha;
				palent.GetAttribute("playfieldA").ValueColor = new RSDKColor(PlayfieldA.R, PlayfieldA.G, PlayfieldA.B);
				palent.GetAttribute("playfieldB").ValueColor = new RSDKColor(PlayfieldB.R, PlayfieldB.G, PlayfieldB.B);
				palent.GetAttribute("bgColor1").ValueColor = new RSDKColor(BGColor1.R, BGColor1.G, BGColor1.B);
				palent.GetAttribute("bgColor2").ValueColor = new RSDKColor(BGColor2.R, BGColor2.G, BGColor2.B);
				palent.GetAttribute("bgColor3").ValueColor = new RSDKColor(BGColor3.R, BGColor3.G, BGColor3.B);
			}
			scene.Write(filename);
		}

		public LayoutData Clone()
		{
			LayoutData result = (LayoutData)MemberwiseClone();
			result.Layout = (SphereType[,])Layout.Clone();
			return result;
		}

		public int WrapH(int x)
		{
			x %= Width;
			if (x < 0)
				x += Width;
			return x;
		}

		public int WrapV(int y)
		{
			y %= Height;
			if (y < 0)
				y += Height;
			return y;
		}
	}

	public enum SphereType { Empty, Blue, Red, Bumper, Yellow, Green, Pink, Ring, StartN, StartW, StartS, StartE, RingFlag = 0x80 }
}
