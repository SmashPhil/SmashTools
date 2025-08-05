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
      postfix: new HarmonyMethod(typeof(XmlParseHelper),
        nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(XmlToObjectUtils),
        nameof(XmlToObjectUtils.DoFieldSearch)),
      postfix: new HarmonyMethod(typeof(XmlParseHelper),
        nameof(XmlParseHelper.ReadCustomAttributes)));
  }
}