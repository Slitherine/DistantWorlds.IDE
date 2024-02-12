using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DistantWorlds.IDE.Stride;
using Microsoft.IO;
using Minimatch;
using Stride.Core.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Stride.Graphics;
using static DistantWorlds.IDE.Dw2Env;
using Image = Stride.Graphics.Image;
using CBool = byte;

namespace DistantWorlds.IDE;

/// <remarks>
/// Some notes on DNNE:
/// DNNE MSBuild output messages only show up at diagnostic level normal and above.
/// It will silently not update the generated source file if there is an error.
/// bool is not understood by DNNE, use byte values 0 and 1.
/// </remarks>
public static class Exports {

    // C++ / C99 boolean support
    private const byte True = 1;

    private const byte False = 0;

    static Exports() {
        // some quick assertions
        Debug.Assert(ObjectId.HashSize == 16);
        Debug.Assert(ObjectId.HashStringLength == 32);
    }

    [DNNE.C99DeclCode( //language=c++
        $"""
         #ifndef __cplusplus
         #include <stdbool.h>
         #endif

         typedef void(* StreamWrite_t)(void* state, unsigned char* buffer, size_t length);

         const size_t ObjectIdByteSize = 16;
         const size_t ObjectIdStringLength = 32;

         """
        + Interop.Array.C
        + Interop.Utf8String.C
        + Interop.Utf16String.C
        + Interop.Request.C
        + Interop.Response.C
        + //language=c++
        """
        typedef enum MessageBoxButtons {
          OK,
          OKCancel,
          YesNo,
          YesNoCancel,
        } MessageBoxButtons_t;
        typedef enum MessageBoxType
        {
          Information,
          Warning,
          Error,
          Question,
        } MessageBoxType_t;
        typedef enum DialogResult
        {
          None,
          Ok,
          Cancel,
          Yes,
          No,
          Abort,
          Ignore,
          Retry,
        } DialogResult_t;

        """
    )]
    [UnmanagedCallersOnly]
    public static void Initialize() {
        Dw2Env.Initialize();
    }

