using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Diagnostics;

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

		private static HashSet<string> suppressedCodes = new HashSet<string>();

		static SmashLog()
		{
			RegisterRichTextBracket("text", Color.white);
			RegisterRichTextBracket("field", new Color(0.5f, 0.35f, 0.95f));
			RegisterRichTextBracket("property", new Color(0.05f, 0.5f, 1));
			RegisterRichTextBracket("method", new Color(1, 0.65f, 0));
			RegisterRichTextBracket("struct", new Color(0, 0.75f, 0.4f));
			RegisterRichTextBracket("class", new Color(0, 0.65f, 0.5f));
			RegisterRichTextBracket("type", new Color(0, 0.65f, 0.5f)); 
			RegisterRichTextBracket("success", new Color(0, 0.5f, 0));
			RegisterRichTextBracket("error", ColorLibrary.LogError);
			RegisterRichTextBracket("warning", Color.yellow);
			RegisterRichTextBracket("mod", new Color(0, 0.5f, 0.5f));
			RegisterRichTextBracket("attribute", new Color(1, 0.4f, 0.4f));
			RegisterRichTextBracket("xml", new Color(0.25f, 0.75f, 0.95f));
		}

		public static void ValidateMethodCalling(MethodInfo method)
		{
			ProjectSetup.Harmony.Patch(method,
				prefix: new HarmonyMethod(typeof(SmashLog), nameof(SmashLog.StartingMethodCall)),
				postfix: new HarmonyMethod(typeof(SmashLog), nameof(SmashLog.EndingMethodCall)));
		}

		private static void StartingMethodCall()
		{
			Log.Message($"Starting method call.\nStackTrace={StackTraceUtility.ExtractStackTrace()}");
		}

		private static void EndingMethodCall()
		{
			Log.Clear();
			Log.Message($"Completed method call.\nStackTrace={StackTraceUtility.ExtractStackTrace()}\n");
		}

		public static void RegisterSuppressionCode(string code)
		{
			if (!suppressedCodes.Add(code))
			{
				Log.Error($"Unable to register {code} as a supression code. It is already being used and codes must be unique.");
			}
		}

		public static bool Suppress(string code)
		{
			return !code.NullOrEmpty() && suppressedCodes.Contains(code);
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

		public static void Warning(string text, string code = "")
		{
			if (!Suppress(code))
			{
				Log.Warning(ColorizeBrackets(text));
			}
		}

		public static void WarningLabel(string label, string text, string code = "")
		{
			if (!Suppress(code))
			{
				Log.Message($"{ColorizeBrackets($"<warning>{label}</warning>")} {ColorizeBrackets(text)}");
			}
		}

		public static void WarningOnce(string text, int key, string code = "")
		{
			if (!Suppress(code))
			{
				if (usedKeys.Contains(key))
				{
					return;
				}
				usedKeys.Add(key);
				Warning(text);
			}
		}

		public static void Error(string text, string code = "")
		{
			if (!Suppress(code))
			{
				Log.Error(ColorizeBrackets(text));
			}
		}

		public static void ErrorLabel(string label, string text, string code = "")
		{
			if (!Suppress(code))
			{
				try
				{
					if (DebugSettings.pauseOnError && Current.ProgramState == ProgramState.Playing)
					{
						Find.TickManager.Pause();
					}
					Log.Message($"{ColorizeBrackets($"<error>{label}</error>")} {ColorizeBrackets(text)}");
					if (!PlayDataLoader.Loaded || Prefs.DevMode)
					{
						Log.TryOpenLogWindow();
					}
				}
				catch (Exception ex)
				{
					Log.Error($"An error occurred while logging an error with a label: {ex}");
				}
			}
		}

		public static void ErrorOnce(string text, int key, string code = "")
		{
			if (!Suppress(code))
			{
				Log.ErrorOnce(ColorizeBrackets(text), key);
			}
		}

		public static void Success(string text)
		{
			Log.Message(ColorizeBrackets($"<success>{text}</success>"));
		}

		public static void SuccessLabel(string label, string text)
		{
			Log.Message($"{ColorizeBrackets($"<success>{label}</success>")} {ColorizeBrackets(text)}");
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
