using System;
using System.Linq;
using System.Text;

namespace MMCS_MSE
{
	class help_functions
	{
		public byte[] checksum32bit(byte[] data)
		{
			string cc = ToBase32String(data);

			byte[] cs = new byte[4];
			int hash = data.GetHashCode();
			if (data.Length % 4 > 0) { return cs; }

			for (int i = 0; i < data.Length; i += 4)
			{
				cs[0] += data[i];
				cs[1] += data[i + 1];
				cs[2] += data[i + 2];
				cs[3] += data[i + 3];
			}
			return cs;
		}

		public static string ToBase32String(byte[] bytes)
		{
			string ValidChars = "QAZ2WSX3" + "EDC4RFV5" + "TGB6YHN7" + "UJM8K9LP";

			StringBuilder sb = new StringBuilder();         // holds the base32 chars
			byte index;
			int hi = 5;
			int currentByte = 0;

			while (currentByte < bytes.Length)
			{
				// do we need to use the next byte?
				if (hi > 8)
				{
					// get the last piece from the current byte, shift it to the right
					// and increment the byte counter
					index = (byte)(bytes[currentByte++] >> (hi - 5));
					if (currentByte != bytes.Length)
					{
						// if we are not at the end, get the first piece from
						// the next byte, clear it and shift it to the left
						index = (byte)(((byte)(bytes[currentByte] << (16 - hi)) >> 3) | index);
					}

					hi -= 3;
				}
				else if (hi == 8)
				{
					index = (byte)(bytes[currentByte++] >> 3);
					hi -= 3;
				}
				else
				{

					// simply get the stuff from the current byte
					index = (byte)((byte)(bytes[currentByte] << (8 - hi)) >> 3);
					hi += 5;
				}

				sb.Append(ValidChars[index]);
			}

			return sb.ToString();
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
