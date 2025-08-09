using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using SmashTools.Animations;
using Verse;

namespace SmashTools.Xml;

[PublicAPI]
public static class XmlParseHelper
{
  private const string ValidAttributeRegex = "^([A-Za-z0-9]*$)";

  internal static readonly Dictionary<string, (AttributeProcessor, string[])> RegisteredAttributes = [];

  public delegate void AttributeProcessor(XmlNode node, string defName, FieldInfo fieldInfo);

  internal static void RegisterParseTypes()
  {
    ParseHelper.Parsers<Rot8>.Register(Rot8.FromString);
    ParseHelper.Parsers<Quadrant>.Register(Quadrant.FromString);
    ParseHelper.Parsers<RimWorldTime>.Register(RimWorldTime.FromString);
    ParseHelper.Parsers<KeyFrame>.Register(ParseKeyFrame);
    ParseHelper.Parsers<Guid>.Register(ParseGuid);
  }

  private static KeyFrame ParseKeyFrame(string entry)
  {
    return KeyFrame.FromString(entry);
  }

  private static Guid ParseGuid(string entry)
  {
    return !entry.NullOrEmpty() ? Guid.Parse(entry) : Guid.Empty;
  }

  /// <summary>
  /// Wraps text into xml object so vanilla parsing can operate on it
  /// </summary>
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

    XmlDocument doc = new();
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
  public static void RegisterAttribute(string attribute,
    AttributeProcessor action, params string[] nodeAllowed)
  {
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant))
    {
      Log.Error(
        $"{ProjectSetup.LogLabel} Cannot register <color=teal>attribute</color> due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }
    RegisteredAttributes.Add(attribute, (action, nodeAllowed));
  }
}