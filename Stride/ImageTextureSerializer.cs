// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DistantWorlds.IDE;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;
using Stride.Graphics;

namespace DistantWorlds.IDE.Stride;

internal class ImageTextureSerializer : ContentSerializerBase<Image> {

  private static readonly FakeType FakeTypeOfGraphicsResourceBase = new FakeType("Stride.Graphics", "GraphicsResourceBase", typeof(ComponentBase));

  private static readonly FakeType FakeTypeOfGraphicsResource = new FakeType("Stride.Graphics", "GraphicsResource", FakeTypeOfGraphicsResourceBase);

  internal static readonly FakeType FakeTypeOfTexture = new FakeType("Stride.Graphics", "Texture", FakeTypeOfGraphicsResource);

  private static readonly FieldInfo ImageHelperImageDescriptionSerializerField
    = typeof(ImageHelper).GetField("ImageDescriptionSerializer", BindingFlags.Public | BindingFlags.Static)!;

  private static DataSerializer<ImageDescription> GetImageDescriptionSerializer()
    => (DataSerializer<ImageDescription>)ImageHelperImageDescriptionSerializerField.GetValue(null)!;

  private static readonly unsafe delegate*<Image, Image, void> InitializeImageFromImage
    = (delegate*<Image, Image, void>)
    typeof(Image).GetMethod("InitializeFrom", BindingFlags.NonPublic | BindingFlags.Instance)!
      .MethodHandle.GetFunctionPointer();

  // internal static void ComputePitch(PixelFormat fmt, int width, int height, out int rowPitch, out int slicePitch, out int widthCount, out int heightCount, PitchFlags flags = PitchFlags.None)
  private static readonly unsafe delegate*<PixelFormat, int, int, out int, out int, out int, out int, void> ImageComputePitch
    = (delegate*<PixelFormat, int, int, out int, out int, out int, out int, void>)
    typeof(Image).GetMethod("ComputePitch", BindingFlags.NonPublic | BindingFlags.Static)!
      .MethodHandle.GetFunctionPointer();

  private static readonly Type TypeOfPitchFlags = typeof(Image).GetNestedType("PitchFlags", BindingFlags.NonPublic)!;

