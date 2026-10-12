using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityStubs.Gui;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.Logging;
using Waylith.Rendering;
using Waylith.UnityCompat;
using Color = UnityEngine.Color;
using float4 = Brutal.Numerics.float4;
using int2 = Brutal.Numerics.int2;
using Mathf = Waylith.Math.Mathf;
using Rect = UnityEngine.Rect;

namespace CoreLib.Windows;

[PublicAPI]
public struct WindowContext()
{
  public required string title;

  public int2? size;

  // Widgets::WindowBGFillColor
  public float4 color = new(0.082f, 0.098f, 0.114f, 1f);

  public bool drawTitleBar = true;
}

[PublicAPI]
public sealed class ApplicationWindow : IDisposable
{
  private readonly Window window;
  private readonly WindowContext context;
  private readonly int2 size;

  private readonly EngineApplication app = EngineApplication.Instance;

  private GlfwWindow glfwWindow;
  private WindowRenderContext renderContext;
  private ApplicationWindowGui gui;
  private ApplicationWindowInput input;
  private FrameResult pendingRecovery = FrameResult.Success;

  private bool disposed;

  public ApplicationWindow([NotNull] Window window, in WindowContext context)
  {
    ArgumentNullException.ThrowIfNull(window);
    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("ApplicationWindow must be owned by the main thread.");

    size = context.size ?? new int2(Mathf.RoundToInt(window.InitialSize.x), Mathf.RoundToInt(window.InitialSize.y));
    if (size.X <= 0 || size.Y <= 0)
      throw new ArgumentOutOfRangeException(nameof(context), "Window dimensions must be larger than zero");

    this.window = window;
    this.context = context;
  }

  public void Run()
  {
    ObjectDisposedException.ThrowIf(disposed, this);

    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("ApplicationWindow must be opened on the main thread.");

    if (glfwWindow != null)
      return;

    if (app == null || app.Headless || app.RenderContext == null)
      throw new InvalidOperationException("A graphical application is required.");

    VerboseLog.Log("Starting ApplicationWindow as windowed dialog.");
    try
    {
      Glfw.WindowHint(GlfwWindowHint.ClientApi, 0);
      Glfw.WindowHint(GlfwWindowHint.Visible, 0);
      Glfw.WindowHint(GlfwWindowHint.Decorated, context.drawTitleBar ? 1 : 0);
      Glfw.WindowHint(GlfwWindowHint.Resizable, 1);
      Glfw.WindowHint(GlfwWindowHint.Floating, 0);
      Glfw.WindowHint(GlfwWindowHint.Maximized, 0);
      Glfw.WindowHint(GlfwWindowHint.FocusOnShow, 1);

      glfwWindow = Glfw.CreateWindow(new GlfwWindow.CreateInfo
      {
        Title = context.title,
        Size = size
      });

      CreateRenderContext();
      app.WaitForRenderThreadIdle();
      gui = new ApplicationWindowGui(app, renderContext);
      input = new ApplicationWindowInput(glfwWindow);

      app.PrePollEvents += Update;
      app.BeforeGraphicsShutdown += OnGraphicsShutdown;

      glfwWindow.Visible = true;
    }
    catch
    {
      Dispose();
      throw;
    }
  }

  private void CreateRenderContext()
  {
    int result = Glfw.CreateWindowSurface(app.VulkanInstance.VkHandle, glfwWindow, out ulong handle);

    if (result != 0 || handle == 0)
    {
      throw new InvalidOperationException(
        $"Could not create window surface: Vulkan result {result}.");
    }

    if (app.Headless || app.RenderContext == null || app.GraphicsDevice == null)
    {
      throw new InvalidOperationException("Cannot create ApplicationWindow in headless mode.");
    }

    var surface = new VkSurfaceKHR((nint)handle);

    try
    {
      if (!app.PhysicalDevice.GetSurfaceSupportKHR(app.GraphicsDevice.PresentFamilyIndex, surface))
      {
        throw new NotSupportedException(
          "The application's present queue cannot present to this window.");
      }

      app.GraphicsDevice.WaitIdleAndRun(() =>
      {
        renderContext = new WindowRenderContext(
          graphicsDevice: app.GraphicsDevice,
          window: glfwWindow,
          surface: surface,
          colorFormat: app.RenderContext.ColorFormat,
          depthFormat: VkFormat.Undefined,
          requestedPresentMode: VkPresentModeKHR.FifoKHR,
          sampleCount: VkSampleCountFlags._1Bit,
          numFramesInFlight: 2);
      });
    }
    catch
    {
      app.VulkanInstance.DestroySurfaceKHR(surface, pAllocator: null);
      throw;
    }
  }

