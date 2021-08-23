using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;

namespace SmashTools.Xml
{
	public static class XmlParseHelper
	{
		private static readonly Dictionary<string, Pair<Action<XmlNode, string, FieldInfo>, string[]>> registeredAttributes = new Dictionary<string, Pair<Action<XmlNode, string, FieldInfo>, string[]>>();

		private const string ValidAttributeRegex = @"^([A-Za-z0-9]*$)";

		/// <summary>
		/// Register custom attribute to be parsed when loading save file
		/// </summary>
		/// <param name="attribute">XmlAttribute name</param>
		/// <param name="action">Action to be executed upon loading of attribute</param>
		/// <param name="nodeAllowed">Specify the only XmlNode that will be able to use this XmlAttribute</param>
		/// <remarks>
		/// <para>Action will only be executed if value of attribute matches value of action</para>
		/// <para>
		/// Param1 = XmlNode's value
		/// </para>
		/// <para>
		/// Param2 = defName of parent ThingDef to the XmlNode
		/// </para>
		/// <para>
		/// Param3 = FieldInfo of XmlNode's associated field
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
		/// <param name="type"></param>
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
								SmashLog.Error($"Unable to execute {attribute.Name} action. Method={registeredAction.Method.Name}. Exception={ex.Message}");
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Exception thrown while trying to apply registered attributes to field {token}. Exception={ex.Message}");
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
								SmashLog.Error($"Unable to execute {attribute.Name} action. Method={registeredAction.Method.Name}. Exception={ex.Message}");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while trying to apply registered attributes to Def of type {node?.Name ?? "[Null]"}. Exception={ex.Message}");
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
	}
}
