using System.IO.Compression;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;

namespace DistantWorlds.IDE.Stride;

public class BundleBuilder {

    public readonly string Name;

    private readonly Dictionary<string, ObjectId> _indexMap = new();

    private readonly Dictionary<ObjectId, string> _reverseIndexMap = new();

    private readonly HashSet<ObjectId> _disableCompressionIds = new();

    public BundleBuilder(string bundleName)
        => Name = bundleName;

    public bool TryAdd(string path, string url, bool compress = true) {
        if (_indexMap.ContainsKey(path))
            return false;

        var newObjId = ObjectId.New();
        while (!_reverseIndexMap.TryAdd(newObjId, path))
            newObjId = ObjectId.New();
        _indexMap[path] = newObjId;
        if (!compress)
            _disableCompressionIds.Add(newObjId);

        return true;
    }

    public void Build(string bundlePath) {
        // Create an incremental package
        var newIncrementalId = ObjectId.New();
        var dir = Path.GetDirectoryName(bundlePath);
        VirtualFileSystem.RemountFileSystem("/data_output", dir);
        var outputDatabase = new ObjectDatabase("/data_output", Name, loadDefaultBundle: false);
        var backend = outputDatabase.BundleBackend;

        var incBundlePath = Path.ChangeExtension(bundlePath, $".{newIncrementalId}.bundle");

        var objects = new Dictionary<ObjectId, BundleOdbBackend.ObjectInfo>();
        foreach (var (objectId, _) in _reverseIndexMap)
            objects.Add(objectId, default);

        using (var packStream = File.Create(bundlePath, 2 * 1024 * 1024)) {
            var header = new BundleOdbBackend.Header();
            header.MagicHeader = BundleOdbBackend.Header.MagicHeaderValid;

            var packBinaryWriter = new BinarySerializationWriter(packStream);
            packBinaryWriter.Write(header);
            // Write dependencies
            packBinaryWriter.Write(new List<string>());
            // Write incremental bundles
            packBinaryWriter.Write(new List<ObjectId>());

            // Save location of object ids
            var packObjectIdPosition = packStream.Position;

            // Write empty object ids (reserve space, will be rewritten later)
            var objectList = objects.ToList();
            packBinaryWriter.Write(objectList);

            // Write index
            packBinaryWriter.Write(_indexMap.ToList());

            var objectOutputStream = packStream;
            int incrementalObjectIndex = 0;
            using var gd = GraphicsDevice.New();
            foreach (var (objectId, _) in objectList) {
                if (backend.Exists(objectId)) {
                    using var objectStream = backend.OpenStream(objectId);
                    // Prepare object info
                    var objectInfo = new BundleOdbBackend.ObjectInfo {
                        StartOffset = objectOutputStream.Position,
                        SizeNotCompressed = objectStream.Length
                    };

                    // re-order the file content so that it is not necessary to seek while reading the input stream (header/object/refs -> header/refs/object)
                    var inputStream = objectStream;
                    var originalStreamLength = objectStream.Length;
                    var streamReader = new BinarySerializationReader(inputStream);
                    var chunkHeader = ChunkHeader.Read(streamReader);
                    if (chunkHeader != null) {
                        // create the reordered stream
                        var reorderedStream = new MemoryStream((int)originalStreamLength);

                        // copy the header
                        var streamWriter = new BinarySerializationWriter(reorderedStream);
                        chunkHeader.Write(streamWriter);

                        // copy the references
                        var newOffsetReferences = reorderedStream.Position;
                        inputStream.Position = chunkHeader.OffsetToReferences;
                        inputStream.CopyTo(reorderedStream);

                        // copy the object
                        var newOffsetObject = reorderedStream.Position;
                        inputStream.Position = chunkHeader.OffsetToObject;
                        inputStream.CopyTo(reorderedStream,
                            chunkHeader.OffsetToReferences - chunkHeader.OffsetToObject);

                        // rewrite the chunk header with correct offsets
                        chunkHeader.OffsetToObject = (int)newOffsetObject;
                        chunkHeader.OffsetToReferences = (int)newOffsetReferences;
                        reorderedStream.Position = 0;
                        chunkHeader.Write(streamWriter);

                        // change the input stream to use reordered stream
                        inputStream = reorderedStream;
                        inputStream.Position = 0;
                    }

                    // compress the stream
                    if (!_disableCompressionIds.Contains(objectId)) {
                        objectInfo.IsCompressed = true;

                        var lz4OutputStream =
                            new global::Stride.Core.LZ4.LZ4Stream(objectOutputStream, CompressionMode.Compress);
                        inputStream.CopyTo(lz4OutputStream);
                        lz4OutputStream.Flush();
                    }
                    // copy the stream "as is"
                    else {
                        // Write stream
                        inputStream.CopyTo(objectOutputStream);
                    }

                    // release the reordered created stream
                    if (chunkHeader != null)
                        inputStream.Dispose();

                    // Add updated object info
                    objectInfo.EndOffset = objectOutputStream.Position;
                    // Note: we add 1 because 0 is reserved for self; first incremental bundle starts at 1
                    objectInfo.IncrementalBundleIndex = 0;
                    objects[objectId] = objectInfo;
                }
                else {
                    var filePath = _reverseIndexMap[objectId];
                    using var fileStream = File.OpenRead(filePath);
                    // Prepare object info
                    var objectInfo = new BundleOdbBackend.ObjectInfo {
                        StartOffset = objectOutputStream.Position,
                    };
                    // serialize the file (diagnose by file extension)
                    var fileExt = Path.GetExtension(filePath);
                    switch (fileExt) {
                        case ".png":
                        case ".jpg":
                        case ".dds":
                        case ".webp":
                        {
                            // texture
                            var image = global::Stride.Graphics.Image.Load(fileStream);
                            var texture = Texture.New(gd, image); // is this step necessary?
                            var start = packBinaryWriter.UnderlyingStream.Position;
                            packBinaryWriter.Write(texture);
                            var end = packBinaryWriter.UnderlyingStream.Position;
                            objectInfo.SizeNotCompressed = end - start;
                            break;
                        }
                    }
                }
            }

            // Rewrite headers
            header.Size = packStream.Length;
            packStream.Position = 0;
            packBinaryWriter.Write(header);

            // Rewrite object with updated offsets/size
            packStream.Position = packObjectIdPosition;
            packBinaryWriter.Write(objects);
        }
    }

}