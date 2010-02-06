using System;
using System.Text;
using System.Collections;
using System.Net;

namespace AoLib
{
	/// <summary>
	/// Summary description for AoString.
	/// </summary>
	public class AoString
	{
		protected String _str;
		protected short _len;
        protected Encoding enc = Encoding.GetEncoding("iso-8859-1");
		public AoString(byte[] data): this(data, 0) {}
		public AoString(byte[] data, int StartIndex)
		{
			if (data == null || data.Length - StartIndex < 3) { return; }

			this._len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, StartIndex));
			this._str = this.enc.GetString(data, 2 + StartIndex, this._len);
		}
		public AoString(String str)
		{
			
			this._str = str;
			if (str == null)
			{
				this._len = 0;
			}
			else
			{
				this._len = (short)str.Length;
			}
		}
		public String Value
		{
			get { return (this._str == null ? String.Empty : this._str); }
		}
		public short Length
		{
			get { return this._len; }
		}
		public int TotalLength
		{
			get { return this.Length + BitConverter.GetBytes(this._len).Length; }
		}
		override public String ToString()
		{
			StringBuilder ret = new StringBuilder(this.Value);
			return ret.ToString();
		}
		public byte[] GetBytes()
		{
			if (this.Value == null)
				return null;
			else
			{
				StringBuilder ret = new StringBuilder(this.Value);

				this._str = ret.ToString();
				this._len = (short)ret.Length;
				
				byte[] b = new byte[this.TotalLength];
				BitConverter.GetBytes(IPAddress.HostToNetworkOrder(this.Length)).CopyTo(b, 0);
				enc.GetBytes(this.Value).CopyTo(b, 2);
				return b;
			}
		}
	}
}