    [UnmanagedCallersOnly]
    public static void DebugBreak() {
        DebugBreakImpl();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
    private static void DebugBreakImpl() {
        Debugger.Launch();
        while (!Debugger.IsAttached)
            Debugger.Break();
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetVersion(char* pBuffer, int bufferSize)
        => ExtractUnmanagedString( // ide assembly version
            typeof(Dw2Env).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            pBuffer, bufferSize);

    [UnmanagedCallersOnly]
    public static unsafe int GetNetVersion(char* pBuffer, int bufferSize)
        => ExtractUnmanagedString( // net runtime version
            typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            pBuffer, bufferSize);

    /// <summary>
    /// Gets the game directory.
    /// </summary>
    /// <param name="pBuffer">Pointer to a buffer to write the game directory to.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    /// <returns>
    /// Zero when not set.
    /// On insufficient buffer size, the negative of the number of bytes required.
    /// The number of bytes written to the buffer.
    /// </returns>
    [UnmanagedCallersOnly]
    public static unsafe int GetGameDirectory(char* pBuffer, int bufferSize)
        => ExtractUnmanagedString(GameDirectory, pBuffer, bufferSize);

    /// <summary>
    /// Gets the user-chosen game directory.
    /// </summary>
    /// <param name="pBuffer">Pointer to a buffer to write the game directory to.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    /// <returns>
    /// Zero when not set.
    /// On insufficient buffer size, the negative of the number of bytes required.
    /// The number of bytes written to the buffer.
    /// </returns>
    [UnmanagedCallersOnly]
    public static unsafe int GetUserChosenGameDirectory(char* pBuffer, int bufferSize)
        => OperatingSystem.IsWindows()
            ? ExtractUnmanagedString(UserChosenGameDirectory, pBuffer, bufferSize)
            : 0;

    [UnmanagedCallersOnly]
    public static void Deisolate()
        => Deisolator.Initialize();

    [UnmanagedCallersOnly]
    public static void ReleaseHandle(nint handle) {
        var h = new GCHandle<object>(handle);
        /*var target = h.Target;
        if (target is IDisposable disposable)
            disposable.Dispose();*/
        // assume finializers will call Dispose
        h.Free();
    }

    [ThreadStatic]
    private static (WeakReference Ref, string? Str) _handleToStringCache;

    [UnmanagedCallersOnly]
    public static unsafe int HandleToString(nint handle, char* buffer, int bufferLength) {
        // this just converts it to a 32-character hex string
        if (handle == 0) return 0;

        var obj = new GCHandle<object>(handle).Target;
        if (obj is null) return 0;
        if (_handleToStringCache.Ref.IsAlive && _handleToStringCache.Ref.Target == obj)
            return ExtractUnmanagedString(_handleToStringCache.Str, buffer, bufferLength);

        var str = obj.ToString();
        var result = ExtractUnmanagedString(str, buffer, bufferLength);
        if (result >= 0)
            return result;

        _handleToStringCache = (new(obj), str);
        return result;
    }

    [UnmanagedCallersOnly]
    public static unsafe nint LoadBundle(char* path, int pathLength) {
        var chars = new ReadOnlySpan<char>(path, pathLength);
        var bundlePath = chars.IsEmpty ? null : new string(chars);
        //Console.WriteLine($"path: 0x{(nuint)path:X8} {bundlePath}");
        if (bundlePath is null) return default;

        try {
            var bundleName = BundleManager.LoadBundle(bundlePath);
            var loadedBundle = BundleManager.GetLoadedBundles()
                .FirstOrDefault(b => b.BundleName == bundleName);
            return loadedBundle is null
                ? default
                : new GCHandle<BundleManager.Bundle>(loadedBundle).Value;
        }
        catch {
            return default;
        }
    }

    [UnmanagedCallersOnly]
    public static unsafe nint QueryBundleObjects(nint bundleHandle, char* glob, int globLength) {
        var bundle = new GCHandle<BundleManager.Bundle>(bundleHandle).Target;
        if (bundle is null) return default;

        var globSpan = new ReadOnlySpan<char>(glob, globLength);
        var globStr = globSpan.IsEmpty ? null : new string(globSpan);
        //Console.WriteLine($"glob: 0x{(nuint)glob:X8} {globStr}");
        var description = bundle.Description;
        var results = description.Assets.Select(a => a.Key);
        if (globStr != null && globStr != "**")
            results = new Minimatcher(globStr).Filter(results);

        var enumerator = results.GetEnumerator();
        return enumerator.MoveNext()
            ? new GCHandle<IEnumerator<string>>(enumerator).Value
            : default;
    }

    [UnmanagedCallersOnly]
    public static unsafe nint ReadQueriedBundleObject(nint bundleQueryHandle, char* buffer, int bufferLength) {
        try {
            var gcHandle = new GCHandle<IEnumerator<string>>(bundleQueryHandle);
            try {
                var enumerator = gcHandle.Target;
                if (enumerator is null) return 0;

                var str = enumerator.Current;

                var result = ExtractUnmanagedString(str, buffer, bufferLength);
                if (result <= 0)
                    return result;

                if (enumerator.MoveNext())
                    return result;

                // enumeration completed
                enumerator.Dispose();
                gcHandle.Free();

                return result;
            }
            catch {
                gcHandle.Free();
            }
        }
        catch {
            // discard
        }

        return 0;
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectId(char* path, int pathLength, byte* pObjectId) {
        if (pObjectId is null) return False;

        var chars = new ReadOnlySpan<char>(path, pathLength);
        var contentPath = chars.IsEmpty ? null : new string(chars);
        var id = BundleManager.GetObjectId(contentPath);
        if (id is null) return False;

        *(ObjectId*)pObjectId = id.Value;
        return True;
    }

    private static readonly Dictionary<string, nint> _bundlePathStringMap = new();

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectOffset(byte* pObjectId, long* pOffset, long* pOffsetEnd,
        char** pSourceDirOrBundlePath) {
        if (pObjectId is null) return False;

        var id = *(ObjectId*)pObjectId;

        var offset = BundleManager.GetObjectOffset(id, out var offsetEnd, out var sourceDirOrBundlePath);

        if (pOffset is not null)
            *pOffset = offset;

        if (pOffsetEnd is not null)
            *pOffsetEnd = offsetEnd;

        if (pSourceDirOrBundlePath is null)
            return True;

        if (sourceDirOrBundlePath is null) {
            *pSourceDirOrBundlePath = null;
            return True;
        }

        var path = string.Intern(sourceDirOrBundlePath);
        // callback is from single threaded js, but just in case...
        lock (_bundlePathStringMap) {
            if (_bundlePathStringMap.TryGetValue(path, out var pointer)) {
                *pSourceDirOrBundlePath = (char*)pointer;
                return True;
            }

            var chars = (char*)NativeMemory.Alloc((nuint)path.Length + 1, sizeof(char));
            path.AsSpan().CopyTo(new(chars, path.Length));
            chars[path.Length] = '\0';
            _bundlePathStringMap.Add(path, (nint)chars);
            *pSourceDirOrBundlePath = chars;
            return True;
        }
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectSize(byte* pObjectId, long* pSize) {
        if (pObjectId is null) return False;

        var id = *(ObjectId*)pObjectId;

        var size = BundleManager.GetObjectSize(id);

        if (pSize is not null)
            *pSize = size;

        return True;
    }

    [ThreadStatic]
    private static (ObjectId Id, string? Value) _objectTypeCache;

    [UnmanagedCallersOnly]
    public static unsafe int GetObjectType(byte* pObjectId,
        char* pObjectType, int objectTypeLength) {
        if (pObjectId == null) return 0;

        var id = *(ObjectId*)pObjectId;

        if (_objectTypeCache.Id == id)
            return ExtractUnmanagedString(_objectTypeCache.Value, pObjectType, objectTypeLength);

        var type = BundleManager.GetObjectQualifiedType(id);
        _objectTypeCache = (id, type);
        return ExtractUnmanagedString(type, pObjectType, objectTypeLength);
    }

    [ThreadStatic]
    private static (ObjectId Id, string? Value) _objectSimplifiedTypeCache;

    // public static string? GetObjectSimplifiedType(ObjectId? id)
    [UnmanagedCallersOnly]
    public static unsafe int GetObjectSimplifiedType(byte* pObjectId,
        char* pSimpleType, int simpleTypeLength) {
        if (pObjectId == null) return 0;

        var id = *(ObjectId*)pObjectId;

        if (_objectSimplifiedTypeCache.Id == id)
            return ExtractUnmanagedString(_objectSimplifiedTypeCache.Value, pSimpleType, simpleTypeLength);

        var simpleType = BundleManager.GetObjectSimplifiedType(id);
        _objectSimplifiedTypeCache = (id, simpleType);
        return ExtractUnmanagedString(simpleType, pSimpleType, simpleTypeLength);
    }

    [UnmanagedCallersOnly]
    public static unsafe nint InstantiateBundleItem(char* url, int urlLength) {
        var chars = new ReadOnlySpan<char>(url, urlLength);
        var urlStr = chars.IsEmpty ? null : new string(chars);
        if (urlStr is null) return 0;

        try {
            var obj = BundleManager.InstantiateObject(urlStr);
            if (obj is null) return 0;

            var h = new GCHandle<object>(obj);
            return h.Value;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to instantiate bundle item: {urlStr}");
            if (Debugger.IsAttached)
                Debug.WriteLine(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static CBool IsImage(nint handle) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        return obj is Image ? True : False;
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryConvertImageToBufferWebp(nint handle, int mipLevel, byte* pBuffer, int bufferSize) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return False;

        try {
            var ums = new UnmanagedMemoryStream(pBuffer, bufferSize);
            BundleManager.ConvertToImages(img, false, (newImg, _) => {
                ums.Position = 0;
                newImg.SaveAsWebp(ums);
                ums.Position = 0;
            }, mipLevel);
        }
        catch {
            return False;
        }

        return True;
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryConvertImageToStreamWebp(nint handle, int mipLevel,
        [DNNE.C99Type("StreamWrite_t")] void* pWriteFn, void* state) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return False;

        try {
            var writeFn = (delegate* unmanaged[Cdecl]<void*, void*, nuint, void>)pWriteFn;
            var ds = new UnmanagedDelegatedWriteOnlyStream(writeFn, state);
            using var ms = GetRecyclableMemoryStream();
            // can't just use ds here, the encoding tries to seek
            BundleManager.ConvertToImages(img, false, (newImg, _) => {
                newImg.SaveAsWebp(ms);
            }, mipLevel);
            ms.Position = 0;
            ms.WriteTo(ds);
        }
        catch {
            return False;
        }

        return True;
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetImageMipLevels(nint handle) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return 0;

        return img.Description.MipLevels;
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetImageWidth(nint handle, int mipLevel) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return -1;

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Width;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetImageHeight(nint handle, int mipLevel) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return -1;

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Height;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetImageDepth(nint handle, int mipLevel) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return -1;

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Depth;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly]
    public static unsafe int GetImageDimensions(nint handle) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return -1;

        return img.Description.Dimension switch {
            TextureDimension.Texture1D => 1,
            TextureDimension.Texture2D => 2,
            TextureDimension.Texture3D => 3,
            TextureDimension.TextureCube => 3,
            _ => -1
        };
    }

    [ThreadStatic]
    private static (Image? Image, string? Value) _imageFormatCache;

    [UnmanagedCallersOnly]
    public static unsafe int GetImageFormat(nint handle, char* pBuffer, int bufferSize) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return 0;
        if (_imageFormatCache.Image == img)
            return ExtractUnmanagedString(_imageFormatCache.Value, pBuffer, bufferSize);

        var formatStr = img.Description.Format.ToString();
        _imageFormatCache = (img, formatStr);
        return ExtractUnmanagedString(formatStr, pBuffer, bufferSize);
    }

    [ThreadStatic]
    private static (Image? Image, string? Value) _imageTextureTypeCache;

    [UnmanagedCallersOnly]
    public static unsafe int GetImageTextureType(nint handle, char* pBuffer, int bufferSize) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return 0;
        if (_imageTextureTypeCache.Image == img)
            return ExtractUnmanagedString(_imageTextureTypeCache.Value, pBuffer, bufferSize);

        var formatStr = img.Description.Dimension.ToString();
        _imageTextureTypeCache = (img, formatStr);
        return ExtractUnmanagedString(formatStr, pBuffer, bufferSize);
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("DialogResult_t")]
    public static unsafe int ShowMessageBox(
        char* message, int messageLength,
        char* title, int titleLength,
        [DNNE.C99Type("MessageBoxButtons_t")] int messageBoxButtons,
        [DNNE.C99Type("MessageBoxType_t")] int messageBoxType) {
        var messageSpan = new ReadOnlySpan<char>(message, messageLength);
        var messageStr = messageSpan.IsEmpty ? null : new string(messageSpan);
        var titleSpan = new ReadOnlySpan<char>(title, titleLength);
        var titleStr = titleSpan.IsEmpty ? null : new string(titleSpan);
        var buttons = (MessageBoxButtons)messageBoxButtons;
        var type = (MessageBoxType)messageBoxType;
        if (Eto.Forms.Application.Instance is null)
            Dw2Env.Initialize();

        return (int)MessageBox.Show(messageStr, titleStr, buttons, type);
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportObject(byte* pObjectId, char* pPath, int pathLength) {
        if (pObjectId is null) return False;

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        var objectId = *(ObjectId*)pObjectId;

        return BundleManager.TryExportObject(objectId, path) ? True : False;
    }

    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportImageAsWebp(nint handle, char* pPath, int pathLength) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return False;

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        try {
            return BundleManager.TryExportImageAsWebp(img, path)
                ? True
                : False;
        }
        catch {
            return False;
        }
    }
    
    [UnmanagedCallersOnly]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportImageAsDds(nint handle, char* pPath, int pathLength) {
        var h = new GCHandle<object>(handle);
        var obj = h.Target;
        if (obj is not Image img) return False;

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        try {
            return BundleManager.TryExportImageAsDds(img, path)
                ? True
                : False;
        }
        catch {
            return False;
        }
    }
    

}