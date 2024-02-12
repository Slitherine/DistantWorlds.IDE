using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using BCnEncoder.Shared;
using DistantWorlds.IDE.ImageSharp;
using JetBrains.Annotations;
using Microsoft.Toolkit.HighPerformance;
using OneOf.Types;
using Pango;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Image = Stride.Graphics.Image;

namespace DistantWorlds.IDE.Stride;

public static class BundleManager {

    public static readonly ServiceRegistry Services;

    public static readonly ContentManager Content;

    public static readonly IContentSerializer<Image> ImageSerializer = new ImageTextureSerializer();

    private static ObjectDatabase? DefaultObjectDatabase;

    private static DatabaseFileProvider DefaultDatabaseFileProvider;

    private static DatabaseFileProviderService DatabaseFileProviderService;

    static BundleManager() {
        Services = new();

        //DriveFileProvider = new("/");

        DefaultObjectDatabase = new(
            VirtualFileSystem.ApplicationDatabasePath,
            VirtualFileSystem.ApplicationDatabaseIndexName,
            VirtualFileSystem.LocalDatabasePath,
            false
        );
        DefaultDatabaseFileProvider = new(DefaultObjectDatabase);
        DatabaseFileProviderService = new(DefaultDatabaseFileProvider);
        Services.AddService<IDatabaseFileProviderService>(DatabaseFileProviderService);

        Content = new(Services) {
            Serializer = {
                LowLevelSerializerSelectorWithReuse = new(true, false, "Default", "Content"),
                LowLevelSerializerSelector = new("Default", "Content")
            }
        };
        Services.AddService<IContentManager>(Content);
        Services.AddService(Content);

        Content.Serializer.RegisterSerializer(ImageSerializer);

        var backend = Content.FileProvider.ObjectDatabase.BundleBackend;
        backend.BundleResolve = ResolveBundle;
    }

#pragma warning disable VSTHRD200
    private static Task<string?> ResolveBundle(string name) {
        if (BundlePaths.TryGetValue(name, out var pathSet))
            if (pathSet.Count == 1) {
                var path = pathSet.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(path)!;
            }

        var fspStackSnapshot = new FileSystemProvider?[BundleSourceFspStack.Count + 1];
        BundleSourceFspStack.CopyTo(fspStackSnapshot!, 0);

        foreach (var fsp in fspStackSnapshot) {
            var found = fsp is not null && fsp
                .ListFiles("", $"{name}.bundle", VirtualSearchOption.TopDirectoryOnly)
                .Length != 0;

            if (found)
                return Task.FromResult($"{fsp!.RootPath}{name}.bundle");
        }

        return Task.FromResult((string?)null);
    }
#pragma warning restore VSTHRD200

    private static Dictionary<string, HashSet<string>> BundlePaths = new();

    private static ThreadLocal<ConcurrentStack<FileSystemProvider>> _BundleSourceFspStack
        = new(() => {
            var fspStack = new ConcurrentStack<FileSystemProvider>();
            // add default bundle source from <game directory>/data/db/bundles
            var gameDir = OperatingSystem.IsWindows()
                ? Dw2Env.UserChosenGameDirectory
                : Dw2Env.GameDirectory;
            // ReSharper disable once InvertIf
            if (gameDir is not null) {
                var fsp = new FileSystemProvider("/data/db/bundles/",
                    Path.Combine(gameDir, "data", "db", "bundles"));
                fspStack.Push(fsp);
            }

            return fspStack;
        });

    private static ConcurrentStack<FileSystemProvider> BundleSourceFspStack => _BundleSourceFspStack.Value!;

    /*
      private class LoadedBundle
      {
          public string BundleName;
          public string BundleUrl;
          public int ReferenceCount;
          public BundleDescription Description;

          // Stream pool to avoid reopening same file multiple time (list, one per incremental file)
          public List<string> Files;
          public List<Stream> Streams;
      }
     */
    public sealed class Bundle {

        private object _bundle;

        public Bundle(object bundle) => _bundle = bundle;

        private static T GetField<T>(object obj, string name)
            => (T)obj.GetType()
                .GetField(name, AnyInstanceBindingFlags)!
                .GetValue(obj)!;

        public string BundleName
            => GetField<string>(_bundle, nameof(BundleName));

        public string BundleUrl
            => GetField<string>(_bundle, nameof(BundleUrl));

        public int ReferenceCount
            => GetField<int>(_bundle, nameof(ReferenceCount));

