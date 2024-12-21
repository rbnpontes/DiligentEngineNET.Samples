using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Diligent;
using SDL;
using Version = Diligent.Version;

namespace DiligentEngineNET.Samples;

public enum GraphicsBackend
{
    D3D11 = 0,
    D3D12,
    Vulkan,
    OpenGL,
}

public abstract unsafe class Application(GraphicsBackend graphicsBackend)
{
    private SDL_Window* _window;
    private IRenderDevice? _renderDevice;
    private IDeviceContext? _immediateContext;
    private IDeviceContext[] _deferredDevices = [];
    private ISwapChain? _swapChain;

    protected IRenderDevice RenderDevice => _renderDevice ?? throw new NullReferenceException();
    protected IDeviceContext ImmediateContext => _immediateContext ?? throw new NullReferenceException();
    protected ISwapChain SwapChain => _swapChain ?? throw new NullReferenceException();

    public Size WindowSize
    {
        get
        {
            var x = 0;
            var y = 0;
            SDL3.SDL_GetWindowSize(_window, &x, &y);
            return new Size(x, y);
        }
    }

    public void Setup()
    {
        SetupSDL();
        SetupDiligentEngine();
        OnSetup();
    }

    public void Run()
    {
        var stop = false;
        var stopWatch = Stopwatch.StartNew();
        while (!stop)
        {
            SDL_Event evt;
            while (SDL3.SDL_PollEvent(&evt))
            {
                if (evt.type == (uint)SDL_EventType.SDL_EVENT_QUIT)
                    stop = true;
            }

            OnUpdate(stopWatch.Elapsed.TotalSeconds);
            stopWatch.Restart();
        }

        OnExit();
        ReleaseDiligentObjects();

        SDL3.SDL_DestroyWindow(_window);
        SDL3.SDL_Quit();
    }

