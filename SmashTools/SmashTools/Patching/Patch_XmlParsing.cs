using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using SmashTools.Xml;
using Verse;

namespace SmashTools.Patching;

internal class Patch_XmlParsing : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
      postfix: new HarmonyMethod(typeof(Patch_XmlParsing),
        nameof(ReadCustomAttributesOnDef)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(XmlToObjectUtils),
        nameof(XmlToObjectUtils.DoFieldSearch)),
      postfix: new HarmonyMethod(typeof(Patch_XmlParsing),
        nameof(ReadCustomAttributes)));
  }

  /// <summary>
  /// Parse XmlNode and handle registered custom attributes
  /// </summary>
  internal static void ReadCustomAttributes(XmlNode fieldNode, FieldInfo __result)
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
      if (node?.NodeType != XmlNodeType.Element)
        return;

      XmlAttributeCollection attributes = node.Attributes;
      if (attributes is null)
        return;

      foreach (XmlAttribute attribute in attributes)
      {
        if (!XmlParseHelper.RegisteredAttributes.TryGetValue(attribute.Name,
          out (XmlParseHelper.AttributeProcessor processor, string[] nodes) data))
          continue;

        if (!data.nodes.NullOrEmpty() && !data.nodes.Contains(node.Name))
        {
          Log.Error(
            $"Unable to execute {attribute.Name}. It is only allowed to be used on nodes=({string.Join(",", data.nodes)}) curNode={node.Name}");
          continue;
        }

        try
        {
          data.processor(node, attribute.Value, fieldInfo);
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Unable to execute {attribute.Name} action. Method={data.processor.Method.Name}.\n{ex}");
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception thrown while trying to apply registered attributes to Def of type {node?.Name ?? "NULL"}.\n{ex}");
    }
  }
}