  private void Update(EngineApplication application)
  {
    if (glfwWindow.ShouldClose)
    {
      Dispose();
      return;
    }

    // Don't acquire swapchain images while minimized.
    if (renderContext.IsFramebufferMinimized())
      return;

    try
    {
      if (pendingRecovery == FrameResult.SurfaceLost)
      {
        Rebuild(recreateSurface: true);
        pendingRecovery = FrameResult.Success;
      }
      else if (pendingRecovery == FrameResult.NeedsResize || renderContext.HasFramebufferSizeMismatch())
      {
        Rebuild(recreateSurface: false);
        pendingRecovery = FrameResult.Success;
      }

      RenderFrame();
    }
    catch
    {
      Dispose();
      throw;
    }
  }

  private void Rebuild(bool recreateSurface)
  {
    app.WaitForRenderThreadIdle();
    app.GraphicsDevice.WaitIdleAndRun(() =>
    {
      gui?.Dispose();
      gui = null;
      if (recreateSurface)
      {
        renderContext.RecreateSurfaceAndRebuild();
      }
      else
      {
        renderContext.Rebuild();
      }
      gui = new ApplicationWindowGui(app, renderContext);
    });
  }

  private unsafe void RenderFrame()
  {
    // IMGUI's current context is global so we need to wait before acquiring for our context
    app.WaitForRenderThreadIdle();
    var (result, acquiredFrame) = renderContext.TryAcquireNextFrame();

    if (result != FrameResult.Success)
    {
      RememberRecovery(result);
      return;
    }

    var (resources, commands) = acquiredFrame;
    object snapshot = gui.Capture(window, input);
    commands.Reset();
    commands.Begin();

    VkClearValue clear = new()
    {
      Color = new VkClearColorValue
      {
        Float32 = context.color
      }
    };
    VkRenderPassBeginInfo pass = new()
    {
      RenderPass = renderContext.GetRenderPass(),
      Framebuffer = resources.Framebuffer,
      RenderArea = new VkRect2D(renderContext.GetExtent()),
      ClearValueCount = 1,
      ClearValues = &clear
    };
    commands.BeginRenderPass(in pass, VkSubpassContents.Inline);
    FrameContext frame = new(renderContext, resources, commands);
    gui.Record(snapshot, in frame);
    commands.EndRenderPass();
    commands.End();

    RememberRecovery(renderContext.TrySubmitFrame());
  }

  private void RememberRecovery(FrameResult result)
  {
    if (result is FrameResult.NeedsResize or FrameResult.SurfaceLost)
    {
      pendingRecovery = result;
    }
  }

  private void OnGraphicsShutdown(EngineApplication application)
  {
    Dispose();
  }

  public void Dispose()
  {
    if (disposed)
      return;

    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("ApplicationWindow must be disposed on the main thread.");

    if (app != null)
    {
      app.PrePollEvents -= Update;
      app.BeforeGraphicsShutdown -= OnGraphicsShutdown;
    }

    try
    {
      if (renderContext != null)
      {
        app?.WaitForRenderThreadIdle();
        app?.GraphicsDevice?.WaitIdleAndRun(() =>
        {
          try
          {
            gui?.Dispose();
            gui = null;
          }
          finally
          {
            renderContext.Dispose();
          }
        });
        renderContext = null;
      }
    }
    finally
    {
      input?.Dispose();
      input = null;
      glfwWindow?.Dispose();
      glfwWindow = null;
      disposed = true;
    }
  }