        public BundleDescription Description
            => GetField<BundleDescription>(_bundle, nameof(Description));

        // NOTE: this is the list of bundle files, not objects in the bundle
        public List<string> Files
            => GetField<List<string>>(_bundle, nameof(Files));

        public List<Stream> Streams
            => GetField<List<Stream>>(_bundle, nameof(Streams));

        public override string ToString()
            => $"[Bundle {BundleName}]";

    }

    private static readonly FieldInfo BundleBackendLoadedBundlesField
        = typeof(BundleOdbBackend).GetField("loadedBundles", AnyInstanceBindingFlags)!;

    public static IEnumerable<Bundle> GetLoadedBundles() {
        return ((IEnumerable)BundleBackendLoadedBundlesField.GetValue
                (Content.FileProvider.ObjectDatabase.BundleBackend)!)
            .Cast<object>().Select(o => new Bundle(o));
    }

    public static string LoadBundle(string bundlePath) {
        var bundleName = Path.GetFileNameWithoutExtension(bundlePath);
        if (string.IsNullOrWhiteSpace(bundleName))
            throw new InvalidOperationException("Invalid bundle file name.");
        if (!File.Exists(bundlePath))
            throw new FileNotFoundException($"Can't find the {bundleName} bundle.", bundlePath);

        var fullPath = Path.GetFullPath(bundlePath);

        var directory = Path.GetDirectoryName(fullPath);

        //fullPath = fullPath.Replace('\\', '/');

        if (!BundlePaths.TryGetValue(bundleName, out var bundlePaths))
            BundlePaths[bundleName] = new() { fullPath };
        else {
            if (!bundlePaths.Add(fullPath)) // already loaded
                return bundleName;
        }

        var pushed = false;
        var root = "data/db/bundles";
        var fsp = BundleSourceFspStack.FirstOrDefault(x => {
                var fspDir = x.GetAbsolutePath("").TrimEnd('/', '\\');
                return fspDir == directory;
            })
            ?? new FileSystemProvider($"/{root = Guid.NewGuid().ToString("N")}/", directory);
        if (BundleSourceFspStack.TryPeek(out var peek) && peek != fsp) {
            BundleSourceFspStack.Push(fsp);
            pushed = true;
        }

        try {
            /*
            var colonIndex = fullPath.IndexOf(':');
            if (colonIndex != -1)
                fullPath = $"/{fullPath.Remove(colonIndex, 1).Replace('\\', '/')}";
            */
            Content.FileProvider.ObjectDatabase.BundleBackend.LoadBundleFromUrl
                (bundleName, Content.FileProvider.ObjectDatabase.ContentIndexMap,
                    $"/{root}/{bundleName}.bundle")
                .ConfigureAwait(false).GetAwaiter().GetResult();

            HashSet<string> files = new();
            foreach (var objId in Content.FileProvider.ObjectDatabase.BundleBackend.EnumerateObjects()) {
                // preload some data 
                try {
                    Content.FileProvider.ObjectDatabase.BundleBackend.TryGetObjectLocation(objId, out var file, out _,
                        out _);
                    if (!files.Add(file)) continue;

                    using (Content.FileProvider.ObjectDatabase.BundleBackend.OpenStream(objId)) {
                        // just open and close stream to load the bundle file
                    }
                }
                catch {
                    // ignore
                }
            }
        }
        finally {
            if (pushed) {
                BundleSourceFspStack.TryPop(out var topFsp);
                Debug.Assert(topFsp == fsp);
                if (!BundleSourceFspStack.Contains(topFsp))
                    VirtualFileSystem.UnregisterProvider(topFsp);
            }
        }

        ObjectInfos = Content.FileProvider.ObjectDatabase.BundleBackend.GetObjectInfos();

        return bundleName;
    }

    public static Dictionary<ObjectId, BundleOdbBackend.ObjectInfo>? ObjectInfos { get; private set; }

    public static ObjectId? GetObjectId(string path) {
        if (Content.FileProvider.ContentIndexMap.TryGetValue(path, out var id))
            return id;

        return null;
    }

    public static long GetObjectOffset(ObjectId? id, out long offsetEnd, out string? sourceDirOrBundlePath) {
        offsetEnd = -1;
        sourceDirOrBundlePath = null;
        if (id is null) return -1;

        if (Content.FileProvider.ObjectDatabase.TryGetObjectLocation(id.Value, out sourceDirOrBundlePath,
                out var offset, out offsetEnd))
            return offset;

        return -1;
    }

