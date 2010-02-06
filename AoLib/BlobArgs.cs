using System;
using System.Collections;

namespace AoLib
{
	/// <summary>
	/// Summary description for BlobArgs.
	/// </summary>
	public class BlobArgs
	{
		private readonly ArrayList _args = null;

		public BlobArgs(String blob)
		{
			int offset = 0;
			ArrayList list = new ArrayList();
            byte[] data = System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(blob);

			System.Diagnostics.Trace.WriteLine(blob);
			System.Diagnostics.Trace.WriteLine(BitConverter.ToString(data));

			char c = PopChar(ref data, ref offset);

			while(c > Char.MinValue)
			{
				switch (c)
				{
					case 'S':
						list.Add(PopString(ref data, ref offset));
						break;

					default:
						break;
				}

				c = PopChar(ref data, ref offset);
			}

			this._args = list;
		}

		public Object[] Args
		{
			get 
			{
				if (this._args == null)
					return null;
				else 
					return this._args.ToArray(); 
			}
		}

		private Char PopChar(ref byte[] data, ref int offset)
		{
			try
			{
				offset += 2;
				return BitConverter.ToChar(data, offset - 2);
			}
			catch
			{
				return Char.MinValue;
			}
		}

		private byte PopByte(ref byte[] data, ref int offset)
		{
			offset++;
			return data[offset - 1];
		}
		private short PopByteShort(ref byte[] data, ref int offset)
		{
			byte i = PopByte(ref data, ref offset);
			byte[] s = new byte[] {i, 0};
			return BitConverter.ToInt16(s, 0);
		}
		private String PopString(ref byte[] data, ref int offset)
		{
			short len = PopByteShort(ref data, ref offset);
            String str = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(data, offset, len);
			offset += len;
			return str;
		}
	}
}
