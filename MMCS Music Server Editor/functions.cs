﻿using System;
using System.Linq;
using System.Text;

namespace MMCS_MSE
{
	class help_functions
	{
		public byte[] checksum32bit(byte[] data)
		{
			UInt32 acc = 0;
			if (data.Length % 4 > 0) { return BitConverter.GetBytes(acc); }

			for (int i = 0; i < data.Length; i += 4)
			{
				byte[] dbytes = new byte[] { data[i], data[i + 1], data[i + 2], data[i+ 3] };
				acc += BitConverter.ToUInt32(dbytes, 0);
			}
			return BitConverter.GetBytes(acc);
		}
		public byte[] checksum32bit(byte[] data1, byte[] data2)
		{
			byte[] data1_cs = checksum32bit(data1);
			byte[] data2_cs = checksum32bit(data2);
			return BitConverter.GetBytes(BitConverter.ToUInt32(data1_cs, 0) + BitConverter.ToUInt32(data2_cs, 0));
		}

		public byte[] HexStringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
				 .Where(x => x % 2 == 0)
				 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				 .ToArray();
		}

		public string ByteArrayToHexString(byte[] bytes)
		{
			return bytes.Select(b => b.ToString("X2"))
				.Aggregate((s1, s2) => s1 + s2);
		}

		public string ByteArrayToString(byte[] bytes)
		{
			string outstr = "";
			Encoding ascii = Encoding.ASCII;
			outstr = new string(ascii.GetChars(bytes));

			return outstr;
		}

		public string ByteArrayToString(byte[] bytes, string code_page)
		{
			string outstr = "";
			Encoding encode = Encoding.GetEncoding(code_page);
			outstr = new string(encode.GetChars(bytes));

			return outstr;
		}

		public byte[] StringToByteArray(string str, string code_page)
		{
			byte[] outarr = new byte[str.Length];
			Encoding encode = Encoding.GetEncoding(code_page);
			byte[] chars = encode.GetBytes(str);
			Array.Copy(chars, 0, outarr, 0, str.Length);
			return outarr;
		}

		public byte[] StringToByteArray(string str, int length)
		{
			byte[] outarr = new byte[length];
			Encoding ascii = Encoding.ASCII;
			byte[] chars = ascii.GetBytes(str);
			Array.Copy(chars, 0, outarr, 0, str.Length);
			return outarr;
		}

		public int ByteArrayLEToInt(byte[] bytearr)
		{
			Array.Reverse(bytearr);
			int outint = BitConverter.ToInt32(bytearr, 0);

			return outint;
		}

		public byte[] IntToByteArrayLE(int number)
		{
			byte[] outarr = new byte[4];
			outarr = BitConverter.GetBytes(number);
			Array.Reverse(outarr);

			return outarr;
		}

		public byte[] spliceByteArray(byte[] inbytearr, ref byte[] outbytearr, int offset, int length)
		{
			Array.Resize(ref outbytearr, length);
			Array.Copy(inbytearr, offset, outbytearr, 0, length);
			return outbytearr;
		}
	}
}