    public static long GetObjectSize(ObjectId? id) {
        if (id is null) return -1;

        return Content.FileProvider.ObjectDatabase.GetSize(id.Value);
    }

    public static string? GetObjectQualifiedType(ObjectId? objId) {
        if (objId is not { } id) return null;
        if (id == default) return null;

        using var s = new MemoryStream();
        using (var zs = Content.FileProvider.ObjectDatabase.OpenStream(id))
            zs.CopyTo(s, 83886080);
        s.Position = 0;

        var h = GetChunkHeader(s);
        if (h is null) return null;

        return h.Type;
    }

    public static string? GetObjectType(ObjectId? id, out string? simpleType) {
        var typeStr = GetObjectQualifiedType(id);
        simpleType = GetObjectSimplifiedType(typeStr);
        return typeStr;
    }

    public static string? GetObjectSimplifiedType(ObjectId? id) {
        var typeStr = GetObjectQualifiedType(id);
        return GetObjectSimplifiedType(typeStr);
    }

    private static string? GetObjectSimplifiedType(string? typeStr) {
        if (typeStr is null)
            return null;

        var type = Type.GetType(typeStr, false);
        if (type is not null)
            return type.Name;

        var firstComma = typeStr.IndexOf(',');
        if (firstComma != -1)
            typeStr = typeStr[..firstComma];
        var firstSpace = typeStr.IndexOf(' ');
        if (firstSpace != -1)
            typeStr = typeStr[..firstSpace];
        var lastDot = typeStr.LastIndexOf('.');
        if (lastDot != -1)
            typeStr = typeStr[(lastDot + 1)..];
        return typeStr;
    }

    private static ChunkHeader GetChunkHeader(Stream s) {
        var startPos = s.Position;
        var r = new BinarySerializationReader(s);
        var h = ChunkHeader.Read(r);
        s.Position = startPos;
        return h;
    }

    public static object? InstantiateObject(string? url) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (url is null) return null;

        var objId = GetObjectId(url);
        if (objId is null) return null;

