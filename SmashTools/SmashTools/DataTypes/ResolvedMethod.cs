using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;
using RimWorld;
using HarmonyLib;

namespace SmashTools
{
	public class ResolvedMethod
	{
		internal const string UnsafeAttributeName = "ExactParams";

		public MethodInfo method;
		public object[] args;

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

				bool exactParameters = true;
				if (xmlNode.Attributes[UnsafeAttributeName] is XmlAttribute unsafeAttribute)
				{
					exactParameters = bool.Parse(unsafeAttribute.Value.ToLowerInvariant());
				}

				if (!exactParameters && argStrings.Length != argTypes.Length)
				{
					Log.Error($"Number of parameters doesn't match number of args passed in. Xml={entry}");
				}
				
				int argDiff = argTypes.Length - argStrings.Length;
				args = new object[argStrings.Length];
				for (int i = 0; i < argStrings.Length; i++)
				{
					string arg = argStrings[i];
					args[i] = ParseHelper.FromString(arg, argTypes[i + argDiff]); //Skip difference from beginning to allow prepended args
				}
			}
			catch (IndexOutOfRangeException)
			{
				Log.Error($"Formatting error in {entry}. Unable to parse into resolved method.");
			}
		}

		public object Invoke(object obj)
		{
			return method.Invoke(obj, args);
		}

		public object InvokeUnsafe(object obj, params object[] prependArgs)
		{
			if (!prependArgs.NullOrEmpty())
			{
				return method.Invoke(obj, args.PrependMany(prependArgs).ToArray());
			}
			return Invoke(obj);
		}
	}

	public class ResolvedMethod<T> : ResolvedMethod
	{
		public object Invoke(object obj, T param)
		{
			return method.Invoke(obj, args.Prepend(param).ToArray());
		}
	}
}
