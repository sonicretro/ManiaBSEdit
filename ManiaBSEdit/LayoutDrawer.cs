using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ManiaBSEdit
{
	static class LayoutDrawer
	{
		public static ColorPalette Palette { get; private set; }
		public static Dictionary<SphereType, BitmapBits> SphereBmps { get; private set; } = new Dictionary<SphereType, BitmapBits>(5);
		public static BitmapBits[] StartBmps { get; private set; } = new BitmapBits[4];

		public static void Init()
		{
			using (Bitmap tmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				Palette = tmp.Palette;
			Bitmap[] bmplist = { Properties.Resources.blue, Properties.Resources.red, Properties.Resources.bumper, Properties.Resources.yellow, Properties.Resources.green, Properties.Resources.pink, Properties.Resources.ring };
			int palind = 1;
			for (int i = 0; i < bmplist.Length; i++)
			{
				bmplist[i].Palette.Entries.CopyTo(Palette.Entries, palind);
				BitmapBits bmp = new BitmapBits(bmplist[i]);
				bmp.IncrementIndexes(palind);
				SphereBmps[(SphereType)(i + 1)] = bmp;
				palind += 16;
			}
			for (int i = 0; i < bmplist.Length; i++)
			{
				BitmapBits bmp = new BitmapBits(SphereBmps[(SphereType)(i + 1)]);
				bmp.DrawBitmapComposited(SphereBmps[SphereType.Ring], 0, 0);
				SphereBmps[(SphereType)(i + 1) | SphereType.RingFlag] = bmp;
			}
			Palette.Entries[0] = Palette.Entries[1] = Color.Transparent;
			Bitmap face;
			switch (System.DateTime.Now.Second % 3)
			{
				case 1:
					face = Properties.Resources.tails;
					break;
				case 2:
					face = Properties.Resources.knuckles;
					break;
				default:
					face = Properties.Resources.sonic;
					break;
			}
			face.Palette.Entries.CopyTo(Palette.Entries, palind);
			BitmapBits facebmp = new BitmapBits(face);
			facebmp.IncrementIndexes(palind);
			palind += 16;
			bmplist = new[] { Properties.Resources.north, Properties.Resources.west, Properties.Resources.south, Properties.Resources.east };
			bmplist[0].Palette.Entries.CopyTo(Palette.Entries, palind);
			for (int i = 0; i < bmplist.Length; i++)
			{
				BitmapBits bmp = new BitmapBits(bmplist[i]);
				bmp.IncrementIndexes(palind);
				bmp.DrawBitmapBehind(facebmp, 0, 0);
				StartBmps[i] = bmp;
			}
		}

		public static BitmapBits DrawLayout(SphereType[,] layout, int gridsize, Rectangle? bounds = null)
		{
			int stX = bounds?.X ?? 0;
			int stY = bounds?.Y ?? 0;
			int width = bounds?.Width ?? layout.GetLength(0);
			int height = bounds?.Height ?? layout.GetLength(1);
			int off = (gridsize - 24) / 2;
			BitmapBits layoutbmp = new BitmapBits(width * gridsize, height * gridsize);
			for (int y = -gridsize / 2; y < layoutbmp.Height; y += gridsize * 2)
			{
				bool row = ((stX & 1) == 1) ^ ((stY & 1) == 1);
				for (int x = -gridsize / 2; x < layoutbmp.Width; x += gridsize)
				{
					layoutbmp.FillRectangle(1, x, row ? y : y + gridsize, gridsize, gridsize);
					row = !row;
				}
			}
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					SphereType sp = layout[x + stX, y + stY];
					if ((sp & ~SphereType.RingFlag) != SphereType.Empty)
						layoutbmp.DrawBitmapComposited(SphereBmps[sp], x * gridsize + off, y * gridsize + off);
				}
			return layoutbmp;
		}

		public static BitmapBits DrawLayout(SphereType?[,] layout, int gridsize)
		{
			int width = layout.GetLength(0);
			int height = layout.GetLength(1);
			int off = (gridsize - 24) / 2;
			BitmapBits layoutbmp = new BitmapBits(width * gridsize, height * gridsize);
			for (int y = -gridsize / 2; y < layoutbmp.Height; y += gridsize * 2)
				for (int x = -gridsize / 2; x < layoutbmp.Width; x += gridsize * 2)
					layoutbmp.FillRectangle(1, x, y, gridsize, gridsize);
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					SphereType? sp = layout[x, y];
					if (sp.HasValue && (sp.Value & ~SphereType.RingFlag) != SphereType.Empty)
						layoutbmp.DrawBitmapComposited(SphereBmps[sp.Value], x * gridsize + off, y * gridsize + off);
				}
			return layoutbmp;
		}

		public static BitmapBits DrawLayout(LayoutData layout, int gridsize, Rectangle? bounds = null)
		{
			BitmapBits layoutbmp = DrawLayout(layout.Layout, gridsize, bounds);
			int off = (gridsize - 24) / 2;
			layoutbmp.DrawBitmapComposited(StartBmps[layout.Angle], (layout.StartX - bounds?.X ?? 0) * gridsize + off, (layout.StartY - bounds?.Y ?? 0) * gridsize + off);
			return layoutbmp;
		}
	}
}
