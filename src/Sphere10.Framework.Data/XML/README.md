# XML

XML serialization through `Tools.Xml`, a deep object-graph serializer, an XML-file-backed dictionary and supporting XML value types.

`Tools.Xml` wraps `XmlSerializer` with a per-type serializer cache and reads and writes objects to strings, files and text readers or writers. Its `DeepWriteToString`, `DeepWriteToFile` and `DeepReadFromFile` methods use `XmlDeepSerializer` and `XmlDeepDeserializer`, which serialize arbitrary object graphs, including collections and dictionaries, that `XmlSerializer` cannot handle. The deep serializer is adapted from a 2006 public code sample by Marcus Deecke and keeps its original header. `XmlSerializationTag` supplies its element names, `TypeInfo` records type creation information, and `BinaryContainer` carries binary data.

`XmlFileDictionary<T1, T2>` is an `IPersistedDictionary` that loads from and saves to an XML file, using either plain or deep serialization. It is the default registry of `SimpleFileStore` in [File Store](../FileStore/README.md). When its file cannot be read, `Load` deletes the file and recreates an empty one. `XmlSchedulerSerializer` saves and restores a scheduler surrogate to an XML file. JSON equivalents of `Tools.Xml` are in [JSON](../JSON/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BinaryContainer`](BinaryContainer.cs) | class | Serializable wrapper for binary data that the deep XML serializer writes and restores. |
| [`BinaryContainerTypeConverter`](BinaryContainer.cs) | class | `TypeConverter` between `BinaryContainer` and `byte[]` or `Stream`. |
| [`CDataString`](CDataString.cs) | class | String wrapper that `XmlSerializer` writes as a CDATA section, with implicit conversions to and from `string`. |
| [`IXmlSerializationTag`](IXmlSerializationTag.cs) | interface | Element and attribute names used by the deep XML serializer and deserializer. |
| [`Tools.Xml`](XmlTool.cs) | tools | Cached `XmlSerializer` read and write helpers plus deep-serialization entry points and an XML file check. |
| [`TypeInfo`](XmlSerializationHelpers.cs) | class | Type and assembly names for deep deserialization, with static checks for collections, dictionaries, lists, arrays and binary constructors. |
| [`XmlDeepDeserializer`](XmlDeepDeserializer.cs) | class | Rebuilds object graphs written by `XmlDeepSerializer` from files, readers, documents or nodes. |
| [`XmlDeepSerializer`](XmlDeepSerializer.cs) | class | Serializes arbitrary object graphs, collections and dictionaries to XML with an optional type dictionary. |
| [`XmlFileDictionary<T1, T2>`](XmlFileDictionary.cs) | class | Dictionary decorator persisted to an XML file through `Load`, `Save` and `Delete`. |
| [`XmlSchedulerSerializer`](XmlSchedulerSerializer.cs) | class | `ISchedulerSerializer` that stores a `SchedulerSerializableSurrogate` in an XML file. |
| [`XmlSerializationTag`](XmlSerializationTag.cs) | class | Default `IXmlSerializationTag` element names for the deep serializer. |
