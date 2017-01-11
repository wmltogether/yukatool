﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace yuka.File {
	class ArchiveIO {
		public static Archive Read(Stream s) {
			BinaryReader br = new BinaryReader(s);
			Dictionary<string, MemoryStream> files = new Dictionary<string, MemoryStream>();

			s.Seek(0x08, SeekOrigin.Begin);
			int headerlength = br.ReadInt32();
			s.Seek(0x04, SeekOrigin.Current);
			int indexoffset = br.ReadInt32();
			int indexlength = br.ReadInt32();

			for(int i = 0; i < indexlength / 0x14; i++) {
				int curoffset = indexoffset + i * 0x14;
				s.Seek(curoffset, SeekOrigin.Begin);
				int nameoffset = br.ReadInt32();
				int namelength = br.ReadInt32();
				int dataoffset = br.ReadInt32();
				int datalength = br.ReadInt32();

				s.Seek(nameoffset, SeekOrigin.Begin);
				string name = Encoding.ASCII.GetString(br.ReadBytes(namelength - 1));
				s.Seek(dataoffset, SeekOrigin.Begin);
				byte[] data = br.ReadBytes(datalength);

				files[name] = new MemoryStream(data);
			}

			return new Archive(files);
		}

		public static void Write(Archive archive, Stream s) {
			BinaryWriter bw = new BinaryWriter(s);

			s.Write(Encoding.ASCII.GetBytes("YKC001\0\0"), 0x00, 0x08);
			bw.Write((int)0x18);
			s.Write(new byte[0x0C], 0x00, 0x0C);

			// dataoffset, datalength, nameoffset, namelength
			Dictionary<string, int[]> offsets = new Dictionary<string, int[]>();

			// Write data sector
			foreach(var file in archive.files) {
				int dataoffset = (int)s.Position;
				if(FlagCollection.current.Has('v')) {
					Console.WriteLine("Packing file: " + file.Key);
				}
				MemoryStream ms = archive.GetInputStream(file.Key);
				ms.CopyTo(s);
				//s.Write(data, 0, data.Length);
				offsets[file.Key] = (new int[] { dataoffset, (int)file.Value.Length, -1, file.Key.Length + 1 });
			}

			// Write name table
			foreach(var entry in offsets) {
				int nameoffset = (int)s.Position;
				s.Write(Encoding.ASCII.GetBytes(entry.Key), 0, Encoding.ASCII.GetByteCount(entry.Key));
				s.WriteByte(0);
				entry.Value[2] = nameoffset;
			}

			int indexoffset = (int)s.Position;

			// Write index
			foreach(var entry in offsets) {
				bw.Write(entry.Value[2]);
				bw.Write(entry.Value[3]);
				bw.Write(entry.Value[0]);
				bw.Write(entry.Value[1]);
				bw.Write((int)0x00);
			}

			// Update header
			int indexlength = (int)s.Position - indexoffset;
			bw.Seek(0x10, SeekOrigin.Begin);
			bw.Write(indexoffset);
			bw.Write(indexlength);

			bw.Close();
		}
	}
}
