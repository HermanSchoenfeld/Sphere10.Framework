# 🛠️ Tools.* Namespace Reference

**Status**: Complete reference for Sphere10 Framework v3.0.0  
**Last Updated**: December 31, 2025

The **Tools namespace** is a central organizational feature of Sphere10 Framework, providing a unified, IntelliSense-discoverable collection of static utility methods across all framework projects. This document catalogs all Tools classes and their primary operations.

## Table of Contents

1. [Core Framework Tools](#core-framework-tools)
2. [Platform-Specific Tools](#platform-specific-tools)
3. [Database Tools](#database-tools)
4. [Usage Patterns](#usage-patterns)
5. [Adding New Tools](#adding-new-tools)

---

## Core Framework Tools

Located in `Sphere10.Framework` and organized by domain:

### Collection & Data Structure Tools

#### **Tools.Array**
Array manipulation, bounds checking, copying, resizing.
```csharp
var subarray = Tools.Array.SubArray(data, startIndex, length);
var resized = Tools.Array.Resize(array, newSize);
bool contains = Tools.Array.Contains(array, value);
```

#### **Tools.Collection**
Collection operations: filtering, mapping, iteration, flattening.
```csharp
var filtered = Tools.Collection.Where(items, predicate);
var transformed = Tools.Collection.Select(items, mapper);
var flattened = Tools.Collection.Flatten(nestedList);
var grouped = Tools.Collection.GroupBy(items, keySelector);
```

#### **Tools.Stream**
Stream decorators, readers, writers, bounded access.
```csharp
var bounded = Tools.Stream.CreateBoundedStream(source, maxBytes);
byte[] data = Tools.Stream.ReadAllBytes(stream);
Tools.Stream.WriteAllBytes(stream, data);
var decorated = Tools.Stream.WrapWithCompression(stream);
```

### Cryptography Tools

#### **Tools.Crypto**
Hashing, signatures, key derivation, cryptographic operations.
```csharp
byte[] hash = Tools.Crypto.SHA256(data);
byte[] hash256 = Tools.Crypto.BLAKE2B(data);
byte[] signature = Tools.Crypto.Sign(privateKey, message);
bool valid = Tools.Crypto.VerifySignature(publicKey, message, signature);
byte[] derived = Tools.Crypto.DeriveKey(password, salt, iterations);
```

### String & Text Tools

#### **Tools.Text**
String manipulation, formatting, validation, generation.
```csharp
string trimmed = Tools.Text.RemoveWhitespace(text);
string truncated = Tools.Text.Truncate(text, 100);
string pascal = Tools.Text.ToPascalCase(text);
string random = Tools.Text.GenerateRandomString(length);
string padded = Tools.Text.PadLeft(text, totalWidth, paddingChar);
bool isEmail = Tools.Text.IsValidEmail(email);
```

#### **Tools.Parse**
String parsing, numeric conversion, safe type conversion.
```csharp
int parsed = Tools.Parse.ToInt32(text, defaultValue);
decimal decimal = Tools.Parse.ToDecimal(text);
Guid guid = Tools.Parse.ToGuid(guidString);
var typed = Tools.Parse.TryParse<T>(text, out var result);
```

### Type & Reflection Tools

#### **Tools.Reflection**
Type inspection, member discovery, attribute analysis.
```csharp
var properties = Tools.Reflection.GetProperties(type);
var methods = Tools.Reflection.GetMethods(type);
bool hasAttribute = Tools.Reflection.HasAttribute<MyAttribute>(type);
var attribute = Tools.Reflection.GetAttribute<MyAttribute>(type);
object instance = Tools.Reflection.CreateInstance(type, args);
```

#### **Tools.Enum**
Enumeration utilities, descriptions, conversion.
```csharp
string description = Tools.Enum.GetDescription(enumValue);
int value = Tools.Enum.ToInt32(enumValue);
MyEnum parsed = Tools.Enum.Parse<MyEnum>(text);
var values = Tools.Enum.GetValues<MyEnum>();
```

#### **Tools.Value**
Value type utilities, conversions, comparisons.
```csharp
bool equals = Tools.Value.Equals(obj1, obj2);
int hashCode = Tools.Value.GetHashCode(obj);
object clone = Tools.Value.Clone(obj);
```

### Numeric & Math Tools

#### **Tools.Math**
Basic mathematical operations, RNG, calculations.
```csharp
double sqrt = Tools.Math.Sqrt(value);
double pow = Tools.Math.Power(base, exponent);
int random = Tools.Math.Random.Next(min, max);
byte[] randomBytes = Tools.Math.Random.NextBytes(count);
```

#### **Tools.MathPlus**
Advanced mathematics, specialized algorithms.
```csharp
var gcd = Tools.MathPlus.GreatestCommonDivisor(a, b);
var lcm = Tools.MathPlus.LeastCommonMultiple(a, b);
bool isPrime = Tools.MathPlus.IsPrime(number);
```

### Memory & Buffer Tools

#### **Tools.Memory**
Buffer operations, memory allocation, size formatting.
```csharp
string readable = Tools.Memory.GetBytesReadable(bytes);  // "1.5 MB"
byte[] allocated = Tools.Memory.AllocateBuffer(size);
Tools.Memory.ClearBuffer(buffer);
byte[] copy = Tools.Memory.CopyBuffer(source);
```

### Temporal Tools

#### **Tools.DateTime**
Date/time parsing, formatting, calculations.
```csharp
DateTime parsed = Tools.DateTime.Parse(dateString);
string formatted = Tools.DateTime.FormatISO8601(dateTime);
TimeSpan elapsed = Tools.DateTime.GetElapsedTime(startTime);
bool isLeapYear = Tools.DateTime.IsLeapYear(year);
```

### I/O & File System Tools

#### **Tools.FileSystem**
File operations, directory management, temp files.
```csharp
string tempFile = Tools.FileSystem.GenerateTempFilename();
string tempDir = Tools.FileSystem.GetTempDirectory();
Tools.FileSystem.WriteAllText(path, content);
string content = Tools.FileSystem.ReadAllText(path);
Tools.FileSystem.DeleteFile(path);
var files = Tools.FileSystem.GetFiles(directory, pattern);
```

### Network Tools

#### **Tools.Network**
Network utilities, IP operations, connectivity.
```csharp
bool online = Tools.Network.IsInternetAvailable();
string publicIP = Tools.Network.GetPublicIPAddress();
bool reachable = Tools.Network.IsPingable(host);
var addresses = Tools.Network.GetLocalIPAddresses();
```

#### **Tools.Url**
URL parsing, encoding, manipulation.
```csharp
string encoded = Tools.Url.EncodeUrl(url);
string decoded = Tools.Url.DecodeUrl(encoded);
string query = Tools.Url.BuildQueryString(parameters);
var uri = Tools.Url.ParseUrl(urlString);
```

#### **Tools.Mail**
Email utilities, composition, validation.
```csharp
bool isValid = Tools.Mail.IsValidEmail(email);
Tools.Mail.SendEmail(to, subject, body);
var message = Tools.Mail.CreateMessage(from, to, subject, body);
```

### Functional & Expression Tools

#### **Tools.Lambda**
Lambda expression utilities and manipulation.
```csharp
var property = Tools.Lambda.GetPropertyName(() => obj.Property);
var method = Tools.Lambda.GetMethodName(() => obj.Method());
```

#### **Tools.Expression**
LINQ expression building and manipulation.
```csharp
var predicate = Tools.Expression.BuildPredicate<T>(field, value);
var combined = Tools.Expression.CombinePredicates(expr1, expr2);
```

#### **Tools.Operator**
Generic operator invocation without reflection overhead.
```csharp
var sum = Tools.Operator.Add(a, b);
var product = Tools.Operator.Multiply(x, y);
bool less = Tools.Operator.LessThan(a, b);
```

### Data Format Tools

#### **Tools.Json**
JSON serialization, parsing, transformation.
```csharp
string json = Tools.Json.Serialize(obj);
var deserialized = Tools.Json.Deserialize<MyType>(json);
var pretty = Tools.Json.PrettyPrint(json);
```

#### **Tools.Xml**
XML parsing, serialization, transformation.
```csharp
string xml = Tools.Xml.Serialize(obj);
var deserialized = Tools.Xml.Deserialize<MyType>(xml);
var document = Tools.Xml.ParseXml(xmlString);
```

### Threading & Concurrency Tools

#### **Tools.Thread**
Threading utilities, synchronization, async helpers.
```csharp
Tools.Thread.Sleep(milliseconds);
Tools.Thread.RunOnThreadPool(action);
var task = Tools.Thread.RunAsync(asyncFunc);
Tools.Thread.WaitAll(tasks);
```

### Environment & Runtime Tools

#### **Tools.Runtime**
Runtime environment, version detection, diagnostics.
```csharp
string version = Tools.Runtime.GetFrameworkVersion();
bool is64Bit = Tools.Runtime.Is64BitProcess();
string osName = Tools.Runtime.GetOperatingSystem();
var processorCount = Tools.Runtime.GetProcessorCount();
```

#### **Tools.Debugger**
Debug-time utilities and diagnostics.
```csharp
bool attached = Tools.Debugger.IsDebuggerAttached();
Tools.Debugger.Break();
Tools.Debugger.Log(message);
```

### Exception & Scope Tools

#### **Tools.Exception**
Exception handling, formatting, analysis.
```csharp
string message = Tools.Exception.FormatException(ex);
var innerMost = Tools.Exception.GetInnerMostException(ex);
Tools.Exception.LogException(ex);
```

#### **Tools.Scope**
Transactional scope management and lifecycle.
```csharp
using var scope = Tools.Scope.CreateScope() {
    // transactional operations
    scope.Commit();
}
```

### Other Framework Tools

#### **Tools.Object**
Object operations, cloning, copying, inspection.
```csharp
object clone = Tools.Object.Clone(source);
Tools.Object.CopyProperties(source, target);
string description = Tools.Object.ToDebugString(obj);
```

#### **Tools.Span**
Span and memory utilities for high-performance code.
```csharp
Span<byte> data = Tools.Span.AllocateSpan(size);
Tools.Span.CopySpan(source, destination);
```

---

## Platform-Specific Tools

### Windows Platform

#### **Tools.WinTool** (Sphere10.Framework.Windows)
Windows-specific operations: registry, services, events, privileges.
```csharp
// Registry operations
bool exists = Tools.WinTool.KeyExists(hostname, keyPath);
var key = Tools.WinTool.OpenKey(hostname, keyPath);
var subkeys = Tools.WinTool.GetSubKeys(hostname, keyPath);

// Service management
bool running = Tools.WinTool.IsServiceRunning(serviceName);
Tools.WinTool.StartService(serviceName);
Tools.WinTool.StopService(serviceName);
var status = Tools.WinTool.GetServiceStatus(serviceName);

// Privileges and security
bool modified = Tools.WinTool.ModifyState(tokenHandle, "SeRestorePrivilege", true);

// Bit operations
ushort hiword = Tools.WinTool.HIWORD(data);
ushort loword = Tools.WinTool.LOWORD(data);
int wheelDelta = Tools.WinTool.GET_WHEEL_DELTA_WPARAM(data);

// Virtual key mapping
Key key = Tools.WinTool.VirtualKeyToKey(virtualKeyCode);
```

#### **Tools.WindowsTool** (Sphere10.Framework.Windows)
Advanced Windows shell operations.
```csharp
// ShortCut creation
Tools.WinShell.CreateShortcutForApplication(
    executable, 
    shortcutPath, 
    arguments
);
string shortcutPath = Tools.WinShell.DetermineStartupShortcutFilename(appName);
```

#### **Tools** (Sphere10.Framework.Windows.Forms)
WinForms-specific utilities.
```csharp
// Drawing utilities
Color lightDark = Tools.DrawingTool.CalculateLightDarkColor(baseColor, factor);
```

### Web Platform

#### **Tools.Web.Html** (Sphere10.Framework.Web.AspNetCore)
HTML generation, parsing, manipulation.
```csharp
string sanitized = Tools.Web.Html.SanitizeHtml(userHtml);
string encoded = Tools.Web.Html.EncodeHtml(text);
var dom = Tools.Web.Html.ParseHtml(htmlString);
```

#### **Tools.Web.AspNetCore** (Sphere10.Framework.Web.AspNetCore)
ASP.NET Core integration.
```csharp
var result = Tools.Web.AspNetCore.CreateResponse(data);
var error = Tools.Web.AspNetCore.CreateErrorResponse(message);
string animationClass = Tools.Web.AspNetCore.GetAnimationClass(animation, delay);
```

#### **Tools.Web.Downloader** (Sphere10.Framework)
HTTP downloads and streaming.
```csharp
byte[] data = Tools.Web.Downloader.Download(url);
Tools.Web.Downloader.DownloadToFile(url, filePath);
```

### Mobile Platforms

#### **Tools.iOSTool** (Sphere10.Framework.iOS)
iOS-specific utilities.
```csharp
UIViewController topMost = Tools.iOSTool.GetTopMostController();
nfloat keyboardHeight = Tools.iOSTool.GetKeyboardHeight(view, notification);
UIImage image = Tools.iOSTool.EmbeddedImage(imageName);
```

#### **Tools.AndroidTool** (Sphere10.Framework.Android)
Android-specific utilities (as available).
```csharp
// Android-specific operations
```

---

## Database Tools

Organized by database provider:

### **Tools.Data** (Sphere10.Framework.Data)
Generic database operations.
```csharp
var connection = Tools.Data.CreateConnection(connectionString);
var adapter = Tools.Data.CreateDataAdapter(connection);
```

### **Tools.Sqlite** (Sphere10.Framework.Data.Sqlite)
SQLite-specific utilities.
```csharp
var connection = Tools.Sqlite.Create(connectionString, pageSize);
Tools.Sqlite.Drop(Tools.Sqlite.GetFilePathFromConnectionString(connStr));
bool exists = Tools.Sqlite.Exists(connectionString);
```

### **Tools.MSSql** (Sphere10.Framework.Data.MSSQL)
SQL Server-specific utilities.
```csharp
var adapter = Tools.MSSql.CreateAdapter(connectionString);
var connection = Tools.MSSql.CreateConnection(connectionString);
```

### **Tools.Firebird** (Sphere10.Framework.Data.Firebird)
Firebird database utilities.
```csharp
var connection = Tools.Firebird.CreateConnection(connectionString);
```

### **Tools.NHibernate** (Sphere10.Framework.Data.NHibernate)
NHibernate ORM integration.
```csharp
var session = Tools.NHibernate.CreateSession(configuration);
var query = Tools.NHibernate.CreateQuery<T>(session);
```

---

## Application & Testing Tools

### **Tools.Config** (Sphere10.Framework.Application)
Configuration and settings management.
```csharp
var value = Tools.Config.Get<T>(key);
Tools.Config.Set(key, value);
bool exists = Tools.Config.KeyExists(key);
```

### **Tools.NUnit** (Sphere10.Framework.NUnit)
NUnit testing utilities.
```csharp
string grid = Tools.NUnit.Convert2DArrayToString(name, array);
```

### **Tools.Drawing** (Sphere10.Framework.Drawing)
Graphics and drawing operations.
```csharp
Color lighter = Tools.DrawingTool.CalculateLightDarkColor(baseColor, 0.1f);
var resized = Tools.DrawingTool.Resize(image, newWidth, newHeight);
```

---

## Usage Patterns

### Discovery Pattern
```csharp
using Tools;  // Import global Tools namespace

// Start typing Tools. and IntelliSense shows all available operations
Tools.Crypto.
Tools.Text.
Tools.FileSystem.
Tools.Collection.
```

### Chaining Operations
```csharp
var result = Tools.Collection
    .Where(items, x => x.IsActive)
    .Select(x => x.Name)
    .OrderBy(x => x)
    .ToList();
```

### Platform-Conditional Usage
```csharp
#if WINDOWS
    Tools.WinTool.StartService("MyService");
#elif IOS
    var topmost = Tools.iOSTool.GetTopMostController();
#endif
```

### Error Handling with Tools
```csharp
try {
    var result = Tools.Parse.ToInt32(input);
} catch (Exception ex) {
    string formatted = Tools.Exception.FormatException(ex);
    Tools.Debugger.Log(formatted);
}
```

---

## Adding New Tools

When creating a new project in Sphere10 Framework, add a Tools class to the Tools namespace:

### Naming Convention
- **File**: `[Feature]Tool.cs` (e.g., `MyFeatureTool.cs`)
- **Class**: `[Feature]Tool` (e.g., `MyFeatureTool`)
- **Namespace**: `Tools` or `Tools.[Domain]` (e.g., `Tools.Web`, `Tools.Database`)

### Template
```csharp
// File: MyFeatureTool.cs
namespace Tools;

/// <summary>
/// Tools for [Feature Description]
/// </summary>
public static class MyFeatureTool {
    /// <summary>
    /// [Operation description]
    /// </summary>
    public static TResult MyOperation<T, TResult>(T input) {
        // Implementation
    }
    
    /// <summary>
    /// [Another operation]
    /// </summary>
    public static void AnotherOperation(string parameter) {
        // Implementation
    }
}
```

### Integration Steps
1. Add `[Feature]Tool.cs` to your project
2. Use `namespace Tools;` (or `Tools.[Domain]`)
3. Add static methods for related operations
4. Update project README to list available tools
5. Document in the global [docs/Tools-Reference.md](../Tools-Reference.md) file

---

## Related Documentation

- [Sphere10.Framework README](../src/Sphere10.Framework/README.md) – Architecture and project overview
- [Code Styling Guide](../Guidelines/Code-Styling.md) – Conventions for Tools implementations
- Individual project READMEs – Domain-specific tool documentation

---

**Version**: 3.0.0  
**Last Updated**: December 31, 2025  
**Framework**: Sphere10 Framework
