using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;
using RimWorld;
using HarmonyLib;
using SmashTools.Xml;

namespace SmashTools
{
	public class ResolvedMethod
	{
		internal const string UnsafeAttributeName = "ExactParams";

		public MethodInfo method;
		public object[] args;

		private int count = -1;

		public int InjectedCount
		{
			get
			{
				if (count < 0)
				{
					Type type = GetType();
					if (!type.IsGenericType)
					{
						count = 0;
					}
					else
					{
						count = type.GetGenericArguments().Length;
					}
				}
				return count;
			}
		}

		public void LoadDataFromXmlCustom(XmlNode xmlNode)
		{
			string entry = xmlNode.InnerText;

			string[] methodInfoBody = entry.Split('(');
			try
			{
				string[] array = methodInfoBody.FirstOrDefault().Split('.');
				string methodName = array[array.Length - 1];
				string typeName;
				if (array.Length == 3)
				{
					typeName = array[0] + "." + array[1];
				}
				else
				{
					typeName = array[0];
				}
				Type type = GenTypes.GetTypeInAnyAssembly(typeName);
				method = AccessTools.Method(type, methodName);

				string argString = methodInfoBody.LastOrDefault().Replace(")", "");
				string[] argStrings = argString.Split(',');
				object[] resolvedArgs = new object[argStrings.Length];
				Type[] argTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

				if (argStrings.Length > argTypes.Length)
				{
					Log.Error($"Number of parameters is less than number of args passed in. Xml={entry}");
					return;
				}

				bool exactParameters = false;
				if (xmlNode.Attributes[UnsafeAttributeName] is XmlAttribute unsafeAttribute)
				{
					exactParameters = bool.Parse(unsafeAttribute.Value.ToLowerInvariant());
				}

				if (exactParameters && (argStrings.Length + InjectedCount) != argTypes.Length)
				{
					Log.Error($"Number of parameters doesn't match number of args passed in. Xml={entry}");
				}
				
				args = new object[argTypes.Length];
				for (int i = InjectedCount; i < argTypes.Length; i++)
				{
					int argIndex = i - InjectedCount;
					if (argStrings.OutOfBounds(argIndex))
					{
						args[i] = Type.Missing; //Handles optional parameters
					}
					else
					{
						string text = argStrings[argIndex];
						if (text.ToUpperInvariant() == "NULL" && (argTypes[i].IsClass || Nullable.GetUnderlyingType(argTypes[i]) != null))
						{
							args[i] = null;
						}
						else
						{
							args[i] = XmlParseHelper.WrapStringAndParse(argTypes[i], text, true);
						}
					}
				}
			}
			catch (IndexOutOfRangeException)
			{
				Log.Error($"Formatting error in {entry}. Unable to parse into resolved method.");
			}
		}

		public virtual object Invoke(object obj)
		{
			return method.Invoke(obj, args);
		}

		public override string ToString()
		{
			string typeName = GenTypes.GetTypeNameWithoutIgnoredNamespaces(method.DeclaringType);
			string readout = $"{typeName}.{method.Name}";
			if (!args.NullOrEmpty())
			{
				readout += $"({string.Join(",", args)})";
			}
			return readout;
		}
	}

	public class ResolvedMethod<T> : ResolvedMethod
	{
		public override object Invoke(object obj)
		{
			throw new MethodAccessException("ResolvedMethod subtypes should never call the base Invoke method.");
		}

		public object Invoke(object obj, T param)
		{
			args[0] = param;
			return method.Invoke(obj, args);
		}
	}
}
