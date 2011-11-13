using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class Table
	{
		Dictionary<Object, Object> hashPart = new Dictionary<Object, Object>();
		public Table MetaTable { set; get; }

		public void RawPut(Object key, Object o)
		{
			hashPart[key] = o;
		}

		public Object RawGet(Object key)
		{
			if (hashPart.ContainsKey(key))
			{
				return hashPart[key];
			}
			return null;
		}

		public Object MetaGet(Object key)
		{
			if (hashPart.ContainsKey(key))
			{
				return hashPart[key];
			}
			else if (MetaTable != null)
			{
				return MetaTable.MetaGet(key);
			}
			return null;
		}

		override public String ToString()
		{
			String retVal = "{";

			foreach (KeyValuePair<Object, Object> hashEntry in hashPart)
			{
				retVal += hashEntry.Key.ToString() + ":" + hashEntry.Value.ToString() + ",";
			}

			if (MetaTable != null)
			{
				retVal += " | " + MetaTable.ToString();
			}

			retVal += "}";
			return retVal;
		}
	}
}
