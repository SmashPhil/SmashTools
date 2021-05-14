using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class SmashLog
	{
		private static string RichTextRegex = @"(<b.*?\>|\<\/b\>)|(<i.*?\>|\<\/i\>)|(<color.*?\>|\<\/color\>)";
		private static string RichTextRegexStartingBrackets = "";
		private static string RichTextRegexEndingBrackets = "";

		internal static Dictionary<string, Color> bracketColor = new Dictionary<string, Color>();

		private static HashSet<int> usedKeys = new HashSet<int>();

		static SmashLog()
		{
			RegisterRichTextBracket("text", new Color(1, 1, 1, 1));
			RegisterRichTextBracket("field", new Color(0.5f, 0.35f, 0.95f));
			RegisterRichTextBracket("property", new Color(0.05f, 0.5f, 1));
			RegisterRichTextBracket("method", new Color(1, 0.65f, 0));
			RegisterRichTextBracket("struct", new Color(0, 0.75f, 0.4f));
			RegisterRichTextBracket("class", new Color(0, 0.65f, 0.5f));
			RegisterRichTextBracket("type", new Color(0, 0.65f, 0.5f)); 
			RegisterRichTextBracket("success", new Color(0, 0.5f, 0));
			RegisterRichTextBracket("error", new Color(1, 0, 0));
			RegisterRichTextBracket("warning", new Color(1, 1, 0));
			RegisterRichTextBracket("mod", new Color(0, 0.5f, 0.5f));
		}

		[Obsolete("Do not use this method outside of development.")]
		public static void QuickMessage(string text)
		{
			Log.Clear();
			Log.Message(text);
		}

		public static void Message(string text)
		{
			Log.Message(ColorizeBrackets(text));
		}

		public static void Warning(string text)
		{
			Log.Warning(ColorizeBrackets(text));
		}

		public static void WarningOnce(string text, int key)
		{
			if (usedKeys.Contains(key))
			{
				return;
			}
			usedKeys.Add(key);
			Warning(text);
		}

		public static void Error(string text)
		{
			Log.Error(ColorizeBrackets(text));
		}

		public static void ErrorOnce(string text, int key)
		{
			Log.ErrorOnce(ColorizeBrackets(text), key);
		}

		internal static string ColorizeBrackets(string text)
		{
			foreach (var textEntry in bracketColor)
			{
				text = Regex.Replace(text, $@"\<{textEntry.Key}.*?\>", $"<color=#{ColorUtility.ToHtmlStringRGBA(textEntry.Value)}>", RegexOptions.Singleline);
				text = Regex.Replace(text, $@"\<\/{textEntry.Key}\>", "</color>", RegexOptions.Singleline);
			}
			return text;
		}

		private static void RegisterRichTextBracket(string textLabel, Color color)
		{
			RichTextRegex += @$"|(<{textLabel}.*?\>|\<\/{textLabel}\>)";
			if (!RichTextRegexStartingBrackets.NullOrEmpty())
			{
				RichTextRegexStartingBrackets += "|";
			}
			if (!RichTextRegexEndingBrackets.NullOrEmpty())
			{
				RichTextRegexEndingBrackets += "|";
			}
			RichTextRegexStartingBrackets += @$"(<{textLabel}.*?\>)";
			RichTextRegexEndingBrackets += $@"(\<\/{textLabel}\>)";
			bracketColor.Add(textLabel, color);
		}

		public static void TestAllBrackets()
		{
			string buildText = "Testing all brackets: ";
			foreach (string bracket in bracketColor.Keys)
			{
				buildText += $"<{bracket}>{bracket}</{bracket}> ";
			}
			Message(buildText);
		}

		public static IEnumerable<CodeInstruction> RemoveRichTextTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.LoadsField(AccessTools.Field(typeof(LogMessage), nameof(LogMessage.text))))
				{
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(SmashLog), nameof(LogTextWithoutRichText)));
				}

				yield return instruction;
			}
		}

		private static string LogTextWithoutRichText(string text)
		{
			try
			{
				string textNoBrackets = Regex.Replace(text, RichTextRegex, "", RegexOptions.Singleline, TimeSpan.FromMilliseconds(5));
				return textNoBrackets;
			}
			catch (Exception)
			{
			}
			return text;
		}
	}
}