    private void SetupSDL()
    {
        if (!SDL3.SDL_Init(0))
            throw new Exception("Failed to initialize SDL");

        _window = SDL3.SDL_CreateWindow("Diligent Engine .NET - Samples", 500, 500,
            SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
        SDL3.SDL_ShowWindow(_window);
    }

    private void SetupDiligentEngine()
    {
        switch (graphicsBackend)
        {
            case GraphicsBackend.D3D11:
                SetupD3D11();
                break;
            case GraphicsBackend.D3D12:
                SetupD3D12();
                break;
            case GraphicsBackend.Vulkan:
                SetupVulkan();
                break;
            case GraphicsBackend.OpenGL:
                SetupOpenGL();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(graphicsBackend), graphicsBackend, null);
        }

        void SetupD3D11()
        {
            var engineFactory = DiligentCore.GetEngineFactoryD3D11();
            if (engineFactory is null)
                throw new NullReferenceException($"Failed to get {nameof(IEngineFactoryD3D11)}");

            var createInfo = new EngineD3D11CreateInfo()
            {
                EnableValidation = true,
                GraphicsAPIVersion = new Version(11, 0),
                NumImmediateContexts = 1
            };
            OnSetupEngineCreateInfo(createInfo);
            var (renderDevice, deviceContexts) = engineFactory.CreateDeviceAndContexts(createInfo);
            _immediateContext = deviceContexts[0];
            _deferredDevices = deviceContexts.Skip(1).ToArray();
            _renderDevice = renderDevice;
            
            var wndSize = WindowSize;
            var swapChainDesc = new SwapChainDesc()
            {
                Width = (uint)wndSize.Width,
                Height = (uint)wndSize.Height,
                BufferCount = 2,
                Usage = SwapChainUsageFlags.RenderTarget,
                IsPrimary = true,
                ColorBufferFormat = TextureFormat.Rgba8Unorm,
                DepthBufferFormat = TextureFormat.D16Unorm
            };
            OnSetupSwapChainDesc(swapChainDesc);
            var swapChain = engineFactory.CreateSwapChain(renderDevice, deviceContexts[0], swapChainDesc,
                new FullScreenModeDesc() { }, GetNativeWindowHandle());
            
            _swapChain = swapChain;
        }

        void SetupD3D12()
        {
            var engineFactory = DiligentCore.GetEngineFactoryD3D12();
            if(engineFactory is null)
                throw new NullReferenceException($"Failed to get {nameof(IEngineFactoryD3D12)}");
            
            engineFactory.LoadD3D12();
            var createInfo = new EngineD3D12CreateInfo()
            {
                EnableValidation = true,
                NumImmediateContexts = 1,
            };
            OnSetupEngineCreateInfo(createInfo);
            var (renderDevice, deviceContexts) = engineFactory.CreateDeviceAndContext(createInfo);
            _immediateContext = deviceContexts[0];
            _deferredDevices = deviceContexts.Skip(1).ToArray();
            _renderDevice = renderDevice;
            
            var wndSize = WindowSize;
            var swapChainDesc = new SwapChainDesc()
            {
                Width = (uint)wndSize.Width,
                Height = (uint)wndSize.Height,
                BufferCount = 2,
                Usage = SwapChainUsageFlags.RenderTarget,
                IsPrimary = true,
                ColorBufferFormat = TextureFormat.Rgba8Unorm,
                DepthBufferFormat = TextureFormat.D16Unorm
            };
            OnSetupSwapChainDesc(swapChainDesc);
            var swapChain = engineFactory.CreateSwapChain(renderDevice, deviceContexts[0], swapChainDesc,
                new FullScreenModeDesc() { }, GetNativeWindowHandle());
            
            _swapChain = swapChain;
        }

        void SetupVulkan()
        {
            var engineFactory = DiligentCore.GetEngineFactoryVk();
            if(engineFactory is null)
                throw new NullReferenceException($"Failed to get {nameof(IEngineFactoryVk)}");

            var createInfo = new EngineVkCreateInfo()
            {
                EnableValidation = true,
                NumImmediateContexts = 1,
            };
            OnSetupEngineCreateInfo(createInfo);
            
            var (renderDevice, deviceContexts) = engineFactory.CreateDeviceAndContexts(createInfo);
            _immediateContext = deviceContexts[0];
            _deferredDevices = deviceContexts.Skip(1).ToArray();
            _renderDevice = renderDevice;
            
            var wndSize = WindowSize;
            var swapChainDesc = new SwapChainDesc()
            {
                Width = (uint)wndSize.Width,
                Height = (uint)wndSize.Height,
                BufferCount = 2,
                Usage = SwapChainUsageFlags.RenderTarget,
                IsPrimary = true,
                ColorBufferFormat = TextureFormat.Rgba8Unorm,
                DepthBufferFormat = TextureFormat.D16Unorm
            };
            OnSetupSwapChainDesc(swapChainDesc);
            var swapChain = engineFactory.CreateSwapChain(renderDevice, deviceContexts[0], swapChainDesc, GetNativeWindowHandle());
            _swapChain = swapChain;
        }

        void SetupOpenGL()
        {
            var engineFactory = DiligentCore.GetEngineFactoryOpenGL();
            if(engineFactory is null)
                throw new NullReferenceException($"Failed to get {nameof(IEngineFactoryOpenGL)}");
            
            var createInfo = new EngineOpenGlCreateInfo()
            {
                EnableValidation = true,
                NumImmediateContexts = 1,
                Window = GetNativeWindowHandle()
            };
            var wndSize = WindowSize;
            var swapChainDesc = new SwapChainDesc()
            {
                Width = (uint)wndSize.Width,
                Height = (uint)wndSize.Height,
                BufferCount = 2,
                Usage = SwapChainUsageFlags.RenderTarget,
                IsPrimary = true,
                ColorBufferFormat = TextureFormat.Rgba8Unorm,
                DepthBufferFormat = TextureFormat.D16Unorm
            };
            
            OnSetupEngineCreateInfo(createInfo);
            OnSetupSwapChainDesc(swapChainDesc);
            // OpenGL doesn't support deferred contexts
            createInfo.NumDeferredContexts = 0;
            createInfo.NumImmediateContexts = 1;
            
            var (renderDevice, immediateContext, swapChain) = engineFactory.CreateDeviceAndSwapChain(createInfo, swapChainDesc);
            _renderDevice = renderDevice;
            _immediateContext = immediateContext;
            _swapChain = swapChain;
        }
    }

    private void ReleaseDiligentObjects()
    {
        _swapChain?.Dispose();
        foreach (var deviceContext in _deferredDevices)
            deviceContext.Dispose();
        _immediateContext?.Dispose();
        _renderDevice?.Dispose();
    }
    private WindowHandle GetNativeWindowHandle()
    {
        var props = SDL3.SDL_GetWindowProperties(_window);

        if (OperatingSystem.IsWindows())
            return WindowHandle.CreateWin32Window(SDL3.SDL_GetPointerProperty(props,
                SDL3.SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero));
        if (OperatingSystem.IsLinux())
        {
            return WindowHandle.CreateLinuxWindow(new LinuxWindowHandle()
            {
                Window_id_ = (uint)SDL3
                    .SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, IntPtr.Zero).ToInt32(),
                display_ = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, IntPtr.Zero),
            });
        }

        throw new PlatformNotSupportedException();
    }

    protected virtual void OnSetupEngineCreateInfo(EngineCreateInfo createInfo)
    {
    }

    protected virtual void OnSetupSwapChainDesc(SwapChainDesc swapChainDesc)
    {
    }

    protected abstract void OnSetup();
    protected abstract void OnUpdate(double dt);
    protected abstract void OnExit();
}