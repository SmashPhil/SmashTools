using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace SmashTools.Xml;

[StaticConstructorOnModInit]
public static class XmlParseHelper
{
  private const string ValidAttributeRegex = "^([A-Za-z0-9]*$)";

  private static readonly Dictionary<string, (AttributeProcessor, string[])>
    registeredAttributes = [];

  public delegate void AttributeProcessor(XmlNode node, string defName, FieldInfo fieldInfo);

  static XmlParseHelper()
  {
    //ParseHelper.Parsers<ValueTuple<float, float>>.Register(ParseValueTuple2_float);
    //ParseHelper.Parsers<ValueTuple<CurvePoint, CurvePoint>>.Register(ParseValueTuple2_CurvePoint);
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
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant,
      TimeSpan.FromMilliseconds(10)))
    {
      Log.Error(
        $"{ProjectSetup.LogLabel} Cannot register <color=teal>attribute</color> due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }

    registeredAttributes.Add(attribute, (action, nodeAllowed));
  }

  /// <summary>
  /// Parse XmlNode and handle registered custom attributes
  /// </summary>
  internal static void ReadCustomAttributes(Type typeBeingDeserialized, XmlNode fieldNode,
    XmlNode xmlRootForDebug, FieldInfo __result)
  {
    ProcessXmlNode(fieldNode, __result);
  }

  /// <summary>
  /// Parse <paramref name="node"/> and handle registered custom attributes on <seealso cref="Def"/> XmlNode.
  /// </summary>
  /// <param name="node"></param>
  internal static void ReadCustomAttributesOnDef(XmlNode node)
  {
    ProcessXmlNode(node, null);
  }

  private static void ProcessXmlNode(XmlNode node, FieldInfo fieldInfo)
  {
    try
    {
      if (node.NodeType != XmlNodeType.Element)
        return;

      XmlAttributeCollection attributes = node.Attributes;
      if (attributes is null)
        return;

      foreach (XmlAttribute attribute in attributes)
      {
        if (!registeredAttributes.TryGetValue(attribute.Name,
          out (AttributeProcessor processor, string[] nodes) data))
          continue;

        if (!data.nodes.NullOrEmpty() && !data.nodes.Contains(node.Name))
        {
          SmashLog.Error(
            $"Unable to execute <attribute>{attribute.Name}</attribute>. It is only allowed to be used on nodes=({string.Join(",", data.nodes)}) curNode={node.Name}");
          continue;
        }

        try
        {
          data.processor(node, attribute.Value, fieldInfo);
        }
        catch (Exception ex)
        {
          SmashLog.Error(
            $"Unable to execute {attribute.Name} action. Method={data.processor.Method.Name}. Exception={ex}");
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception thrown while trying to apply registered attributes to Def of type {node?.Name ?? "[Null]"}. Exception={ex}");
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