  // Keep input on the native window, independent of swapchain/GUI renderer recreation.
  private sealed class ApplicationWindowInput : IDisposable
  {
    private readonly GlfwWindow source;
    private readonly Queue<Event> events = new();
    private readonly bool[] held = new bool[8];
    private readonly bool[] renderedHeld = new bool[8];
    private readonly int[] clicks = new int[8];
    private readonly double[] lastClickTime = new double[8];
    private readonly Vector2[] lastClickPosition = new Vector2[8];
    private Vector2 mouse;
    private bool inside;

    public ApplicationWindowInput(GlfwWindow source)
    {
      this.source = source;
      mouse = Position(source.CursorPos);
      inside = mouse.x >= 0 && mouse.y >= 0 && mouse.x < source.Size.X && mouse.y < source.Size.Y;
      source.OnMouseButton += OnMouseButton;
      source.OnCursorPos += OnCursorPos;
      source.OnCursorEnter += OnCursorEnter;
      source.OnScroll += OnScroll;
      source.OnFocus += OnFocus;
    }

    private static Vector2 Position(Brutal.Numerics.double2 value) =>
      new((float)value.X, (float)value.Y);

    private Vector2 ToPixels(Vector2 position)
    {
      int2 size = source.Size;
      int2 pixels = source.FramebufferSize;
      return new Vector2(position.x * pixels.X / Math.Max(1, size.X),
        position.y * pixels.Y / Math.Max(1, size.Y));
    }

    private EventModifiers Modifiers()
    {
      EventModifiers result = EventModifiers.None;
      if (Down(GlfwKey.LeftShift) || Down(GlfwKey.RightShift)) result |= EventModifiers.Shift;
      if (Down(GlfwKey.LeftControl) || Down(GlfwKey.RightControl)) result |= EventModifiers.Control;
      if (Down(GlfwKey.LeftAlt) || Down(GlfwKey.RightAlt)) result |= EventModifiers.Alt;
      if (Down(GlfwKey.LeftSuper) || Down(GlfwKey.RightSuper)) result |= EventModifiers.Command;
      return result;
    }

    private bool Down(GlfwKey key) => (int)source.GetKey(key) != 0;

    private static Event NewEvent(EventType type, Vector2 position, EventModifiers modifiers) =>
      new()
      {
        type = type, rawType = type, mousePosition = position, modifiers = modifiers,
        shift = (modifiers & EventModifiers.Shift) != 0,
        alt = (modifiers & EventModifiers.Alt) != 0
      };

    // Explicit positions here are already in pixels, as returned by TryDequeue.
    public Event CreateEvent(EventType type, Vector2? position = null) =>
      NewEvent(type, position ?? (inside ? ToPixels(mouse) : new Vector2(-10000, -10000)), Modifiers());

    public bool TryDequeue(out Event value)
    {
      if (!events.TryDequeue(out value))
        return false;
      value.mousePosition = ToPixels(value.mousePosition);
      if (value.type is EventType.MouseMove or EventType.MouseDrag)
        value.delta = ToPixels(value.delta);
      return true;
    }

    public void Advance(Event value)
    {
      if ((uint)value.button >= renderedHeld.Length)
        return;
      if (value.rawType == EventType.MouseDown)
        renderedHeld[value.button] = true;
      else if (value.rawType == EventType.MouseUp)
        renderedHeld[value.button] = false;
    }

    public bool IsHeld(int button) => button >= 0 && button < renderedHeld.Length && renderedHeld[button];

    // Unity's legacy Input API reads this device, while our GLFW callbacks belong
    // to another native window. Temporarily publish that window's state for GUI code.
    public sealed class MouseScope : IDisposable
    {
      private const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;
      private static readonly MethodInfo SetPressed = typeof(ButtonControl).GetMethod("SetPressed", Internal)
        ?? throw new MissingMethodException(nameof(ButtonControl), "SetPressed");
      private static readonly MethodInfo SetPosition = typeof(UnityEngine.InputSystem.Pointer).GetMethod("SetPosition", Internal)
        ?? throw new MissingMethodException(nameof(UnityEngine.InputSystem.Pointer), "SetPosition");
      private static readonly FieldInfo WasPressed = typeof(ButtonControl).GetField("_wasPressedThisFrame", Internal)
        ?? throw new MissingFieldException(nameof(ButtonControl), "_wasPressedThisFrame");
      private static readonly FieldInfo WasReleased = typeof(ButtonControl).GetField("_wasReleasedThisFrame", Internal)
        ?? throw new MissingFieldException(nameof(ButtonControl), "_wasReleasedThisFrame");
      private static readonly FieldInfo WasRepeated = typeof(ButtonControl).GetField("_wasRepeatedThisFrame", Internal)
        ?? throw new MissingFieldException(nameof(ButtonControl), "_wasRepeatedThisFrame");

