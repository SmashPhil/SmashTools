using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_FolderPicker : Window
	{
		public Dialog_FolderPicker(string rootDir)
		{

		}

		public override Vector2 InitialSize => new Vector2(640, 360);

		public override void DoWindowContents(Rect inRect)
		{
			throw new NotImplementedException();
		}
	}
}
