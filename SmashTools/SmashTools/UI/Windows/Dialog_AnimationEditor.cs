using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using System.Runtime;
using Verse.Noise;

namespace SmashTools.Animations
{
	public partial class Dialog_AnimationEditor : Window, IHighPriorityOnGUI
	{
		private List<TabRecord> tabs = new List<TabRecord>();
		private DialogTab dialogTab = DialogTab.Controller;

		public IAnimator animator;

		private AnimationControllerEditor controllerEditor;
		private AnimationClipEditor clipEditor;

		public Dialog_AnimationEditor(IAnimator animator)
		{
			SetWindowProperties();
			InitializeTabs();

			this.animator = animator;
			controllerEditor = new AnimationControllerEditor(this);
			clipEditor = new AnimationClipEditor(this);
		}

		private AnimationEditor ActiveTab
		{
			get
			{
				return dialogTab switch
				{
					DialogTab.Animator => clipEditor,
					DialogTab.Controller => controllerEditor,
					_ => throw new NotImplementedException(),
				};
			}
		}

		private bool UnsavedChanges { get; set; }

		public float EditorMargin => base.Margin;

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(UI.screenWidth * 0.75f, UI.screenHeight * 0.75f);
			}
		}

		public override void PostOpen()
		{
			base.PostOpen();
			LoadAnimator(animator);
		}

		private void SetWindowProperties()
		{
			this.resizeable = true;
			this.doCloseX = true;
			this.closeOnAccept = false;
			this.closeOnClickedOutside = false;
			this.closeOnCancel = false;
			this.draggable = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = true;
			this.forcePause = true;
		}

		public void ChangeMade()
		{
			UnsavedChanges = true;
		}

		private void LoadAnimator(IAnimator animator)
		{
			if (CameraView.InUse)
			{
				CameraView.Close();
			}
			this.animator = animator;

			controllerEditor.AnimatorLoaded(animator);
			clipEditor.AnimatorLoaded(animator);
		}

		public override void PostClose()
		{
			base.PostClose();
			CameraView.Close();
			controllerEditor.OnClose();
			clipEditor.OnClose();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			controllerEditor.Update();
			clipEditor.Update();
		}

		public void OnGUIHighPriority()
		{
			if (KeyBindingDefOf.Cancel.KeyDownEvent)
			{
				Event.current.Use();
				if (UnsavedChanges)
				{
					Find.WindowStack.Add(new Dialog_Confirm($"You have unsaved changes. Close anyways?", delegate ()
					{
						Close();
					}));
				}
				else
				{
					Close();
				}
			}
			controllerEditor.OnGUIHighPriority();
			clipEditor.OnGUIHighPriority();
		}

		private void InitializeTabs()
		{
			tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("ST_ControllerWindow".Translate(), delegate ()
			{
				dialogTab = DialogTab.Controller;
			}, () => dialogTab == DialogTab.Controller));
			tabs.Add(new TabRecord("ST_AnimationWindow".Translate(), delegate ()
			{
				dialogTab = DialogTab.Animator;
			}, () => dialogTab == DialogTab.Animator));
		}

		public override void DoWindowContents(Rect inRect)
		{
			ResetControlFocus();

			GUIState.Push();
			
			Text.Font = GameFont.Small;
			Rect tabRect = new Rect(inRect.x, inRect.y + 32, inRect.width, 32);
			TabDrawer.DrawTabs(tabRect, tabs);
			inRect.yMin += tabRect.height;

			GUI.enabled = animator != null;
			{
				ActiveTab.Draw(inRect);
			}
			GUI.enabled = true;

			GUIState.Pop();
		}

		private void ResetControlFocus()
		{
			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return ||
				Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Escape))
			{
				UI.UnfocusCurrentControl();
			}
		}

		private enum DialogTab
		{
			Animator,
			Controller
		}
	}
}
