using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Verse;


namespace SmashTools
{
	public class ResolvedMethod
	{
		internal const string UnsafeAttributeName = "ExactParams";

		public MethodInfo method;
		public object[] args;

		private int runtimeArgs = -1;

		public ResolvedMethod()
		{
		}

		public ResolvedMethod(MethodInfo method)
		{
			this.method = method;
			RecacheRuntimeArgCount();
			RecacheInjectedCount();
			LoadDefaultArgs();
		}

		public int InjectedCount { get; private set; }

		public int RuntimeArguments
		{
			get
			{
				if (runtimeArgs < 0)
				{
					RecacheRuntimeArgCount();
				}
				return runtimeArgs;
			}
		}

		private void RecacheRuntimeArgCount()
		{
			Type type = GetType();
			if (!type.IsGenericType)
			{
				runtimeArgs = 0;
			}
			else
			{
				runtimeArgs = type.GetGenericArguments().Length;
			}
		}

		private void RecacheInjectedCount()
		{
			InjectedCount = 0;
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = RuntimeArguments; i < parameters.Length; i++)
			{
				ParameterInfo parameter = parameters[i];

				//Arguments with prefix are injected arguments
				if (!parameter.Name.StartsWith("__"))
				{
					return; //Injected args must immediately follow any runtime arguments
				}
				InjectedCount++;
			}
		}

		private void LoadDefaultArgs()
		{
			ParameterInfo[] parameters = method.GetParameters();
			args = new object[parameters.Length];
			for (int i = RuntimeArguments + InjectedCount; i < parameters.Length; i++)
			{
				args[i] = parameters[i].ParameterType.GetDefaultValue();
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
				ParameterInfo[] parameters = method.GetParameters();
				//Type[] argTypes = .Select(p => p.ParameterType).ToArray();

				if (argStrings.Length > parameters.Length)
				{
					Log.Error($"Number of parameters is less than number of args passed in. Xml={entry}");
					return;
				}

				bool exactParameters = false;
				if (xmlNode.Attributes[UnsafeAttributeName] is XmlAttribute unsafeAttribute)
				{
					exactParameters = bool.Parse(unsafeAttribute.Value.ToLowerInvariant());
				}

				RecacheRuntimeArgCount();
				RecacheInjectedCount();

				if (exactParameters && (argStrings.Length + RuntimeArguments) != parameters.Length)
				{
					Log.Error($"Number of parameters doesn't match number of args passed in. Xml={entry}");
				}

				LoadDefaultArgs();
				for (int i = RuntimeArguments + InjectedCount; i < parameters.Length; i++)
				{
					int argIndex = i - RuntimeArguments;
					ParameterInfo parameter = parameters[0];

					if (argStrings.OutOfBounds(argIndex))
					{
						args[i] = Type.Missing; //Handles optional parameters
						continue;
					}

					string text = argStrings[argIndex];
					if (text.ToUpperInvariant() == "NULL" && (parameters[i].ParameterType.IsClass || Nullable.GetUnderlyingType(parameters[i].ParameterType) != null))
					{
						args[i] = null;
					}
					else
					{
						args[i] = ParseArgument(parameters[i].ParameterType, text);
					}
				}
			}
			catch (IndexOutOfRangeException)
			{
				Log.Error($"Formatting error in {entry}. Unable to parse into resolved method.");
			}
		}

		private static object ParseArgument(Type type, string entry)
		{
			Type valueType = Nullable.GetUnderlyingType(type);
			if (valueType != null)
			{
				if (entry.NullOrEmpty())
				{
					return null;
				}
				return ParseHelper.FromString(entry, type);
			}

			if (ParseHelper.HandlesType(type))
			{
				return ParseHelper.FromString(entry, type);
			}
			
			if (type.IsSubclassOf(typeof(Def)))
			{
				return GenDefDatabase.GetDef(type, entry);
			}
			Log.ErrorOnce($"Unhandled type {type.Name} in ResolvedMethod arguments.", type.GetHashCode());
			return type.GetDefaultValue();
		}

		public virtual object Invoke(object obj, params object[] injectedArgs)
		{
			InjectArguments(injectedArgs);
			return method.Invoke(obj, args);
		}

		public override string ToString()
		{
			if (method == null)
			{
				return null;
			}
			string type = GenTypes.GetTypeNameWithoutIgnoredNamespaces(method.DeclaringType);
			string readout = $"{type}.{method.Name}";
			if (!args.NullOrEmpty())
			{
				readout += $"({string.Join(",", args.Select(obj => obj?.ToString() ?? "NULL"))})";
			}
			return readout;
		}

		public string ToStringSignature()
		{
			string readout = method.Name;
			if (!args.NullOrEmpty())
			{
				readout += $"( {string.Join(", ", args.Select(obj => obj.GetType()))} )";
			}
			return readout;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void InjectArguments(object[] injectedArgs)
		{
			if (InjectedCount > 0 && !injectedArgs.NullOrEmpty())
			{
				for (int i = RuntimeArguments; i < injectedArgs.Length; i++)
				{
					args[i] = injectedArgs[i];
				}
			}
		}
	}

	public class ResolvedMethod<T> : ResolvedMethod
	{
		public override object Invoke(object obj, params object[] injectedArgs)
		{
			throw new MethodAccessException("ResolvedMethod subtypes should never call the base Invoke method.");
		}

		public object Invoke(object obj, T param, params object[] injectedArgs)
		{
			InjectArguments(injectedArgs);
			args[0] = param;
			return method.Invoke(obj, args);
		}
	}
}