      private readonly UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
      private readonly ButtonControl[] buttons = new ButtonControl[5];
      private readonly bool[] pressed = new bool[5];
      private readonly bool[] wasPressed = new bool[5];
      private readonly bool[] wasReleased = new bool[5];
      private readonly bool[] wasRepeated = new bool[5];
      private readonly Vector2 position;

      public MouseScope()
      {
        position = mouse.position.ReadValue();
        for (int i = 0; i < buttons.Length; i++)
        {
          ButtonControl button = i switch
          {
            0 => mouse.leftButton,
            1 => mouse.rightButton,
            2 => mouse.middleButton,
            3 => mouse.forwardButton,
            _ => mouse.backButton
          };
          buttons[i] = button;
          pressed[i] = button.isPressed;
          wasPressed[i] = (bool)WasPressed.GetValue(button);
          wasReleased[i] = (bool)WasReleased.GetValue(button);
          wasRepeated[i] = (bool)WasRepeated.GetValue(button);
        }
      }

      public void Apply(ApplicationWindowInput input, Event guiEvent, Vector2 physicalMouse, int height)
      {
        for (int i = 0; i < buttons.Length; i++)
        {
          SetPressed.Invoke(buttons[i], [input.IsHeld(i)]);
          WasPressed.SetValue(buttons[i], guiEvent.rawType == EventType.MouseDown && guiEvent.button == i);
          WasReleased.SetValue(buttons[i], guiEvent.rawType == EventType.MouseUp && guiEvent.button == i);
          WasRepeated.SetValue(buttons[i], false);
        }
        float scale = Prefs.UIScale;
        SetPosition.Invoke(mouse, [new Vector2(physicalMouse.x * scale,
          (height - physicalMouse.y) * scale)]);
      }

      public void Dispose()
      {
        for (int i = 0; i < buttons.Length; i++)
        {
          SetPressed.Invoke(buttons[i], [pressed[i]]);
          WasPressed.SetValue(buttons[i], wasPressed[i]);
          WasReleased.SetValue(buttons[i], wasReleased[i]);
          WasRepeated.SetValue(buttons[i], wasRepeated[i]);
        }
        SetPosition.Invoke(mouse, [position]);
      }
    }

    private void OnMouseButton(GlfwWindow sender, GlfwMouseButton button,
      GlfwButtonAction action, GlfwModifier modifiers)
    {
      int index = (int)button;
      if ((uint)index >= held.Length)
        return;
      mouse = Position(sender.CursorPos);
      inside = Contains(mouse);
      bool pressed = action == GlfwButtonAction.Press;
      if (!pressed && !held[index])
        return;
      held[index] = pressed;
      if (pressed)
      {
        double now = (double)System.Diagnostics.Stopwatch.GetTimestamp() /
          System.Diagnostics.Stopwatch.Frequency;
        clicks[index] = now - lastClickTime[index] <= 0.5 &&
          (mouse - lastClickPosition[index]).sqrMagnitude <= 25 ? clicks[index] + 1 : 1;
        lastClickTime[index] = now;
        lastClickPosition[index] = mouse;
      }
      Event value = NewEvent(pressed ? EventType.MouseDown : EventType.MouseUp, mouse,
        (EventModifiers)((int)modifiers & 15));
      value.button = index;
      value.clickCount = clicks[index];
      events.Enqueue(value);
    }

    private void OnCursorPos(GlfwWindow sender, Brutal.Numerics.double2 position)
    {
      Vector2 previous = mouse;
      mouse = Position(position);
      inside = Contains(mouse);
      int button = Array.FindIndex(held, down => down);
      Event value = NewEvent(button >= 0 ? EventType.MouseDrag : EventType.MouseMove, mouse, Modifiers());
      value.button = Math.Max(0, button);
      value.delta = mouse - previous;
      events.Enqueue(value);
    }

