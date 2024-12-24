using DiligentEngineNET.Samples;

// Sample List
var samplesMap = new Dictionary<string, Func<GraphicsBackend, Application>>()
{
    { "Triangle Sample", (backend)=> new TriangleSample(backend) },
    { "Cube Sample", (backend)=> new CubeSample(backend) },
    { "Cube Texture Sample", (backend)=> new CubeTextureSample(backend) },
    { "Instancing Sample", (backend)=> new InstancingSample(backend, 25) },
    { "Texture Sample", (backend)=> new TextureArraySample(backend, 25) },
};

Console.WriteLine("- Choose one of samples:");
var sampleKeys = samplesMap.Keys.ToArray();
for (var i = 0; i < sampleKeys.Length; i++)
    Console.WriteLine($"[{i}] {sampleKeys[i]}");
var selectedKey = (uint)Convert.ToInt32(Console.ReadLine());
if(selectedKey >= samplesMap.Count)
    throw new Exception("Invalid selection");

Console.WriteLine("- Choose Graphics Backend:");
var backendTypes = Enum.GetValues<GraphicsBackend>();
for(var i = 0; i < backendTypes.Length; i++)
    Console.WriteLine($"[{i}] {backendTypes[i]}");

var selectedBackend = (uint)Convert.ToInt32(Console.ReadLine());
if(selectedBackend >= backendTypes.Length)
    throw new Exception("Invalid selection");

Console.Clear();
Console.WriteLine($"[{selectedBackend}] Running sample: {sampleKeys[selectedKey]}");

// Finally run sample
var sampleFn = samplesMap[sampleKeys[selectedKey]];
var sample = sampleFn((GraphicsBackend)selectedBackend);
sample.Setup();
sample.Run();