  // internal unsafe Image(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable, PitchFlags pitchFlags = PitchFlags.None, int rowStride = 0)
  private static readonly ConstructorInfo _ImageCtor
    = typeof(Image).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {
      typeof(ImageDescription),
      typeof(IntPtr),
      typeof(int),
      typeof(GCHandle?),
      typeof(bool),
      TypeOfPitchFlags,
      typeof(int)
    }, null)!;

  /*
  private static readonly object NonePitchFlags = Enum.ToObject(TypeOfPitchFlags, 0);

  private static Image ConstructImage(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable)
    => (Image)_ImageCtor.Invoke(new[] { description, dataPointer, offset, handle!, bufferIsDisposable, NonePitchFlags, 0 });
    */

  private static readonly unsafe delegate *<Image, ImageDescription, IntPtr, int, GCHandle?, bool, int, int, void> _ImageCtorPtr
    = (delegate *<Image, ImageDescription, IntPtr, int, GCHandle?, bool, int, int, void>)_ImageCtor.MethodHandle.GetFunctionPointer();

  private static readonly Func<Image> UninitializedImageFactory
    = Internals.GetUninitializedObjectFactory<Image>();

  private static unsafe Image CreateImage(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable) {
    var obj = UninitializedImageFactory();
    _ImageCtorPtr(obj, description, dataPointer, offset, handle!, bufferIsDisposable, 0, 0);
    return obj;
  }

  /// <inheritdoc/>
  public override Type SerializationType => FakeTypeOfTexture; // typeof(Texture);

  public override unsafe void Serialize(ContentSerializerContext context, SerializationStream stream, Image textureData) {
    if (context.Mode == ArchiveMode.Deserialize) {
      var isStreamable = stream.ReadBoolean();
      if (!isStreamable) {
        var image = Image.Load(stream.UnderlyingStream);
        InitializeImageFromImage(textureData, image);
      }
      else {
        // Read image header
        var imageDescription = new ImageDescription();
        GetImageDescriptionSerializer().Serialize(ref imageDescription, ArchiveMode.Deserialize, stream);

        // Read content storage header
        ContentStorageHeader storageHeader;
        ContentStorageHeader.Read(stream, out storageHeader);

        // Deserialize whole texture to image without streaming feature
        var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
        DeserializeImage(contentSerializerContext.ContentManager, textureData, ref imageDescription, ref storageHeader);
      }
    }
    else {
      textureData.Save(stream.UnderlyingStream, ImageFileType.Stride);
    }
  }

  public override object Construct(ContentSerializerContext context)
    => UninitializedImageFactory(); //new Image();

  private static unsafe void DeserializeImage(ContentManager contentManager, Image obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader) {
    using var content = new ContentStreamingService();
    // Get content storage container
    var storage = content.GetStorage(ref storageHeader);
    if (storage == null)
      throw new ContentStreamingException("Missing content storage.");

    storage.LockChunks();

    // Cache data
    var fileProvider = contentManager.FileProvider;
    var format = imageDescription.Format;
    var isBlockCompressed =
      format is >= PixelFormat.BC1_Typeless and <= PixelFormat.BC5_SNorm
        or >= PixelFormat.BC6H_Typeless and <= PixelFormat.BC7_UNorm_SRgb;

    // Calculate total size
    var size = 0;
    for (var mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++) {
      var mipWidth = Math.Max(1, imageDescription.Width >> mipIndex);
      var mipHeight = Math.Max(1, imageDescription.Height >> mipIndex);
      if (isBlockCompressed && (mipWidth % 4 != 0 || mipHeight % 4 != 0)) {
        mipWidth = unchecked((int)((uint)(mipWidth + 3) & ~3U));
        mipHeight = unchecked((int)((uint)(mipHeight + 3) & ~3U));
      }

      ImageComputePitch(format, mipWidth, mipHeight,
        out var rowPitch,
        out var slicePitch,
        out var widthPacked,
        out var heightPacked);

      size += slicePitch;
    }

    size *= imageDescription.ArraySize;

    // Preload chunks
    for (var mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++)
      storage.GetChunk(mipIndex)?.GetData(fileProvider);

    // Allocate buffer for image data
    var buffer = Utilities.AllocateMemory(size);

    try {
      // Load image data to the buffer
      var bufferPtr = buffer;
      for (var arrayIndex = 0; arrayIndex < imageDescription.ArraySize; arrayIndex++) {
        for (var mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++) {
          var mipWidth = Math.Max(1, imageDescription.Width >> mipIndex);
          var mipHeight = Math.Max(1, imageDescription.Height >> mipIndex);
          if (isBlockCompressed && (mipWidth % 4 != 0 || mipHeight % 4 != 0)) {
            mipWidth = unchecked((int)((uint)(mipWidth + 3) & ~3U));
            mipHeight = unchecked((int)((uint)(mipHeight + 3) & ~3U));
          }

          ImageComputePitch(format, mipWidth, mipHeight,
            out var rowPitch,
            out var slicePitch,
            out var widthPacked,
            out var heightPacked);
          var chunk = storage.GetChunk(mipIndex);
          if (chunk == null || chunk.Size != slicePitch * imageDescription.ArraySize)
            throw new ContentStreamingException("Data chunk is missing or has invalid size.", storage);

          var data = chunk.GetData(fileProvider);
          if (!chunk.IsLoaded)
            throw new ContentStreamingException("Data chunk is not loaded.", storage);

          Unsafe.CopyBlockUnaligned((void*)bufferPtr, (void*)data, (uint)chunk.Size);
          bufferPtr += chunk.Size;
        }
      }

      // Initialize image
      //var image = new Image(imageDescription, buffer, 0, null, true);
      var image = CreateImage(imageDescription, buffer, 0, null!, true);
      InitializeImageFromImage(obj, image);
      //obj.InitializeFrom(image);
    }
    catch {
      // Free memory in case of error
      Utilities.FreeMemory(buffer);

      throw;
    }

    storage.UnlockChunks();
  }

}