    private void OnCursorEnter(GlfwWindow sender, bool entered)
    {
      inside = entered;
      mouse = Position(sender.CursorPos);
    }

    private bool Contains(Vector2 position) => position.x >= 0 && position.y >= 0 &&
      position.x < source.Size.X && position.y < source.Size.Y;

    private void OnScroll(GlfwWindow sender, Brutal.Numerics.double2 offset)
    {
      mouse = Position(sender.CursorPos);
      Event value = NewEvent(EventType.ScrollWheel, mouse, Modifiers());
      // Match RimWorldUiPump's GLFW-to-IMGUI scroll convention.
      value.delta = Position(offset) * -3f;
      events.Enqueue(value);
    }

    private void OnFocus(GlfwWindow sender, GlfwFocusEvent focus)
    {
      if (focus != GlfwFocusEvent.LostFocus)
        return;
      events.Clear();
      // Release outside the content to cancel presses rather than activate a button.
      for (int i = 0; i < held.Length; i++)
      {
        if (!held[i]) continue;
        held[i] = false;
        Event value = NewEvent(EventType.MouseUp, new Vector2(-10000, -10000), EventModifiers.None);
        value.button = i;
        events.Enqueue(value);
      }
    }

    public void Dispose()
    {
      source.OnMouseButton -= OnMouseButton;
      source.OnCursorPos -= OnCursorPos;
      source.OnCursorEnter -= OnCursorEnter;
      source.OnScroll -= OnScroll;
      source.OnFocus -= OnFocus;
      events.Clear();
    }
  }

  /// <summary>
  /// Hosts GUI passes for one window's contents without dispatching the game's OnGUI.
  /// </summary>
  private sealed class ApplicationWindowGui : IDisposable
  {
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public |
      BindingFlags.NonPublic;

    private readonly WindowRenderContext target;
    private readonly object host;
    private readonly IGuiBackend backend;
    private readonly ITextMeshProBackend textBackend;
    private readonly ImGuiContextPtr imguiContext;
    private readonly MethodInfo beginFrame;
    private readonly MethodInfo endFrame;
    private readonly MethodInfo render;
    private readonly Action beginBackendFrame;
    private readonly Action endBackendFrame;
    private readonly Action<bool, Vector2> beginPass;
    private readonly Action endPass;
    private readonly WindowStack shellStack = new();
    private readonly List<Window> shellWindows;
    private readonly GuiStaticState localState = new();
    private readonly GuiStaticState savedState = new();
    private Window sourceWindow;
    private int frameNumber;
    private bool disposed;

