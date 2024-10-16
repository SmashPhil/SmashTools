using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace SmashTools.Xml
{
	[StaticConstructorOnModInit]
	public static class XmlParseHelper
	{
		private static readonly Dictionary<string, Pair<Action<XmlNode, string, FieldInfo>, string[]>> registeredAttributes = new Dictionary<string, Pair<Action<XmlNode, string, FieldInfo>, string[]>>();

		private const string ValidAttributeRegex = @"^([A-Za-z0-9]*$)";

		static XmlParseHelper()
		{
			ParseHelper.Parsers<ValueTuple<float, float>>.Register(ParseValueTuple2_float);
			ParseHelper.Parsers<ValueTuple<CurvePoint, CurvePoint>>.Register(ParseValueTuple2_CurvePoint);
			ParseHelper.Parsers<KeyFrame>.Register(ParseKeyFrame);
			ParseHelper.Parsers<Guid>.Register(ParseGuid);
		}

		private static ValueTuple<float, float> ParseValueTuple2_float(string entry)
		{
			Vector2 vector2 = ParseHelper.FromStringVector2(entry);
			return (vector2.x, vector2.y);
		}

		private static ValueTuple<CurvePoint, CurvePoint> ParseValueTuple2_CurvePoint(string entry)
		{
			entry = entry.Replace("(", "");
			entry = entry.Replace(")", "");
			string[] array = entry.Split(',');

			if (array.Length == 2)
			{
				CurvePoint curvePoint = ParseHelper.ParseCurvePoint($"{array[0]},{array[1]}");
				return (curvePoint, curvePoint);
			}
			else if (array.Length == 4)
			{
				CurvePoint curvePoint1 = ParseHelper.ParseCurvePoint($"{array[0]},{array[1]}");
				CurvePoint curvePoint2 = ParseHelper.ParseCurvePoint($"{array[2]},{array[3]}");
				return (curvePoint1, curvePoint2);
			}
			throw new InvalidOperationException();
		}

		private static KeyFrame ParseKeyFrame(string entry)
		{
			return KeyFrame.FromString(entry);
		}

		private static Guid ParseGuid(string entry)
		{
			if (entry.NullOrEmpty())
			{
				return Guid.Empty;
			}
			return Guid.Parse(entry);
		}

		/// <summary>
		/// Wraps text into xml object so vanilla parsing can operate on it
		/// </summary>
		/// <param name="type"></param>
		/// <param name="innerText"></param>
		/// <param name="doPostLoad"></param>
		public static object WrapStringAndParse(Type type, string innerText, bool doPostLoad = true)
		{
			if (ParseHelper.HandlesType(type))
			{
				return ParseHelper.FromString(innerText, type);
			}
			if (type != typeof(string))
			{
				innerText = innerText.Trim();
			}
			XmlDocument doc = new XmlDocument();
			doc.LoadXml($"<temp>{innerText}</temp>");
			XmlNode newNode = doc.DocumentElement;
			return DirectXmlToObject.GetObjectFromXmlMethod(type)(newNode, doPostLoad);
		}

		/// <summary>
		/// Register custom attribute to be parsed when loading save file
		/// </summary>
		/// <param name="attribute">XmlAttribute name</param>
		/// <param name="action">Action to be executed upon loading of attribute</param>
		/// <param name="nodeAllowed">Specify the only XmlNode that will be able to use this XmlAttribute</param>
		/// <remarks>
		/// <para>Action will only be executed if value of attribute matches value of action</para>
		/// <para>
		/// Arg1 = XmlNode's value
		/// </para>
		/// <para>
		/// Arg2 = defName of parent ThingDef to the XmlNode
		/// </para>
		/// <para>
		/// Arg3 = FieldInfo of XmlNode's associated field
		/// </para>
		/// </remarks>
		public static void RegisterAttribute(string attribute, Action<XmlNode, string, FieldInfo> action, params string[] nodeAllowed)
		{
			if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10)))
			{
				Log.Error($"{ProjectSetup.ProjectLabel} Cannot register <color=teal>attribute</color> due to invalid naming. Only alphanumeric characters may be used.");
				return;
			}
			registeredAttributes.Add(attribute, new Pair<Action<XmlNode, string, FieldInfo>, string[]>(action, nodeAllowed));
		}

		/// <summary>
		/// Parse <paramref name="debugXmlNode"/> and handle registered custom attributes
		/// </summary>
		/// <param name="token"></param>
		/// <param name="debugXmlNode"></param>
		/// <param name="__result"></param>
		public static void ReadCustomAttributes(string token, XmlNode debugXmlNode, FieldInfo __result)
		{
			try
			{
				XmlNode curNode = debugXmlNode.SelectSingleNode(token);
				XmlAttributeCollection attributes = curNode.Attributes;
				if (attributes != null)
				{
					foreach (XmlAttribute attribute in attributes)
					{
						if (registeredAttributes.TryGetValue(attribute.Name, out Pair<Action<XmlNode, string, FieldInfo>, string[]> attributeData))
						{
							var registeredAction = attributeData.First;
							string[] specificNodes = attributeData.Second;
							if (!specificNodes.NullOrEmpty() && !specificNodes.Contains(curNode.Name))
							{
								SmashLog.Error($"Unable to execute <attribute>{attribute.Name}</attribute>. It is only allowed to be used on nodes=({string.Join(",", specificNodes)}) curNode={curNode.Name}");
								return;
							}
							try
							{
								registeredAction(curNode, attribute.Value, __result);
							}
							catch (Exception ex)
							{
								SmashLog.Error($"Unable to execute {attribute.Name} action. Method={registeredAction.Method.Name}. Exception={ex}");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while trying to apply registered attributes to field {token}. Exception={ex}");
			}
		}

		/// <summary>
		/// Parse <paramref name="node"/> and handle registered custom attributes on <seealso cref="Def"/> XmlNode.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="loadingAsset"></param>
		public static void ReadCustomAttributesOnDef(XmlNode node, LoadableXmlAsset loadingAsset)
		{
			try
			{
				if (node.NodeType != XmlNodeType.Element)
				{
					return;
				}
				Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(node.Name, null);
				if (typeInAnyAssembly is null)
				{
					return;
				}
				if (!typeof(Def).IsAssignableFrom(typeInAnyAssembly))
				{
					return;
				}
				XmlAttributeCollection attributes = node.Attributes;
				if (attributes != null)
				{
					foreach (XmlAttribute attribute in attributes)
					{
						if (registeredAttributes.TryGetValue(attribute.Name, out Pair<Action<XmlNode, string, FieldInfo>, string[]> attributeData))
						{
							var registeredAction = attributeData.First;
							string[] specificNodes = attributeData.Second;
							if (!specificNodes.NullOrEmpty() && !specificNodes.Contains(node.Name))
							{
								SmashLog.Error($"Unable to execute <attribute>{attribute.Name}</attribute>. It is only allowed to be used on nodes=({string.Join(",", specificNodes)}) curNode={node.Name}");
								return;
							}
							try
							{
								registeredAction(node, attribute.Value, null);
							}
							catch (Exception ex)
							{
								SmashLog.Error($"Unable to execute {attribute.Name} action. Method={registeredAction.Method.Name}. Exception={ex}");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while trying to apply registered attributes to Def of type {node?.Name ?? "[Null]"}. Exception={ex}");
			}
		}

		/// <summary>
		/// Traverse backwards from the <paramref name="curNode"/> until the defName node is found.
		/// </summary>
		/// <param name="curNode"></param>
		/// <returns>Empty string if not found and the document element is reached</returns>
		public static string BackSearchDefName(XmlNode curNode)
		{
			XmlNode defNode = curNode.SelectSingleNode("defName");
			XmlNode parentNode = curNode;
			while (defNode is null)
			{
				parentNode = parentNode.ParentNode;
				if (parentNode is null)
				{
					return string.Empty;
				}
				defNode = parentNode.SelectSingleNode("defName");
			}
			return defNode.InnerText;
		}

		/// <summary>
		/// <see cref="ModContentPack.LoadPatches"/>
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="___patches"></param>
		/// <param name="___loadedAnyPatches"></param>
		public static bool PatchOperationsMayRequire(ModContentPack __instance, ref List<PatchOperation> ___patches, ref bool ___loadedAnyPatches)
		{
			___patches = new List<PatchOperation>();
			___loadedAnyPatches = false;
			List<LoadableXmlAsset> list = DirectXmlLoader.XmlAssetsInModFolder(__instance, "Patches/", null).ToList();
			for (int i = 0; i < list.Count; i++)
			{
				XmlElement documentElement = list[i].xmlDoc.DocumentElement;
				if (documentElement.Name != "Patch")
				{
					Log.Error(string.Format("Unexpected document element in patch XML; got {0}, expected 'Patch'", documentElement.Name));
				}
				else
				{
					foreach (XmlNode xmlNode in documentElement.ChildNodes)
					{
						if (xmlNode.NodeType == XmlNodeType.Element)
						{
							if (xmlNode.Name != "Operation")
							{
								Log.Error(string.Format("Unexpected element in patch XML; got {0}, expected 'Operation'", xmlNode.Name));
							}
							else
							{
								if (CanLoadWithModList<PatchOperation>(xmlNode))
								{
									PatchOperation patchOperation = DirectXmlToObject.ObjectFromXml<PatchOperation>(xmlNode, false);
									patchOperation.sourceFile = list[i].FullFilePath;
									___patches.Add(patchOperation);
									___loadedAnyPatches = true;
								}
								else
								{
									Log.Warning($"Skipping XmlNode from MayRequire. Node = {xmlNode.InnerText}");
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// <see cref="LoadedModManager.ParseAndProcessXML(XmlDocument, Dictionary{XmlNode, LoadableXmlAsset})"/>
		/// </summary>
		/// <param name="xmlDoc"></param>
		/// <param name="assetlookup"></param>
		/// <param name="___runningMods"></param>
		/// <param name="___patchedDefs"></param>
		public static bool ParseAndProcessXmlMayRequire(XmlDocument xmlDoc, Dictionary<XmlNode, LoadableXmlAsset> assetlookup, List<ModContentPack> ___runningMods, ref List<Def> ___patchedDefs)
		{
			XmlNodeList childNodes = xmlDoc.DocumentElement.ChildNodes;
			List<XmlNode> list = new List<XmlNode>();
			foreach (XmlNode xmlNode in childNodes)
			{
				list.Add(xmlNode);
			}

			//DeepProfiler.Start("Loading asset nodes " + list.Count);
			try
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].NodeType == XmlNodeType.Element)
					{
						LoadableXmlAsset loadableXmlAsset;
						//DeepProfiler.Start("assetlookup.TryGetValue");
						try
						{
							assetlookup.TryGetValue(list[i], out loadableXmlAsset);
						}
						finally
						{
							//DeepProfiler.End();
						}
						//DeepProfiler.Start("XmlInheritance.TryRegister");
						try
						{
							XmlInheritance.TryRegister(list[i], loadableXmlAsset?.mod);
						}
						finally
						{
							//DeepProfiler.End();
						}
					}
				}
			}
			finally
			{
				//DeepProfiler.End();
			}
			//DeepProfiler.Start("XmlInheritance.Resolve()");
			try
			{
				XmlInheritance.Resolve();
			}
			finally
			{
				//DeepProfiler.End();
			}

			//What is this? Decompiler garbage? It shows up in both my decompilers
			___runningMods.FirstOrDefault(); 

			//DeepProfiler.Start("Loading defs for " + list.Count + " nodes");
			try
			{
				foreach (XmlNode xmlNode in list)
				{
					if (CanLoadWithModList<Def>(xmlNode))
					{
						LoadableXmlAsset loadableXmlAsset = assetlookup.TryGetValue(xmlNode, null);
						if (DirectXmlLoader.DefFromNode(xmlNode, loadableXmlAsset) is Def def)
						{
							if (loadableXmlAsset?.mod is ModContentPack modContentPack)
							{
								modContentPack.AddDef(def, loadableXmlAsset.name);
							}
							else
							{
								___patchedDefs.Add(def);
							}
						}
					}
					else
					{
						Log.Warning($"Skipping def load. XmlNode = {xmlNode.InnerText}.");
					}
				}
			}
			finally
			{
				DeepProfiler.End();
			}
			return false;
		}

		private static bool CanLoadWithModList<T>(XmlNode xmlNode, Action<XmlAttribute, XmlAttribute> mayRequireCallback = null)
		{
			XmlAttribute mayRequireAttribute = xmlNode.Attributes["MayRequire"];
			XmlAttribute mayRequireAnyOfAttribute = xmlNode.Attributes["MayRequireAnyOf"];
			if (GenTypes.IsDef(typeof(T)) && mayRequireCallback != null)
			{
				mayRequireCallback(mayRequireAttribute, mayRequireAnyOfAttribute);
				return true;
			}
			if (mayRequireAttribute != null && !mayRequireAttribute.Value.NullOrEmpty())
			{
				bool hasActiveMods = ModsConfig.AreAllActive(mayRequireAttribute.Value);
				if (!hasActiveMods && DirectXmlCrossRefLoader.MistypedMayRequire(mayRequireAttribute.Value))
				{
					Log.Error($"Faulty MayRequire: {mayRequireAttribute.Value}");
					return false;
				}
				return hasActiveMods;
			}
			else if (mayRequireAnyOfAttribute != null && !mayRequireAnyOfAttribute.Value.NullOrEmpty())
			{
				return ModsConfig.IsAnyActiveOrEmpty(mayRequireAnyOfAttribute.Value.Split(','), trimNames: true);
			}
			return true;
		}
	}
}