        return InstantiateObject(url, objId.Value);
    }

    private const BindingFlags AnyInstanceBindingFlags
        = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    private static readonly unsafe delegate *<ContentSerializer, Type, Type, IContentSerializer>
        GetSerializer = (delegate *<ContentSerializer, Type, Type, IContentSerializer>)
            typeof(ContentSerializer).GetMethod("GetSerializer", AnyInstanceBindingFlags)!
                .MethodHandle.GetFunctionPointer();

    private static readonly Type TypeContentSzrCtx = typeof(ContentSerializerContext);

    private static readonly unsafe delegate *<ContentSerializerContext, SerializationStream, void>
        SerializeReferences = (delegate *<ContentSerializerContext, SerializationStream, void>)
            TypeContentSzrCtx.GetMethod("SerializeReferences", AnyInstanceBindingFlags)!
                .MethodHandle.GetFunctionPointer();

    private static readonly unsafe delegate *<ContentSerializerContext, SerializationStream, IContentSerializer, object,
        void>
        SerializeContent = (delegate *<ContentSerializerContext, SerializationStream, IContentSerializer, object, void>)
            TypeContentSzrCtx.GetMethod("SerializeContent", AnyInstanceBindingFlags)!
                .MethodHandle.GetFunctionPointer();

    private static unsafe object? InstantiateObject(string url, ObjectId objectId) {
        // note: if url is wrong, embedded references will fail to resolve or be wrong

        using var srcStream = Content.FileProvider.ObjectDatabase.OpenStream(objectId);
        Stream stream;
        if (srcStream.CanSeek)
            stream = srcStream;
        else {
            srcStream.CopyTo(stream = new MemoryStream());
            stream.Position = 0;
        }

        // Open asset binary stream
        // Read header
        var streamReader = new BinarySerializationReader(stream);
        var chunkHeader = ChunkHeader.Read(streamReader);
        Type? headerObjType = null;
        Type? objType = null;
        if (chunkHeader != null && chunkHeader.Type != null) {
            var comma = chunkHeader.Type.IndexOf(',');
            if (comma != -1) {
                var typeName = chunkHeader.Type[..comma];
                var asmName = new AssemblyName(chunkHeader.Type[(comma + 1)..]);
                if (typeName is "Stride.Graphics.Texture"
                    && asmName.Name is "Stride.Graphics") {
                    headerObjType = ImageTextureSerializer.FakeTypeOfTexture;
                    objType = typeof(Image);
                }
                else
                    AssemblyRegistry.GetType(chunkHeader.Type, false);
            }
        }

        if (headerObjType is null || objType is null)
            return null;

        // Find serializer
        var serializer = GetSerializer(Content.Serializer, headerObjType, objType);
        if (serializer == null)
            throw new InvalidOperationException(
                $"Content serializer for {headerObjType}/{objType} could not be found.");

        var contentSerializerContext = (ContentSerializerContext)
            Activator.CreateInstance(TypeContentSzrCtx, AnyInstanceBindingFlags, null,
                new object[] { url, ArchiveMode.Deserialize, Content }, null)!;

        // Read chunk references
        if (chunkHeader != null && chunkHeader.OffsetToReferences != -1) {
            // Seek to where references are stored and deserialize them
            streamReader.UnderlyingStream.Seek(chunkHeader.OffsetToReferences, SeekOrigin.Begin);
            SerializeReferences(contentSerializerContext, streamReader);
            streamReader.UnderlyingStream.Seek(chunkHeader.OffsetToObject, SeekOrigin.Begin);
        }

        var result = serializer.Construct(contentSerializerContext);

        SerializeContent(contentSerializerContext, streamReader, serializer, result);

        return result;
    }

    private static CompressionFormat GetCompressionFormat(PixelFormat fmt) {
        return fmt switch {
            PixelFormat.BC1_UNorm => CompressionFormat.Bc1,
            PixelFormat.BC1_UNorm_SRgb => CompressionFormat.Bc1,
            PixelFormat.BC1_Typeless => CompressionFormat.Bc1,
            PixelFormat.BC2_UNorm => CompressionFormat.Bc2,
            PixelFormat.BC2_UNorm_SRgb => CompressionFormat.Bc2,
            PixelFormat.BC2_Typeless => CompressionFormat.Bc2,
            PixelFormat.BC3_UNorm => CompressionFormat.Bc3,
            PixelFormat.BC3_UNorm_SRgb => CompressionFormat.Bc3,
            PixelFormat.BC3_Typeless => CompressionFormat.Bc3,
            PixelFormat.BC4_UNorm => CompressionFormat.Bc4,
            PixelFormat.BC4_SNorm => CompressionFormat.Bc4,
            PixelFormat.BC4_Typeless => CompressionFormat.Bc4,
            PixelFormat.BC5_UNorm => CompressionFormat.Bc5,
            PixelFormat.BC5_SNorm => CompressionFormat.Bc5,
            PixelFormat.BC5_Typeless => CompressionFormat.Bc5,
            PixelFormat.BC6H_Sf16 => CompressionFormat.Bc6S,
            PixelFormat.BC6H_Uf16 => CompressionFormat.Bc6U,
            PixelFormat.BC6H_Typeless => CompressionFormat.Bc6S,
            PixelFormat.BC7_UNorm => CompressionFormat.Bc7,
            PixelFormat.BC7_UNorm_SRgb => CompressionFormat.Bc7,
            PixelFormat.BC7_Typeless => CompressionFormat.Bc7,

            PixelFormat.R8G8B8A8_UNorm => CompressionFormat.Rgba,
            PixelFormat.R8G8B8A8_UNorm_SRgb => CompressionFormat.Rgba,
            PixelFormat.R8G8B8A8_Typeless => CompressionFormat.Rgba,
            PixelFormat.R8G8B8A8_SNorm => CompressionFormat.Rgba,
            PixelFormat.R8G8B8A8_SInt => CompressionFormat.Rgba,
            PixelFormat.R8G8B8A8_UInt => CompressionFormat.Rgba,

            PixelFormat.R16G16B16A16_UNorm => CompressionFormat.Rgba,
            PixelFormat.R16G16B16A16_Typeless => CompressionFormat.Rgba,
            PixelFormat.R16G16B16A16_SNorm => CompressionFormat.Rgba,
            PixelFormat.R16G16B16A16_SInt => CompressionFormat.Rgba,
            PixelFormat.R16G16B16A16_UInt => CompressionFormat.Rgba,

            PixelFormat.R10G10B10A2_Typeless => CompressionFormat.Rgba,
            PixelFormat.R10G10B10A2_UInt => CompressionFormat.Rgba,
            PixelFormat.R10G10B10A2_UNorm => CompressionFormat.Rgba,

            PixelFormat.B8G8R8A8_UNorm => CompressionFormat.Bgra,
            PixelFormat.B8G8R8A8_UNorm_SRgb => CompressionFormat.Bgra,
            PixelFormat.B8G8R8A8_Typeless => CompressionFormat.Bgra,

            PixelFormat.R8G8_UNorm => CompressionFormat.Rg,
            PixelFormat.R8G8_UInt => CompressionFormat.Rg,
            PixelFormat.R8G8_SInt => CompressionFormat.Rg,
            PixelFormat.R8G8_SNorm => CompressionFormat.Rg,

            PixelFormat.R16G16_UNorm => CompressionFormat.Rg,
            PixelFormat.R16G16_UInt => CompressionFormat.Rg,
            PixelFormat.R16G16_SInt => CompressionFormat.Rg,
            PixelFormat.R16G16_SNorm => CompressionFormat.Rg,
            PixelFormat.R16G16_Float => CompressionFormat.Rg,

            PixelFormat.R8_UNorm => CompressionFormat.R,
            PixelFormat.R8_UInt => CompressionFormat.R,
            PixelFormat.R8_Typeless => CompressionFormat.R,
            PixelFormat.R8_SNorm => CompressionFormat.R,
            PixelFormat.R8_SInt => CompressionFormat.R,
            PixelFormat.A8_UNorm => CompressionFormat.R,

            PixelFormat.R16_UNorm => CompressionFormat.R,
            PixelFormat.R16_UInt => CompressionFormat.R,
            PixelFormat.R16_Typeless => CompressionFormat.R,
            PixelFormat.R16_SNorm => CompressionFormat.R,
            PixelFormat.R16_SInt => CompressionFormat.R,
            PixelFormat.R16_Float => CompressionFormat.R,

            _ => CompressionFormat.Unknown
        };
    }

    private static readonly ThreadLocal<BCnEncoder.Decoder.BcDecoder> LazyBCnDecoder
        = new(() => new(), false);

    private static BCnEncoder.Decoder.BcDecoder BCnDecoder => LazyBCnDecoder.Value!;

    public static unsafe void ConvertToImages(Image img, bool includeMips,
        [InstantHandle] Action<SixLabors.ImageSharp.Image, int> withConverted, int mipLevel = 0, int maxMipLevel = -1) {
        if (img.Description.Dimension == TextureDimension.Texture1D) {
            throw new NotImplementedException("1D textures not supported yet.");
        }

        if (img.Description.Dimension == TextureDimension.Texture3D) {
            throw new NotImplementedException("3D textures not supported yet.");
        }

        if (img.Description.Dimension == TextureDimension.TextureCube) {
            throw new NotImplementedException("Cubemap not supported yet.");
        }

        var fmt = img.Description.Format;
        var boxIndex = 0;

        maxMipLevel = maxMipLevel < 0
            ? img.Description.MipLevels - 1
            : Math.Max(img.Description.MipLevels - 1, maxMipLevel);

        for (var i = mipLevel; i <= maxMipLevel; i++) {
            var box = img.GetPixelBuffer(0, i);
            boxIndex++;
            var p = box.DataPointer;
            var width = box.Width;
            var height = box.Height;

            var compressionFormat = GetCompressionFormat(fmt);
            if (compressionFormat == CompressionFormat.Unknown)
                throw new NotImplementedException($"Unsupported format {fmt}");

            if (compressionFormat is not
                (CompressionFormat.R
                or CompressionFormat.Rg
                or CompressionFormat.Rgb
                or CompressionFormat.Rgba)) {
                using var ums = new UnmanagedMemoryStream((byte*)p.ToPointer(), box.BufferStride);
                if (compressionFormat is CompressionFormat.Bc6S or CompressionFormat.Bc6U) {
                    var mem2D = BCnDecoder.DecodeRawHdr2D(ums, width, height, compressionFormat);
                    if (mem2D.TryGetMemory(out var mem)) {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RgbVector>(mem.Span.AsBytes(), width,
                            height);
                        withConverted(isImg, boxIndex);
                        if (!includeMips) break;
                    }
                    else
                        throw new NotImplementedException("Failed to get contiguous memory of decoded image box.");
                }
                else {
                    var mem2D = BCnDecoder.DecodeRaw2D(ums, width, height, compressionFormat);
                    if (mem2D.TryGetMemory(out var mem)) {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(mem.Span.AsBytes(), width, height);
                        withConverted(isImg, boxIndex);
                        if (!includeMips) break;
                    }
                    else
                        throw new NotImplementedException("Failed to get contiguous memory of decoded image box.");
                }
            }
            else {
                var span = new Span<byte>((byte*)p.ToPointer(), box.BufferStride);
                switch (fmt) {
                    case PixelFormat.R32G32B32A32_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RgbaVector>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R32G32B32_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RgbVector>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R8G8B8A8_SNorm:
                    case PixelFormat.R8G8B8A8_SInt:
                    case PixelFormat.R8G8B8A8_UInt:
                    case PixelFormat.R8G8B8A8_UNorm:
                    case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    case PixelFormat.R8G8B8A8_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.B8G8R8A8_UNorm:
                    case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    case PixelFormat.B8G8R8A8_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R16G16B16A16_SNorm:
                    case PixelFormat.R16G16B16A16_SInt:
                    case PixelFormat.R16G16B16A16_UInt:
                    case PixelFormat.R16G16B16A16_UNorm:
                    case PixelFormat.R16G16B16A16_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Rgba64>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R10G10B10A2_UInt:
                    case PixelFormat.R10G10B10A2_UNorm:
                    case PixelFormat.R10G10B10A2_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Rgba1010102>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R32G32_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RgVector>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R16G16_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RgHalfVector>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R16G16_UInt:
                    case PixelFormat.R16G16_UNorm:
                    case PixelFormat.R16G16_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<Rg32>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R32_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RFloat>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R16_Float: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<RHalf>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.R16_UInt:
                    case PixelFormat.R16_UNorm:
                    case PixelFormat.R16_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<L16>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    case PixelFormat.A8_UNorm:
                    case PixelFormat.R8_UInt:
                    case PixelFormat.R8_UNorm:
                    case PixelFormat.R8_Typeless: {
                        var isImg = SixLabors.ImageSharp.Image.LoadPixelData<L8>(span, width, height);
                        withConverted(isImg, boxIndex);
                        break;
                    }
                    default:
                        throw new NotImplementedException($"Unsupported format {fmt}");
                }

                if (!includeMips) break;
            }
        }
    }

    public static void UnloadAllBundles() {
        /* broken in stride, doesn't work, not properly implemented
        for (;;) {
          var bundles = GetLoadedBundles().ToArray();
          if (bundles.Length == 0) return;

          foreach (var bundle in bundles)
            try {
              Content.FileProvider.ObjectDatabase.BundleBackend.
            }
            catch {
              // ffs stride
            }
        }*/
    }

    public static bool TryExportObject(in ObjectId objectId, string path) {
        if (objectId == default) return false;
        if (string.IsNullOrWhiteSpace(path)) return false;

        try {
            ThreadPool.QueueUserWorkItem(static t => {
                var (objectId, path) = ((ObjectId, string))t!;
                using var srcStream = Content.FileProvider.ObjectDatabase.OpenStream(objectId);
                using var dstStream = File.OpenWrite(path);
                srcStream.CopyTo(dstStream);
            }, (objectId, path), false);
            return true;
        }
        catch {
            return false;
        }
    }

    public static bool TryExportImageAsWebp(Image img, string path) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (img is null) return false;
        if (string.IsNullOrWhiteSpace(path)) return false;

        try {
            ThreadPool.QueueUserWorkItem(static t => {
                var (img, path) = ((Image, string))t!;
                using var ms = GetRecyclableMemoryStream();
                ConvertToImages(img, false, (newImg, _) => {
                    newImg.SaveAsWebp(ms);
                }, 0, 0);
                using var fs = File.Create(path,
                    2 * 1024 * 1024,
                    FileOptions.SequentialScan);
                ms.Position = 0;
                ms.WriteTo(fs);
            }, (img, path), false);
            return true;
        }
        catch {
            return false;
        }
    }

    public static bool TryExportImageAsDds(Image img, string path) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (img is null) return false;
        if (string.IsNullOrWhiteSpace(path)) return false;

        try {
            ThreadPool.QueueUserWorkItem(static t => {
                var (img, path) = ((Image, string))t!;
                using var fs = File.Create(path,
                    2 * 1024 * 1024,
                    FileOptions.SequentialScan);
                img.Save(fs, ImageFileType.Dds);
            }, (img, path), false);
            return true;
        }
        catch {
            return false;
        }
    }

    public static BundleBuilder CreateBundleBuilder(string bundleName) {
        return new(bundleName);
    }

}