    public ApplicationWindowGui(EngineApplication application, WindowRenderContext target)
    {
      this.target = target;
      shellWindows = (List<Window>)RequireField(typeof(WindowStack), "windows").GetValue(shellStack);
      Type hostType = FindType("RimWorldWaylith.Imgui.ImGuiHost");
      Type backendType = FindType("RimWorldWaylith.Imgui.ImGuiGuiBackend");
      Type bakerType = FindType("RimWorldWaylith.Rendering.CameraTextureBaker");
      Type snapshotType = FindType("RimWorldWaylith.Imgui.ImGuiDrawSnapshot");

      // Validate the version-dependent contract before creating native resources.
      ConstructorInfo hostConstructor = hostType.GetConstructor(Members, null,
        [typeof(EngineApplication), typeof(int)], null)
        ?? throw new MissingMethodException(hostType.FullName, ".ctor");
      ConstructorInfo backendConstructor = backendType.GetConstructor(Members, null,
        [hostType, bakerType], null)
        ?? throw new MissingMethodException(backendType.FullName, ".ctor");
      beginFrame = RequireMethod(hostType, "BeginFrame", typeof(int), typeof(float));
      endFrame = RequireMethod(hostType, "EndFrame", snapshotType);
      render = RequireMethod(hostType, "Render", snapshotType, typeof(FrameContext).MakeByRefType());
      FieldInfo contextField = RequireField(hostType, "context");
      MethodInfo tmpBackend = RequireMethod(hostType, "get_TmpFontBackend");
      MethodInfo backendBegin = RequireMethod(backendType, "BeginFrame");
      MethodInfo backendEnd = RequireMethod(backendType, "EndFrame");
      MethodInfo passBegin = RequireMethod(backendType, "BeginPass", typeof(bool), typeof(Vector2));
      MethodInfo passEnd = RequireMethod(backendType, "EndPass");

      // The installed ImGuiHost constructor only reads the device and target from app.
      // Give it a shallow, never-run/never-disposed view with our render context.
      // Do NOT temporarily change the live application's target or construct a new app.
      var applicationView = (EngineApplication)RequireMethod(typeof(object), "MemberwiseClone")
       .Invoke(application, null);
      RequireField(typeof(EngineApplication), "<RenderContext>k__BackingField")
       .SetValue(applicationView, target);

      ImGuiContextPtr previous = ImGui.GetCurrentContext();
      try
      {
        host = hostConstructor.Invoke([applicationView, target.FramesInFlight + 1]);
        imguiContext = (ImGuiContextPtr)contextField.GetValue(host);
        backend = (IGuiBackend)backendConstructor.Invoke([host, null]);
        textBackend = (ITextMeshProBackend)Invoke(tmpBackend, host);
        beginBackendFrame = backendBegin.CreateDelegate<Action>(backend);
        endBackendFrame = backendEnd.CreateDelegate<Action>(backend);
        beginPass = passBegin.CreateDelegate<Action<bool, Vector2>>(backend);
        endPass = passEnd.CreateDelegate<Action>(backend);
      }
      catch
      {
        if (host is IDisposable disposable)
          disposable.Dispose();
        throw;
      }
      finally
      {
        ImGui.SetCurrentContext(previous);
      }
    }

    public object Capture(Window window, ApplicationWindowInput input)
    {
      using var mouseScope = new ApplicationWindowInput.MouseScope();
      ImGuiContextPtr previousContext = ImGui.GetCurrentContext();
      IGuiBackend previousBackend = GuiBackend.Current;
      ITextMeshProBackend previousTextBackend = TextMeshProBackend.Current;
      Event previousEvent = Event.current;
      UIRoot uiRoot = Find.UIRoot
        ?? throw new InvalidOperationException("ApplicationWindow requires RimWorld's UI root.");
      WindowStack previousStack = uiRoot.windows;
      Window previousDrawnWindow = previousStack?.currentlyDrawnWindow;
      Rect previousWindowRect = window.windowRect;
      GameFont previousFont = Text.Font;
      TextAnchor previousAnchor = Text.Anchor;
      bool previousWordWrap = Text.WordWrap;
      int previousWidth = UI.screenWidth;
      int previousHeight = UI.screenHeight;
      UnityScreenState screen = UnityRuntime.Current.Screen;
      int screenWidth = screen.Width;
      int screenHeight = screen.Height;
      int windowWidth = screen.WindowWidth;
      int windowHeight = screen.WindowHeight;
      bool fullScreen = screen.FullScreen;
      bool frameStarted = false;
      bool backendStarted = false;

