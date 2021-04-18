using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;

namespace SmashTools.Xml
{
    public static class XmlParseHelper
    {
        private static readonly Dictionary<string, Action<XmlNode, string, FieldInfo>> registeredAttributes = new Dictionary<string, Action<XmlNode, string, FieldInfo>>();

        private const string ValidAttributeRegex = @"^([A-Za-z0-9]*$)";

        /// <summary>
        /// Register custom attribute to be parsed when loading save file
        /// </summary>
        /// <param name="attribute">Attribute name</param>
        /// <param name="action">Action to be executed upon loading of attribute</param>
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
        public static void RegisterAttribute(string attribute, Action<XmlNode, string, FieldInfo> action)
        {
            if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10)))
            {
                Log.Error($"{ProjectSetup.ProjectLabel} Cannot register <color=teal>attribute</color> due to invalid naming. Only alphanumeric characters may be used.");
                return;
            }
            registeredAttributes.Add(attribute, action);
        }

        public static void ReadCustomAttributes(Type type, string token, XmlNode debugXmlNode, FieldInfo __result)
        {
            try
            {
                XmlNode curNode = debugXmlNode.SelectSingleNode(token);
            
                foreach (var register in registeredAttributes)
                {
                    XmlAttribute attribute = curNode.Attributes[register.Key];
                    if (attribute != null)
                    {
                        register.Value(curNode, attribute.Value, __result);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Exception thrown while trying to apply registered attributes to field {token}. Exception={ex.Message}");
            }
            
        }

        public static void ReadCustomAttributesOnDef(XmlNode node, LoadableXmlAsset loadingAsset)
        {
            try
            {
                if (node.NodeType != XmlNodeType.Element)
			    {
                    return;
			    }
                Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(node.Name, null);
			    if (typeInAnyAssembly == null)
			    {
				    return;
			    }
			    if (!typeof(Def).IsAssignableFrom(typeInAnyAssembly))
			    {
				    return;
			    }
                foreach (var register in registeredAttributes)
                {
                    XmlAttribute attribute = node.Attributes[register.Key];
                    if (attribute != null)
                    {
                        register.Value(node, attribute.Value, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception thrown while trying to apply registered attributes to Def of type {node?.Name ?? "[Null]"}. Exception={ex.Message}");
            }
        }
    }
}