      savedState.Capture();
      try
      {
        if (!shellWindows.Contains(window))
          shellWindows.Insert(0, window);
        sourceWindow = window;
        uiRoot.windows = shellStack;
        localState.Restore();
        GuiBackend.Current = backend;
        TextMeshProBackend.Current = textBackend;
        ImGui.SetCurrentContext(imguiContext);

        VkExtent2D extent = target.GetExtent();
        // One GUI unit per framebuffer pixel; input is converted from GLFW window units.
        UI.screenWidth = extent.Width;
        UI.screenHeight = extent.Height;
        UnityRuntime.Current.ConfigureScreen(extent.Width, extent.Height, false);
        UnityRuntime.Current.ConfigureScreenWindow(extent.Width, extent.Height);
        window.windowRect = new Rect(0, 0, extent.Width, extent.Height);
        shellStack.currentlyDrawnWindow = window;
        foreach (Window popup in shellWindows.ToArray())
        {
          if (!ReferenceEquals(popup, window))
            popup.WindowUpdate();
        }
        GUI.color = GUI.backgroundColor = GUI.contentColor = Color.white;
        GUI.enabled = true;
        GUI.changed = false;
        if (frameNumber == 0)
          GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;

        object snapshot = Invoke(beginFrame, host, frameNumber++, 1f / 60f);
        frameStarted = true;
        beginBackendFrame();
        backendStarted = true;
        // GLFW callbacks only enqueue. Dispatch on the next PrePollEvents, after the
        // previous poll has completed, with this window's backend and GUI state active.
        while (input.TryDequeue(out Event pending))
        {
          DrawPass(window, input, mouseScope,
            input.CreateEvent(EventType.Layout, pending.mousePosition));
          input.Advance(pending);
          DrawPass(window, input, mouseScope, pending);
        }
        DrawPass(window, input, mouseScope, input.CreateEvent(EventType.Layout));
        DrawPass(window, input, mouseScope, input.CreateEvent(EventType.Repaint));
        endBackendFrame();
        backendStarted = false;
        // EndFrame renders ImGui, uploads fonts and copies the resulting draw data.
        frameStarted = false;
        Invoke(endFrame, host, snapshot);
        return snapshot;
      }
      finally
      {
        try
        {
          if (backendStarted)
            endBackendFrame();
          if (frameStarted)
            ImGui.EndFrame();
        }
        finally
        {
          try
          {
            localState.Capture();
          }
          finally
          {
            UI.screenWidth = previousWidth;
            UI.screenHeight = previousHeight;
            UnityRuntime.Current.ConfigureScreen(screenWidth, screenHeight, fullScreen);
            UnityRuntime.Current.ConfigureScreenWindow(windowWidth, windowHeight);
            window.windowRect = previousWindowRect;
            uiRoot.windows = previousStack;
            if (previousStack != null)
              previousStack.currentlyDrawnWindow = previousDrawnWindow;
            Text.Font = previousFont;
            Text.Anchor = previousAnchor;
            Text.WordWrap = previousWordWrap;
            savedState.Restore();
            Event.current = previousEvent;
            GuiBackend.Current = previousBackend;
            TextMeshProBackend.Current = previousTextBackend;
            ImGui.SetCurrentContext(previousContext);
          }
        }
      }
    }

    private void DrawPass(Window window, ApplicationWindowInput input,
      ApplicationWindowInput.MouseScope mouseScope, Event guiEvent)
    {
      // Setting GUI.matrix calls the backend, which rewrites Event.current's mouse
      // position from its previous pass. Keep the queued position until BeginPass.
      Vector2 physicalMouse = guiEvent.mousePosition;
      mouseScope.Apply(input, guiEvent, physicalMouse, UI.screenHeight);
      Event.current = guiEvent;
      GUI.matrix = Matrix4x4.identity;
      GUIUtility.ResetControlIds();
      beginPass(guiEvent.type == EventType.Repaint, physicalMouse);
      try
      {
        GUI.DispatchInputToWindows();
        GUILayoutUtility.BeginBehaviourGUI(0);
        try
        {
          // Menus opened by this pass become visible on the next pass, so the
          // MouseUp that created one cannot also activate its first option.
          Window[] popups = shellWindows.ToArray();
          shellStack.currentlyDrawnWindow = window;
          var contentRect = new Rect(0, 0, UI.screenWidth, UI.screenHeight);
          backend.BeginWindowContent(window.windowRect);
          try
          {
            GUI.BeginGroup(window.windowRect);
            try
            {
              window.DoWindowContents(contentRect);
            }
            finally
            {
              GUI.EndGroup();
            }
            foreach (Window popup in popups)
            {
              if (!ReferenceEquals(popup, window))
                popup.ExtraOnGUI();
            }
            foreach (Window popup in popups)
            {
              if (!ReferenceEquals(popup, window))
                popup.WindowOnGUI();
            }
            GUI.ProcessPendingWindows();
          }
          finally
          {
            backend.EndWindowContent();
          }
        }
        finally
        {
          GUILayoutUtility.EndBehaviourGUI();
        }
      }
      finally
      {
        endPass();
      }
    }

    public void Record(object snapshot, in FrameContext frame)
    {
      ImGuiContextPtr previous = ImGui.GetCurrentContext();
      try
      {
        ImGui.SetCurrentContext(imguiContext);
        Invoke(render, host, snapshot, frame);
      }
      finally
      {
        ImGui.SetCurrentContext(previous);
      }
    }

    public void Dispose()
    {
      if (disposed)
        return;
      disposed = true;
      UIRoot uiRoot = Find.UIRoot;
      WindowStack previousStack = uiRoot?.windows;
      try
      {
        if (uiRoot != null)
        {
          uiRoot.windows = shellStack;
          foreach (Window popup in shellWindows.ToArray())
          {
            if (!ReferenceEquals(popup, sourceWindow))
              shellStack.TryRemove(popup, false);
          }
        }
      }
      finally
      {
        if (uiRoot != null)
          uiRoot.windows = previousStack;
      }
      ImGuiContextPtr previous = ImGui.GetCurrentContext();
      try
      {
        // Renderer disposal uses GetPlatformIO/GetIO, so select our context first.
        ImGui.SetCurrentContext(imguiContext);
        ((IDisposable)host).Dispose();
      }
      finally
      {
        ImGui.SetCurrentContext(previous);
      }
    }

    private static Type FindType(string name)
    {
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetType(name, false) is { } type)
          return type;
      }
      throw new NotSupportedException($"This Waylith build lacks the expected GUI type: {name}");
    }

    private static FieldInfo RequireField(Type type, string name) =>
      type.GetField(name, Members) ?? throw new MissingFieldException(type.FullName, name);

    private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters) =>
      type.GetMethod(name, Members, null, parameters, null)
      ?? throw new MissingMethodException(type.FullName, name);

    private static object Invoke(MethodInfo method, object instance, params object[] arguments)
    {
      try
      {
        return method.Invoke(instance, arguments);
      }
      catch (TargetInvocationException exception) when (exception.InnerException != null)
      {
        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        throw;
      }
    }

    /// <summary>
    /// UnityStubs stores control maps, pending windows and layout caches statically.
    /// Isolate their contents while preserving the main GUI's collection objects.
    /// </summary>
    private sealed class GuiStaticState
    {
      private static readonly FieldInfo[] Fields = GetFields();
      private readonly object[] values = new object[Fields.Length];
      private bool captured;

      private static FieldInfo[] GetFields()
      {
        var fields = new List<FieldInfo>();
        foreach (Type type in new[] { typeof(GUI), typeof(GUIUtility), typeof(GUILayoutUtility) })
        {
          foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic |
                     BindingFlags.Public))
          {
            if (!field.IsLiteral && (!field.IsInitOnly ||
                typeof(IDictionary).IsAssignableFrom(field.FieldType) ||
                (typeof(IList).IsAssignableFrom(field.FieldType) && !field.FieldType.IsArray)))
              fields.Add(field);
          }
        }
        return fields.ToArray();
      }

      public void Capture()
      {
        for (int i = 0; i < Fields.Length; i++)
        {
          object value = Fields[i].GetValue(null);
          if (Fields[i].IsInitOnly)
          {
            values[i] ??= Activator.CreateInstance(Fields[i].FieldType, nonPublic: true);
            CopyCollection(value, values[i]);
          }
          else
            values[i] = value;
        }
        captured = true;
      }

      public void Restore()
      {
        for (int i = 0; i < Fields.Length; i++)
        {
          if (Fields[i].IsInitOnly)
          {
            object destination = Fields[i].GetValue(null);
            if (captured)
              CopyCollection(values[i], destination);
            else if (destination is IDictionary dictionary)
              dictionary.Clear();
            else if (destination is IList list)
              list.Clear();
          }
          else if (captured)
            Fields[i].SetValue(null, values[i]);
        }
      }

      private static void CopyCollection(object source, object destination)
      {
        if (source is IDictionary dictionary && destination is IDictionary copy)
        {
          copy.Clear();
          foreach (DictionaryEntry entry in dictionary)
            copy.Add(entry.Key, entry.Value);
        }
        else if (source is IList list && destination is IList copyList)
        {
          copyList.Clear();
          foreach (object item in list)
            copyList.Add(item);
        }
      }
    }
  }
}
